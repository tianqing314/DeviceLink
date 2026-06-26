namespace DeviceLink.Device.ConST860
{
    /// <summary>
    /// 目标值范围
    /// </summary>
    public class TargetPressureRange
    {
        /// <summary>下限</summary>
        public double Low { get; set; }

        /// <summary>上限</summary>
        public double High { get; set; }

        /// <summary>单位</summary>
        public string Unit { get; set; } = string.Empty;

        public override string ToString() => $"({Low} ~ {High}) {Unit}";
    }
}
