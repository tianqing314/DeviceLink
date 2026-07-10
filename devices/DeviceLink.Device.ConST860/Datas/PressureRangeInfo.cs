namespace DeviceLink.Device.ConST860;

/// <summary>
/// 量程信息
/// </summary>
public class PressureRangeInfo
{
    /// <summary>
    /// 量程索引
    /// </summary>
    public string Index { get; set; } = string.Empty;

    /// <summary>
    /// 量程描述
    /// </summary>
    public string Range { get; set; } = string.Empty;

    /// <summary>
    /// 单位
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{Index}, {Range} {Unit}";
    }
}
