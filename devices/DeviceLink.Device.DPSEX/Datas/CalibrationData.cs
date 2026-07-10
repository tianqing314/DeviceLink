namespace DeviceLink.Device.DPSEX.Datas;

/// <summary>
/// 校准前压力数据
/// 对应 Xmas11 DPS_EXPressureData
/// MRMC 指令返回校准前的测量压力值和单位
/// </summary>
public class CalibrationData
{
    /// <summary>
    /// 压力测量值
    /// </summary>
    public double MeasureValue { get; set; } = double.NaN;

    /// <summary>
    /// 压力单位（如 "kPa", "MPa" 等）
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid => !double.IsNaN(MeasureValue);

    public override string ToString() => $"{MeasureValue} {Unit}";
}
