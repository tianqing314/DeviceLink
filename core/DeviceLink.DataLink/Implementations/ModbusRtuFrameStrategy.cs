using System;

namespace DeviceLink.DataLink
{
    /// <summary>
    /// Modbus RTU 帧策略 —— Modbus RTU协议的帧格式。
    /// 帧格式：[设备地址][功能码][数据...][CRC低字节][CRC高字节]
    /// </summary>
    public class ModbusRtuFrameStrategy : IFrameStrategy
    {
        /// <inheritdoc/>
        public string Name => "ModbusRTU";

        /// <inheritdoc/>
        public byte[] BuildFrame(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data.Length < 2)
                throw new ArgumentException("Modbus RTU数据至少需要包含设备地址和功能码");

            // 计算CRC16校验
            ushort crc = CalculateCrc16(data, data.Length);

            // 组装帧：数据 + CRC低字节 + CRC高字节
            var frame = new byte[data.Length + 2];
            Array.Copy(data, 0, frame, 0, data.Length);
            frame[data.Length] = (byte)(crc & 0xFF);        // CRC低字节
            frame[data.Length + 1] = (byte)((crc >> 8) & 0xFF); // CRC高字节
            return frame;
        }

        /// <inheritdoc/>
        public bool TryParseFrame(byte[] accumulated, out int frameLength, out byte[] frameData)
        {
            frameLength = 0;
            frameData = Array.Empty<byte>();

            if (accumulated == null || accumulated.Length < 4) // 最小帧：地址+功能码+CRC
                return false;

            // Modbus RTU帧长度取决于功能码和数据长度
            // 这里采用简化处理：假设帧长度在合理范围内
            // 实际应用中可能需要更复杂的解析逻辑

            // 尝试不同的帧长度
            for (int len = 4; len <= accumulated.Length; len++)
            {
                if (accumulated.Length < len)
                    continue;

                // 提取数据部分（不含CRC）
                var data = new byte[len - 2];
                Array.Copy(accumulated, 0, data, 0, data.Length);

                // 提取CRC
                ushort receivedCrc = (ushort)(accumulated[len - 2] | (accumulated[len - 1] << 8));

                // 计算CRC
                ushort calculatedCrc = CalculateCrc16(data, data.Length);

                if (receivedCrc == calculatedCrc)
                {
                    frameLength = len;
                    frameData = data;
                    return true;
                }
            }

            return false;
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
