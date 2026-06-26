namespace DeviceLink.Device.ConST171A
{
    /// <summary>
    /// 泵电流 —— DIAGnostic:PUMP:CURRent? 返回值
    /// 格式：0.5A，0.3A（前级泵, 增压泵）
    /// </summary>
    public class PumpCurrents
    {
        /// <summary>前级泵电流（A）</summary>
        public double PreStagePump { get; set; } = double.NaN;

        /// <summary>增压泵电流（A）</summary>
        public double BoostPump { get; set; } = double.NaN;

        /// <inheritdoc/>
        public override string ToString() => $"前级={PreStagePump}A, 增压={BoostPump}A";
    }
}
