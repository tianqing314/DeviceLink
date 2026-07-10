namespace DeviceLink.Device.DPSEX.Datas;

/// <summary>
/// 压力校准状态
/// 对应 Xmas11 PCAL_GetState() / PCAL_GetState2()
/// </summary>
public class CalibrationState
{
    /// <summary>
    /// 校准数据是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 温补是否生效
    /// </summary>
    public bool IsTemperatureCompensated { get; set; }

    /// <summary>
    /// 线性是否生效
    /// </summary>
    public bool IsLinearized { get; set; }

    /// <summary>
    /// 校准数据是否生效
    /// </summary>
    public bool IsCalibrationActive { get; set; }

    /// <summary>
    /// 厂家校准是否已完成
    /// </summary>
    public bool IsFactoryCalibrated { get; set; }

    /// <summary>
    /// 用户校准是否已完成
    /// </summary>
    public bool IsUserCalibrated { get; set; }

    /// <summary>
    /// 校准点数量（2=两点校准, 3=三点校准）
    /// </summary>
    public int CalibrationPointCount { get; set; } = 2;

    public override string ToString() =>
        $"FCal={IsFactoryCalibrated}, UCal={IsUserCalibrated}, Pts={CalibrationPointCount}";
}
