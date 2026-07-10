namespace DeviceLink.Device.DPSEX.Datas;

/// <summary>
/// 压力量程详细信息（含量程、压力类型、精确度等级）
/// 对应 Xmas11 GetPressureRangeDetailedInfo() 返回的 PressureRangeDetailedInfo
/// ORAN 指令返回 7 个字段时解析
/// </summary>
public class PressureRangeInfo
{
    /// <summary>
    /// 量程下限值
    /// </summary>
    public double Low { get; set; } = double.NaN;

    /// <summary>
    /// 量程上限值
    /// </summary>
    public double High { get; set; } = double.NaN;

    /// <summary>
    /// 压力单位（如 "kPa", "MPa" 等）
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// 传感器类型
    /// G = 表压 (Gauge)
    /// A = 绝压 (Absolute)
    /// D = 差压 (Differential)
    /// S = 密封表压 (Sealed)
    /// V = 真空 (Vacuum)
    /// </summary>
    public string PressureType { get; set; } = string.Empty;

    /// <summary>
    /// 精确度等级索引值（原始值）
    /// </summary>
    public int AccuracyIndex { get; set; }

    /// <summary>
    /// 精确度百分比值
    /// 1=0.2%, 2=0.1%, 3=0.05%, 4=0.02%, 5=0.025%, 6=0.008%
    /// 101~255 = (index-100)/100 %
    /// </summary>
    public double AccuracyPercent { get; set; }

    public override string ToString() =>
        $"{Low} ~ {High} {Unit}, Type={PressureType}, Acc={AccuracyPercent}%";
}
