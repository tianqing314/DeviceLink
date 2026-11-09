using System;

namespace DeviceLink.DataLink
{
    /// <summary>
    /// Modbus RTU 帧策略 —— 按协议规范实现帧定界与 CRC16 校验。
    ///
    /// 帧格式：[设备地址][功能码][数据...][CRC 低字节][CRC 高字节]
    ///
    /// 定界规则（这是本策略的核心，不是"试一试"）：
    /// · 异常响应：功能码最高位为 1，整帧固定 <b>5</b> 字节；
    /// · 读类响应（0x01~0x04、0x0C、0x11、0x14、0x15、0x17）：第 3 字节为字节数，
    ///   整帧 = 3 + 字节数 + 2；
    /// · 写类响应（0x05、0x06、0x0F、0x10、0x16）：整帧固定 <b>8</b> 字节；
    /// · 0x0B（通信事件计数器）：整帧固定 <b>7</b> 字节。
    ///
    /// 为什么不再用"枚举长度试 CRC"：
    /// CRC16 的碰撞概率是 1/65536，帧越长、候选越多，误判概率越高，
    /// 一旦误判就会切出错误的帧边界，把垃圾当成合法响应交给上层。
    /// 因此主路径必须是"按功能码算长度 + CRC 复核"，滑动搜索只作为错帧恢复的兜底。
    /// </summary>
    public class ModbusRtuFrameStrategy : IFrameStrategy
    {
        /// <summary>Modbus RTU 规范规定的单帧最大字节数（ADU 上限 256 字节）。</summary>
        public const int MaxFrameLength = 256;

        /// <summary>最小合法帧长：异常响应 5 字节（地址 + 功能码|0x80 + 异常码 + CRC×2）。</summary>
        public const int MinFrameLength = 5;

        /// <inheritdoc/>
        public string Name => "ModbusRTU";

        /// <inheritdoc/>
        public byte[] BuildFrame(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data.Length < 2)
                throw new ArgumentException("Modbus RTU 数据至少需要包含设备地址和功能码");

            ushort crc = CalculateCrc16(data, data.Length);

            var frame = new byte[data.Length + 2];
            Array.Copy(data, 0, frame, 0, data.Length);
            frame[data.Length] = (byte)(crc & 0xFF);              // CRC 低字节在前
            frame[data.Length + 1] = (byte)((crc >> 8) & 0xFF);   // CRC 高字节在后
            return frame;
        }

        /// <inheritdoc/>
        public bool TryParseFrame(byte[] accumulated, out int frameLength, out byte[] frameData)
        {
            frameLength = 0;
            frameData = Array.Empty<byte>();

            if (accumulated == null || accumulated.Length < MinFrameLength)
                return false;

            // ① 主路径：按功能码精确计算帧长，CRC 只做复核
            if (TryGetExpectedLength(accumulated, 0, out int expected))
            {
                if (accumulated.Length < expected)
                    return false;   // 数据未收齐：等待，不判定为坏帧

                if (VerifyCrc(accumulated, 0, expected))
                {
                    frameLength = expected;
                    frameData = ExtractPayload(accumulated, 0, expected);
                    return true;
                }
                // CRC 不符说明当前字节流已错位，转入兜底重同步
            }

            // ② 兜底：逐字节滑动帧起点后重新按功能码定长（用于错帧后的重新对齐）。
            //    搜索窗口限制在一帧长度内，避免在超长缓冲区上退化成全量扫描。
            int maxStart = Math.Min(accumulated.Length - MinFrameLength, MaxFrameLength);
            for (int start = 1; start <= maxStart; start++)
            {
                if (!TryGetExpectedLength(accumulated, start, out int length)) continue;
                if (start + length > accumulated.Length) continue;
                if (!VerifyCrc(accumulated, start, length)) continue;

                frameLength = start + length;
                frameData = ExtractPayload(accumulated, start, length);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 按功能码推断整帧长度（含 CRC）。返回 false 表示无法确定（未知功能码或数据不足）。
        /// </summary>
        private static bool TryGetExpectedLength(byte[] buffer, int start, out int length)
        {
            length = 0;
            if (buffer.Length - start < 2) return false;

            byte functionCode = buffer[start + 1];

            // 异常响应：[地址][功能码|0x80][异常码][CRC 低][CRC 高]
            if ((functionCode & 0x80) != 0)
            {
                length = 5;
                return true;
            }

            switch (functionCode)
            {
                // 读类：第 3 字节是后续数据字节数
                case 0x01: // 读线圈
                case 0x02: // 读离散输入
                case 0x03: // 读保持寄存器
                case 0x04: // 读输入寄存器
                case 0x0C: // 读通信事件日志
                case 0x11: // 报告从站 ID
                case 0x14: // 读文件记录
                case 0x15: // 写文件记录
                case 0x17: // 读写多个寄存器
                    if (buffer.Length - start < 3) return false;
                    length = 3 + buffer[start + 2] + 2;
                    return length >= MinFrameLength && length <= MaxFrameLength;

                // 写类：响应回显前 6 字节，整帧固定 8 字节
                case 0x05: // 写单个线圈
                case 0x06: // 写单个寄存器
                case 0x0F: // 写多个线圈
                case 0x10: // 写多个寄存器
                case 0x16: // 掩码写寄存器
                    length = 8;
                    return true;

                // 通信事件计数器：[地址][0x0B][状态 2B][事件计数 2B][CRC 2B]
                case 0x0B:
                    length = 7;
                    return true;

                default:
                    return false;
            }
        }

        private static bool VerifyCrc(byte[] buffer, int start, int totalLength)
        {
            if (totalLength < 4 || buffer.Length - start < totalLength) return false;

            int crcPos = start + totalLength - 2;
            ushort received = (ushort)(buffer[crcPos] | (buffer[crcPos + 1] << 8));
            ushort calculated = CalculateCrc16(buffer, start, totalLength - 2);
            return received == calculated;
        }

        /// <summary>取出不含 CRC 的载荷（保持既有约定：交给上层的帧数据不带校验字节）。</summary>
        private static byte[] ExtractPayload(byte[] buffer, int start, int totalLength)
        {
            var payload = new byte[totalLength - 2];
            Array.Copy(buffer, start, payload, 0, payload.Length);
            return payload;
        }

        /// <summary>计算 Modbus CRC16（多项式 0xA001，初值 0xFFFF）。</summary>
        private static ushort CalculateCrc16(byte[] data, int length) => CalculateCrc16(data, 0, length);

        /// <summary>计算指定区间内的 Modbus CRC16（多项式 0xA001，初值 0xFFFF）。</summary>
        private static ushort CalculateCrc16(byte[] data, int start, int length)
        {
            ushort crc = 0xFFFF;

            for (int i = start; i < start + length; i++)
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
