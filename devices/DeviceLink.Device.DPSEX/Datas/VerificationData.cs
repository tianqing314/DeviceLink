namespace DeviceLink.Device.DPSEX.Datas;

/// <summary>
/// 检定信息
/// 对应 Xmas11 VerificationData
/// </summary>
public class VerificationData
{
    #region 标准器信息

    /// <summary>
    /// 检定时间（如 "20201209191334"）
    /// </summary>
    public string VerificatiTime { get; set; } = string.Empty;

    /// <summary>
    /// 标准器名称（如 "ConST822"）
    /// </summary>
    public string SensorName { get; set; } = string.Empty;

    /// <summary>
    /// 标准器量程（如 "-100.000~700.000"）
    /// </summary>
    public string SensorRange { get; set; } = string.Empty;

    /// <summary>
    /// 标准器精度（如 "0.005%RD+0.005%FS"）
    /// </summary>
    public string SensorAccuracy { get; set; } = string.Empty;

    #endregion

    #region 被检精度信息

    /// <summary>
    /// 校准前最大示值误差
    /// </summary>
    public string IndicationMaxErrorBefore { get; set; } = string.Empty;

    /// <summary>
    /// 校准后最大示值误差
    /// </summary>
    public string IndicationMaxErrorAfter { get; set; } = string.Empty;

    /// <summary>
    /// 校准前最大回程误差
    /// </summary>
    public string HysterisisMaxErrorBefore { get; set; } = string.Empty;

    /// <summary>
    /// 校准后最大回程误差
    /// </summary>
    public string HysterisisMaxErrorAfter { get; set; } = string.Empty;

    #endregion

    #region 校准点数据

    /// <summary>
    /// 校准点1 原始字符串 (标准值,未校准值,上次校准后值)
    /// </summary>
    public string FirstStr { get; set; } = string.Empty;

    /// <summary>
    /// 校准点1 标准值
    /// </summary>
    public double FirstStdValue { get; set; } = double.NaN;

    /// <summary>
    /// 校准点1 未校准值
    /// </summary>
    public double FirstCancelValue { get; set; } = double.NaN;

    /// <summary>
    /// 校准点1 上次校准后值
    /// </summary>
    public double FirstEffectValue { get; set; } = double.NaN;

    /// <summary>
    /// 校准点2 原始字符串
    /// </summary>
    public string SecondStr { get; set; } = string.Empty;

    /// <summary>
    /// 校准点2 标准值
    /// </summary>
    public double SecondStdValue { get; set; } = double.NaN;

    /// <summary>
    /// 校准点2 未校准值
    /// </summary>
    public double SecondCancelValue { get; set; } = double.NaN;

    /// <summary>
    /// 校准点2 上次校准后值
    /// </summary>
    public double SecondEffectValue { get; set; } = double.NaN;

    /// <summary>
    /// 校准点3 原始字符串
    /// </summary>
    public string ThirdStr { get; set; } = string.Empty;

    /// <summary>
    /// 校准点3 标准值
    /// </summary>
    public double ThirdStdValue { get; set; } = double.NaN;

    /// <summary>
    /// 校准点3 未校准值
    /// </summary>
    public double ThirdCancelValue { get; set; } = double.NaN;

    /// <summary>
    /// 校准点3 上次校准后值
    /// </summary>
    public double ThirdEffectValue { get; set; } = double.NaN;

    /// <summary>
    /// 校准点4 原始字符串
    /// </summary>
    public string FourthStr { get; set; } = string.Empty;

    /// <summary>
    /// 校准点4 标准值
    /// </summary>
    public double FourthStdValue { get; set; } = double.NaN;

    /// <summary>
    /// 校准点4 未校准值
    /// </summary>
    public double FourthCancelValue { get; set; } = double.NaN;

    /// <summary>
    /// 校准点4 上次校准后值
    /// </summary>
    public double FourthEffectValue { get; set; } = double.NaN;

    /// <summary>
    /// 校准点5 原始字符串
    /// </summary>
    public string FifthStr { get; set; } = string.Empty;

    /// <summary>
    /// 校准点5 标准值
    /// </summary>
    public double FifthStdValue { get; set; } = double.NaN;

    /// <summary>
    /// 校准点5 未校准值
    /// </summary>
    public double FifthCancelValue { get; set; } = double.NaN;

    /// <summary>
    /// 校准点5 上次校准后值
    /// </summary>
    public double FifthEffectValue { get; set; } = double.NaN;

    #endregion

    #region 模块运行信息

    /// <summary>
    /// 电源电压（单位 V）
    /// </summary>
    public double PowerVoltage { get; set; }

    /// <summary>
    /// 压力传感器输入阻抗（单位 Ω）
    /// </summary>
    public double InputImpedance { get; set; }

    /// <summary>
    /// 恒流恒压激励源大小（恒流单位 mA，恒压单位 V）
    /// </summary>
    public double ConstantStimulate { get; set; }

    /// <summary>
    /// TMP117 温度（单位 ℃）
    /// </summary>
    public double TMP117 { get; set; }

    /// <summary>
    /// MCU 温度（单位 ℃）
    /// </summary>
    public double MCU { get; set; }

    #endregion
}
