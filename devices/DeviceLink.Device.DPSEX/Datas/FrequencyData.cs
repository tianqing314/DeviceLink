namespace DeviceLink.Device.DPSEX.Datas;

/// <summary>
/// 频率数据
/// 对应 Xmas11 FrequencyData
/// </summary>
public class FrequencyData
{
    /// <summary>
    /// 频率数据1
    /// </summary>
    public double Frequency1 { get; set; } = double.NaN;

    /// <summary>
    /// 频率数据2
    /// </summary>
    public double Frequency2 { get; set; } = double.NaN;

    public override string ToString() => $"F1={Frequency1}, F2={Frequency2}";
}
