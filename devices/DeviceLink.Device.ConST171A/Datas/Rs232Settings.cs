namespace DeviceLink.Device.ConST171A;

/// <summary>
/// RS232 串口参数 —— SYSTem:RS232:INFo? 返回值
/// 格式：9600,8,One,None
/// </summary>
public class Rs232Settings
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
    /// 停止位（None/One/Two/OnePointFive）
    /// </summary>
    public string StopBits { get; set; } = string.Empty;

    /// <summary>
    /// 校验位（None/Odd/Even）
    /// </summary>
    public string Parity { get; set; } = string.Empty;

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{BaudRate},{DataBits},{StopBits},{Parity}";
    }
}
