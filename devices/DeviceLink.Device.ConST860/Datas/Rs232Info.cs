namespace DeviceLink.Device.ConST860;

/// <summary>
/// RS232 串口参数
/// </summary>
public class Rs232Info
{
    /// <summary>
    /// 波特率
    /// </summary>
    public int BaudRate { get; set; }

    /// <summary>
    /// 数据位
    /// </summary>
    public int DataBits { get; set; }

    /// <summary>
    /// 停止位
    /// </summary>
    public string StopBits { get; set; } = string.Empty;

    /// <summary>
    /// 校验位
    /// </summary>
    public string Parity { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{BaudRate},{DataBits},{StopBits},{Parity}";
    }
}
