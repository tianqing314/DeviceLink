namespace DeviceLink.Device.DPSEX.Datas;

/// <summary>
/// 驱动板大气压和温度数据
/// 对应 Xmas11 ActuatorBoardData
/// </summary>
public class ActuatorBoardData
{
    /// <summary>
    /// 压力值
    /// </summary>
    public double PressureValue { get; set; } = double.NaN;

    /// <summary>
    /// 压力单位
    /// </summary>
    public string PressureUnit { get; set; } = string.Empty;

    /// <summary>
    /// 温度值
    /// </summary>
    public double TemperatureValue { get; set; } = double.NaN;

    /// <summary>
    /// 温度单位
    /// </summary>
    public string TemperatureUnit { get; set; } = string.Empty;

    public override string ToString() =>
        $"P={PressureValue} {PressureUnit}, T={TemperatureValue} {TemperatureUnit}";
}
