using System;

namespace DeviceLink.DataLink
{
    /// <summary>
    /// 定长帧策略 —— 固定长度的帧格式。
    /// 适用于固定长度的协议，如某些二进制协议。
    /// </summary>
    public class FixedLengthFrameStrategy : IFrameStrategy
    {
        private readonly int _frameLength;

        /// <summary>
        /// 创建定长帧策略
        /// </summary>
        /// <param name="frameLength">帧长度（字节）</param>
        public FixedLengthFrameStrategy(int frameLength)
        {
            if (frameLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(frameLength), "帧长度必须大于0");

            _frameLength = frameLength;
        }

        /// <inheritdoc/>
        public string Name => $"FixedLength({_frameLength})";

        /// <inheritdoc/>
        public byte[] BuildFrame(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data.Length > _frameLength)
                throw new ArgumentException($"数据长度({data.Length})超过帧长度({_frameLength})");

            // 创建固定长度的帧，不足部分补0
            var frame = new byte[_frameLength];
            Array.Copy(data, 0, frame, 0, data.Length);
            return frame;
        }

        /// <inheritdoc/>
        public bool TryParseFrame(byte[] accumulated, out int frameLength, out byte[] frameData)
        {
            frameLength = 0;
            frameData = Array.Empty<byte>();

            if (accumulated == null || accumulated.Length < _frameLength)
                return false;

            frameLength = _frameLength;
            frameData = new byte[_frameLength];
            Array.Copy(accumulated, 0, frameData, 0, _frameLength);
            return true;
        }
    }
}
