namespace DeviceLink.Device.DPSEX.Datas;

/// <summary>
/// 工作模式测量类型枚举
/// 对应 Xmas11 WorkModeTestTypeEnum
/// </summary>
public enum WorkModeTestType
{
    /// <summary>
    /// 控制模式
    /// </summary>
    Control = 0,

    /// <summary>
    /// 普通模式
    /// </summary>
    Normal = 1,

    /// <summary>
    /// 低功耗模式
    /// </summary>
    LowPower = 2,

    /// <summary>
    /// 未知
    /// </summary>
    Unknown = -1
}

/// <summary>
/// 工作模式测量配置
/// 对应 Xmas11 WorkModeMeasure
/// MTYPE 指令返回的完整配置
/// </summary>
public class WorkModeTestConfig
{
    /// <summary>
    /// 测量类型
    /// </summary>
    public WorkModeTestType Type { get; set; } = WorkModeTestType.Unknown;

    /// <summary>
    /// 时间间隔（秒）
    /// </summary>
    public double IntervalSeconds { get; set; }

    /// <summary>
    /// 采样次数
    /// </summary>
    public double SampleCount { get; set; }

    public override string ToString() =>
        $"Type={Type}, Interval={IntervalSeconds}s, Count={SampleCount}";
}
