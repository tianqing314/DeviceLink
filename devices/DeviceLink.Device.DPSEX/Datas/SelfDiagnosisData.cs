namespace DeviceLink.Device.DPSEX.Datas;

/// <summary>
/// 自诊断数据
/// 对应 Xmas11 SelfDiagnosisData
/// SELACK 指令返回 14 个诊断项
/// </summary>
public class SelfDiagnosisData
{
    /// <summary>
    /// 诊断项列表
    /// </summary>
    public List<SelfDiagnosisItem> Items { get; set; } = new();
}

/// <summary>
/// 自诊断单项
/// 对应 Xmas11 SelfDiagnosisItem
/// </summary>
public class SelfDiagnosisItem
{
    /// <summary>
    /// 项目代号
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 故障码
    /// </summary>
    public int FaultNo { get; set; }

    /// <summary>
    /// 测量值（字符串，原始格式）
    /// </summary>
    public string MeasureValue { get; set; } = string.Empty;

    public override string ToString() =>
        $"[{Sort}] Fault={FaultNo}, Value={MeasureValue}";
}
