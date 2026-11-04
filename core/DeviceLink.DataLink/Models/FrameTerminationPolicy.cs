namespace DeviceLink.DataLink
{
    /// <summary>
    /// 帧终止符追加策略 —— 决定分隔符帧策略在组装发送帧时是否追加终止符。
    /// 默认 SkipIfAlreadyTerminated：协议层已自带终止符时不再重复追加。
    /// </summary>
    public enum FrameTerminationPolicy
    {
        /// <summary>
        /// 载荷已以终止符结尾时不再追加（默认，单终止符）。
        /// 载荷本身不带终止符时仍会补一个，因此不会出现漏加终止符的情况。
        /// </summary>
        SkipIfAlreadyTerminated = 0,

        /// <summary>
        /// 总是追加终止符（兼容模式）。
        /// 即使编解码器 Encode 的结果已自带终止符，也会再追加一次（历史双终止符行为）。
        /// </summary>
        AppendAlways = 1
    }
}
