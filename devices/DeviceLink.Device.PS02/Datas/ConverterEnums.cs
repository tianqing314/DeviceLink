using System;

namespace DeviceLink.Device.PS02;

/// <summary>
/// 转接板错误码
/// </summary>
public enum ConverterErrorCode : byte
{
    /// <summary>
    /// 无错误
    /// </summary>
    None = 0x00,
    /// <summary>
    /// CRC 校验错误
    /// </summary>
    CrcError = 100,
    /// <summary>
    /// 无此指令
    /// </summary>
    UnknownCommand = 101,
    /// <summary>
    /// 当前状态不支持此操作
    /// </summary>
    NotSupported = 102,
    /// <summary>
    /// 密码错误
    /// </summary>
    PasswordError = 103,
    /// <summary>
    /// 参数格式错误
    /// </summary>
    FormatError = 104,
    /// <summary>
    /// 参数超范围
    /// </summary>
    OutOfRange = 105,
    /// <summary>
    /// 执行错误
    /// </summary>
    ExecutionError = 106,
    /// <summary>
    /// 参数错误
    /// </summary>
    ParameterError = 107
}

/// <summary>
/// 扫描从设备返回的接口类型
/// </summary>
public enum DeviceInterfaceType : byte
{
    /// <summary>
    /// 未连接设备
    /// </summary>
    NotConnected = 0,
    /// <summary>
    /// OWI 电流接口
    /// </summary>
    OwiCurrent = 1,
    /// <summary>
    /// OWI 电压接口
    /// </summary>
    OwiVoltage = 2,
    /// <summary>
    /// 485 接口
    /// </summary>
    Rs485 = 3
}

/// <summary>
/// 输出项目代号
/// </summary>
public enum OutputProject : byte
{
    /// <summary>
    /// 关闭所有档位
    /// </summary>
    Off = 0,
    /// <summary>
    /// MaOut（电流输出）
    /// </summary>
    MaOut = 1,
    /// <summary>
    /// VOut（电压输出）
    /// </summary>
    VOut = 2
}

/// <summary>
/// 输出值类型
/// </summary>
public enum OutputValueType : byte
{
    /// <summary>
    /// 输出零点
    /// </summary>
    Zero = 0,
    /// <summary>
    /// 输出满量程
    /// </summary>
    FullScale = 1
}

/// <summary>
/// 测量设备类别
/// </summary>
public enum MeasurementDeviceCategory : byte
{
    /// <summary>
    /// 测量OWI模块输出
    /// </summary>
    OwiModule = 0,
    /// <summary>
    /// 测量标准板输出
    /// </summary>
    StandardBoard = 1
}

/// <summary>
/// 测量项目代号
/// </summary>
public enum MeasurementProject : byte
{
    /// <summary>
    /// 关闭所有档位
    /// </summary>
    Off = 0,
    /// <summary>
    /// 电流测量 (Ameas)
    /// </summary>
    Current = 1,
    /// <summary>
    /// 电压测量 (Vmeas)
    /// </summary>
    Voltage = 2
}

/// <summary>
/// 校准项目代号
/// </summary>
public enum CalibrationProject : byte
{
    /// <summary>
    /// 电流校准
    /// </summary>
    Current = 0x01,
    /// <summary>
    /// 电压校准
    /// </summary>
    Voltage = 0x02
}

/// <summary>
/// 模块电源控制
/// </summary>
public enum ModulePowerState : byte
{
    /// <summary>
    /// 关闭压力模块供电
    /// </summary>
    Off = 0,
    /// <summary>
    /// 开启压力模块供电
    /// </summary>
    On = 1
}

/// <summary>
/// 扫描控制命令
/// </summary>
public enum ScanCommand : byte
{
    /// <summary>
    /// 开始扫描
    /// </summary>
    Start = 0,
    /// <summary>
    /// 停止扫描
    /// </summary>
    Stop = 1
}

/// <summary>
/// 测量结果（标准板卡功能码 0x0211 返回 2 字节）
/// </summary>
public class MeasurementResult
{
    /// <summary>
    /// 测量项目代号
    /// </summary>
    public MeasurementProject Project { get; set; }
    /// <summary>
    /// 输出值类型：0=输出零点，1=输出满量程
    /// </summary>
    public OutputValueType ValueType { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"项目:{Project}, 输出值类型:{ValueType}";
    }
}

/// <summary>
/// 转发板卡测量结果（功能码 0x0211 返回 9 字节）
/// </summary>
public class ConverterMeasurementResult
{
    /// <summary>
    /// 测量项目代号
    /// </summary>
    public MeasurementProject Project { get; set; }
    /// <summary>
    /// 测量原始值（float32，小端）
    /// </summary>
    public float RawValue { get; set; }
    /// <summary>
    /// 测量最终值（float32，小端）
    /// </summary>
    public float FinalValue { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"项目:{Project}, 原始值:{RawValue:F6}, 最终值:{FinalValue:F6}";
    }
}

/// <summary>
/// 标准板卡校准数据（功能码 0x0280/0x0281）
/// 685校准用
/// </summary>
public class StandardBoardCalibrationData
{
    /// <summary>
    /// 685 SN 号（16字节 ASCII）
    /// </summary>
    public string ConST685Sn { get; set; } = string.Empty;

    /// <summary>
    /// 685 校准日期
    /// </summary>
    public DateTime ConST685CalibrationDate { get; set; }

    /// <summary>
    /// 实际值列表 - 电压（2个 float32，小端，共8字节）
    /// 索引0: 零位电压，索引1: 满度电压
    /// </summary>
    public float[] ActualVoltageValues { get; set; } = new float[2];

    /// <summary>
    /// 实际值列表 - 电流（2个 float32，小端，共8字节）
    /// 索引0: 零位电流，索引1: 满度电流
    /// </summary>
    public float[] ActualCurrentValues { get; set; } = new float[2];

    /// <summary>
    /// 校准日期
    /// </summary>
    public DateTime CalibrationDate { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"685SN:{ConST685Sn}, 685校准日期:{ConST685CalibrationDate:yyyy-MM-dd}, " +
               $"校准日期:{CalibrationDate:yyyy-MM-dd}";
    }
}

/// <summary>
/// 校准数据（转发板卡功能码 0x0280/0x0281）
/// 标准板卡校准用
/// </summary>
public class CalibrationData
{
    /// <summary>
    /// 基准板 SN 号（16字节 ASCII）
    /// </summary>
    public string StandardBoardSn { get; set; } = string.Empty;

    /// <summary>
    /// 基准板校准日期
    /// </summary>
    public DateTime StandardBoardCalibrationDate { get; set; }

    /// <summary>
    /// 基准板校准值列表 - 电压（2个 float32，小端，共8字节）
    /// 索引0: 零位电压，索引1: 满度电压
    /// </summary>
    public float[] StandardVoltageValues { get; set; } = new float[2];

    /// <summary>
    /// 基准板校准值列表 - 电流（2个 float32，小端，共8字节）
    /// 索引0: 零位电流，索引1: 满度电流
    /// </summary>
    public float[] StandardCurrentValues { get; set; } = new float[2];

    /// <summary>
    /// 校准日期
    /// </summary>
    public DateTime CalibrationDate { get; set; }

    /// <summary>
    /// 实际值列表 - 电压（2个 float32，小端，共8字节）
    /// 索引0: 零位电压，索引1: 满度电压
    /// </summary>
    public float[] ActualVoltageValues { get; set; } = new float[2];

    /// <summary>
    /// 实际值列表 - 电流（2个 float32，小端，共8字节）
    /// 索引0: 零位电流，索引1: 满度电流
    /// </summary>
    public float[] ActualCurrentValues { get; set; } = new float[2];

    /// <summary>
    /// 电压校准系数 K 值
    /// </summary>
    public float VoltageK { get; set; }

    /// <summary>
    /// 电压校准系数 B 值
    /// </summary>
    public float VoltageB { get; set; }

    /// <summary>
    /// 电流校准系数 K 值
    /// </summary>
    public float CurrentK { get; set; }

    /// <summary>
    /// 电流校准系数 B 值
    /// </summary>
    public float CurrentB { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"标准板SN:{StandardBoardSn}, 校准日期:{CalibrationDate:yyyy-MM-dd}, " +
               $"电压K:{VoltageK:F6},B:{VoltageB:F6}, 电流K:{CurrentK:F6},B:{CurrentB:F6}";
    }
}
