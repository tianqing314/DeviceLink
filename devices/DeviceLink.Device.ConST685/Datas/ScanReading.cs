using System;

namespace DeviceLink.Device.ConST685
{
    /// <summary>
    /// 时间刻度 —— 对应 Xmas11 TimeTick
    /// </summary>
    public class TimeTick
    {
        /// <summary>
        /// 时间
        /// </summary>
        public DateTime TickTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 扫描读数 —— 对应 Xmas11 DIReading
    /// JSON:SCAN:DATA? JSON:SCAN:SCONnection:DATA? 返回值
    /// </summary>
    public class ScanReading
    {
        /// <summary>
        /// 类名称（反序列化标识）
        /// </summary>
        public string ClassName { get; set; } = "DIReading";

        /// <summary>
        /// 通道名称
        /// </summary>
        public string ChannelName { get; set; } = string.Empty;

        /// <summary>
        /// 电测原始值列表
        /// </summary>
        public System.Collections.Generic.List<double> Values { get; set; } = new System.Collections.Generic.List<double>();

        /// <summary>
        /// 电测滤波后值列表
        /// </summary>
        public System.Collections.Generic.List<double> ValuesFiltered { get; set; } = new System.Collections.Generic.List<double>();

        /// <summary>
        /// 数据时间列表
        /// </summary>
        public System.Collections.Generic.List<TimeTick> DateTimeTicks { get; set; } = new System.Collections.Generic.List<TimeTick>();

        /// <summary>
        /// 电测单位 Id（参见 SCPI 单位 Id 列表）
        /// </summary>
        public ushort Unit { get; set; }

        /// <summary>
        /// 小数位数
        /// </summary>
        public int ValueDecimals { get; set; }

        /// <summary>
        /// 温度值列表（仅温度通道）
        /// </summary>
        public System.Collections.Generic.List<double> TempValues { get; set; } = new System.Collections.Generic.List<double>();

        /// <summary>
        /// 温度单位 Id
        /// </summary>
        public ushort TempUnit { get; set; }

        /// <summary>
        /// 温度小数位数
        /// </summary>
        public int TempDecimals { get; set; }

        /// <summary>
        /// 冷端补偿温度列表（仅 TC 通道）
        /// </summary>
        public System.Collections.Generic.List<double> CJCs { get; set; } = new System.Collections.Generic.List<double>();

        /// <summary>
        /// 冷端电测值单位 Id
        /// </summary>
        public ushort CJCUnit { get; set; }

        /// <summary>
        /// 冷端电测值列表（仅 TC 通道，外部冷端）
        /// </summary>
        public System.Collections.Generic.List<double> CjcRaws { get; set; } = new System.Collections.Generic.List<double>();

        /// <summary>
        /// 冷端电测值单位 Id
        /// </summary>
        public ushort CJCRawsUnit { get; set; }

        /// <summary>
        /// 冷端小数位数
        /// </summary>
        public int CJCDecimals { get; set; }

        /// <summary>
        /// 输入值/原始信号值列表（仅变送器通道）
        /// </summary>
        public System.Collections.Generic.List<double> InputValues { get; set; } = new System.Collections.Generic.List<double>();

        /// <summary>
        /// 输入值单位 Id
        /// </summary>
        public ushort InputUnit { get; set; }

        /// <summary>
        /// 输入值单位名称
        /// </summary>
        public string InputUnitName { get; set; } = string.Empty;

        /// <summary>
        /// 输入值小数位数
        /// </summary>
        public int InputDecimals { get; set; }

        /// <summary>
        /// 开关状态列表（仅开关量通道，按位获取）
        /// </summary>
        public System.Collections.Generic.List<ushort> SwitchValues { get; set; } = new System.Collections.Generic.List<ushort>();

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(ChannelName);

        /// <inheritdoc/>
        public override string ToString() =>
            $"Ch={ChannelName},Values={Values.Count}pts,Unit={Unit},TempUnit={TempUnit}";
    }
}
