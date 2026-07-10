namespace DeviceLink.Device.ConST685
{
    /// <summary>
    /// 板卡/接线盒模块信息 —— 对应 Xmas11 DIModuleInfo
    /// JSON:MODule:INFormation? 返回值
    /// </summary>
    public class ModuleInfo
    {
        /// <summary>
        /// 类名称（反序列化标识）
        /// </summary>
        public string ClassName { get; set; } = "DIModuleInfo";

        /// <summary>
        /// 接线盒编号/板卡索引（前面板为 0，内嵌盒为 1，外接盒为 2,3,4）
        /// </summary>
        public byte Index { get; set; }

        /// <summary>
        /// 板卡类型（0=前面板，1=温度盒，2=量程盒）
        /// </summary>
        public byte Category { get; set; }

        /// <summary>
        /// 序列号
        /// </summary>
        public string SN { get; set; } = string.Empty;

        /// <summary>
        /// 硬件版本
        /// </summary>
        public string HwVersion { get; set; } = string.Empty;

        /// <summary>
        /// 软件/固件版本
        /// </summary>
        public string SwVersion { get; set; } = string.Empty;

        /// <summary>
        /// 标签
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// 统计通道数
        /// </summary>
        public byte TotalChannelCount { get; set; }

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid => Index > 0 || !string.IsNullOrEmpty(SN);

        /// <inheritdoc/>
        public override string ToString() =>
            $"Index={Index},Type={Category},SN={SN},HW={HwVersion},FW={SwVersion},Channels={TotalChannelCount},Label={Label}";
    }
}
