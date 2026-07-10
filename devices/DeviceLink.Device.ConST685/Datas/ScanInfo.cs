namespace DeviceLink.Device.ConST685
{
    /// <summary>
    /// 扫描信息 —— 对应 Xmas11 DIScanInfo
    /// JSON:SCAN:STARt? 返回值
    /// </summary>
    public class ScanInfo
    {
        /// <summary>
        /// 类名称（反序列化标识）
        /// </summary>
        public string ClassName { get; set; } = "DIScanInfo";

        /// <summary>
        /// 通道名称
        /// </summary>
        public string ChannelName { get; set; } = string.Empty;

        /// <summary>
        /// 采样速率（100=快速, 1000=中速, 4000=慢速）
        /// </summary>
        public ushort NPLC { get; set; } = 100;

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(ChannelName);

        /// <inheritdoc/>
        public override string ToString() => $"Ch={ChannelName},NPLC={NPLC}";
    }
}
