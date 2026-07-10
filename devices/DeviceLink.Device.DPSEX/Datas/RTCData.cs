namespace DeviceLink.Device.DPSEX.Datas;

/// <summary>
/// RTC 时钟数据
/// 对应 Xmas11 RTCData
/// </summary>
public class RTCData
{
    /// <summary>
    /// 年月日（如 "2026-07-10"）
    /// </summary>
    public string Date { get; set; } = string.Empty;

    /// <summary>
    /// 时分秒（如 "14:30:00"）
    /// </summary>
    public string Time { get; set; } = string.Empty;

    public override string ToString() => $"{Date} {Time}";
}
