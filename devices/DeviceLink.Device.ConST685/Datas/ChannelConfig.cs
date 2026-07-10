namespace DeviceLink.Device.ConST685
{
    /// <summary>
    /// 通道配置 —— 对应 Xmas11 DIFunctionChannelConfig
    /// JSON:MODule:CONFig? JSON:CHANnel:CONFig 返回值
    /// </summary>
    public class ChannelConfig
    {
        /// <summary>
        /// 类名称（反序列化标识）
        /// </summary>
        public string ClassName { get; set; } = "DIFunctionChannelConfig";

        /// <summary>
        /// 通道名称（如 "CH0-00"、"CH1-08B"）
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 标签
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// 功能类型（0=DCV, 1=DCI, 2=Resistance, 3=工业RTD, 4=Thermistor,
        /// 100=工业TC, 101=Switch, 102=SPRT, 103=电压变送器, 104=电流变送器,
        /// 105=标准TC, 106=自定义RTD, 110=StandardResistance, 0xFF=None）
        /// </summary>
        public byte ElectricalFunctionType { get; set; } = 0xFF;

        /// <summary>
        /// 量程 Index
        /// </summary>
        public byte Range { get; set; }

        /// <summary>
        /// 是否为自动量程
        /// </summary>
        public bool IsAutoRange { get; set; }

        /// <summary>
        /// 通道间采集延时
        /// </summary>
        public uint Delay { get; set; }

        /// <summary>
        /// 滤波数据量
        /// </summary>
        public int FilteringCount { get; set; }

        /// <summary>
        /// 扩展信息 1
        /// </summary>
        public string ChannelInfo1 { get; set; } = string.Empty;

        /// <summary>
        /// 扩展信息 2
        /// </summary>
        public string ChannelInfo2 { get; set; } = string.Empty;

        /// <summary>
        /// 扩展信息 3
        /// </summary>
        public string ChannelInfo3 { get; set; } = string.Empty;

        /// <summary>
        /// 扩展信息 4
        /// </summary>
        public string ChannelInfo4 { get; set; } = string.Empty;

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(Name);

        /// <inheritdoc/>
        public override string ToString() =>
            $"Name={Name},Enable={Enabled},Label={Label},Type={ElectricalFunctionType},Range={Range}";
    }
}
