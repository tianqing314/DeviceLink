namespace DeviceLink.Device.DPSEX.Datas;

/// <summary>
/// 压力数据输出速度枚举
/// 对应 Xmas11 DataOutputSpeedEnum
/// SPEED 指令：0=低速, 1=高速
/// </summary>
public enum OutputSpeed
{
    /// <summary>
    /// 低速输出
    /// </summary>
    Low = 0,

    /// <summary>
    /// 高速输出
    /// </summary>
    High = 1
}
