namespace DeviceLink.Device.ConST860;

/// <summary>
/// 判稳设置信息
/// </summary>
public class StabilityInfo
{
    /// <summary>
    /// 稳定度设置：0=百分比, 1=波动值
    /// </summary>
    public int Type { get; set; }

    /// <summary>
    /// 波动值
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// 波动值单位
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// 百分比值
    /// </summary>
    public double PercentValue { get; set; }

    /// <summary>
    /// 百分比单位
    /// </summary>
    public string PercentUnit { get; set; } = string.Empty;

    /// <summary>
    /// 稳定时间（秒）
    /// </summary>
    public int Seconds { get; set; }

    public override string ToString()
    {
        return $"Type={Type}, Value={Value} {Unit}, Percent={PercentValue} {PercentUnit}, Seconds={Seconds}";
    }
}
