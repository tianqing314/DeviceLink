namespace DeviceLink.Device.ConST171A;

/// <summary>
/// 泵温度 —— DIAGnostic:PUMP:TEMP? 返回值
/// 格式：20.5°C，26°C（前级泵, 增压泵）
/// </summary>
public class PumpTemperatures
{
    /// <summary>
    /// 前级泵温度（℃）
    /// </summary>
    public double PreStagePump { get; set; } = double.NaN;

    /// <summary>
    /// 增压泵温度（℃）
    /// </summary>
    public double BoostPump { get; set; } = double.NaN;

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"前级={PreStagePump}°C, 增压={BoostPump}°C";
    }
}
