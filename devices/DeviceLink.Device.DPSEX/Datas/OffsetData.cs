namespace DeviceLink.Device.DPSEX.Datas;

/// <summary>
/// 回差修正数据
/// 对应 Xmas11 OffsetData
/// </summary>
public class OffsetData
{
    /// <summary>
    /// 是否生效
    /// </summary>
    public bool IsEffect { get; set; }

    /// <summary>
    /// 系数值
    /// </summary>
    public double Coefficient { get; set; }

    /// <summary>
    /// 版本信息
    /// </summary>
    public string Version { get; set; } = string.Empty;

    public override string ToString() =>
        $"Effect={IsEffect}, Coef={Coefficient}, Ver={Version}";
}
