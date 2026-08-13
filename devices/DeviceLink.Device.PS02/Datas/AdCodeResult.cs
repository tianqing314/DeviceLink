using System;

namespace DeviceLink.Device.PS02;

/// <summary>
/// 调试模式电流输出档位（寄存器 0x8003 的 DAC 值，大端存储）
/// </summary>
public enum DebugCurrentOutput : ushort
{
    /// <summary>
    /// 4mA 电流输出（DAC = 0x2900，指令字节 29 00）
    /// </summary>
    Current4mA = 0x2900,

    /// <summary>
    /// 20mA 电流输出（DAC = 0xCF49，指令字节 CF 49）
    /// </summary>
    Current20mA = 0xCF49
}

/// <summary>
/// 调试模式电压输出档位（寄存器 0x8003 的 DAC 值，大端存储）
/// </summary>
public enum DebugVoltageOutput : ushort
{
    /// <summary>
    /// 0.5V 电压输出（DAC = 0x0666，指令字节 06 66）
    /// </summary>
    Voltage0_5V = 0x0666,

    /// <summary>
    /// 10V 电压输出（DAC = 0x7FFF，满量程，指令字节 7F FF）
    /// </summary>
    Voltage10V = 0x7FFF
}

/// <summary>
/// 读 AD 码指令（F40 读寄存器 0x7000，32 个寄存器 = 64 字节）的解析结果。
///
/// 响应数据布局（参考《PS02整机自检测试项.xlsx》数据转换表）：
///   偏移 0  : 压力ADC（int32 小端）
///   偏移 4  : 温度ADC（int32 小端）
///   偏移 8  : 温度补偿压力值（int32 小端）
///   偏移 12 : 修正后的压力值（int32 小端）
///   偏移 16 : 校准压力值（int32 小端）
///   偏移 20 : 滤波后压力值（int32 小端）
///   偏移 24 : 计算的压力值（int32 小端）
///   偏移 28 : TMP1075 温度值（int16 大端，值/16*0.0625 = ℃）
/// </summary>
public class AdCodeResult
{
    /// <summary>
    /// 压力 ADC 原始值（int32 小端）
    /// </summary>
    public int PressureAdc { get; set; }

    /// <summary>
    /// 温度 ADC 原始值（int32 小端）
    /// </summary>
    public int TemperatureAdc { get; set; }

    /// <summary>
    /// 温度补偿压力值（int32 小端）
    /// </summary>
    public int TemperatureCompensatedPressure { get; set; }

    /// <summary>
    /// 修正后的压力值（int32 小端）
    /// </summary>
    public int CorrectedPressure { get; set; }

    /// <summary>
    /// 校准压力值（int32 小端）
    /// </summary>
    public int CalibratedPressure { get; set; }

    /// <summary>
    /// 滤波后压力值（int32 小端）
    /// </summary>
    public int FilteredPressure { get; set; }

    /// <summary>
    /// 计算的压力值（int32 小端）
    /// </summary>
    public int ComputedPressure { get; set; }

    /// <summary>
    /// TMP1075 温度值原始编码（int16 大端）
    /// </summary>
    public short Tm1075Raw { get; set; }

    // ═══════════════════════════════════════════════════════════
    // 换算值（公式参考《PS02整机自检测试项.xlsx》）
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 压力换算值（kPa）= PressureAdc * 0.000270886 + 6.96966528
    /// </summary>
    public double PressureKpa => PressureAdc * 0.000270886 + 6.96966528;

    /// <summary>
    /// 桥阻值（欧姆）= 32715571200 / TemperatureAdc；温度ADC为0时返回 NaN
    /// </summary>
    public double BridgeResistance => TemperatureAdc != 0 ? 32715571200.0 / TemperatureAdc : double.NaN;

    /// <summary>
    /// 温度补偿压力换算值（kPa）= 值 / 131072
    /// </summary>
    public double TemperatureCompensatedPressureKpa => TemperatureCompensatedPressure / 131072.0;

    /// <summary>
    /// 修正后压力换算值（kPa）= 值 / 131072
    /// </summary>
    public double CorrectedPressureKpa => CorrectedPressure / 131072.0;

    /// <summary>
    /// 校准压力换算值（kPa）= 值 / 131072
    /// </summary>
    public double CalibratedPressureKpa => CalibratedPressure / 131072.0;

    /// <summary>
    /// 滤波后压力换算值（kPa）= 值 / 131072
    /// </summary>
    public double FilteredPressureKpa => FilteredPressure / 131072.0;

    /// <summary>
    /// 计算压力换算值（kPa）= 值 / 131072
    /// </summary>
    public double ComputedPressureKpa => ComputedPressure / 131072.0;

    /// <summary>
    /// TMP1075 温度值（℃）= Tm1075Raw / 16 * 0.0625
    /// </summary>
    public double Tm1075Temperature => Tm1075Raw / 16.0 * 0.0625;

    // ═══════════════════════════════════════════════════════════
    // 判定指标（参考《PS02通信指令示例》）
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 判定：压力AD绝对值 &lt; 300000
    /// </summary>
    public bool IsPressureAdcOk => Math.Abs((long)PressureAdc) < 300000;

    /// <summary>
    /// 判定：桥阻值介于 9000~11000 欧姆
    /// </summary>
    public bool IsBridgeResistanceOk => BridgeResistance >= 9000 && BridgeResistance <= 11000;

    /// <summary>
    /// 判定：温度介于 15~30℃
    /// </summary>
    public bool IsTemperatureOk => Tm1075Temperature >= 15 && Tm1075Temperature <= 30;

    /// <summary>
    /// 全部判定通过
    /// </summary>
    public bool IsAllOk => IsPressureAdcOk && IsBridgeResistanceOk && IsTemperatureOk;

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"压力ADC:{PressureAdc}({PressureKpa:F3}kPa,{(IsPressureAdcOk ? "OK" : "NG")}), " +
               $"温度ADC:{TemperatureAdc}(桥阻:{BridgeResistance:F1}Ω,{(IsBridgeResistanceOk ? "OK" : "NG")}), " +
               $"TMP1075温度:{Tm1075Temperature:F2}℃,{(IsTemperatureOk ? "OK" : "NG")}, " +
               $"总体:{(IsAllOk ? "OK" : "NG")}";
    }
}
