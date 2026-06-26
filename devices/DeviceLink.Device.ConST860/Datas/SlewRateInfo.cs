namespace DeviceLink.Device.ConST860
{
    /// <summary>
    /// 控制速率信息
    /// </summary>
    public class SlewRateInfo
    {
        /// <summary>速率类型：0=不限制, 1=限制</summary>
        public int Type { get; set; }

        /// <summary>速率值（不限制时为 "MAX"）</summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>单位</summary>
        public string Unit { get; set; } = string.Empty;

        public override string ToString() => $"{Type}, {Value}, {Unit}";
    }
}
