using System;

namespace DeviceLink.Device.ConST860
{
    /// <summary>
    /// 压力值（含单位）
    /// </summary>
    public class PressureValue
    {
        /// <summary>压力值</summary>
        public double Value { get; set; } = double.NaN;

        /// <summary>压力单位</summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>是否有效</summary>
        public bool IsValid => !double.IsNaN(Value);

        public override string ToString() => $"{Value} {Unit}";
    }
}
