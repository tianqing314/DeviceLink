using System;

namespace DeviceLink.Core.Framing
{
    /// <summary>
    /// 定长帧策略 —— 累积数据达到指定字节数即为一个完整帧。
    /// 适用于固定长度二进制协议。
    /// </summary>
    public class FixedLengthFrameStrategy : IFrameStrategy
    {
        private readonly int _frameSize;

        public FixedLengthFrameStrategy(int frameSize)
        {
            if (frameSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(frameSize), "帧长度必须大于0");
            _frameSize = frameSize;
        }

        public bool TryMatchFrame(byte[] accumulated, out int frameLength)
        {
            frameLength = _frameSize;
            return accumulated != null && accumulated.Length >= _frameSize;
        }
    }
}
