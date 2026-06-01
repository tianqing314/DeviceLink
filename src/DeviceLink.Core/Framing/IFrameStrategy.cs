using System;

namespace DeviceLink.Core.Framing
{
    /// <summary>
    /// 帧分割策略 —— 从字节流中判断完整帧的边界。
    /// 纯函数，无状态，可组合。
    /// </summary>
    public interface IFrameStrategy
    {
        /// <summary>
        /// 尝试从累积缓冲区中匹配一个完整帧。
        /// </summary>
        /// <param name="accumulated">当前累积的字节缓冲区</param>
        /// <param name="frameLength">输出：完整帧的字节长度（从0开始）</param>
        /// <returns>true 表示已匹配到完整帧边界</returns>
        bool TryMatchFrame(byte[] accumulated, out int frameLength);
    }
}
