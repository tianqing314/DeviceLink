namespace DeviceLink.Device.ConST171A
{
    /// <summary>
    /// 压力范围 —— PRESsure:RANGe? 返回值
    /// </summary>
    public class PressureRange
    {
        /// <summary>下限压力值（kPa）</summary>
        public double Min { get; set; }

        /// <summary>上限压力值（kPa）</summary>
        public double Max { get; set; }

        /// <summary>是否有效</summary>
        public bool IsValid => !double.IsNaN(Min) && !double.IsNaN(Max) && Max >= Min;

        /// <inheritdoc/>
        public override string ToString() => $"{Min}:{Max} kPa";
    }
}
