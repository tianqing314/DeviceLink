namespace DeviceLink.Device.ConST685
{
    /// <summary>
    /// 扫描配置信息 —— [MEASure:]SCAN:STARt? 返回值
    /// </summary>
    public class ScanConfig
    {
        /// <summary>
        /// NPLC 采样工频周期（100/1000/4000）
        /// </summary>
        public int Nplc { get; set; }

        /// <summary>
        /// 扫描通道数量
        /// </summary>
        public int ChannelCount { get; set; }

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid => Nplc > 0;

        /// <inheritdoc/>
        public override string ToString() => $"NPLC={Nplc},Channels={ChannelCount}";
    }
}
