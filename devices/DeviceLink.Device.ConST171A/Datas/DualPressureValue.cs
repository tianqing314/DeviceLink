namespace DeviceLink.Device.ConST171A
{
    /// <summary>
    /// 多路压力值 —— PRESsure?（无参数）返回设备全部气源压力
    /// 实际返回格式：正压值,单位,真空值,单位,前级值,单位（共 6 字段）
    /// </summary>
    public class DualPressureValue
    {
        /// <summary>正压气源压力值</summary>
        public double PositiveValue { get; set; } = double.NaN;

        /// <summary>正压气源压力单位</summary>
        public string PositiveUnit { get; set; } = string.Empty;

        /// <summary>真空气源压力值</summary>
        public double VacuumValue { get; set; } = double.NaN;

        /// <summary>真空气源压力单位</summary>
        public string VacuumUnit { get; set; } = string.Empty;

        /// <summary>前级泵压力值（部分固件版本返回）</summary>
        public double PreValue { get; set; } = double.NaN;

        /// <summary>前级泵压力单位</summary>
        public string PreUnit { get; set; } = string.Empty;

        /// <summary>正压和真空双路是否都有效</summary>
        public bool IsValid => !double.IsNaN(PositiveValue) && !double.IsNaN(VacuumValue);

        /// <inheritdoc/>
        public override string ToString() =>
            double.IsNaN(PreValue)
                ? $"正压={PositiveValue} {PositiveUnit}, 真空={VacuumValue} {VacuumUnit}"
                : $"正压={PositiveValue} {PositiveUnit}, 真空={VacuumValue} {VacuumUnit}, 前级={PreValue} {PreUnit}";
    }
}
