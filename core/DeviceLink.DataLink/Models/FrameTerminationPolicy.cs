namespace DeviceLink.DataLink
{
    /// <summary>
    /// 帧终止符追加策略 —— 决定分隔符帧策略在组装发送帧时是否无条件追加终止符。
    /// 默认值为 AppendAlways，保持框架既有线上字节完全不变。
    /// </summary>
    public enum FrameTerminationPolicy
    {
        /// <summary>
        /// 总是追加终止符（默认）。
        /// 即使编解码器 Encode 的结果已自带终止符，帧策略仍会再追加一次（现状行为）。
        /// </summary>
        AppendAlways = 0,

        /// <summary>
        /// 载荷已以终止符结尾时不再追加，即发送单终止符。
        /// 仅对分隔符型帧策略（DelimiterFrameStrategy）有意义；
        /// 切换前必须在真机上验证设备可接受单终止符。
        /// </summary>
        SkipIfAlreadyTerminated = 1
    }
}
