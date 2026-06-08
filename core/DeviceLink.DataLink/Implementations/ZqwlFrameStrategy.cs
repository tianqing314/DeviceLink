using System;

namespace DeviceLink.DataLink
{
    /// <summary>
    /// ZQWL（智嵌物联）二进制帧策略。
    /// 
    /// 帧格式：[48 3A] [addr] [func] [data x8] [checksum] [45 44]
    ///   - 帧头：0x48 0x3A（"H:"）
    ///   - 地址：1 字节
    ///   - 功能码：1 字节
    ///   - 数据区：8 字节（固定）
    ///   - 校验和：1 字节（帧头到数据区所有字节之和，取低 8 位）
    ///   - 帧尾：0x45 0x44（"ED"）
    ///   - 总帧长：15 字节
    /// </summary>
    public class ZqwlFrameStrategy : IFrameStrategy
    {
        private const byte Header0 = 0x48;
        private const byte Header1 = 0x3A;
        private const byte Footer0 = 0x45;
        private const byte Footer1 = 0x44;
        private const int FrameLength = 15;
        private const int DataAreaLength = 8;

        /// <inheritdoc/>
        public string Name => "ZQWL";

        /// <inheritdoc/>
        public byte[] BuildFrame(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            // data = [addr][func][8 bytes data] = 10 bytes
            if (data.Length < 2)
                throw new ArgumentException("ZQWL数据至少需要包含地址和功能码");

            // 确保数据区为 8 字节（不足补 0x00）
            var dataArea = new byte[DataAreaLength];
            int copyLen = Math.Min(data.Length - 2, DataAreaLength);
            Array.Copy(data, 2, dataArea, 0, copyLen);

            // 构建帧（不含校验和）
            var frame = new byte[FrameLength];
            frame[0] = Header0;
            frame[1] = Header1;
            frame[2] = data[0]; // addr
            frame[3] = data[1]; // func
            Array.Copy(dataArea, 0, frame, 4, DataAreaLength);

            // 计算校验和：帧头到数据区所有字节之和，取低 8 位
            int sum = 0;
            for (int i = 0; i < 12; i++) // bytes 0..11 (header + addr + func + data)
            {
                sum += frame[i];
            }
            frame[12] = (byte)(sum & 0xFF);

            // 帧尾
            frame[13] = Footer0;
            frame[14] = Footer1;

            return frame;
        }

        /// <inheritdoc/>
        public bool TryParseFrame(byte[] accumulated, out int frameLength, out byte[] frameData)
        {
            frameLength = 0;
            frameData = Array.Empty<byte>();

            if (accumulated == null || accumulated.Length < FrameLength)
                return false;

            // 查找帧头 48 3A
            int headerPos = FindHeader(accumulated);
            if (headerPos < 0)
                return false;

            // 检查是否有足够数据
            if (accumulated.Length < headerPos + FrameLength)
                return false;

            // 验证帧尾 45 44
            if (accumulated[headerPos + 13] != Footer0 || accumulated[headerPos + 14] != Footer1)
                return false;

            // 验证校验和
            int sum = 0;
            for (int i = headerPos; i < headerPos + 12; i++)
            {
                sum += accumulated[i];
            }
            if (accumulated[headerPos + 12] != (byte)(sum & 0xFF))
                return false;

            // 提取帧数据：[addr][func][8 bytes data]
            frameLength = headerPos + FrameLength;
            frameData = new byte[10]; // addr + func + 8 data bytes
            frameData[0] = accumulated[headerPos + 2]; // addr
            frameData[1] = accumulated[headerPos + 3]; // func
            Array.Copy(accumulated, headerPos + 4, frameData, 2, 8);

            return true;
        }

        private static int FindHeader(byte[] data)
        {
            for (int i = 0; i <= data.Length - 2; i++)
            {
                if (data[i] == Header0 && data[i + 1] == Header1)
                    return i;
            }
            return -1;
        }
    }
}
