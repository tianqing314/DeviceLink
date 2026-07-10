namespace DeviceLink.Device.ConST860;

/// <summary>
/// 默认大气压值
/// </summary>
public class AtmosphericPressure
{
    /// <summary>
    /// 压力值
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// 单位
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{Value} {Unit}";
    }
}
