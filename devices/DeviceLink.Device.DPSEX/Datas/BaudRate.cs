namespace DeviceLink.Device.DPSEX.Datas;

/// <summary>
/// 波特率枚举
/// 对应 Xmas11 BaudRateEnum
/// OBAUQ 指令
/// </summary>
public enum BaudRate
{
    /// <summary>
    /// 1200
    /// </summary>
    Baud1200 = 0,

    /// <summary>
    /// 2400
    /// </summary>
    Baud2400 = 1,

    /// <summary>
    /// 4800
    /// </summary>
    Baud4800 = 2,

    /// <summary>
    /// 9600
    /// </summary>
    Baud9600 = 3,

    /// <summary>
    /// 19200
    /// </summary>
    Baud19200 = 4,

    /// <summary>
    /// 38400
    /// </summary>
    Baud38400 = 5
}
