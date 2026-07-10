using DeviceLink.DeviceBase;
using DeviceLink.Device.DPSEX.Datas;
using DeviceLink.Protocol;
using DeviceLink.Session;
using System.Globalization;
using System.IO.Ports;
using System.Net;
using System.Text.RegularExpressions;

namespace DeviceLink.Device.DPSEX;

/// <summary>
/// DPSEX 系列智能数字压力模块。
/// 
/// 支持 ConST 协议，通过串口/TCP/MQTT 任意通道连接。
/// 
/// 协议格式：地址:R/W:命令:参数...:\0
/// 例：255:R:MRMD:\0 → 读取测量数据
///     255:W:OAV:64:\0 → 写放大倍数 64
/// 
/// 使用示例：
///   var transport = new SerialPortTransport("COM3", 9600);
///   var frameStrategy = new DelimiterFrameStrategy(new byte[]{0});
///   var dataLink = new DirectDataLink(transport, frameStrategy);
///   var session = new DirectSession(dataLink);
///   var codec = new ConSTCodec(255);
///   var dpsex = new DPSEX(session, codec);
///   await dpsex.OpenAsync();
///   var pressure = await dpsex.GetPressureAsync();
/// </summary>
public class DPSEX : DeviceLink.DeviceBase.DeviceBase
{
    private readonly ConSTCodec _codec;

    #region 构造函数

    /// <summary>
    /// 构造函数（串口通讯方式使用）
    /// </summary>
    /// <param name="serialPortName">
    /// 串口号（如 COM3）
    /// </param>
    /// <param name="baudRate">
    /// 波特率
    /// </param>
    /// <param name="dataBits">
    /// 数据位
    /// </param>
    /// <param name="stopBits">
    /// 停止位
    /// </param>
    /// <param name="parity">
    /// 校验位
    /// </param>
    /// <param name="address">
    /// ConST 设备地址（默认 255）
    /// </param>
    public DPSEX(string serialPortName, int baudRate = 9600, int dataBits = 8,
        StopBits stopBits = StopBits.One, Parity parity = Parity.None, byte address = 255)
        : base(serialPortName, baudRate, dataBits, stopBits, parity, new ConSTCodec(address))
    {
        _codec = (ConSTCodec)Codec;
    }

    /// <summary>
    /// 构造函数（串口通讯方式使用，默认配置）
    /// </summary>
    /// <param name="serialPortName">
    /// 串口号（如 COM3）
    /// </param>
    /// <param name="address">
    /// ConST 设备地址（默认 255）
    /// </param>
    public DPSEX(string serialPortName, byte address = 255)
        : base(serialPortName, new ConSTCodec(address))
    {
        _codec = (ConSTCodec)Codec;
    }

    /// <summary>
    /// 构造函数（TCP 通讯方式使用）
    /// </summary>
    /// <param name="ipAddress">
    /// IP 地址
    /// </param>
    /// <param name="port">
    /// 端口号
    /// </param>
    /// <param name="address">
    /// ConST 设备地址（默认 255）
    /// </param>
    public DPSEX(IPAddress ipAddress, int port, byte address = 255)
        : base(ipAddress, port, new ConSTCodec(address))
    {
        _codec = (ConSTCodec)Codec;
    }

    /// <summary>
    /// 构造函数（通信设置实例方式适用）
    /// </summary>
    /// <param name="settings">
    /// 通信配置
    /// </param>
    /// <param name="address">
    /// ConST 设备地址（默认 255）
    /// </param>
    public DPSEX(DeviceCommSettings settings, byte address = 255)
        : base(settings, new ConSTCodec(address))
    {
        _codec = (ConSTCodec)Codec;
    }

    /// <summary>
    /// 构造函数（MQTT 通讯方式使用）
    /// </summary>
    /// <param name="brokerHost">
    /// MQTT Broker 地址
    /// </param>
    /// <param name="brokerPort">
    /// MQTT Broker 端口号
    /// </param>
    /// <param name="requestTopic">
    /// 请求主题（设备接收命令的主题）
    /// </param>
    /// <param name="responseTopic">
    /// 响应主题（设备发送响应的主题）
    /// </param>
    /// <param name="address">
    /// ConST 设备地址（默认 255）
    /// </param>
    /// <param name="requestTimeoutMs">
    /// 请求超时时间（毫秒，默认 5000）
    /// </param>
    public DPSEX(string brokerHost, int brokerPort, string requestTopic, string responseTopic,
        byte address = 255, int requestTimeoutMs = 5000)
        : base(new MqttSession(new MqttSessionOptions
        {
            BrokerHost = brokerHost,
            BrokerPort = brokerPort,
            RequestTopic = requestTopic,
            ResponseTopic = responseTopic,
            RequestTimeoutMs = requestTimeoutMs
        }), new ConSTCodec(address))
    {
        _codec = (ConSTCodec)Codec;
    }

    /// <summary>
    /// 配置构造函数默认信息
    /// </summary>
    protected override void ConstructDefaultInfo()
    {
        base.ConstructDefaultInfo();
        Name = "DPSEX";
    }

    #endregion 构造函数

    // ═══════════════════════════════════════════════════════════
    // 测量
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 读取无修正的原始测量数据（MRMN 指令）
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 原始测量数据数组
    /// </returns>
    public async Task<string[]> GetRawMeasurementAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("MRMN"),
            raw => _codec.ExtractFields(raw, 3),
            ct);
    }

    /// <summary>
    /// 读取设备内部温度（OTEMP 指令）
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 温度值
    /// </returns>
    public async Task<double> GetTemperatureAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("OTEMP"),
            raw =>
            {
                var text = _codec.ExtractField(raw, 3);
                // 去掉尾部 "℃" 等非数字字符
                text = text.TrimEnd('℃', '℉', ' ');
                return double.TryParse(text,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var v) ? v : double.NaN;
            },
            ct);
    }

    /// <summary>
    /// 读取传感器的激励电流/电压及 mV 输出（ORIV 指令）
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 传感器激励数据数组
    /// </returns>
    public async Task<string[]> GetSensorExcitationAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("ORIV"),
            raw => _codec.ExtractFields(raw, 3),
            ct);
    }

    // ═══════════════════════════════════════════════════════════
    // 设备信息（读）
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 获取软件版本号（OVER 指令）
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 版本号
    /// </returns>
    public async Task<string> GetVersionAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("OVER"),
            raw => _codec.ExtractField(raw, 3),
            ct);
    }

    /// <summary>
    /// 获取硬件版本号（OHVER 指令）
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 硬件版本号
    /// </returns>
    public async Task<string> GetHardwareVersionAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("OHVER"),
            raw => _codec.ExtractField(raw, 3),
            ct);
    }

    /// <summary>
    /// 获取设备序列号（OTYPE 指令）
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 序列号
    /// </returns>
    public async Task<string> GetSerialNumberAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("OTYPE"),
            raw => _codec.ExtractField(raw, 3),
            ct);
    }

    /// <summary>
    /// 读取生产日期（ODATE 指令）
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 生产日期
    /// </returns>
    public async Task<string> GetProductionDateAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("ODATE"),
            raw => _codec.ExtractField(raw, 3),
            ct);
    }

    /// <summary>
    /// 读取仪器编号（OCODE 指令）
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 仪器编号
    /// </returns>
    public async Task<string> GetInstrumentCodeAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("OCODE"),
            raw => _codec.ExtractField(raw, 3),
            ct);
    }

    /// <summary>
    /// 读取设备地址（OADD 指令）
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 设备地址
    /// </returns>
    public async Task<int> GetAddressAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("OADD"),
            raw =>
            {
                var text = _codec.ExtractField(raw, 3);
                return int.TryParse(text, out var v) ? v : -1;
            },
            ct);
    }

    /// <summary>
    /// 读取校准日期（ODCAL 指令）
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 校准日期
    /// </returns>
    public async Task<string> GetCalibrationDateAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("ODCAL"),
            raw => _codec.ExtractField(raw, 3),
            ct);
    }

    /// <summary>
    /// 读 TAG 标签（TAG 指令，参数为长度）
    /// </summary>
    /// <param name="length">
    /// 标签长度（默认48）
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// TAG标签
    /// </returns>
    public async Task<string> GetTagAsync(int length = 48, CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("TAG", length.ToString()),
            raw => _codec.ExtractField(raw, 3),
            ct);
    }

    /// <summary>
    /// 读取当前压力单位（OUNIT 指令，返回单位字符串）
    /// 对应 Xmas11 GetPressureUnit()
    /// </summary>
    /// <param name="ct">取消令牌</param>
    /// <returns>压力单位字符串</returns>
    public async Task<string> GetPressureUnitAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("OUNIT"),
            raw => _codec.ExtractField(raw, 3),
            ct);
    }

    /// <summary>
    /// 读温补/线性/校准标志状态（OSTAT 指令）
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 状态信息
    /// </returns>
    public async Task<string> GetStatusAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("OSTAT"),
            raw => _codec.ExtractField(raw, 3),
            ct);
    }

    /// <summary>
    /// 读工作模式（MWORK 指令），返回 PressureWorkMode 枚举
    /// 对应 Xmas11 GetPressureWorkMode()
    /// </summary>
    /// <param name="ct">取消令牌</param>
    /// <returns>工作模式枚举</returns>
    public async Task<PressureWorkMode> GetWorkModeEnumAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("MWORK"),
            raw =>
            {
                var text = _codec.ExtractField(raw, 3);
                return int.TryParse(text, out var v) ? (PressureWorkMode)v : PressureWorkMode.Normal;
            },
            ct);
    }

    /// <summary>
    /// 获取设备标识信息（IDN 指令）
    /// 对应 Xmas11 GetInfo()
    /// </summary>
    /// <param name="ct">取消令牌</param>
    /// <returns>设备标识信息</returns>
    public async Task<string> GetDeviceInfoAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("IDN"),
            raw => _codec.ExtractField(raw, 3),
            ct);
    }

    /// <summary>
    /// 获取精度等级信息（ONACCY 指令）
    /// 对应 Xmas11 GetAccuracyInfo()
    /// </summary>
    /// <param name="ct">取消令牌</param>
    /// <returns>精度等级信息</returns>
    public async Task<int> GetAccuracyInfoAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("ONACCY"),
            raw =>
            {
                var text = _codec.ExtractField(raw, 3);
                return int.TryParse(text, out var v) ? v : -1;
            },
            ct);
    }

    /// <summary>
    /// 读取带单位的压力值（MRMD 指令）
    /// 对应 Xmas11 GetPressure() with Pressure object
    /// </summary>
    /// <param name="ct">取消令牌</param>
    /// <returns>带单位的压力值</returns>
    public async Task<PressureValue> GetPressureWithUnitAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("MRMD"),
            raw =>
            {
                var fields = _codec.ExtractFields(raw, 3);
                var valueText = fields.Length > 0 ? fields[0] : string.Empty;
                var unitText = fields.Length > 1 ? fields[1] : string.Empty;
                var value = ParseNumericPart(valueText);
                var unit = ParseUnitPart(unitText);
                return new PressureValue { Value = value, Unit = unit };
            },
            ct);
    }

    /// <summary>
    /// 读取校准前数据（MRMC 指令）
    /// 对应 Xmas11 PCal_GetValidateBeforeData()
    /// </summary>
    /// <param name="ct">取消令牌</param>
    /// <returns>校准前数据</returns>
    public async Task<CalibrationData> GetCalibrationBeforeDataAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("MRMC"),
            raw =>
            {
                var fields = _codec.ExtractFields(raw, 3);
                var valueText = fields.Length > 0 ? fields[0] : string.Empty;
                var unitText = fields.Length > 1 ? fields[1] : string.Empty;
                var value = ParseNumericPart(valueText);
                var unit = ParseUnitPart(unitText);
                return new CalibrationData
                {
                    MeasureValue = value,
                    Unit = unit
                };
            },
            ct);
    }

    /// <summary>
    /// 读取校准状态（OCALI 指令）
    /// 对应 Xmas11 PCAL_GetState()
    /// </summary>
    /// <param name="ct">取消令牌</param>
    /// <returns>校准状态信息</returns>
    public async Task<CalibrationState> GetCalibrationStateAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("OCALI"),
            raw =>
            {
                var text = _codec.ExtractField(raw, 3);
                var parts = text.Split(',');
                if (parts.Length < 7)
                    return new CalibrationState();

                return new CalibrationState
                {
                    IsValid = true,
                    IsTemperatureCompensated = parts[0] == "1",
                    IsLinearized = parts[1] == "1",
                    IsCalibrationActive = parts[2] == "1",
                    IsFactoryCalibrated = parts[3] == "1",
                    IsUserCalibrated = parts[4] == "1",
                    CalibrationPointCount = int.TryParse(parts[5], out var count) ? count : 0
                };
            },
            ct);
    }

    /// <summary>
    /// 读取厂家校准日期（OFDCAL 指令）
    /// 对应 Xmas11 PCal_GetFactoryDate()
    /// </summary>
    /// <param name="ct">取消令牌</param>
    /// <returns>厂家校准日期</returns>
    public async Task<string> GetFactoryCalibrationDateAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("OFDCAL"),
            raw => _codec.ExtractField(raw, 3),
            ct);
    }

    /// <summary>
    /// 读取工作模式测试类型（MTYPE 指令）
    /// 对应 Xmas11 GetWorkModeTestType()
    /// </summary>
    /// <param name="ct">取消令牌</param>
    /// <returns>工作模式测试类型</returns>
    public async Task<WorkModeTestType> GetWorkModeTestTypeAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("MTYPE"),
            raw =>
            {
                var text = _codec.ExtractField(raw, 3);
                return int.TryParse(text, out var v) ? (WorkModeTestType)v : WorkModeTestType.Unknown;
            },
            ct);
    }

    /// <summary>
    /// 读取自诊断结果（SELACK 指令）
    /// 对应 Xmas11 GetSelfDiagnosis()
    /// </summary>
    /// <param name="ct">取消令牌</param>
    /// <returns>自诊断数据</returns>
    public async Task<SelfDiagnosisData> GetSelfDiagnosisAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("SELACK"),
            raw =>
            {
                var text = _codec.ExtractField(raw, 3);
                var data = new SelfDiagnosisData();
                var items = text.Split(',');
                foreach (var item in items)
                {
                    var parts = item.Split(' ');
                    if (parts.Length >= 3)
                    {
                        data.Items.Add(new SelfDiagnosisItem
                        {
                            Sort = int.TryParse(parts[0], out var sort) ? sort : 0,
                            FaultNo = int.TryParse(parts[1], out var faultNo) ? faultNo : 0,
                            MeasureValue = parts[2]
                        });
                    }
                }
                return data;
            },
            ct);
    }

    /// <summary>
    /// 读取危险压力记录（DANGP 指令）
    /// 对应 Xmas11 GetDanger()
    /// </summary>
    /// <param name="ct">取消令牌</param>
    /// <returns>危险压力记录</returns>
    public async Task<string> GetDangerRecordAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("DANGP"),
            raw => _codec.ExtractField(raw, 3),
            ct);
    }

    /// <summary>
    /// 读取过压标志（OVERP 指令）
    /// 对应 Xmas11 GetOverPressure()
    /// </summary>
    /// <param name="ct">取消令牌</param>
    /// <returns>过压标志</returns>
    public async Task<bool> GetOverPressureFlagAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("OVERP"),
            raw =>
            {
                var text = _codec.ExtractField(raw, 3);
                return int.TryParse(text, out var v) ? v == 1 : false;
            },
            ct);
    }

    /// <summary>
    /// 读取检定信息总条数（CALSUM 指令）
    /// 对应 Xmas11 GetVerificationTotalNumber()
    /// </summary>
    /// <param name="ct">取消令牌</param>
    /// <returns>检定信息总条数</returns>
    public async Task<int> GetVerificationTotalNumberAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("CALSUM"),
            raw =>
            {
                var text = _codec.ExtractField(raw, 3);
                return int.TryParse(text, out var v) ? v : 0;
            },
            ct);
    }

    /// <summary>
    /// 读取指定点的检定信息（CALINFO 指令）
    /// 对应 Xmas11 GetPointVerificationInfo()
    /// </summary>
    /// <param name="pointIndex">校准点索引</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>检定信息</returns>
    public async Task<VerificationData> GetPointVerificationInfoAsync(int pointIndex, CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("CALINFO", pointIndex.ToString()),
            raw =>
            {
                var text = _codec.ExtractField(raw, 3);
                var parts = text.Split(':');
                var data = new VerificationData();
                if (parts.Length >= 17)
                {
                    data.VerificatiTime = parts[0];
                    data.SensorName = parts[1];
                    data.SensorRange = parts[2];
                    data.SensorAccuracy = parts[3];
                    data.IndicationMaxErrorBefore = parts[4];
                    data.HysterisisMaxErrorBefore = parts[5];
                    data.IndicationMaxErrorAfter = parts[6];
                    data.HysterisisMaxErrorAfter = parts[7];
                    data.TMP117 = double.TryParse(parts[8], NumberStyles.Any, CultureInfo.InvariantCulture, out var tmp) ? tmp : double.NaN;
                    data.MCU = double.TryParse(parts[9], NumberStyles.Any, CultureInfo.InvariantCulture, out var mcu) ? mcu : double.NaN;
                    data.FirstStr = parts[10];
                    data.SecondStr = parts[11];
                    data.ThirdStr = parts[12];
                    data.FourthStr = parts[13];
                    data.FifthStr = parts[14];
                    data.FirstEffectValue = double.TryParse(parts[15], NumberStyles.Any, CultureInfo.InvariantCulture, out var eff1) ? eff1 : double.NaN;
                    data.SecondEffectValue = double.TryParse(parts[16], NumberStyles.Any, CultureInfo.InvariantCulture, out var eff2) ? eff2 : double.NaN;
                    // 解析校准点数据
                    ParseCalibrationPoint(data.FirstStr, out var std1, out var cancel1, out _);
                    data.FirstStdValue = std1;
                    data.FirstCancelValue = cancel1;
                    ParseCalibrationPoint(data.SecondStr, out var std2, out var cancel2, out _);
                    data.SecondStdValue = std2;
                    data.SecondCancelValue = cancel2;
                    ParseCalibrationPoint(data.ThirdStr, out var std3, out var cancel3, out _);
                    data.ThirdStdValue = std3;
                    data.ThirdCancelValue = cancel3;
                    ParseCalibrationPoint(data.FourthStr, out var std4, out var cancel4, out _);
                    data.FourthStdValue = std4;
                    data.FourthCancelValue = cancel4;
                    ParseCalibrationPoint(data.FifthStr, out var std5, out var cancel5, out _);
                    data.FifthStdValue = std5;
                    data.FifthCancelValue = cancel5;
                }
                return data;
            },
            ct);
    }

    /// <summary>
    /// 读取实时时钟（ORTC 指令）
    /// 对应 Xmas11 GetRTC()
    /// </summary>
    /// <param name="ct">取消令牌</param>
    /// <returns>实时时钟数据</returns>
    public async Task<RTCData> GetRTCAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("ORTC"),
            raw =>
            {
                var fields = _codec.ExtractFields(raw, 3);
                return new RTCData
                {
                    Date = fields.Length > 0 ? fields[0] : string.Empty,
                    Time = fields.Length > 1 ? fields[1] : string.Empty
                };
            },
            ct);
    }

    /// <summary>
    /// 读取频率信息（OFREQ 指令）
    /// 对应 Xmas11 GetFrequency()
    /// </summary>
    /// <param name="ct">取消令牌</param>
    /// <returns>频率数据</returns>
    public async Task<FrequencyData> GetFrequencyAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("OFREQ"),
            raw =>
            {
                var text = _codec.ExtractField(raw, 3);
                var parts = text.Split(',');
                return new FrequencyData
                {
                    Frequency1 = parts.Length > 0 && double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var f1) ? f1 : double.NaN,
                    Frequency2 = parts.Length > 1 && double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var f2) ? f2 : double.NaN
                };
            },
            ct);
    }

    /// <summary>
    /// 读取执行器板信息（OACT 指令）
    /// 对应 Xmas11 GetActuatorBoard()
    /// </summary>
    /// <param name="ct">取消令牌</param>
    /// <returns>执行器板数据</returns>
    public async Task<ActuatorBoardData> GetActuatorBoardAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("OACT"),
            raw =>
            {
                var text = _codec.ExtractField(raw, 3);
                var parts = text.Split(',');
                var data = new ActuatorBoardData();
                if (parts.Length >= 4)
                {
                    if (double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var pVal))
                        data.PressureValue = pVal;
                    data.PressureUnit = parts[1];
                    if (double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var tVal))
                        data.TemperatureValue = tVal;
                    data.TemperatureUnit = parts[3];
                }
                return data;
            },
            ct);
    }

    /// <summary>
    /// 读取详细的量程信息（ORAN 指令，7个字段）
    /// 对应 Xmas11 GetPressureRangeDetailedInfo()
    /// </summary>
    /// <param name="ct">取消令牌</param>
    /// <returns>详细量程信息</returns>
    public async Task<PressureRangeInfo> GetPressureRangeDetailedInfoAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("ORAN"),
            raw =>
            {
                var fields = _codec.ExtractFields(raw, 3);
                var info = new PressureRangeInfo();
                if (fields.Length >= 7)
                {
                    info.Low = double.TryParse(fields[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var low) ? low : 0;
                    info.High = double.TryParse(fields[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var high) ? high : 0;
                    info.Unit = fields[2];
                    info.PressureType = fields[3];
                    info.AccuracyIndex = int.TryParse(fields[4], out var index) ? index : 0;
                    info.AccuracyPercent = double.TryParse(fields[5], NumberStyles.Any, CultureInfo.InvariantCulture, out var percent) ? percent : 0;
                }
                return info;
            },
            ct);
    }

    /// <summary>
    /// 读取压力数据输出速度（SPEED 指令，返回 OutputSpeed 枚举）
    /// 对应 Xmas11 GetSPEED()
    /// </summary>
    /// <param name="ct">取消令牌</param>
    /// <returns>输出速度</returns>
    public async Task<OutputSpeed> GetSpeedEnumAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("SPEED"),
            raw =>
            {
                var text = _codec.ExtractField(raw, 3);
                return int.TryParse(text, out var v) ? (OutputSpeed)v : OutputSpeed.Low;
            },
            ct);
    }

    // ═══════════════════════════════════════════════════════════
    // 设备信息（写）
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 写生产日期（ODATE 指令）
    /// </summary>
    /// <param name="date">
    /// 日期
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task SetProductionDateAsync(string date, CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("ODATE", date), ct);
    }

    /// <summary>
    /// 写仪器编号（OCODE 指令）
    /// </summary>
    /// <param name="code">
    /// 编号
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task SetInstrumentCodeAsync(string code, CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("OCODE", code), ct);
    }

    /// <summary>
    /// 写设备地址（OADD 指令，1~255）
    /// </summary>
    /// <param name="newAddress">
    /// 新地址
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task SetAddressAsync(byte newAddress, CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("OADD", newAddress.ToString()), ct);
    }

    /// <summary>
    /// 写校准日期（ODCAL 指令）
    /// </summary>
    /// <param name="date">
    /// 日期
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task SetCalibrationDateAsync(string date, CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("ODCAL", date), ct);
    }

    /// <summary>
    /// 写 TAG 标签（TAG 指令）
    /// </summary>
    /// <param name="tag">
    /// 标签
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task SetTagAsync(string tag, CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("TAG", tag.Length.ToString(), tag), ct);
    }

    // ═══════════════════════════════════════════════════════════
    // 配置
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 压力零点校准（OZERO 指令）
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task PressureZeroAsync(CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("OZERO"), ct);
    }

    /// <summary>
    /// 写放大倍数（OAV 指令，参数 1/2/4/8/16/32/64/128）
    /// </summary>
    /// <param name="value">
    /// 放大倍数
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task SetAmplificationAsync(int value, CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("OAV", value.ToString()), ct);
    }

    /// <summary>
    /// 读放大倍数（OAV 指令）
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 放大倍数
    /// </returns>
    public async Task<int> GetAmplificationAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("OAV"),
            raw =>
            {
                var text = _codec.ExtractField(raw, 3);
                return int.TryParse(text, out var v) ? v : -1;
            },
            ct);
    }

    /// <summary>
    /// 写恒流/恒压值（OIS 指令，1-9=恒流，30=恒压）
    /// </summary>
    /// <param name="value">
    /// 恒流/恒压值
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task SetConstantCurrentAsync(int value, CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("OIS", value.ToString()), ct);
    }

    /// <summary>
    /// 读恒流/恒压值（OIS 指令）
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 恒流/恒压值
    /// </returns>
    public async Task<int> GetConstantCurrentAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("OIS"),
            raw =>
            {
                var text = _codec.ExtractField(raw, 3);
                return int.TryParse(text, out var v) ? v : -1;
            },
            ct);
    }

    /// <summary>
    /// 写工作模式（MWORK 指令，使用 PressureWorkMode 枚举）
    /// 对应 Xmas11 SetPressureWorkMode()
    /// </summary>
    /// <param name="mode">工作模式</param>
    /// <param name="ct">取消令牌</param>
    public async Task SetWorkModeEnumAsync(PressureWorkMode mode, CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("MWORK", ((int)mode).ToString()), ct);
    }

    /// <summary>
    /// 写压力单位（OUNIT 指令，1~12 对应不同单位）
    /// </summary>
    /// <param name="unitCode">
    /// 单位代码
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task SetUnitAsync(int unitCode, CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("OUNIT", unitCode.ToString()), ct);
    }

    /// <summary>
    /// 写波特率（OBAUQ 指令：1200/2400/4800/9600/19200/38400）
    /// </summary>
    /// <param name="baudRate">
    /// 波特率
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task SetBaudRateAsync(int baudRate, CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("OBAUQ", baudRate.ToString()), ct);
    }

    /// <summary>
    /// 更新波特率（OBAUDR 指令）
    /// 对应 Xmas11 UpdateBaudRate()
    /// </summary>
    /// <param name="baudRate">波特率（1200/2400/4800/9600/19200/38400）</param>
    /// <param name="ct">取消令牌</param>
    public async Task UpdateBaudRateAsync(int baudRate, CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("OBAUDR", baudRate.ToString()), ct);
    }

    /// <summary>
    /// 写设备序列号（OTYPE 指令）
    /// 对应 Xmas11 SetSerialNumber()
    /// </summary>
    /// <param name="serialNumber">序列号</param>
    /// <param name="ct">取消令牌</param>
    public async Task SetSerialNumberAsync(string serialNumber, CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("OTYPE", serialNumber), ct);
    }

    /// <summary>
    /// 写精度等级信息（ONACCY 指令）
    /// 对应 Xmas11 SetAccuracyInfo()
    /// </summary>
    /// <param name="accuracyInfo">精度等级信息</param>
    /// <param name="ct">取消令牌</param>
    public async Task SetAccuracyInfoAsync(int accuracyInfo, CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("ONACCY", accuracyInfo.ToString()), ct);
    }

    /// <summary>
    /// 写工作模式测试类型（MTYPE 指令）
    /// 对应 Xmas11 SetWorkModeTestType()
    /// </summary>
    /// <param name="type">工作模式测试类型</param>
    /// <param name="ct">取消令牌</param>
    public async Task SetWorkModeTestTypeAsync(WorkModeTestType type, CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("MTYPE", ((int)type).ToString()), ct);
    }

    /// <summary>
    /// 写压力数据输出速度（SPEED 指令，使用 OutputSpeed 枚举）
    /// 对应 Xmas11 SetSPEED()
    /// </summary>
    /// <param name="speed">输出速度</param>
    /// <param name="ct">取消令牌</param>
    public async Task SetSpeedEnumAsync(OutputSpeed speed, CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("SPEED", ((int)speed).ToString()), ct);
    }

    /// <summary>
    /// 写传感器类型（OSENS 指令，G=表压, A=绝压, D=差压）
    /// </summary>
    /// <param name="type">
    /// 传感器类型
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task SetSensorTypeAsync(string type, CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("OSENS", type), ct);
    }

    /// <summary>
    /// 恢复出厂设置（OFALT 指令）
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task RestoreFactoryAsync(CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("OFALT"), ct);
    }

    /// <summary>
    /// 仪表软复位（ORPP 指令）
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task SoftResetAsync(CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("ORPP"), ct);
    }

    /// <summary>
    /// 设置连续输出模式（OCONT 指令，0=关闭, 1/2/3=不同上传格式）
    /// </summary>
    /// <param name="mode">
    /// 模式
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task SetContinuousOutputAsync(int mode, CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("OCONT", mode.ToString()), ct);
    }

    /// <summary>
    /// 写测量速率（MRATE 指令，7/15/30/60/120/240 次/分钟）
    /// </summary>
    /// <param name="rate">
    /// 速率
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task SetMeasurementRateAsync(int rate, CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("MRATE", rate.ToString()), ct);
    }

    // ═══════════════════════════════════════════════════════════
    // 压力清零 / 绝压校准
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 取消清零（MZERO 指令）
    /// 对应 Xmas11 CanclePressureZero()
    /// </summary>
    public async Task CancelPressureZeroAsync(CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("MZERO"), ct);
    }

    /// <summary>
    /// 绝压清零（ABSZ 指令），也可用于对当前标准压力校准
    /// 对应 Xmas11 AbsolutePressureZero()
    /// </summary>
    /// <param name="pressureValue">压力值</param>
    /// <param name="unit">单位（默认 kPa）</param>
    public async Task AbsolutePressureZeroAsync(double pressureValue, string unit = "KPA", CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("ABSZ", $"{pressureValue}:{unit}"), ct);
    }

    /// <summary>
    /// 设置单点校准日期（ABSZD 指令）
    /// 对应 Xmas11 SetSingleCalibrationDate()
    /// </summary>
    public async Task SetSingleCalibrationDateAsync(DateTime time, CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("ABSZD", $"{time.Year}-{time.Month}-{time.Day}"), ct);
    }

    // ═══════════════════════════════════════════════════════════
    // 压力校准
    // ═══════════════════════════════════════════════════════════

    #region 压力校准

    /// <summary>
    /// 压力校准——开始校准（OCFS 指令）
    /// 对应 Xmas11 PCal_Start()
    /// </summary>
    public async Task StartCalibrationAsync(CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("OCFS"), ct);
    }

    /// <summary>
    /// 压力校准——执行校准（OCF 指令）
    /// 对应 Xmas11 PCal_Cal()
    /// </summary>
    /// <param name="item">校准项（Z=零点, M=中间, F=满度）</param>
    /// <param name="pressureValue">压力值</param>
    public async Task ExecuteCalibrationAsync(PressureCalItem item, double pressureValue, CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("OCF", ((int)item).ToString(), pressureValue.ToString(CultureInfo.InvariantCulture)), ct);
    }

    /// <summary>
    /// 压力校准——结束校准（OCFOK 指令）
    /// 对应 Xmas11 PCal_Stop()
    /// </summary>
    /// <param name="isSave">是否保存结果</param>
    public async Task StopCalibrationAsync(bool isSave, CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("OCFOK", isSave ? "1" : "0"), ct);
    }

    /// <summary>
    /// 压力校准——安全保存并结束校准
    /// 对应 Xmas11 PCal_SafeStopAndSave()
    /// </summary>
    public async Task SafeStopAndSaveCalibrationAsync(CancellationToken ct = default)
    {
        await StopCalibrationAsync(true, ct);
        await StartCalibrationAsync(ct);
        await StopCalibrationAsync(false, ct);
        await Task.Delay(1000, ct);
    }

    /// <summary>
    /// 压力校准——取消厂家校准（OCFCL 指令）
    /// 对应 Xmas11 PCal_CancelCal()
    /// </summary>
    public async Task CancelCalibrationAsync(CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("OCFCL", "1"), ct);
    }

    /// <summary>
    /// 压力校准——校准下限压力
    /// 对应 Xmas11 PCal_CalLower()
    /// </summary>
    public async Task CalibrateLowerAsync(double pressureValue, CancellationToken ct = default)
    {
        await StopCalibrationAsync(false, ct);
        await Task.Delay(300, ct);
        await StartCalibrationAsync(ct);
        await Task.Delay(300, ct);
        await ExecuteCalibrationAsync(PressureCalItem.Z, pressureValue, ct);
        await Task.Delay(300, ct);
        await SafeStopAndSaveCalibrationAsync(ct);
    }

    /// <summary>
    /// 压力校准——校准中间点
    /// 对应 Xmas11 PCal_CalMiddle()
    /// </summary>
    public async Task CalibrateMiddleAsync(double pressureValue, CancellationToken ct = default)
    {
        await StopCalibrationAsync(false, ct);
        await Task.Delay(300, ct);
        await StartCalibrationAsync(ct);
        await Task.Delay(300, ct);
        await ExecuteCalibrationAsync(PressureCalItem.M, pressureValue, ct);
        await Task.Delay(300, ct);
        await SafeStopAndSaveCalibrationAsync(ct);
    }

    /// <summary>
    /// 压力校准——校准上限压力（满度）
    /// 对应 Xmas11 PCal_CalUpper()
    /// </summary>
    public async Task CalibrateUpperAsync(double pressureValue, CancellationToken ct = default)
    {
        await StopCalibrationAsync(false, ct);
        await Task.Delay(300, ct);
        await StartCalibrationAsync(ct);
        await Task.Delay(300, ct);
        await ExecuteCalibrationAsync(PressureCalItem.F, pressureValue, ct);
        await Task.Delay(300, ct);
        await SafeStopAndSaveCalibrationAsync(ct);
    }

    #endregion 压力校准

    // ═══════════════════════════════════════════════════════════
    // 过压数据 / 自诊断
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 清除危险压力记录 / 过压记录（DANGP 指令）
    /// 对应 Xmas11 ClearOverpressureData() / SetDanger()
    /// </summary>
    public async Task ClearOverpressureDataAsync(CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("DANGP"), ct);
    }

    /// <summary>
    /// 主机启动/退出自诊断模式（OSCK 指令）
    /// 对应 Xmas11 SetPattern()
    /// </summary>
    /// <param name="enable">true=启动, false=退出</param>
    public async Task SetPatternAsync(bool enable, CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("OSCK", enable ? "1" : "0"), ct);
    }

    /// <summary>
    /// 启动自诊断并获取结果（OSCK 指令，参数 2）
    /// 对应 Xmas11 SetPatternGetResult()
    /// </summary>
    /// <param name="ct">取消令牌</param>
    /// <returns>自诊断结果</returns>
    public async Task<SelfDiagnosisData> SetPatternGetResultAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Write("OSCK", "2"),
            raw =>
            {
                var text = _codec.ExtractField(raw, 3);
                var data = new SelfDiagnosisData();
                var items = text.Split(',');
                foreach (var item in items)
                {
                    var parts = item.Split(' ');
                    if (parts.Length >= 3)
                    {
                        data.Items.Add(new SelfDiagnosisItem
                        {
                            Sort = int.TryParse(parts[0], out var sort) ? sort : 0,
                            FaultNo = int.TryParse(parts[1], out var faultNo) ? faultNo : 0,
                            MeasureValue = parts[2]
                        });
                    }
                }
                return data;
            },
            ct);
    }

    // ═══════════════════════════════════════════════════════════
    // 检定信息存储
    // ═══════════════════════════════════════════════════════════

    #region 检定信息

    /// <summary>
    /// 开始存储检定信息（OCALS 指令）
    /// 对应 Xmas11 StartStore()
    /// </summary>
    public async Task StartStoreAsync(CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("OCALS"), ct);
    }

    /// <summary>
    /// 更改第几条检定信息（OCALDOT 指令）
    /// 对应 Xmas11 SetItemNumber()
    /// </summary>
    public async Task SetItemNumberAsync(int number, CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("OCALDOT", number.ToString()), ct);
    }

    /// <summary>
    /// 写入检定时间和标准器信息（OCALIN 指令）
    /// 对应 Xmas11 SetStandardInfo()
    /// </summary>
    public async Task SetStandardInfoAsync(VerificationData item, CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("OCALIN",
            $"{item.VerificatiTime}:{item.SensorName}:{item.SensorRange}:{item.SensorAccuracy}"), ct);
    }

    /// <summary>
    /// 写入校准前后被检精度（OCALAC 指令）
    /// 对应 Xmas11 SetCheckedPrecision()
    /// </summary>
    public async Task SetCheckedPrecisionAsync(VerificationData item, CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("OCALAC",
            $"{item.IndicationMaxErrorBefore}:{item.HysterisisMaxErrorBefore}:{item.IndicationMaxErrorAfter}:{item.HysterisisMaxErrorAfter}"), ct);
    }

    /// <summary>
    /// 写入校准数据（OCALDA 指令，5个校准点）
    /// 对应 Xmas11 SetCheckedData()
    /// </summary>
    public async Task SetCheckedDataAsync(VerificationData item, CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("OCALDA",
            $"{item.FirstStr}:{item.SecondStr}:{item.ThirdStr}:{item.FourthStr}:{item.FifthStr}"), ct);
    }

    /// <summary>
    /// 检定信息存储完毕（OCAKOK 指令）
    /// 对应 Xmas11 SaveVerification()
    /// </summary>
    public async Task SaveVerificationAsync(CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("OCAKOK"), ct);
    }

    /// <summary>
    /// 删除所有检定信息（ODELA 指令）
    /// 对应 Xmas11 DeleteVerification()
    /// </summary>
    public async Task DeleteVerificationAsync(CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("ODELA", "1"), ct);
    }

    /// <summary>
    /// 擦除铁电指令（ERAS 指令，密码 211273）
    /// 对应 Xmas11 WipeCommand()
    /// </summary>
    public async Task WipeCommandAsync(CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("ERAS", "211273"), ct);
    }

    /// <summary>
    /// 擦除外扩 EEPROM 指令（ERASEXT 指令，密码 211273）
    /// 对应 Xmas11 WipeExtCommand()
    /// </summary>
    public async Task WipeExtCommandAsync(CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.Write("ERASEXT", "211273"), ct);
    }

    #endregion 检定信息

    // ═══════════════════════════════════════════════════════════
    // 辅助方法
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 解析字符串中的数字部分（如 "-15.0000inH2O" → -15.0000）
    /// </summary>
    private static double ParseNumericPart(string input)
    {
        if (string.IsNullOrEmpty(input))
            return double.NaN;

        var match = Regex.Match(input, @"[-+]?[0-9]*\.?[0-9]+");
        if (match.Success && double.TryParse(match.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
            return v;
        return double.NaN;
    }

    /// <summary>
    /// 解析字符串中的单位部分（如 "-15.0000inH2O" → "inH2O"）
    /// </summary>
    private static string ParseUnitPart(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var match = Regex.Match(input, @"[a-zA-Z°℃℉%]+[a-zA-Z0-9°℃℉%]*$");
        return match.Success ? match.Value : string.Empty;
    }

    /// <summary>
    /// 解析校准点数据（格式："标准值,未校准值,上次校准后值"）
    /// </summary>
    private static void ParseCalibrationPoint(string data, out double stdValue, out double cancelValue, out double effectValue)
    {
        stdValue = double.NaN;
        cancelValue = double.NaN;
        effectValue = double.NaN;

        if (string.IsNullOrEmpty(data))
            return;

        var parts = data.Split(',');
        if (parts.Length >= 3)
        {
            double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out stdValue);
            double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out cancelValue);
            double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out effectValue);
        }
    }
}
