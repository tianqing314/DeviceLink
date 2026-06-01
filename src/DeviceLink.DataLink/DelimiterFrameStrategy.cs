using System;
using System.Text;

namespace DeviceLink.DataLink
{
    /// <summary>
    /// 分隔符帧策略 —— 检测累积缓冲区中分隔符的出现次数。
    /// 覆盖 ConST 协议 (\0)、SCPI 协议 (\n)、AT 指令 (\r\n) 等场景。
    /// </summary>
    public class DelimiterFrameStrategy : IFrameStrategy
    {
        private readonly byte[] _delimiter;
        private readonly int _minDelimiterCount;

        /// <summary>
        /// 创建分隔符帧策略
        /// </summary>
        /// <param name="delimiter">帧结束分隔符，如 new byte[]{0} 或 Encoding.ASCII.GetBytes("\r\n")</param>
        /// <param name="minDelimiterCount">最少需匹配的分隔符次数（广播场景等，默认1，即遇第一个分隔符即分帧）</param>
        public DelimiterFrameStrategy(byte[] delimiter, int minDelimiterCount = 1)
        {
            _delimiter = delimiter ?? throw new ArgumentNullException(nameof(delimiter));
            _minDelimiterCount = minDelimiterCount > 0 ? minDelimiterCount : 1;
        }

        /// <summary>
        /// 用字符串创建分隔符帧策略（使用 UTF-8 编码）
        /// </summary>
        /// <param name="delimiter">分隔符字符串</param>
        /// <param name="minDelimiterCount">最少需匹配的分隔符次数</param>
        public DelimiterFrameStrategy(string delimiter, int minDelimiterCount = 1)
            : this(Encoding.UTF8.GetBytes(delimiter), minDelimiterCount)
        { }

        /// <inheritdoc/>
        public string Name => $"Delimiter({BitConverter.ToString(_delimiter)})";

        /// <inheritdoc/>
        public byte[] BuildFrame(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            // 添加分隔符到数据末尾
            var frame = new byte[data.Length + _delimiter.Length];
            Array.Copy(data, 0, frame, 0, data.Length);
            Array.Copy(_delimiter, 0, frame, data.Length, _delimiter.Length);
            return frame;
        }

        /// <inheritdoc/>
        public bool TryParseFrame(byte[] accumulated, out int frameLength, out byte[] frameData)
        {
            frameLength = 0;
            frameData = Array.Empty<byte>();

            if (accumulated == null || accumulated.Length == 0 || _delimiter.Length == 0)
                return false;

            int matchCount = 0;
            int pos = 0;

            while (pos <= accumulated.Length - _delimiter.Length)
            {
                if (MatchAt(accumulated, pos))
                {
                    matchCount++;
                    if (matchCount >= _minDelimiterCount)
                    {
                        frameLength = pos + _delimiter.Length;
                        // 提取帧数据（不含分隔符）
                        frameData = new byte[frameLength - _delimiter.Length];
                        Array.Copy(accumulated, 0, frameData, 0, frameData.Length);
                        return true;
                    }
                    pos += _delimiter.Length;
                }
                else
                {
                    pos++;
                }
            }

            return false;
        }

        private bool MatchAt(byte[] data, int offset)
        {
            for (int i = 0; i < _delimiter.Length; i++)
            {
                if (data[offset + i] != _delimiter[i])
                    return false;
            }
            return true;
        }
    }
}
