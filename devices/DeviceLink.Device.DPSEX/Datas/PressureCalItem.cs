namespace DeviceLink.Device.DPSEX.Datas;

/// <summary>
/// 压力校准项枚举
/// 对应 Xmas11 PressureCalItem
/// </summary>
public enum PressureCalItem
{
    /// <summary>
    /// 零点校准
    /// </summary>
    Z = 0,

    /// <summary>
    /// 中间点校准
    /// </summary>
    M = 1,

    /// <summary>
    /// 满度校准
    /// </summary>
    F = 2
}
