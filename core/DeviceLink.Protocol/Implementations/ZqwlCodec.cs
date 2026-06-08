using System;
using System.Text;

namespace DeviceLink.Protocol
{
    /// <summary>
    /// ZQWL（智嵌物联）继电器协议编解码器。
    /// 
    /// 协议格式：[48 3A] [addr] [func] [8 bytes data] [checksum] [45 44]
    /// 
    /// 功能码：
    ///   0x52 = 读取输入状态
    ///   0x57 = 设置全部输出（批量）
    ///   0x70 = 设置单路输出
    ///   0x53 = 读取全部输出状态
    ///   0x72 = 读取单路输出状态
    ///   0x66 = 读取版本号
    ///   0x0A = 读取模拟量输入
    /// 
    /// 数据区（8字节）含义：
    ///   - 读输入(0x52)：全部填 0x00
    ///   - 设置输出(0x70)：[channel] [state] [00] [00] [00..]
    ///   - 批量设置(0x57)：每字节代表 1 路（BNRC8）/ 2 路（BNRC16）/ 4 路（BNRC32）
    /// </summary>
    public class ZqwlCodec : IProtocolCodec
    {
        private readonly byte _address;

        /// <summary>
        /// 创建 ZQWL 协议编解码器
        /// </summary>
        /// <param name="address">设备地址（默认 1）</param>
        public ZqwlCodec(byte address = 1)
        {
            _address = address;
        }

        /// <inheritdoc/>
        public string ProtocolName => "ZQWL";

        /// <summary>设备地址</summary>
        public byte Address => _address;

        /// <inheritdoc/>
        public byte[] Encode(Command command)
        {
            var parts = command.Id.Split('.');
            var operation = parts[0];

            byte funcCode;
            byte[] data = new byte[8];

            switch (operation)
            {
                case "GetInput":
                    funcCode = 0x52;
                    // data 全 0
                    break;

                case "SetOutput":
                    funcCode = 0x70;
                    if (parts.Length >= 3)
                    {
                        data[0] = byte.Parse(parts[1]); // channel
                        data[1] = byte.Parse(parts[2]); // state (0=off, 1=on)
                    }
                    // data[2..7] = 0x00（不影响其他路）
                    break;

                case "GetOutput":
                    funcCode = 0x72;
                    if (parts.Length >= 2)
                    {
                        data[0] = byte.Parse(parts[1]); // channel
                    }
                    break;

                case "CloseAll":
                    funcCode = 0x57;
                    // data 由参数填充
                    if (command.Parameters.Length > 0)
                    {
                        for (int i = 0; i < Math.Min(command.Parameters.Length, 8); i++)
                        {
                            data[i] = Convert.ToByte(command.Parameters[i], 16);
                        }
                    }
                    break;

                case "OpenAll":
                    funcCode = 0x57;
                    // data 由参数填充
                    if (command.Parameters.Length > 0)
                    {
                        for (int i = 0; i < Math.Min(command.Parameters.Length, 8); i++)
                        {
                            data[i] = Convert.ToByte(command.Parameters[i], 16);
                        }
                    }
                    break;

                case "GetAllStatuses":
                    funcCode = 0x53;
                    // data 全填 0xAA（参考原始实现）
                    for (int i = 0; i < 8; i++) data[i] = 0xAA;
                    break;

                case "GetVersion":
                    funcCode = 0x66;
                    // data 全 0
                    break;

                case "GetAnalogInput":
                    funcCode = 0x0A;
                    if (parts.Length >= 2)
                    {
                        data[0] = byte.Parse(parts[1]); // channel index (0-based)
                    }
                    break;

                default:
                    throw new ArgumentException($"未知的ZQWL操作: {operation}");
            }

            // 组装：[addr][func][8 bytes data]
            var result = new byte[10];
            result[0] = _address;
            result[1] = funcCode;
            Array.Copy(data, 0, result, 2, 8);
            return result;
        }

        /// <inheritdoc/>
        public string DecodeText(byte[] raw)
        {
            if (raw == null || raw.Length == 0)
                return string.Empty;

            var sb = new StringBuilder();
            for (int i = 0; i < raw.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(raw[i].ToString("X2"));
            }
            return sb.ToString();
        }

        /// <inheritdoc/>
        public bool IsErrorResponse(byte[] raw, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (raw == null || raw.Length < 2)
                return false;

            // ZQWL 协议无明确错误码定义，检查功能码是否为 0xFF
            if (raw.Length >= 2 && raw[1] == 0xFF)
            {
                errorMessage = "ZQWL设备返回错误响应";
                return true;
            }
            return false;
        }

        /// <summary>
        /// 从响应中提取指定路的输入状态（BNRC8 使用）
        /// </summary>
        /// <param name="raw">原始响应（帧数据，不含帧头帧尾）</param>
        /// <param name="channel">通道号（1-based）</param>
        /// <returns>true=开，false=关</returns>
        public bool ExtractInputState(byte[] raw, int channel)
        {
            // raw[0]=addr, raw[1]=func, raw[2..9]=data
            if (raw.Length < 10)
                return false;
            return raw[1 + channel] == 0x01;
        }

        /// <summary>
        /// 从响应中提取版本号字符串
        /// </summary>
        /// <param name="raw">原始响应</param>
        /// <returns>版本号字符串</returns>
        public string ExtractVersion(byte[] raw)
        {
            // 帧结构: [addr][func][8字节数据]
            // 版本从数据区开头 (raw[2]) 开始
            if (raw.Length >= 10)
            {
                var verBytes = new byte[8];
                Array.Copy(raw, 2, verBytes, 0, 8);
                return Encoding.UTF8.GetString(verBytes).TrimEnd('\0');
            }
            return string.Empty;
        }

        /// <summary>
        /// 从响应中提取模拟量值（BNRC16/BNRC32 使用）
        /// </summary>
        /// <param name="raw">原始响应</param>
        /// <returns>模拟量原始值</returns>
        public int ExtractAnalogValue(byte[] raw)
        {
            if (raw.Length > 7)
            {
                // 数据区 offset 5 开始取 2 字节
                return raw[5] | (raw[6] << 8);
            }
            return 0;
        }
    }
}
