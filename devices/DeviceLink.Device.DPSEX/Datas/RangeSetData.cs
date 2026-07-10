namespace DeviceLink.Device.DPSEX.Datas;

/// <summary>
/// 标定量程系数（K、B）
/// 对应 Xmas11 RangeSetData
/// </summary>
public class RangeSetData
{
    /// <summary>
    /// K 值（斜率）
    /// </summary>
    public double KValue { get; set; }

    /// <summary>
    /// B 值（截距）
    /// </summary>
    public double BValue { get; set; }

    public override string ToString() => $"K={KValue}, B={BValue}";
}
