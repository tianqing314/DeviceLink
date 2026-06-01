using System;
using System.Text;

namespace DeviceLink.Protocol
{
    /// <summary>
    /// Modbus RTU 协议编解码器。
    /// 
    /// Modbus RTU 是一种二进制协议，常用于工业设备通信。
    /// 帧格式：[设备地址][功能码][数据...][CRC低字节][CRC高字节]
    /// 
    /// 常用功能码：
    ///   0x03: 读保持寄存器
    ///   0x06: 写单个寄存器
    ///   0x10: 写多个寄存器
    /// </summary>
    public class ModbusRtuCodec : IProtocolCodec
    {
        private readonly byte _slaveAddress;

        /// <summary>
        /// 创建 Modbus RTU 协议编解码器
        /// </summary>
        /// <param name="slaveAddress">从站地址（1~247，默认1）</param>
        public ModbusRtuCodec(byte slaveAddress = 1)
        {
            _slaveAddress = slaveAddress;
        }

        /// <inheritdoc/>
        public string ProtocolName => "ModbusRTU";

        /// <inheritdoc/>
        public byte[] Encode(Command command)
        {
            // 解析命令ID，格式：功能码.寄存器地址.寄存器数量
            // 例如："3.0.10" 表示读取从地址0开始的10个保持寄存器
            var parts = command.Id.Split('.');
            if (parts.Length < 2)
                throw new ProtocolException($"Modbus命令ID格式错误: {command.Id}，应为'功能码.寄存器地址[.寄存器数量]'");

            byte functionCode = byte.Parse(parts[0]);
            ushort registerAddress = ushort.Parse(parts[1]);
            ushort registerCount = command.Parameters.Length > 0 ? ushort.Parse(command.Parameters[0]) : (ushort)1;

            var data = new byte[6]; // 功能码 + 地址 + 数量
            data[0] = functionCode;
            data[1] = (byte)(registerAddress >> 8);   // 地址高字节
            data[2] = (byte)(registerAddress & 0xFF); // 地址低字节
            data[3] = (byte)(registerCount >> 8);     // 数量高字节
            data[4] = (byte)(registerCount & 0xFF);   // 数量低字节

            // 对于写入命令，添加写入数据
            if (functionCode == 0x06) // 写单个寄存器
            {
                if (command.Parameters.Length < 2)
                    throw new ProtocolException("写单个寄存器需要提供写入值");

                ushort value = ushort.Parse(command.Parameters[1]);
                data = new byte[6];
                data[0] = functionCode;
                data[1] = (byte)(registerAddress >> 8);
                data[2] = (byte)(registerAddress & 0xFF);
                data[3] = (byte)(value >> 8);
                data[4] = (byte)(value & 0xFF);
            }
            else if (functionCode == 0x10) // 写多个寄存器
            {
                if (command.Parameters.Length < 2)
                    throw new ProtocolException("写多个寄存器需要提供写入值");

                var values = new ushort[command.Parameters.Length - 1];
                for (int i = 1; i < command.Parameters.Length; i++)
                {
                    values[i - 1] = ushort.Parse(command.Parameters[i]);
                }

                int dataLength = 7 + values.Length * 2;
                data = new byte[dataLength];
                data[0] = functionCode;
                data[1] = (byte)(registerAddress >> 8);
                data[2] = (byte)(registerAddress & 0xFF);
                data[3] = (byte)(registerCount >> 8);
                data[4] = (byte)(registerCount & 0xFF);
                data[5] = (byte)(values.Length * 2); // 字节数

                for (int i = 0; i < values.Length; i++)
                {
                    data[6 + i * 2] = (byte)(values[i] >> 8);
                    data[7 + i * 2] = (byte)(values[i] & 0xFF);
                }
            }

            // 组装完整帧：从站地址 + 数据 + CRC
            var frame = new byte[1 + data.Length + 2];
            frame[0] = _slaveAddress;
            Array.Copy(data, 0, frame, 1, data.Length);

            // 计算CRC
            ushort crc = CalculateCrc16(frame, frame.Length - 2);
            frame[frame.Length - 2] = (byte)(crc & 0xFF);        // CRC低字节
            frame[frame.Length - 1] = (byte)((crc >> 8) & 0xFF); // CRC高字节

            return frame;
        }

        /// <inheritdoc/>
        public string DecodeText(byte[] raw)
        {
            if (raw == null || raw.Length == 0)
                return string.Empty;

            var sb = new StringBuilder();
            for (int i = 0; i < raw.Length; i++)
            {
                sb.Append(raw[i].ToString("X2"));
                if (i < raw.Length - 1)
                    sb.Append(" ");
            }
            return sb.ToString();
        }

        /// <inheritdoc/>
        public bool IsErrorResponse(byte[] raw, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (raw == null || raw.Length < 5)
                return false;

            // Modbus 错误响应：从站地址 + (功能码 | 0x80) + 错误码 + CRC
            if ((raw[1] & 0x80) != 0)
            {
                byte errorCode = raw[2];
                errorMessage = errorCode switch
                {
                    0x01 => "非法功能码",
                    0x02 => "非法数据地址",
                    0x03 => "非法数据值",
                    0x04 => "从站设备故障",
                    0x05 => "确认",
                    0x06 => "从站设备忙",
                    0x08 => "存储奇偶性差错",
                    0x0A => "不可用网关路径",
                    0x0B => "网关目标设备响应失败",
                    _ => $"未知错误码: 0x{errorCode:X2}"
                };
                return true;
            }

            return false;
        }

        /// <summary>
        /// 从 Modbus 响应中提取寄存器值
        /// </summary>
        /// <param name="raw">原始响应数据</param>
        /// <returns>寄存器值数组</returns>
        public ushort[] ExtractRegisters(byte[] raw)
        {
            if (raw == null || raw.Length < 5)
                return Array.Empty<ushort>();

            // 响应格式：从站地址 + 功能码 + 字节数 + 数据... + CRC
            if (raw[1] == 0x03) // 读保持寄存器响应
            {
                int byteCount = raw[2];
                int registerCount = byteCount / 2;
                var registers = new ushort[registerCount];

                for (int i = 0; i < registerCount; i++)
                {
                    registers[i] = (ushort)((raw[3 + i * 2] << 8) | raw[4 + i * 2]);
                }

                return registers;
            }

            return Array.Empty<ushort>();
        }

        /// <summary>
        /// 计算Modbus CRC16校验
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="length">数据长度</param>
        /// <returns>CRC16值</returns>
        private static ushort CalculateCrc16(byte[] data, int length)
        {
            ushort crc = 0xFFFF;

            for (int i = 0; i < length; i++)
            {
                crc ^= data[i];

                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x0001) != 0)
                    {
                        crc >>= 1;
                        crc ^= 0xA001;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }

            return crc;
        }
    }
}
