using DeviceLink.DeviceBase;
using DeviceLink.Protocol;
using Newtonsoft.Json;
using System.Globalization;
using System.Net;

namespace DeviceLink.Device.ConST685;

/// <summary>ConST685 多路温场测量/校准设备类
/// 基于 ConST685 SCPI 通讯指令集实现，严格遵守 OSI 七层模型架构
/// 
/// OSI 通信栈配置：
/// ┌─────────────────────────────────────────┐
/// │ 应用层: ConST685Base                      │
/// ├─────────────────────────────────────────┤
/// │ 协议层: ScpiCodec (CRLF 分隔)            │
/// ├─────────────────────────────────────────┤
/// │ 数据链路层: DelimiterFrameStrategy(\r\n) │
/// ├─────────────────────────────────────────┤
/// │ 物理层: SerialPortTransport / TcpTransport│
/// └─────────────────────────────────────────┘
/// 
/// 指令文档：docs/ConST685/ConST685通讯指令集(仅限内部使用).pdf V1.1 2022
/// </summary>
public class ConST685Base : DeviceLink.DeviceBase.DeviceBase
{
    #region 属性字段

    private readonly ScpiCodec _codec;
    private static readonly byte[] CrlfDelimiter = new byte[] { 0x0D, 0x0A };
    private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto,
        DateFormatString = "yyyy-MM-dd HH:mm:ss fff",
        SerializationBinder = new KnownTypesBinder()
    };

    #endregion

    #region 构造函数

    /// <summary>TCP/IP 连接
    /// </summary>
    public ConST685Base(IPAddress ipAddress, int port) : base(ipAddress, port, new ScpiCodec("\r\n")) { _codec = (ScpiCodec)Codec; }

    /// <summary>
    /// TCP/IP 连接（字符串 IP）
    /// </summary>
    public ConST685Base(string ipAddress, int port) : base(IPAddress.Parse(ipAddress), port, new ScpiCodec("\r\n")) { _codec = (ScpiCodec)Codec; }

    /// <summary>
    /// 通信配置
    /// </summary>
    public ConST685Base(DeviceCommSettings settings) : base(settings, new ScpiCodec("\r\n")) { _codec = (ScpiCodec)Codec; }

    /// <summary>
    /// 串口通信（默认 9600,8,1,None）
    /// </summary>
    public ConST685Base(string portName, int baudRate = 9600, int dataBits = 8,
        System.IO.Ports.StopBits stopBits = System.IO.Ports.StopBits.One,
        System.IO.Ports.Parity parity = System.IO.Ports.Parity.None)
        : base(portName, baudRate, dataBits, stopBits, parity, new ScpiCodec("\r\n"), CrlfDelimiter)
    { _codec = (ScpiCodec)Codec; }

    /// <summary>
    /// 构造默认设备信息
    /// </summary>
    protected override void ConstructDefaultInfo() { base.ConstructDefaultInfo(); Name = "ConST685"; }

    #endregion

    #region 通用指令 —— IEEE488.2 共同指令

    /// <summary>
    /// 仪器标识查询 —— *IDN?（返回 厂家,型号,序列号,软件版本号）
    /// </summary>
    public Task<DeviceIdentification> GetIdentificationAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("*IDN"), ParseIdentification, ct);
    }

    /// <summary>
    /// 清除寄存器标志（清除错误队列）—— *CLS
    /// </summary>
    public Task ClearErrorsAsync(CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("*CLS"), ct);
    }

    /// <summary>
    /// 状态复位（主程序复位）—— *RST
    /// </summary>
    public Task ResetAsync(CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("*RST"), ct);
    }

    /// <summary>
    /// 设置标准事件使能寄存器值 —— *ESE enableValue
    /// </summary>
    public Task SetStandardEventEnableAsync(int enableValue, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("*ESE", enableValue.ToString()), ct);
    }

    /// <summary>
    /// 读取标准事件使能寄存器值 —— *ESE?
    /// </summary>
    public Task<int> GetStandardEventEnableAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("*ESE"), r => ParseInt(_codec.ExtractString(r)), ct);
    }

    /// <summary>
    /// 读取标准事件寄存器值（读取后清零）—— *ESR?
    /// </summary>
    public Task<int> GetStandardEventStatusAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("*ESR"), r => ParseInt(_codec.ExtractString(r)), ct);
    }

    /// <summary>
    /// 操作完成查询 —— *OPC?（执行后返回 1）
    /// </summary>
    public Task<int> GetOperationCompleteAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("*OPC"), r => ParseInt(_codec.ExtractString(r)), ct);
    }

    /// <summary>
    /// 设置状态字节使能寄存器值 —— *SRE enableValue
    /// </summary>
    public Task SetStatusByteEnableAsync(int enableValue, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("*SRE", enableValue.ToString()), ct);
    }

    /// <summary>
    /// 读取状态字节使能寄存器值 —— *SRE?
    /// </summary>
    public Task<int> GetStatusByteEnableAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("*SRE"), r => ParseInt(_codec.ExtractString(r)), ct);
    }

    /// <summary>
    /// 读取状态字节寄存器值 —— *STB?
    /// </summary>
    public Task<int> GetStatusByteAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("*STB"), r => ParseInt(_codec.ExtractString(r)), ct);
    }

    /// <summary>
    /// 等待操作完成 —— *WAI
    /// </summary>
    public Task WaitAsync(CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("*WAI"), ct);
    }

    #endregion

    #region 系统指令 —— SYSTem

    /// <summary>查询版本信息（忽略参数返回 SCPI 版本，指定模块返回对应版本）—— SYSTem:VERSion? [module]
    /// 模块可选值："APPLication", "ElECtricity:FIRMware", "ElECtricity:HARDware", "OS:FIRMware", "OS:HARDware", "JUNCtion:HARDware", "JUNCtion:FIRMware"/// </summary>
    public Task<string> GetVersionAsync(string module = null!, CancellationToken ct = default)
    {
        return SendForResultAsync(
            module != null ? Command.Read("SYSTem:VERSion", $"\"{module}\"") : Command.Read("SYSTem:VERSion"),
            r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 查询错误队列中的下一个错误 —— SYSTem:ERRor[:NEXT]?
    /// </summary>
    public Task<ScpiError> GetErrorAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("SYSTem:ERRor"), ParseScpiError, ct);
    }

    /// <summary>
    /// 设置系统日期 —— SYSTem:DATE year,month,day
    /// </summary>
    public Task SetDateAsync(int year, int month, int day, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("SYSTem:DATE", year.ToString(), month.ToString(), day.ToString()), ct);
    }

    /// <summary>
    /// 查询系统日期 —— SYSTem:DATE?（返回 yyyy,MM,dd）
    /// </summary>
    public Task<string> GetDateAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("SYSTem:DATE"), r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 设置系统时间 —— SYSTem:TIME hour,minute,second
    /// </summary>
    public Task SetTimeAsync(int hour, int minute, int second, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("SYSTem:TIME", hour.ToString(), minute.ToString(), second.ToString()), ct);
    }

    /// <summary>
    /// 查询系统时间 —— SYSTem:TIME?（返回 HH,mm,ss）
    /// </summary>
    public Task<string> GetTimeAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("SYSTem:TIME"), r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 设置系统本地锁定状态 —— SYSTem:KLOCk 1|0
    /// </summary>
    public Task SetLocalLockAsync(bool enable, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("SYSTem:KLOCk", enable ? "1" : "0"), ct);
    }

    /// <summary>
    /// 查询系统本地锁定状态 —— SYSTem:KLOCk?
    /// </summary>
    public Task<bool> GetLocalLockAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("SYSTem:KLOCk"), r => IsOne(_codec.ExtractString(r)), ct);
    }

    /// <summary>
    /// 设置提示音状态 —— SYSTem:BEEPer:ALARm 1|0
    /// </summary>
    public Task SetAlarmSoundAsync(bool enable, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("SYSTem:BEEPer:ALARm", enable ? "1" : "0"), ct);
    }

    /// <summary>
    /// 设置按键音状态 —— SYSTem:BEEPer:TOUCh 1|0
    /// </summary>
    public Task SetTouchSoundAsync(bool enable, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("SYSTem:BEEPer:TOUCh", enable ? "1" : "0"), ct);
    }

    /// <summary>
    /// 设置自动开关机 —— SYSTem:STATe 1|0
    /// </summary>
    public Task SetDeviceSwitchStateAsync(bool enable, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("SYSTem:STATe", enable ? "1" : "0"), ct);
    }

    /// <summary>
    /// 获取开关机状态 —— SYSTem:STATe?
    /// </summary>
    public Task<bool> GetDeviceSwitchStateAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("SYSTem:STATe"), r => IsOne(_codec.ExtractString(r)), ct);
    }

    /// <summary>
    /// 查询系统端口号 —— SYSTem:COMMunicate:SOCKet:PORT?
    /// </summary>
    public Task<int> GetPortAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("SYSTem:COMMunicate:SOCKet:PORT"), r => ParseInt(_codec.ExtractString(r)), ct);
    }

    /// <summary>
    /// 设置系统端口号 —— SYSTem:COMMunicate:SOCKet:PORT port
    /// </summary>
    public Task SetPortAsync(int port, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("SYSTem:COMMunicate:SOCKet:PORT", port.ToString()), ct);
    }

    // ---- WIFI ----

    /// <summary>
    /// 设置 WIFI 状态 —— SYSTem:COMMunicate:SOCKet:WLAN[:STATe] 1|0
    /// </summary>
    public Task SetWlanStateAsync(bool enable, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("SYSTem:COMMunicate:SOCKet:WLAN:STATe", enable ? "1" : "0"), ct);
    }

    /// <summary>
    /// 查询 WIFI 状态 —— SYSTem:COMMunicate:SOCKet:WLAN[:STATe]?
    /// </summary>
    public Task<bool> GetWlanStateAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("SYSTem:COMMunicate:SOCKet:WLAN:STATe"), r => IsOne(_codec.ExtractString(r)), ct);
    }

    /// <summary>
    /// 设置 WIFI IP 地址 —— SYSTem:COMMunicate:SOCKet:WLAN:ADDRess ip
    /// </summary>
    public Task SetWlanAddressAsync(string ip, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("SYSTem:COMMunicate:SOCKet:WLAN:ADDRess", ip), ct);
    }

    /// <summary>
    /// 查询 WIFI IP 地址 —— SYSTem:COMMunicate:SOCKet:WLAN:ADDRess?
    /// </summary>
    public Task<string> GetWlanAddressAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("SYSTem:COMMunicate:SOCKet:WLAN:ADDRess"), r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 设置 WIFI 子网掩码 —— SYSTem:COMMunicate:SOCKet:WLAN:MASK mask
    /// </summary>
    public Task SetWlanMaskAsync(string mask, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("SYSTem:COMMunicate:SOCKet:WLAN:MASK", mask), ct);
    }

    /// <summary>
    /// 查询 WIFI 子网掩码 —— SYSTem:COMMunicate:SOCKet:WLAN:MASK?
    /// </summary>
    public Task<string> GetWlanMaskAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("SYSTem:COMMunicate:SOCKet:WLAN:MASK"), r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 设置 WIFI 网关 —— SYSTem:COMMunicate:SOCKet:WLAN:GATeway gateway
    /// </summary>
    public Task SetWlanGatewayAsync(string gateway, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("SYSTem:COMMunicate:SOCKet:WLAN:GATeway", gateway), ct);
    }

    /// <summary>
    /// 查询 WIFI 网关 —— SYSTem:COMMunicate:SOCKet:WLAN:GATeway?
    /// </summary>
    public Task<string> GetWlanGatewayAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("SYSTem:COMMunicate:SOCKet:WLAN:GATeway"), r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 查询 WIFI MAC 地址 —— SYSTem:COMMunicate:SOCKet:WLAN:MAC?
    /// </summary>
    public Task<string> GetWlanMacAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("SYSTem:COMMunicate:SOCKet:WLAN:MAC"), r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 设置 WIFI DHCP 状态 —— SYSTem:COMMunicate:SOCKet:WLAN:DHCP[:STATe] 1|0
    /// </summary>
    public Task SetWlanDhcpAsync(bool enable, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("SYSTem:COMMunicate:SOCKet:WLAN:DHCP:STATe", enable ? "1" : "0"), ct);
    }

    /// <summary>
    /// 查询 WIFI DHCP 状态 —— SYSTem:COMMunicate:SOCKet:WLAN:DHCP[:STATe]?
    /// </summary>
    public Task<bool> GetWlanDhcpAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("SYSTem:COMMunicate:SOCKet:WLAN:DHCP:STATe"), r => IsOne(_codec.ExtractString(r)), ct);
    }

    // ---- 以太网 ----

    /// <summary>
    /// 设置以太网 IP 地址 —— SYSTem:COMMunicate:SOCKet:ETHernet:ADDRess ip
    /// </summary>
    public Task SetEthernetAddressAsync(string ip, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("SYSTem:COMMunicate:SOCKet:ETHernet:ADDRess", ip), ct);
    }

    /// <summary>
    /// 查询以太网 IP 地址 —— SYSTem:COMMunicate:SOCKet:ETHernet:ADDRess?
    /// </summary>
    public Task<string> GetEthernetAddressAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("SYSTem:COMMunicate:SOCKet:ETHernet:ADDRess"), r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 设置以太网子网掩码 —— SYSTem:COMMunicate:SOCKet:ETHernet:MASK mask
    /// </summary>
    public Task SetEthernetMaskAsync(string mask, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("SYSTem:COMMunicate:SOCKet:ETHernet:MASK", mask), ct);
    }

    /// <summary>
    /// 查询以太网子网掩码 —— SYSTem:COMMunicate:SOCKet:ETHernet:MASK?
    /// </summary>
    public Task<string> GetEthernetMaskAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("SYSTem:COMMunicate:SOCKet:ETHernet:MASK"), r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 设置以太网网关 —— SYSTem:COMMunicate:SOCKet:ETHernet:GATeway gateway
    /// </summary>
    public Task SetEthernetGatewayAsync(string gateway, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("SYSTem:COMMunicate:SOCKet:ETHernet:GATeway", gateway), ct);
    }

    /// <summary>
    /// 查询以太网网关 —— SYSTem:COMMunicate:SOCKet:ETHernet:GATeway?
    /// </summary>
    public Task<string> GetEthernetGatewayAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("SYSTem:COMMunicate:SOCKet:ETHernet:GATeway"), r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 设置以太网 DHCP 状态 —— SYSTem:COMMunicate:SOCKet:ETHernet:DHCP[:STATe] 1|0
    /// </summary>
    public Task SetEthernetDhcpAsync(bool enable, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("SYSTem:COMMunicate:SOCKet:ETHernet:DHCP:STATe", enable ? "1" : "0"), ct);
    }

    /// <summary>
    /// 查询以太网 DHCP 状态 —— SYSTem:COMMunicate:SOCKet:ETHernet:DHCP[:STATe]?
    /// </summary>
    public Task<bool> GetEthernetDhcpAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("SYSTem:COMMunicate:SOCKet:ETHernet:DHCP:STATe"), r => IsOne(_codec.ExtractString(r)), ct);
    }

    /// <summary>
    /// 查询以太网 MAC 地址 —— SYSTem:COMMunicate:SOCKet:ETHernet:MAC?
    /// </summary>
    public Task<string> GetEthernetMacAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("SYSTem:COMMunicate:SOCKet:ETHernet:MAC"), r => _codec.ExtractString(r), ct);
    }

    // ---- 蓝牙 ----

    /// <summary>
    /// 设置蓝牙状态 —— SYSTem:COMMunicate:BLUEtooth:STATe 1|0
    /// </summary>
    public Task SetBluetoothStateAsync(bool enable, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("SYSTem:COMMunicate:BLUEtooth:STATe", enable ? "1" : "0"), ct);
    }

    /// <summary>
    /// 查询蓝牙状态 —— SYSTem:COMMunicate:BLUEtooth:STATe?
    /// </summary>
    public Task<bool> GetBluetoothStateAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("SYSTem:COMMunicate:BLUEtooth:STATe"), r => IsOne(_codec.ExtractString(r)), ct);
    }

    /// <summary>
    /// 获取蓝牙名称 —— SYSTem:COMMunicate:BLUEtooth:NAMe?
    /// </summary>
    public Task<string> GetBluetoothNameAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("SYSTem:COMMunicate:BLUEtooth:NAMe"), r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 设置蓝牙名称 —— SYSTem:COMMunicate:BLUEtooth:NAMe name
    /// </summary>
    public Task SetBluetoothNameAsync(string name, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("SYSTem:COMMunicate:BLUEtooth:NAMe", name), ct);
    }

    #endregion

    #region 显示指令 —— DISPlay

    /// <summary>
    /// 查询屏幕亮度 —— DISPlay:BRIGhtness? type（type: 0=正常, 1=AC 模式）
    /// </summary>
    public Task<string> GetBrightnessAsync(int type = 0, CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("DISPlay:BRIGhtness", type.ToString()), r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 设置屏幕亮度 —— DISPlay:BRIGhtness type,level
    /// </summary>
    public Task SetBrightnessAsync(int type, string level, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("DISPlay:BRIGhtness", type.ToString(), level), ct);
    }

    #endregion

    #region 测量指令 —— MODule / SCAN / CHANnel

    /// <summary>
    /// 获取接线盒信息列表 —— JSON:MODule:INFormation?（返回 List&lt;DIModuleInfo&gt; 格式 JSON）
    /// </summary>
    public Task<List<ModuleInfo>> GetModuleInfoListAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("JSON:MODule:INFormation"), r =>
            JsonConvert.DeserializeObject<List<ModuleInfo>>(_codec.ExtractString(r), _jsonSettings) ?? new List<ModuleInfo>(), ct);
    }

    /// <summary>
    /// 设置接线盒标签 —— MODule:LABel index,"label"
    /// </summary>
    public Task SetModuleLabelAsync(int index, string label, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("MODule:LABel", index.ToString(), $"\"{label}\""), ct);
    }

    /// <summary>
    /// 获取接线盒通道配置列表 —— JSON:MODule:CONFig? moduleIndex（返回 List&lt;DIFunctionChannelConfig&gt; 格式 JSON）
    /// </summary>
    public Task<List<ChannelConfig>> GetModuleConfigListAsync(int moduleIndex, CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("JSON:MODule:CONFig", moduleIndex.ToString()), r =>
            JsonConvert.DeserializeObject<List<ChannelConfig>>(_codec.ExtractString(r), _jsonSettings) ?? new List<ChannelConfig>(), ct);
    }

    /// <summary>
    /// 设置接线盒 JSON 通道配置 —— JSON:MODule:CONFig moduleIndex,"jsonStr"
    /// </summary>
    public Task SetModuleConfigJsonAsync(int moduleIndex, string jsonStr, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("JSON:MODule:CONFig", moduleIndex.ToString(), $"\"{jsonStr}\""), ct);
    }

    /// <summary>
    /// 设置接线盒通道配置（详细参数）—— MEASure:CHANnel:CONFig "chName",enable,"label",elecType,range,delay,autoRange,filter,"otherParam"
    /// </summary>
    public Task SetChannelConfigAsync(string chName, bool enable, string label, int elecType,
        int range, int delay, bool autoRange, int filter, string otherParam, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("MEASure:CHANnel:CONFig",
            $"\"{chName}\"", enable ? "1" : "0", $"\"{label}\"", elecType.ToString(),
            range.ToString(), delay.ToString(), autoRange ? "1" : "0", filter.ToString(), $"\"{otherParam}\""), ct);
    }

    /// <summary>
    /// 设置通道 JSON 配置 —— JSON:CHANnel:CONFig "jsonStr"
    /// </summary>
    public Task SetChannelConfigJsonAsync(string jsonStr, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("JSON:CHANnel:CONFig", $"\"{jsonStr}\""), ct);
    }

    /// <summary>
    /// 开始扫描 —— JSON:SCAN:STARt "jsonStr"（参数为 DIScanInfo 序列化的 JSON 字符串）
    /// </summary>
    public Task StartScanAsync(string jsonStr, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("JSON:SCAN:STARt", $"\"{jsonStr}\""), ct);
    }

    /// <summary>
    /// 开始扫描 —— JSON:SCAN:STARt（传入 ScanInfo 对象自动序列化）
    /// </summary>
    public Task StartScanAsync(ScanInfo info, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("JSON:SCAN:STARt", $"\"{JsonConvert.SerializeObject(info, _jsonSettings)}\""), ct);
    }

    /// <summary>
    /// 获取当前扫描配置 —— JSON:SCAN:STARt?（返回 DIScanInfo）
    /// </summary>
    public Task<ScanInfo> GetScanConfigAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("JSON:SCAN:STARt"), r =>
            JsonConvert.DeserializeObject<ScanInfo>(_codec.ExtractString(r), _jsonSettings) ?? new ScanInfo(), ct);
    }

    /// <summary>
    /// 获取所有扫描通道配置 —— JSON:SCAN:STARt? 1（返回 List&lt;DIScanInfo&gt;）
    /// </summary>
    public Task<List<ScanInfo>> GetAllScanConfigAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("JSON:SCAN:STARt", "1"), r =>
            JsonConvert.DeserializeObject<List<ScanInfo>>(_codec.ExtractString(r), _jsonSettings) ?? new List<ScanInfo>(), ct);
    }

    /// <summary>
    /// 停止扫描 —— SCAN:STOP
    /// </summary>
    public Task StopScanAsync(CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("SCAN:STOP"), ct);
    }

    /// <summary>
    /// 获取扫描数据 —— JSON:SCAN:DATA? count[,time]（返回 List&lt;DIReading&gt; 格式 JSON）
    /// </summary>
    public Task<List<ScanReading>> GetScanDataAsync(int count, long time = 0, CancellationToken ct = default)
    {
        return SendForResultAsync(
            time > 0 ? Command.Read("JSON:SCAN:DATA", count.ToString(), time.ToString()) : Command.Read("JSON:SCAN:DATA", count.ToString()),
            r => JsonConvert.DeserializeObject<List<ScanReading>>(_codec.ExtractString(r), _jsonSettings) ?? new List<ScanReading>(), ct);
    }

    /// <summary>获取最新一次扫描数据（文本格式）—— SCAN:DATA:Last? [timeFormat]
    /// timeFormat: 0=不带时间, 1=yyyy:MM:dd HH:mm:ss fff, 2=long/// </summary>
    public Task<string> GetLastScanDataAsync(int timeFormat = 0, CancellationToken ct = default)
    {
        return SendForResultAsync(
            timeFormat > 0 ? Command.Read("SCAN:DATA:Last", timeFormat.ToString()) : Command.Read("SCAN:DATA:Last"),
            r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 获取智能接线下扫描数据 —— JSON:SCAN:SCONnection:DATA? count（返回 List&lt;DIReading&gt; 格式 JSON）
    /// </summary>
    public Task<List<ScanReading>> GetSmartConnectionDataAsync(int count, CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("JSON:SCAN:SCONnection:DATA", count.ToString()), r =>
            JsonConvert.DeserializeObject<List<ScanReading>>(_codec.ExtractString(r), _jsonSettings) ?? new List<ScanReading>(), ct);
    }

    /// <summary>
    /// 多路扫描开始 —— JSON:SCAN:MULT:STARt "jsonStr"
    /// </summary>
    public Task StartMultiScanAsync(string jsonStr, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("JSON:SCAN:MULT:STARt", $"\"{jsonStr}\""), ct);
    }

    /// <summary>
    /// 设置当前通道清零状态 —— CHANnel:ZERo 1|0
    /// </summary>
    public Task SetCurrentChannelZeroAsync(bool enable, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("CHANnel:ZERo", enable ? "1" : "0"), ct);
    }

    /// <summary>
    /// 设置三线电阻恒流换向 —— CHANnel:RESIstance:COMMutation ON|OFF
    /// </summary>
    public Task SetChannelResistanceCommutationAsync(bool enable, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("CHANnel:RESIstance:COMMutation", enable ? "ON" : "OFF"), ct);
    }

    #endregion

    #region 校准指令 —— CALibration

    /// <summary>开始电测校准扫描 —— CALibration:ELECtricity:SCAN mode,function,range[,channel]
    /// mode: 0=APF, 10=ADC; function: 0=DCV,1=DCI,2=Resistance,3=PRT,4=Thermistor; range: 量程索引; channel: 通道/// </summary>
    public Task StartCalibrationScanAsync(int mode, int function, int range, int channel = -1, CancellationToken ct = default)
    {
        return SendNonQueryAsync(
            channel >= 0
                ? Command.Write("CALibration:ELECtricity:SCAN", mode.ToString(), function.ToString(), range.ToString(), channel.ToString())
                : Command.Write("CALibration:ELECtricity:SCAN", mode.ToString(), function.ToString(), range.ToString()),
            ct);
    }

    /// <summary>
    /// 读取电测校准扫描数据（原始字符串）—— CALibration:ELECtricity:SCAN?
    /// </summary>
    public Task<string> GetCalibrationScanAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("CALibration:ELECtricity:SCAN"), r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 读取电测校准扫描结果（解析后）—— CALibration:ELECtricity:SCAN?
    /// </summary>
    public Task<CalibrationResult> GetCalibrationScanResultAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("CALibration:ELECtricity:SCAN"), ParseCalibrationResult, ct);
    }

    /// <summary>
    /// 启动 CJC 冷端校准 —— CALibration:ELECtricity:CJCenable 1|0
    /// </summary>
    public Task SetCjcCalibrationScanAsync(bool enable, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("CALibration:ELECtricity:CJCenable", enable ? "1" : "0"), ct);
    }

    /// <summary>
    /// 设置校准数据 —— CALibration:ELECtricity:DATA role,password,channel,function,range,unitID,count,"points","values",year,month,day
    /// </summary>
    public Task SetCalibrationDataAsync(string role, string password, int channel, int function,
        int range, int unitId, int count, string points, string values,
        int year, int month, int day, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("CALibration:ELECtricity:DATA",
            role, password, channel.ToString(), function.ToString(), range.ToString(),
            unitId.ToString(), count.ToString(), $"\"{points}\"", $"\"{values}\"",
            year.ToString(), month.ToString(), day.ToString()), ct);
    }

    /// <summary>
    /// 清除清零校准数据 —— CALibration:ELECtricity:CZERo function,range
    /// </summary>
    public Task ClearZeroCalDataAsync(int function, int range, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("CALibration:ELECtricity:CZERo", function.ToString(), range.ToString()), ct);
    }

    #endregion

    #region 存储指令 —— MMEMory

    /// <summary>
    /// 返回存储器大小 —— MMEMory:FREE[:ALL]?
    /// </summary>
    public Task<MemoryInfo> GetMemoryFreeAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("MMEMory:FREE"), ParseMemoryInfo, ct);
    }

    /// <summary>
    /// 返回磁盘大小 —— MMEMory:DISK:FREE? disk_name
    /// </summary>
    public Task<string> GetDiskFreeAsync(string diskName, CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("MMEMory:DISK:FREE", diskName), r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 返回文件夹文件列表 —— MMEMory:CATalog? [directory_name]
    /// </summary>
    public Task<string> GetCatalogAsync(string directoryName = null!, CancellationToken ct = default)
    {
        return SendForResultAsync(
            directoryName != null ? Command.Read("MMEMory:CATalog", $"\"{directoryName}\"") : Command.Read("MMEMory:CATalog"),
            r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 查询文件是否存在 —— MMEMory:EXISt:FILE? filename
    /// </summary>
    public Task<bool> FileExistsAsync(string filename, CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("MMEMory:EXISt:FILE", $"\"{filename}\""),
            r => _codec.ExtractString(r).Trim().Equals("TRUE", StringComparison.OrdinalIgnoreCase), ct);
    }

    /// <summary>
    /// 查询文件夹是否存在 —— MMEMory:EXISt:DIREctory? directory_name
    /// </summary>
    public Task<bool> DirectoryExistsAsync(string directoryName, CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("MMEMory:EXISt:DIREctory", $"\"{directoryName}\""),
            r => _codec.ExtractString(r).Trim().Equals("TRUE", StringComparison.OrdinalIgnoreCase), ct);
    }

    /// <summary>
    /// 往文件里写入数据 —— MMEMory:DATA filename,data,APPend|TRUNcate
    /// </summary>
    public Task<string> WriteFileDataAsync(string filename, string data, bool append, CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Write("MMEMory:DATA", $"\"{filename}\"", data, append ? "APPend" : "TRUNcate"),
            r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 读取文件数据 —— MMEMory:DATA? filename
    /// </summary>
    public Task<string> ReadFileDataAsync(string filename, CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("MMEMory:DATA", $"\"{filename}\""), r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 将文件解压到指定目录 —— MMEMory:UNPAck filename,path[,password]
    /// </summary>
    public Task<string> UnpackFileAsync(string filename, string path, string password = null!, CancellationToken ct = default)
    {
        return SendForResultAsync(
            password != null
                ? Command.Write("MMEMory:UNPAck", $"\"{filename}\"", $"\"{path}\"", $"\"{password}\"")
                : Command.Write("MMEMory:UNPAck", $"\"{filename}\"", $"\"{path}\""),
            r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 删除文件 —— MMEMory:DELete filename
    /// </summary>
    public Task<string> DeleteFileAsync(string filename, CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Write("MMEMory:DELete", $"\"{filename}\""), r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 删除目录 —— MMEMory:DELete:DIRectory directoryName[,rescursive]
    /// </summary>
    public Task<string> DeleteDirectoryAsync(string directoryName, bool recursive = false, CancellationToken ct = default)
    {
        return SendForResultAsync(
            recursive
                ? Command.Write("MMEMory:DELete:DIRectory", $"\"{directoryName}\"", "1")
                : Command.Write("MMEMory:DELete:DIRectory", $"\"{directoryName}\""),
            r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 往配置文件里写入数据 —— MMEMory:VALue file_name,data
    /// </summary>
    public Task WriteConfigValueAsync(string fileName, string data, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("MMEMory:VALue", $"\"{fileName}\"", data), ct);
    }

    /// <summary>
    /// 从配置文件里读取数据 —— MMEMory:VALue? file_name,key
    /// </summary>
    public Task<string> ReadConfigValueAsync(string fileName, string key, CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("MMEMory:VALue", $"\"{fileName}\"", key), r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 写入文件（含 CRC） —— MMEMory:FILE fileName,data,crc
    /// </summary>
    public Task WriteFileWithCrcAsync(string fileName, string data, int crc, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("MMEMory:FILE", $"\"{fileName}\"", data, crc.ToString()), ct);
    }

    /// <summary>
    /// 计算文件校验码 —— MMEMory:CHECk? fileName,MD5|CRC16
    /// </summary>
    public Task<string> GetFileChecksumAsync(string fileName, string algorithm, CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("MMEMory:CHECk", $"\"{fileName}\"", algorithm), r => _codec.ExtractString(r), ct);
    }

    #endregion

    #region 诊断指令 —— DIAGnostic

    /// <summary>
    /// 读主机序列号 —— DIAGnostic:IDN?
    /// </summary>
    public Task<string> GetDiagnosticSerialNumberAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("DIAGnostic:IDN"), r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 写主机序列号 —— DIAGnostic:IDN sn
    /// </summary>
    public Task SetDiagnosticSerialNumberAsync(string sn, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("DIAGnostic:IDN", sn), ct);
    }

    /// <summary>
    /// 读主机型号 —— DIAGnostic:MODel?
    /// </summary>
    public Task<string> GetDiagnosticModelAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("DIAGnostic:MODel"), r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 写主机型号 —— DIAGnostic:MODel model
    /// </summary>
    public Task SetDiagnosticModelAsync(string model, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("DIAGnostic:MODel", model), ct);
    }

    /// <summary>
    /// 读主机 Tag 值 —— DIAGnostic:TAG?
    /// </summary>
    public Task<string> GetDiagnosticTagAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("DIAGnostic:TAG"), r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 写主机 Tag 值 —— DIAGnostic:TAG tag
    /// </summary>
    public Task SetDiagnosticTagAsync(string tag, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("DIAGnostic:TAG", tag), ct);
    }

    /// <summary>
    /// 读主机名称 —— DIAGnostic:NAME?
    /// </summary>
    public Task<string> GetDiagnosticNameAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("DIAGnostic:NAME"), r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 写主机名称 —— DIAGnostic:NAME name
    /// </summary>
    public Task SetDiagnosticNameAsync(string name, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("DIAGnostic:NAME", name), ct);
    }

    /// <summary>
    /// 读主机 Guid 值 —— DIAGnostic:GUID?
    /// </summary>
    public Task<string> GetDiagnosticGuidAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("DIAGnostic:GUID"), r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 写主机 Guid 值 —— DIAGnostic:GUID guid
    /// </summary>
    public Task SetDiagnosticGuidAsync(string guid, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("DIAGnostic:GUID", guid), ct);
    }

    /// <summary>
    /// 读主机 Host 自定义字段 —— DIAGnostic:HOST? keyName
    /// </summary>
    public Task<string> GetDiagnosticHostAsync(string keyName, CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("DIAGnostic:HOST", keyName), r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 写主机 Host 自定义字段 —— DIAGnostic:HOST keyName,value
    /// </summary>
    public Task SetDiagnosticHostAsync(string keyName, string value, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("DIAGnostic:HOST", keyName, value), ct);
    }

    /// <summary>
    /// 查询配置文件 —— DIAGnostic:PROFile? fileName,section,key
    /// </summary>
    public Task<string> GetDiagnosticProfileAsync(string fileName, string section, string key, CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("DIAGnostic:PROFile", $"\"{fileName}\"", section, key), r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 编辑配置文件 —— DIAGnostic:PROFile "fileName",section,key,value
    /// </summary>
    public Task SetDiagnosticProfileAsync(string fileName, string section, string key, string value, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("DIAGnostic:PROFile", $"\"{fileName}\"", section, key, value), ct);
    }

    /// <summary>
    /// 读主机应用程序软件版本 —— DIAGnostic:VERSion? [ALL|"file-name"]
    /// </summary>
    public Task<string> GetDiagnosticVersionAsync(string param = null!, CancellationToken ct = default)
    {
        return SendForResultAsync(
                param != null ? Command.Read("DIAGnostic:VERSion", param) : Command.Read("DIAGnostic:VERSion"),
                r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 检测设备是否存在 —— 发送 DIAGnostic:VERSion? 指令，返回包含 "TAU-HOST" 则认为设备存在
    /// </summary>
    public async Task<bool> IsExistAsync(CancellationToken ct = default)
    {
        try
        {
            var version = await GetDiagnosticVersionAsync(ct: ct);
            return !string.IsNullOrEmpty(version) && version.ToUpper().Contains("TAU-HOST");
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 查询功能开启状态 —— DIAGnostic:FEATures:ENABle? [0|4|5|6|ALL|WLAN|BLE|USBSerial]
    /// </summary>
    public Task<bool> GetFeatureEnabledAsync(string feature = null!, CancellationToken ct = default)
    {
        return SendForResultAsync(
                feature != null ? Command.Read("DIAGnostic:FEATures:ENABle", feature) : Command.Read("DIAGnostic:FEATures:ENABle"),
                r => IsOne(_codec.ExtractString(r)), ct);
    }

    /// <summary>
    /// 设置功能开启状态 —— DIAGnostic:FEATures:ENABle [项目,]1|0
    /// </summary>
    public Task SetFeatureEnabledAsync(string feature, bool enable, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("DIAGnostic:FEATures:ENABle", feature ?? "ALL", enable ? "1" : "0"), ct);
    }

    /// <summary>
    /// 软件升级 —— DIAGnostic:SYSTem:UPDAte "fileName"
    /// </summary>
    public Task SystemUpdateAsync(string fileName, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("DIAGnostic:SYSTem:UPDAte", $"\"{fileName}\""), ct);
    }

    /// <summary>
    /// 查询设备 PID VID —— DIAGnostic:SYSTem:VPID?
    /// </summary>
    public Task<string> GetSystemVpidAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("DIAGnostic:SYSTem:VPID"), r => _codec.ExtractString(r), ct);
    }


    /// <summary>
    /// 恢复出厂设置 —— DIAGnostic:SYSTem:RESTore Manufactor|User,password
    /// </summary>
    public Task SystemRestoreAsync(string role, string password, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("DIAGnostic:SYSTem:RESTore", role, password), ct);
    }

    /// <summary>
    /// 查询当前可用图片 —— DIAGnostic:LOGO?
    /// </summary>
    public Task<string> GetLogoListAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("DIAGnostic:LOGO"), r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 设置开启 logo —— DIAGnostic:LOGO name
    /// </summary>
    public Task SetLogoAsync(string name, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("DIAGnostic:LOGO", name), ct);
    }

    /// <summary>
    /// 删除 logo —— DIAGnostic:LOGO:DELete name
    /// </summary>
    public Task DeleteLogoAsync(string name, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("DIAGnostic:LOGO:DELete", name), ct);
    }

    /// <summary>
    /// 设置 boot logo —— DIAGnostic:BLOGo "name"（含扩展名）
    /// </summary>
    public Task SetBootLogoAsync(string name, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("DIAGnostic:BLOGo", $"\"{name}\""), ct);
    }

    /// <summary>
    /// 查询语言 —— DIAGnostic:LANGuage?
    /// </summary>
    public Task<string> GetDiagnosticLanguageAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("DIAGnostic:LANGuage"), r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 设置语言 —— DIAGnostic:LANGuage lcid[,reboot]
    /// </summary>
    public Task SetDiagnosticLanguageAsync(int lcid, int reboot = 0, CancellationToken ct = default) =>
        SendNonQueryAsync(Command.Write("DIAGnostic:LANGuage", lcid.ToString(), reboot.ToString()), ct);

    /// <summary>
    /// 设置支持语言配置 —— DIAGnostic:LANGuage:CONFig "lcids"
    /// </summary>
    public Task SetSupportedLanguagesAsync(string lcids, CancellationToken ct = default) =>
        SendNonQueryAsync(Command.Write("DIAGnostic:LANGuage:CONFig", $"\"{lcids}\""), ct);

    /// <summary>
    /// 查询支持语言配置 —— DIAGnostic:LANGuage:CONFig?
    /// </summary>
    public Task<string> GetSupportedLanguagesAsync(CancellationToken ct = default) =>
        SendForResultAsync(Command.Read("DIAGnostic:LANGuage:CONFig"), r => _codec.ExtractString(r), ct);

    /// <summary>读取系统电压信息 —— DIAGnostic:SYSTem:INFOs:VOLTages?
    /// 返回 3 个逗号分隔值：AD通道12V值,5V是否正常,3.3V是否正常/// </summary>
    public Task<SystemVoltageInfo> GetSystemVoltagesAsync(CancellationToken ct = default) =>
        SendForResultAsync(Command.Read("DIAGnostic:SYSTem:INFOs:VOLTages"), ParseSystemVoltageInfo, ct);

    /// <summary>
    /// 读取电测板序列号 —— DIAGnostic:ELECtricity:IDN?
    /// </summary>
    public Task<string> GetElectricitySerialNumberAsync(CancellationToken ct = default) =>
        SendForResultAsync(Command.Read("DIAGnostic:ELECtricity:IDN"), r => _codec.ExtractString(r), ct);

    /// <summary>
    /// 写入电测板序列号 —— DIAGnostic:ELECtricity:IDN "sn"
    /// </summary>
    public Task SetElectricitySerialNumberAsync(string sn, CancellationToken ct = default) =>
        SendNonQueryAsync(Command.Write("DIAGnostic:ELECtricity:IDN", $"\"{sn}\""), ct);

    /// <summary>
    /// 读取接线盒继电器切换次数 —— DIAGnostic:ELECtric:MODule:RELay? moduleIndex
    /// </summary>
    public Task<int> GetModuleRelayCountAsync(int moduleIndex, CancellationToken ct = default) =>
        SendForResultAsync(Command.Read("DIAGnostic:ELECtric:MODule:RELay", moduleIndex.ToString()), r => ParseInt(_codec.ExtractString(r)), ct);

    /// <summary>
    /// 写入接线盒继电器切换次数 —— DIAGnostic:ELECtric:MODule:RELay moduleIndex,count
    /// </summary>
    public Task SetModuleRelayCountAsync(int moduleIndex, int count, CancellationToken ct = default) =>
        SendNonQueryAsync(Command.Write("DIAGnostic:ELECtric:MODule:RELay", moduleIndex.ToString(), count.ToString()), ct);

    /// <summary>
    /// 读取接线盒序列号 —— DIAGnostic:ELECtric:MODule:IDN? moduleIndex
    /// </summary>
    public Task<string> GetModuleSerialNumberAsync(int moduleIndex, CancellationToken ct = default) =>
        SendForResultAsync(Command.Read("DIAGnostic:ELECtric:MODule:IDN", moduleIndex.ToString()), r => _codec.ExtractString(r), ct);

    /// <summary>
    /// 写入接线盒序列号 —— DIAGnostic:ELECtric:MODule:IDN moduleIndex,sn
    /// </summary>
    public Task SetModuleSerialNumberAsync(int moduleIndex, string sn, CancellationToken ct = default) =>
        SendNonQueryAsync(Command.Write("DIAGnostic:ELECtric:MODule:IDN", moduleIndex.ToString(), sn), ct);

    /// <summary>
    /// 重启电测板 —— DIAGnostic:ELECtricity:REBoot
    /// </summary>
    public Task RebootElectricityBoardAsync(CancellationToken ct = default) =>
        SendNonQueryAsync(Command.Write("DIAGnostic:ELECtricity:REBoot"), ct);

    /// <summary>写入设备发行类别 —— DIAGnostic:CATEGORY category
    /// 0=U=完整版，1=S=简版，2=H=685-H版/// </summary>
    public Task SetDeviceCategoryAsync(int category, CancellationToken ct = default) =>
        SendNonQueryAsync(Command.Write("DIAGnostic:CATEGORY", category.ToString()), ct);

    /// <summary>
    /// 读取设备本次开机时间（毫秒）—— DIAGnostic:SYSTem:RUNTime?
    /// </summary>
    public Task<long> GetSystemRuntimeAsync(CancellationToken ct = default) =>
        SendForResultAsync(Command.Read("DIAGnostic:SYSTem:RUNTime"), r =>
        {
            var text = _codec.ExtractString(r);
            return long.TryParse(text.Trim(), out var v) ? v : -1L;
        }, ct);

    /// <summary>
    /// 读取 IO 板版本 —— DIAGnostic:VERSion:LPC? "version"（"FIRMware" 或 "HARDware"）
    /// </summary>
    public Task<string> GetLpcVersionAsync(string version, CancellationToken ct = default) =>
        SendForResultAsync(Command.Read("DIAGnostic:VERSion:LPC", $"\"{version}\""), r => _codec.ExtractString(r), ct);

    /// <summary>
    /// 配置 IO 板输入输出 —— DIAGnostic:LPC:DIOMode mode
    /// </summary>
    public Task SetLpcDiomodeAsync(byte mode, CancellationToken ct = default) =>
        SendNonQueryAsync(Command.Write("DIAGnostic:LPC:DIOMode", mode.ToString()), ct);

    /// <summary>
    /// 设置输出引脚电平高低 —— DIAGnostic:LPC:DIOLevel level
    /// </summary>
    public Task SetLpcDioLevelAsync(byte level, CancellationToken ct = default) =>
        SendNonQueryAsync(Command.Write("DIAGnostic:LPC:DIOLevel", level.ToString()), ct);

    /// <summary>
    /// 查询输入引脚状态 —— DIAGnostic:LPC:DIOLevel?
    /// </summary>
    public Task<byte> GetLpcDioLevelAsync(CancellationToken ct = default) =>
        SendForResultAsync(Command.Read("DIAGnostic:LPC:DIOLevel"), r =>
        {
            var text = _codec.ExtractString(r);
            return byte.TryParse(text.Trim(), out var v) ? v : (byte)0;
        }, ct);

    /// <summary>
    /// 报警输出控制 —— DIAGnostic:LPC:AIOMode mode
    /// </summary>
    public Task SetLpcAiomodeAsync(byte mode, CancellationToken ct = default) =>
        SendNonQueryAsync(Command.Write("DIAGnostic:LPC:AIOMode", mode.ToString()), ct);

    /// <summary>
    /// 配置报警输出有效电平 —— DIAGnostic:LPC:AIOLevel level
    /// </summary>
    public Task SetLpcAioLevelAsync(byte level, CancellationToken ct = default) =>
        SendNonQueryAsync(Command.Write("DIAGnostic:LPC:AIOLevel", level.ToString()), ct);

    /// <summary>
    /// 设置触发信号滤波时间 —— DIAGnostic:LPC:TIOinterval interval
    /// </summary>
    public Task SetLpcTioIntervalAsync(byte interval, CancellationToken ct = default) =>
        SendNonQueryAsync(Command.Write("DIAGnostic:LPC:TIOinterval", interval.ToString()), ct);

    /// <summary>
    /// 设置触发类型 —— DIAGnostic:LPC:TIOType type
    /// </summary>
    public Task SetLpcTioTypeAsync(byte type, CancellationToken ct = default) =>
        SendNonQueryAsync(Command.Write("DIAGnostic:LPC:TIOType", type.ToString()), ct);

    /// <summary>
    /// 查询触发信号状态 —— DIAGnostic:LPC:TIOStatus?
    /// </summary>
    public Task<byte> GetLpcTioStatusAsync(CancellationToken ct = default) =>
        SendForResultAsync(Command.Read("DIAGnostic:LPC:TIOStatus"), r =>
        {
            var text = _codec.ExtractString(r);
            return byte.TryParse(text.Trim(), out var v) ? v : (byte)0;
        }, ct);

    /// <summary>
    /// 设置计数门限 —— DIAGnostic:LPC:CIOLimit maxCount
    /// </summary>
    public Task SetLpcCioLimitAsync(uint maxCount, CancellationToken ct = default) =>
        SendNonQueryAsync(Command.Write("DIAGnostic:LPC:CIOLimit", maxCount.ToString()), ct);

    /// <summary>
    /// 查询计数器报警状态 —— DIAGnostic:LPC:CIOStatus?
    /// </summary>
    public Task<string> GetLpcCioStatusAsync(CancellationToken ct = default) =>
        SendForResultAsync(Command.Read("DIAGnostic:LPC:CIOStatus"), r => _codec.ExtractString(r), ct);

    /// <summary>
    /// 清零计数器 —— DIAGnostic:LPC:CIOReset
    /// </summary>
    public Task ResetLpcCioAsync(CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("DIAGnostic:LPC:CIOReset"), ct);
    }

    /// <summary>
    /// 设置计数器抖动抑制 —— DIAGnostic:LPC:CIOFilter 1|0
    /// </summary>
    public Task SetLpcCioFilterAsync(bool state, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("DIAGnostic:LPC:CIOFilter", state ? "1" : "0"), ct);
    }

    /// <summary>
    /// 重置触发信号状态 —— DIAGnostic:LPC:TIOReset
    /// </summary>
    public Task ResetLpcTioAsync(CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("DIAGnostic:LPC:TIOReset"), ct);
    }

    /// <summary>屏幕自检 —— DIAGnostic:SCREen:CHECker type[,param]
    /// type: 0=所有, 1=单项测试, 2=功能测试, 3=LAN测试, 4=WiFi测试, 5=A/B按键测试, 6=主USB测试/// </summary>
    public Task<string> ScreenCheckerAsync(int type, int function = 0, CancellationToken ct = default)
    {
        return SendForResultAsync(
            function > 0
                ? Command.Write("DIAGnostic:SCREen:CHECker", type.ToString(), function.ToString())
                : Command.Write("DIAGnostic:SCREen:CHECker", type.ToString()),
            r => _codec.ExtractString(r), ct);
    }

    /// <summary>测试串口通信 —— DIAGnostic:COM:CHECker COMM[,Data]
    /// COM0=自发自收测试，COM3=读取版本信息/// </summary>
    public Task<string> CheckComAsync(string com, string data = null!, CancellationToken ct = default)
    {
        return SendForResultAsync(
            data != null
                ? Command.Write("DIAGnostic:COM:CHECker", com, $"\"{data}\"")
                : Command.Write("DIAGnostic:COM:CHECker", com),
            r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 启用/关闭转发窗口 —— DIAGnostic:FORWardform state[,ip]
    /// </summary>
    public Task SetForwardFormAsync(bool state, string ip = null!, CancellationToken ct = default)
    {
        return SendNonQueryAsync(
            ip != null
                ? Command.Write("DIAGnostic:FORWardform", state ? "1" : "0", $"\"{ip}\"")
                : Command.Write("DIAGnostic:FORWardform", state ? "1" : "0"),
            ct);
    }

    /// <summary>
    /// 查询转发窗口是否打开 —— DIAGnostic:FORWardform?
    /// </summary>
    public Task<bool> GetForwardFormAsync(CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read("DIAGnostic:FORWardform"), r => IsOne(_codec.ExtractString(r)), ct);
    }

    /// <summary>
    /// 给转发窗口发送显示值 —— DIAGnostic:FORWardform:VALue value,unit
    /// </summary>
    public Task SetForwardFormValueAsync(string value, string unit, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("DIAGnostic:FORWardform:VALue", value, unit), ct);
    }

    /// <summary>
    /// 设置转发窗口锁屏 —— DIAGnostic:LOCKforward 1|0
    /// </summary>
    public Task SetForwardLockAsync(bool state, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write("DIAGnostic:LOCKforward", state ? "1" : "0"), ct);
    }

    #endregion

    #region 测试数据指令 —— TDATa

    /// <summary>开始测试 —— TDATa:{type}:STARt "params"
    /// type: SE=传感器测试, SW=开关测试, TF=温源测试, ETF=空间温场测试/// </summary>
    public Task StartTestAsync(string type, string paramsStr, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write($"TDATa:{type}:STARt", $"\"{paramsStr}\""), ct);
    }

    /// <summary>
    /// 停止测试 —— TDATa:{type}:STOP
    /// </summary>
    public Task StopTestAsync(string type, CancellationToken ct = default)
    {
        return SendNonQueryAsync(Command.Write($"TDATa:{type}:STOP"), ct);
    }

    /// <summary>
    /// 读取测试数据 —— TDATa:{type}:DATA? [count]
    /// </summary>
    public Task<string> GetTestDataAsync(string type, int count = 1, CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read($"TDATa:{type}:DATA", count.ToString()), r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 查询测试状态 —— TDATa:{type}:STAT?
    /// </summary>
    public Task<string> GetTestStatusAsync(string type, CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read($"TDATa:{type}:STAT"), r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 搜索测试文件 —— TDATa:{type}? "condition"
    /// </summary>
    public Task<string> SearchTestFilesAsync(string type, string condition, CancellationToken ct = default)
    {
        return SendForResultAsync(Command.Read($"TDATa:{type}", $"\"{condition}\""), r => _codec.ExtractString(r), ct);
    }

    /// <summary>
    /// 删除测试文件 —— DELete:TDATa:{type} "filename"[,"name"]
    /// </summary>
    public Task<string> DeleteTestFileAsync(string type, string filename, string name = null!, CancellationToken ct = default)
    {
        return SendForResultAsync(
            name != null
                ? Command.Read($"DELete:TDATa:{type}", $"\"{filename}\"", $"\"{name}\"")
                : Command.Read($"DELete:TDATa:{type}", $"\"{filename}\""),
            r => _codec.ExtractString(r), ct);
    }

    #endregion

    #region 校准数据指令 —— CALibration:ELECtricity

    /// <summary>
    /// 获取校准数据 —— CALibration:ELECtricity:DATA? Manufactor,3721,&lt;channel&gt;,&lt;function&gt;,&lt;range&gt;
    /// 参数结构与 Xmas11 TAUBase.GetCalibrationData 完全一致
    /// 注意：Xmas11 的 out 参数 (isGetCalDataPass, dataStatus) 已包含在返回的 CalibrationData 对象中
    /// </summary>
    /// <param name="channel">校准通道模式（包含通道号、模式、名称）</param>
    /// <param name="function">校准扫描功能</param>
    /// <param name="range">校准扫描量程</param>
    /// <returns>校准数据（包含 IsGetCalDataPass 和 DataStatus 属性）</returns>
    public async Task<CalibrationData> GetCalibrationDataAsync(
        CalChannelMode channel,
        CalScanFunction function,
        CalScanRange range)
    {
        // 构建SCPI命令：格式与 Xmas11 TAUBase.GetCalibrationData 完全一致
        // CALibration:ELECtricity:DATA? Manufactor,3721,{ModeID},{function},{range}
        string modeID = channel.ModeID;
        string functionStr = ((int)function).ToString();
        string rangeStr = ((int)range).ToString();

        var content = await SendForResultAsync(
            Command.Read("CALibration:ELECtricity:DATA",
                "Manufactor,3721",
                modeID,
                functionStr,
                rangeStr),
            r => _codec.ExtractString(r),
            CancellationToken.None);

        if (string.IsNullOrEmpty(content))
        {
            return new CalibrationData { IsGetCalDataPass = false, DataStatus = "No response" };
        }

        var dataStr = content.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        if (dataStr.Length < 5)
        {
            return new CalibrationData { IsGetCalDataPass = false, DataStatus = $"Invalid data length: {dataStr.Length}" };
        }

        // 解析基础信息
        int unitId = ParseInt(dataStr[0]);
        int pointCount = ParseInt(dataStr[1]);

        if (pointCount <= 0)
        {
            return new CalibrationData { IsGetCalDataPass = false, DataStatus = "Invalid point count" };
        }

        // 验证数据长度：2 + pointCount * 2 + 3
        int expectedLength = 2 + pointCount * 2 + 3;
        if (dataStr.Length < expectedLength)
        {
            return new CalibrationData
            {
                IsGetCalDataPass = false,
                DataStatus = $"Data length mismatch: expected {expectedLength}, got {dataStr.Length}"
            };
        }

        // 解析标准值列表
        var standardList = new List<double>(pointCount);
        for (int i = 0; i < pointCount; i++)
        {
            standardList.Add(ParseDouble(dataStr[2 + i]));
        }

        // 解析校准点列表
        var calPointList = new List<double>(pointCount);
        for (int i = 0; i < pointCount; i++)
        {
            calPointList.Add(ParseDouble(dataStr[2 + pointCount + i]));
        }

        // 解析日期
        int year = ParseInt(dataStr[2 + pointCount * 2]);
        int month = ParseInt(dataStr[2 + pointCount * 2 + 1]);
        int day = ParseInt(dataStr[2 + pointCount * 2 + 2]);

        return new CalibrationData
        {
            ID = $"{channel.ID}_{(int)function}_{(int)range}",
            Key = $"{channel.Name}_{functionStr}_{rangeStr}",
            PointCount = pointCount,
            StandardList = standardList,
            StandardUnit = GetUnitByFunction(function),
            CalPointList = calPointList,
            CalPointUnit = GetUnitByFunction(function),
            Year = year,
            Month = month,
            Day = day,
            UnitId = unitId,
            IsGetCalDataPass = true,
            DataStatus = "OK"
        };
    }

    /// <summary>
    /// 根据校准功能获取单位名称
    /// </summary>
    private static string GetUnitByFunction(CalScanFunction function)
    {
        return function switch
        {
            CalScanFunction.V => "V",
            CalScanFunction.I => "A",
            CalScanFunction.R => "Ω",
            CalScanFunction.PRT or CalScanFunction.RTC or CalScanFunction.Cjc => "°C",
            _ => string.Empty
        };
    }

    #endregion

    #region 私有解析方法

    private static double ParseDouble(string text)
    {
        return double.TryParse(text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : double.NaN;
    }

    private static int ParseInt(string text)
    {
        return int.TryParse(text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : -1;
    }

    private static bool IsOne(string text)
    {
        return text.Trim() == "1";
    }

    private DeviceIdentification ParseIdentification(byte[] raw)
    {
        var text = _codec.DecodeText(raw);
        var parts = text.Split(',');
        // ConST685 *IDN? 返回格式：'序列号',固件版本号
        // 例如：'685018010023',TAU-HOST 1.0.0.92
        return new DeviceIdentification
        {
            SerialNumber = parts.Length >= 1 ? parts[0].Trim().Trim('\'') : string.Empty,
            FirmwareVersion = parts.Length >= 2 ? parts[1].Trim() : string.Empty
        };
    }

    private ScpiError ParseScpiError(byte[] raw)
    {
        var text = _codec.DecodeText(raw);
        var commaIdx = text.IndexOf(',');
        if (commaIdx >= 0)
        {
            return new ScpiError
            {
                Code = ParseInt(text.Substring(0, commaIdx)),
                Message = text.Substring(commaIdx + 1).Trim(' ', '"')
            };
        }
        return new ScpiError
        {
            Code = ParseInt(text),
            Message = string.Empty
        };
    }

    private MemoryInfo ParseMemoryInfo(byte[] raw)
    {
        var text = _codec.DecodeText(raw);
        var parts = text.Split(',');
        return new MemoryInfo
        {
            FreeBytes = parts.Length >= 1 ? long.TryParse(parts[0].Trim(), out var f) ? f : -1 : -1,
            UsedBytes = parts.Length >= 2 ? long.TryParse(parts[1].Trim(), out var u) ? u : -1 : -1
        };
    }

    private SystemVoltageInfo ParseSystemVoltageInfo(byte[] raw)
    {
        var text = _codec.DecodeText(raw);
        var parts = text.Split(',');
        for (int i = 0; i < parts.Length; i++) parts[i] = parts[i].Trim();

        return new SystemVoltageInfo
        {
            Voltage12V = parts.Length >= 1 ? ParseDouble(parts[0]) : double.NaN,
            Is5VNormal = parts.Length >= 2 ? IsOne(parts[1]) : false,
            Is33VNormal = parts.Length >= 3 ? IsOne(parts[2]) : false
        };
    }

    private CalibrationResult ParseCalibrationResult(byte[] raw)
    {
        var text = _codec.DecodeText(raw);
        var parts = text.Split(',');
        // 格式：错误码,模式,功能,量程,完成状态,原始值
        if (parts.Length >= 6)
        {
            return new CalibrationResult
            {
                ErrorCode = parts[0].Trim(),
                CalibrationMode = ParseInt(parts[1]),
                CalibrationFunction = ParseInt(parts[2]),
                CalibrationRange = ParseInt(parts[3]),
                IsSuccess = parts.Length >= 5 && parts[4].Trim() == "1",
                OriginalValue = parts.Length >= 6 ? ParseDouble(parts[5]) : double.NaN
            };
        }
        return new CalibrationResult();
    }

    #endregion
}
