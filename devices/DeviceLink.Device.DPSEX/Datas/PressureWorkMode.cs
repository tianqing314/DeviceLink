namespace DeviceLink.Device.DPSEX.Datas;

/// <summary>
/// 压力工作模式枚举
/// 对应 Xmas11 PressureWorkMode
/// MWORK 指令
/// </summary>
public enum PressureWorkMode
{
    /// <summary>
    /// 普通模式
    /// </summary>
    Normal = 0,

    /// <summary>
    /// 峰值模式
    /// </summary>
    Peak = 1,

    /// <summary>
    /// 线性模式
    /// </summary>
    Linear = 2,

    /// <summary>
    /// 线性+峰值模式
    /// </summary>
    LinearPeak = 3
}
