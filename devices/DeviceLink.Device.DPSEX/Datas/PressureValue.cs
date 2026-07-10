namespace DeviceLink.Device.DPSEX.Datas;

/// <summary>
/// 压力测量值（含单位字符串）
/// 对应 Xmas11 GetPressure() 返回的 Pressure 对象
/// </summary>
public class PressureValue
{
    /// <summary>
    /// 压力值
    /// </summary>
    public double Value { get; set; } = double.NaN;

    /// <summary>
    /// 压力单位（如 "kPa", "MPa", "psi" 等）
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid => !double.IsNaN(Value);

    public override string ToString() => $"{Value} {Unit}";
}
