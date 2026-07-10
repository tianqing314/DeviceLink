namespace DeviceLink.Device.ConST860;

/// <summary>
/// 模块信息
/// </summary>
public class ModuleInfo
{
    /// <summary>
    /// 序列号
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// 量程
    /// </summary>
    public string Range { get; set; } = string.Empty;

    /// <summary>
    /// 压力类型：G=表压, A=绝压, D=差压
    /// </summary>
    public string PressureType { get; set; } = string.Empty;

    /// <summary>
    /// 版本
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 精度
    /// </summary>
    public string Accuracy { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{SerialNumber}, {Range}, {PressureType}, {Version}, {Accuracy}";
    }
}
