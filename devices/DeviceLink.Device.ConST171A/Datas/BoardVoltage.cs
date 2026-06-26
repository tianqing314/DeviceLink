namespace DeviceLink.Device.ConST171A
{
    /// <summary>
    /// 主板电压 —— DIAGnostic:BOARd:VOLTage? 返回值
    /// 格式：23.8V，3.3V，3.3V
    /// </summary>
    public class BoardVoltage
    {
        /// <summary>24V 供电电压</summary>
        public double Voltage24V { get; set; } = double.NaN;

        /// <summary>正压（Boost）传感器电压</summary>
        public double BoostSensorVoltage { get; set; } = double.NaN;

        /// <summary>真空（Vacuum）传感器电压</summary>
        public double VacuumSensorVoltage { get; set; } = double.NaN;

        /// <summary>是否有效</summary>
        public bool IsValid => !double.IsNaN(Voltage24V);

        /// <inheritdoc/>
        public override string ToString() => $"{Voltage24V}V, {BoostSensorVoltage}V, {VacuumSensorVoltage}V";
    }
}
