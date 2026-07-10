namespace DeviceLink.Device.ConST860;

/// <summary>
/// 去皮信息
/// </summary>
public class TareInfo
{
    /// <summary>
    /// 使能：0=关闭, 1=开启
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 去皮值
    /// </summary>
    public double Value { get; set; }

    public override string ToString()
    {
        return $"Enabled={Enabled}, Value={Value}";
    }
}
