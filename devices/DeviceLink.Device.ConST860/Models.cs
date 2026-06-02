using System;

namespace DeviceLink.Device.ConST860
{
    /// <summary>
    /// 压力值（含单位）
    /// </summary>
    public class PressureValue
    {
        /// <summary>压力值</summary>
        public double Value { get; set; } = double.NaN;

        /// <summary>压力单位</summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>是否有效</summary>
        public bool IsValid => !double.IsNaN(Value);

        public override string ToString() => $"{Value} {Unit}";
    }

    /// <summary>
    /// 压力类型信息
    /// </summary>
    public class PressureTypeInfo
    {
        /// <summary>压力类型：G=表压, A=绝压, D=差压</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>是否支持表绝压切换</summary>
        public bool CanSwitch { get; set; }

        public override string ToString() => $"{Type}, CanSwitch={CanSwitch}";
    }

    /// <summary>
    /// 目标值范围
    /// </summary>
    public class TargetPressureRange
    {
        /// <summary>下限</summary>
        public double Low { get; set; }

        /// <summary>上限</summary>
        public double High { get; set; }

        /// <summary>单位</summary>
        public string Unit { get; set; } = string.Empty;

        public override string ToString() => $"({Low} ~ {High}) {Unit}";
    }

    /// <summary>
    /// 量程信息
    /// </summary>
    public class PressureRangeInfo
    {
        /// <summary>量程索引</summary>
        public string Index { get; set; } = string.Empty;

        /// <summary>量程描述</summary>
        public string Range { get; set; } = string.Empty;

        /// <summary>单位</summary>
        public string Unit { get; set; } = string.Empty;

        public override string ToString() => $"{Index}, {Range} {Unit}";
    }

    /// <summary>
    /// 模块信息
    /// </summary>
    public class ModuleInfo
    {
        /// <summary>序列号</summary>
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>量程</summary>
        public string Range { get; set; } = string.Empty;

        /// <summary>压力类型：G=表压, A=绝压, D=差压</summary>
        public string PressureType { get; set; } = string.Empty;

        /// <summary>版本</summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>精度</summary>
        public string Accuracy { get; set; } = string.Empty;

        public override string ToString() => $"{SerialNumber}, {Range}, {PressureType}, {Version}, {Accuracy}";
    }

    /// <summary>
    /// 滤波信息
    /// </summary>
    public class FilterInfo
    {
        /// <summary>使能：0=关闭, 1=开启</summary>
        public bool Enabled { get; set; }

        /// <summary>滤波类型：0=一阶滤波, 1=平均滤波</summary>
        public int FilterType { get; set; }

        /// <summary>滤波参数（一阶滤波=系数0-1, 平均滤波=采样时间1-20s）</summary>
        public double Value { get; set; }

        public override string ToString() => $"Enabled={Enabled}, Type={FilterType}, Value={Value}";
    }

    /// <summary>
    /// 控制信息
    /// </summary>
    public class ControlInfo
    {
        /// <summary>实时值</summary>
        public double Value { get; set; }

        /// <summary>目标值</summary>
        public double Target { get; set; }

        /// <summary>单位</summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>量程</summary>
        public string Range { get; set; } = string.Empty;

        /// <summary>压力类型</summary>
        public string PressureType { get; set; } = string.Empty;

        /// <summary>是否稳定</summary>
        public bool IsStable { get; set; }

        /// <summary>控制状态</summary>
        public string State { get; set; } = string.Empty;

        /// <summary>扩展接口信息</summary>
        public string ExtendInfo { get; set; } = string.Empty;

        public override string ToString() => $"{Value}, {Target}, {Unit}, {Range}, {PressureType}, {IsStable}, {State}, {ExtendInfo}";
    }

    /// <summary>
    /// 控制速率信息
    /// </summary>
    public class SlewRateInfo
    {
        /// <summary>速率类型：0=不限制, 1=限制</summary>
        public int Type { get; set; }

        /// <summary>速率值（不限制时为 "MAX"）</summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>单位</summary>
        public string Unit { get; set; } = string.Empty;

        public override string ToString() => $"{Type}, {Value}, {Unit}";
    }

    /// <summary>
    /// 判稳设置信息
    /// </summary>
    public class StabilityInfo
    {
        /// <summary>稳定度设置：0=百分比, 1=波动值</summary>
        public int Type { get; set; }

        /// <summary>波动值</summary>
        public double Value { get; set; }

        /// <summary>波动值单位</summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>百分比值</summary>
        public double PercentValue { get; set; }

        /// <summary>百分比单位</summary>
        public string PercentUnit { get; set; } = string.Empty;

        /// <summary>稳定时间（秒）</summary>
        public int Seconds { get; set; }

        public override string ToString() => $"Type={Type}, Value={Value} {Unit}, Percent={PercentValue} {PercentUnit}, Seconds={Seconds}";
    }

    /// <summary>
    /// 高度差修正信息
    /// </summary>
    public class HeightCorrectionInfo
    {
        /// <summary>使能：0=关闭, 1=开启</summary>
        public bool Enabled { get; set; }

        /// <summary>单位制：0=英制, 1=公制</summary>
        public int UnitType { get; set; }

        /// <summary>高度差（公制cm, 英制in）</summary>
        public double Height { get; set; }

        /// <summary>介质密度（公制kg/m³, 英制lb/ft³）</summary>
        public double Density { get; set; }

        /// <summary>重力加速度（公制m/s², 英制ft/s²）</summary>
        public double Gravity { get; set; }

        /// <summary>温度（℃）</summary>
        public double Temperature { get; set; }

        public override string ToString() => $"Enabled={Enabled}, UnitType={UnitType}, Height={Height}, Density={Density}, Gravity={Gravity}, Temp={Temperature}";
    }

    /// <summary>
    /// 去皮信息
    /// </summary>
    public class TareInfo
    {
        /// <summary>使能：0=关闭, 1=开启</summary>
        public bool Enabled { get; set; }

        /// <summary>去皮值</summary>
        public double Value { get; set; }

        public override string ToString() => $"Enabled={Enabled}, Value={Value}";
    }

    /// <summary>
    /// 压力开关动作值
    /// </summary>
    public class SwitchValueInfo
    {
        /// <summary>关闭值</summary>
        public double CloseValue { get; set; }

        /// <summary>关闭值单位</summary>
        public string CloseUnit { get; set; } = string.Empty;

        /// <summary>打开值</summary>
        public double OpenValue { get; set; }

        /// <summary>打开值单位</summary>
        public string OpenUnit { get; set; } = string.Empty;

        public override string ToString() => $"Close={CloseValue} {CloseUnit}, Open={OpenValue} {OpenUnit}";
    }

    /// <summary>
    /// 扩展接口状态
    /// </summary>
    public class ExtendedInterfaceState
    {
        /// <summary>CPS状态</summary>
        public bool Cps { get; set; }

        /// <summary>DRV1状态</summary>
        public bool Drv1 { get; set; }

        /// <summary>DRV2状态</summary>
        public bool Drv2 { get; set; }

        /// <summary>DO1状态</summary>
        public bool Do1 { get; set; }

        /// <summary>DO2状态</summary>
        public bool Do2 { get; set; }

        /// <summary>DO3状态</summary>
        public bool Do3 { get; set; }

        /// <summary>DC24状态</summary>
        public bool Dc24 { get; set; }

        /// <summary>Switch状态</summary>
        public bool Switch { get; set; }

        public override string ToString() => $"CPS={Cps}, DRV1={Drv1}, DRV2={Drv2}, DO1={Do1}, DO2={Do2}, DO3={Do3}, DC24={Dc24}, Switch={Switch}";
    }

    /// <summary>
    /// 扩展接口输出模式信息
    /// </summary>
    public class ExtendedInterfaceModeInfo
    {
        /// <summary>当前输出模式</summary>
        public int CurrentMode { get; set; }

        /// <summary>可用输出模式列表</summary>
        public int[] AvailableModes { get; set; } = Array.Empty<int>();

        public override string ToString() => $"Current={CurrentMode}, Available=[{string.Join(",", AvailableModes)}]";
    }

    /// <summary>
    /// RS232 串口参数
    /// </summary>
    public class Rs232Info
    {
        /// <summary>波特率</summary>
        public int BaudRate { get; set; }

        /// <summary>数据位</summary>
        public int DataBits { get; set; }

        /// <summary>停止位</summary>
        public string StopBits { get; set; } = string.Empty;

        /// <summary>校验位</summary>
        public string Parity { get; set; } = string.Empty;

        public override string ToString() => $"{BaudRate},{DataBits},{StopBits},{Parity}";
    }

    /// <summary>
    /// 默认大气压值
    /// </summary>
    public class AtmosphericPressure
    {
        /// <summary>压力值</summary>
        public double Value { get; set; }

        /// <summary>单位</summary>
        public string Unit { get; set; } = string.Empty;

        public override string ToString() => $"{Value} {Unit}";
    }

    /// <summary>
    /// 量程模式
    /// </summary>
    public enum RangeMode
    {
        /// <summary>手动量程</summary>
        Manual = 0,

        /// <summary>自动量程</summary>
        Auto = 1
    }

    /// <summary>
    /// 控制状态
    /// </summary>
    public enum ControlState
    {
        /// <summary>排空</summary>
        VENT = 0,

        /// <summary>测量</summary>
        MEASURE = 1,

        /// <summary>控制</summary>
        CONTROL = 2
    }

    /// <summary>
    /// 控制模式
    /// </summary>
    public enum ControlModeType
    {
        /// <summary>快速</summary>
        Fast = 0,

        /// <summary>标准</summary>
        Standard = 1,

        /// <summary>自定义</summary>
        Custom = 2
    }

    /// <summary>
    /// 压力开关类型
    /// </summary>
    public enum SwitchType
    {
        /// <summary>机械开关</summary>
        Mechanical = 0,

        /// <summary>NPN</summary>
        NPN = 1,

        /// <summary>PNP</summary>
        PNP = 2
    }
}
