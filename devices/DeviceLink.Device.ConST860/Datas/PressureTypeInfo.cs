namespace DeviceLink.Device.ConST860;

/// <summary>
/// 压力类型信息
/// </summary>
public class PressureTypeInfo
{
    /// <summary>
    /// 压力类型：G=表压, A=绝压, D=差压
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 是否支持表绝压切换
    /// </summary>
    public bool CanSwitch { get; set; }

    public override string ToString()
    {
        return $"{Type}, CanSwitch={CanSwitch}";
    }
}
