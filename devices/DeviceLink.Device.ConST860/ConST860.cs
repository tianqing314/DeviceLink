using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DeviceLink.DeviceBase;
using DeviceLink.Protocol;
using DeviceLink.Session;

namespace DeviceLink.Device.ConST860
{
    /// <summary>
    /// ConST860 压力控制器设备类
    /// 基于 SCPI 指令集实现，严格遵守 OSI 七层模型架构
    /// 
    /// OSI 通信栈配置：
    /// ┌─────────────────────────────────────────┐
    /// │ 应用层: ConST860                          │
    /// ├─────────────────────────────────────────┤
    /// │ 协议层: ScpiCodec (CRLF 分隔)            │
    /// ├─────────────────────────────────────────┤
    /// │ 数据链路层: DelimiterFrameStrategy(\r\n) │
    /// ├─────────────────────────────────────────┤
    /// │ 物理层: SerialPortTransport / TcpTransport│
    /// └─────────────────────────────────────────┘
    /// </summary>
    public class ConST860 : DeviceLink.DeviceBase.DeviceBase
    {
        #region 属性字段

        private readonly ScpiCodec _codec;
        private static readonly byte[] CrlfDelimiter = new byte[] { 0x0D, 0x0A };

        #endregion

        #region 构造函数

        /// <summary>TCP/IP 连接</summary>
        public ConST860(IPAddress ipAddress, int port) : base(ipAddress, port, new ScpiCodec("\r\n")) { _codec = (ScpiCodec)Codec; }

        /// <summary>TCP/IP 连接（字符串 IP）</summary>
        public ConST860(string ipAddress, int port) : base(IPAddress.Parse(ipAddress), port, new ScpiCodec("\r\n")) { _codec = (ScpiCodec)Codec; }

        /// <summary>通信配置（USB / MQTT 等自定义场景）</summary>
        public ConST860(DeviceCommSettings settings) : base(settings, new ScpiCodec("\r\n")) { _codec = (ScpiCodec)Codec; }

        /// <summary>串口通信（默认 9600,8,1,None）</summary>
        public ConST860(string portName, int baudRate = 9600, int dataBits = 8,
            System.IO.Ports.StopBits stopBits = System.IO.Ports.StopBits.One,
            System.IO.Ports.Parity parity = System.IO.Ports.Parity.None)
            : base(portName, baudRate, dataBits, stopBits, parity, new ScpiCodec("\r\n"), CrlfDelimiter)
        { _codec = (ScpiCodec)Codec; }

        /// <summary>构造默认设备信息</summary>
        protected override void ConstructDefaultInfo() { base.ConstructDefaultInfo(); Name = "ConST860"; }

        #endregion

        #region 通用指令

        // ---- IEEE488.2 共同指令 -------------------------------------------------

        /// <summary>仪器标识查询 —— *IDN?</summary>
        public Task<string> GetIdentificationAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("*IDN"), r => _codec.ExtractString(r), ct);

        /// <summary>清除寄存器标志（清除错误队列）—— *CLS</summary>
        public Task ClearErrorsAsync(CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("*CLS"), ct);

        /// <summary>状态复位（恢复出厂默认状态）—— *RST</summary>
        public Task ResetAsync(CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("*RST"), ct);

        // ---- 系统指令 SYSTem ----------------------------------------------------

        /// <summary>读取系统时间 —— SYSTem:TIME?</summary>
        public Task<string> GetSystemTimeAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:TIME"), r => _codec.ExtractString(r), ct);

        /// <summary>设置系统时间 —— SYSTem:TIME hour,minute,second</summary>
        public Task SetSystemTimeAsync(int hour, int minute, int second, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:TIME", hour.ToString(), minute.ToString(), second.ToString()), ct);

        /// <summary>读取系统日期 —— SYSTem:DATE?</summary>
        public Task<string> GetSystemDateAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:DATE"), r => _codec.ExtractString(r), ct);

        /// <summary>设置系统日期 —— SYSTem:DATE year,month,day</summary>
        public Task SetSystemDateAsync(int year, int month, int day, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:DATE", year.ToString(), month.ToString(), day.ToString()), ct);

        /// <summary>读取时间格式 —— SYSTem:TIME:FORMat?</summary>
        public Task<int> GetTimeFormatAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:TIME:FORMat"), r => int.TryParse(_codec.ExtractString(r), out var v) ? v : -1, ct);

        /// <summary>设置时间格式 —— SYSTem:TIME:FORMat format</summary>
        public Task SetTimeFormatAsync(int format, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:TIME:FORMat", format.ToString()), ct);

        /// <summary>读取日期格式 —— SYSTem:DATE:FORMat?</summary>
        public Task<int> GetDateFormatAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:DATE:FORMat"), r => int.TryParse(_codec.ExtractString(r), out var v) ? v : -1, ct);

        /// <summary>设置日期格式 —— SYSTem:DATE:FORMat format</summary>
        public Task SetDateFormatAsync(int format, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:DATE:FORMat", format.ToString()), ct);

        /// <summary>读取日期分隔符 —— SYSTem:DATE:SEParator?</summary>
        public Task<string> GetDateSeparatorAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:DATE:SEParator"), r => _codec.ExtractString(r), ct);

        /// <summary>设置日期分隔符 —— SYSTem:DATE:SEParator separator</summary>
        public Task SetDateSeparatorAsync(int separator, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:DATE:SEParator", separator.ToString()), ct);

        /// <summary>读取音量 —— SYSTem:VOLUme?</summary>
        public Task<int> GetVolumeAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:VOLUme"), r => int.TryParse(_codec.ExtractString(r), out var v) ? v : -1, ct);

        /// <summary>设置音量 —— SYSTem:VOLUme volume</summary>
        public Task SetVolumeAsync(int volume, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:VOLUme", volume.ToString()), ct);

        /// <summary>读取触摸声音状态 —— SYSTem:VOLUme:TOUCH?（true=开启）</summary>
        public Task<bool> GetTouchSoundAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:VOLUme:TOUCH"), r => IsOne(_codec.ExtractString(r)), ct);

        /// <summary>设置触摸声音状态 —— SYSTem:VOLUme:TOUCH state</summary>
        public Task SetTouchSoundAsync(bool enable, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:VOLUme:TOUCH", enable ? "1" : "0"), ct);

        /// <summary>读取提示声音状态 —— SYSTem:VOLUme:PROMpt?（true=开启）</summary>
        public Task<bool> GetPromptSoundAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:VOLUme:PROMpt"), r => IsOne(_codec.ExtractString(r)), ct);

        /// <summary>设置提示声音状态 —— SYSTem:VOLUme:PROMpt state</summary>
        public Task SetPromptSoundAsync(bool enable, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:VOLUme:PROMpt", enable ? "1" : "0"), ct);

        /// <summary>读取超量程声音状态 —— SYSTem:VOLUme:OVERrange?（true=开启）</summary>
        public Task<bool> GetOverrangeSoundAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:VOLUme:OVERrange"), r => IsOne(_codec.ExtractString(r)), ct);

        /// <summary>设置超量程声音状态 —— SYSTem:VOLUme:OVERrange state</summary>
        public Task SetOverrangeSoundAsync(bool enable, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:VOLUme:OVERrange", enable ? "1" : "0"), ct);

        /// <summary>读取系统亮度 —— SYSTem:BRIGhtness?</summary>
        public Task<int> GetBrightnessAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:BRIGhtness"), r => int.TryParse(_codec.ExtractString(r), out var v) ? v : -1, ct);

        /// <summary>设置系统亮度 —— SYSTem:BRIGhtness brightness</summary>
        public Task SetBrightnessAsync(int brightness, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:BRIGhtness", brightness.ToString()), ct);

        /// <summary>读取指定模块版本 —— SYSTem:VERSion? module</summary>
        public Task<string> GetVersionAsync(string module = "APPLication", CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:VERSion", module), r => _codec.ExtractString(r), ct);

        /// <summary>读取当前语言 —— SYSTem:LANGuage?</summary>
        public Task<string> GetLanguageAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:LANGuage"), r => _codec.ExtractString(r), ct);

        /// <summary>设置当前语言 —— SYSTem:LANGuage language</summary>
        public Task SetLanguageAsync(string language, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:LANGuage", language), ct);

        /// <summary>切换到主界面 —— SYSTem:HOME</summary>
        public Task GoHomeAsync(CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:HOME"), ct);

        /// <summary>查询锁屏状态 —— SYSTem:LOCK?（true=已启用锁屏）</summary>
        public Task<bool> GetLockAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:LOCK"), r => IsOne(_codec.ExtractString(r)), ct);

        /// <summary>设置锁屏状态 —— SYSTem:LOCK state</summary>
        public Task SetLockAsync(bool enable, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:LOCK", enable ? "1" : "0"), ct);

        /// <summary>读取错误信息 —— SYSTem:ERRor?</summary>
        public Task<string> GetErrorAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:ERRor"), r => _codec.ExtractString(r), ct);

        // ---- 通讯指令 WLAN / Ethernet / RS232 -----------------------------------

        /// <summary>读取 WLAN 状态 —— SYSTem:WLAN:STATe?（true=开启）</summary>
        public Task<bool> GetWlanStateAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:WLAN:STATe"), r => IsOne(_codec.ExtractString(r)), ct);

        /// <summary>设置 WLAN 状态 —— SYSTem:WLAN:STATe state</summary>
        public Task SetWlanStateAsync(bool enable, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:WLAN:STATe", enable ? "1" : "0"), ct);

        /// <summary>读取 WLAN IP 地址 —— SYSTem:WLAN:ADDRess?</summary>
        public Task<string> GetWlanAddressAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:WLAN:ADDRess"), r => _codec.ExtractString(r), ct);

        /// <summary>设置 WLAN IP 地址 —— SYSTem:WLAN:ADDRess ip</summary>
        public Task SetWlanAddressAsync(string ip, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:WLAN:ADDRess", ip), ct);

        /// <summary>读取 WLAN 子网掩码 —— SYSTem:WLAN:MASK?</summary>
        public Task<string> GetWlanMaskAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:WLAN:MASK"), r => _codec.ExtractString(r), ct);

        /// <summary>设置 WLAN 子网掩码 —— SYSTem:WLAN:MASK mask</summary>
        public Task SetWlanMaskAsync(string mask, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:WLAN:MASK", mask), ct);

        /// <summary>读取 WLAN 网关 —— SYSTem:WLAN:GATeway?</summary>
        public Task<string> GetWlanGatewayAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:WLAN:GATeway"), r => _codec.ExtractString(r), ct);

        /// <summary>设置 WLAN 网关 —— SYSTem:WLAN:GATeway gateway</summary>
        public Task SetWlanGatewayAsync(string gateway, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:WLAN:GATeway", gateway), ct);

        /// <summary>读取 WLAN DHCP 状态 —— SYSTem:WLAN:DHCP?（true=开启）</summary>
        public Task<bool> GetWlanDhcpAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:WLAN:DHCP"), r => IsOne(_codec.ExtractString(r)), ct);

        /// <summary>设置 WLAN DHCP 状态 —— SYSTem:WLAN:DHCP state</summary>
        public Task SetWlanDhcpAsync(bool enable, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:WLAN:DHCP", enable ? "1" : "0"), ct);

        /// <summary>读取 WLAN MAC 地址 —— SYSTem:WLAN:MAC?</summary>
        public Task<string> GetWlanMacAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:WLAN:MAC"), r => _codec.ExtractString(r), ct);

        /// <summary>读取 WLAN SSID —— SYSTem:WLAN:SSID? [all]</summary>
        public Task<string> GetWlanSsidAsync(string all = null, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:WLAN:SSID", all), r => _codec.ExtractString(r), ct);

        /// <summary>连接 WLAN —— SYSTem:WLAN:CONNect name[,password]</summary>
        public Task ConnectWlanAsync(string name, string password = null, CancellationToken ct = default) =>
            SendNonQueryAsync(password != null ? Command.Write("SYSTem:WLAN:CONNect", name, password) : Command.Write("SYSTem:WLAN:CONNect", name), ct);

        /// <summary>断开 WLAN —— SYSTem:WLAN:DISConnect</summary>
        public Task DisconnectWlanAsync(CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:WLAN:DISConnect"), ct);

        /// <summary>读取以太网 IP 地址 —— SYSTem:ETHernet:ADDRess?</summary>
        public Task<string> GetEthernetAddressAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:ETHernet:ADDRess"), r => _codec.ExtractString(r), ct);

        /// <summary>设置以太网 IP 地址 —— SYSTem:ETHernet:ADDRess ip</summary>
        public Task SetEthernetAddressAsync(string ip, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:ETHernet:ADDRess", ip), ct);

        /// <summary>读取以太网子网掩码 —— SYSTem:ETHernet:MASK?</summary>
        public Task<string> GetEthernetMaskAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:ETHernet:MASK"), r => _codec.ExtractString(r), ct);

        /// <summary>设置以太网子网掩码 —— SYSTem:ETHernet:MASK mask</summary>
        public Task SetEthernetMaskAsync(string mask, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:ETHernet:MASK", mask), ct);

        /// <summary>读取以太网网关 —— SYSTem:ETHernet:GATeway?</summary>
        public Task<string> GetEthernetGatewayAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:ETHernet:GATeway"), r => _codec.ExtractString(r), ct);

        /// <summary>设置以太网网关 —— SYSTem:ETHernet:GATeway gateway</summary>
        public Task SetEthernetGatewayAsync(string gateway, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:ETHernet:GATeway", gateway), ct);

        /// <summary>读取以太网 DHCP 状态 —— SYSTem:ETHernet:DHCP?（true=开启）</summary>
        public Task<bool> GetEthernetDhcpAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:ETHernet:DHCP"), r => IsOne(_codec.ExtractString(r)), ct);

        /// <summary>设置以太网 DHCP 状态 —— SYSTem:ETHernet:DHCP state</summary>
        public Task SetEthernetDhcpAsync(bool enable, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:ETHernet:DHCP", enable ? "1" : "0"), ct);

        /// <summary>读取以太网 MAC 地址 —— SYSTem:ETHernet:MAC?</summary>
        public Task<string> GetEthernetMacAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:ETHernet:MAC"), r => _codec.ExtractString(r), ct);

        /// <summary>读取 RS232 串口参数 —— SYSTem:RS232:Info?</summary>
        public Task<Rs232Info> GetRs232InfoAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:RS232:Info"), r => new Rs232Info
            {
                BaudRate = int.TryParse(_codec.ExtractField(r, ',', 0), out var br) ? br : 0,
                DataBits = int.TryParse(_codec.ExtractField(r, ',', 1), out var db) ? db : 0,
                StopBits = _codec.ExtractField(r, ',', 2),
                Parity = _codec.ExtractField(r, ',', 3)
            }, ct);

        /// <summary>设置 RS232 串口参数 —— SYSTem:RS232:Info BaudRate,DataBits,StopBits,Parity</summary>
        public Task SetRs232InfoAsync(int baudRate, int dataBits, string stopBits, string parity, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:RS232:Info", baudRate.ToString(), dataBits.ToString(), stopBits, parity), ct);

        #endregion

        #region 压力控制指令

        // ---- 压力通用模块 PRESsure:MODule ---------------------------------------

        /// <summary>读取模块压力单位 —— PRESsure:MODule:UNIT? moduleId</summary>
        public Task<string> GetModuleUnitAsync(int moduleId, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:MODule:UNIT", moduleId.ToString()), r => _codec.ExtractString(r), ct);

        /// <summary>设置模块压力单位 —— PRESsure:MODule:UNIT moduleId,unit</summary>
        public Task SetModuleUnitAsync(int moduleId, string unit, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:MODule:UNIT", moduleId.ToString(), unit), ct);

        /// <summary>读取模块可用单位列表 —— PRESsure:MODule:UNIT:LIST?</summary>
        public Task<string> GetModuleUnitListAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:MODule:UNIT:LIST"), r => _codec.ExtractString(r), ct);

        /// <summary>读取模块分辨率 —— PRESsure:MODule:RESOlution? moduleId</summary>
        public Task<int> GetModuleResolutionAsync(int moduleId, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:MODule:RESOlution", moduleId.ToString()), r => int.TryParse(_codec.ExtractString(r), out var v) ? v : -1, ct);

        /// <summary>设置模块分辨率 —— PRESsure:MODule:RESOlution moduleId,value</summary>
        public Task SetModuleResolutionAsync(int moduleId, int value, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:MODule:RESOlution", moduleId.ToString(), value.ToString()), ct);

        /// <summary>模块清零 —— PRESsure:MODule:ZERO moduleId</summary>
        public Task ModuleZeroAsync(int moduleId, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:MODule:ZERO", moduleId.ToString()), ct);

        /// <summary>取消模块清零 —— PRESsure:MODule:ZERO:CANCel moduleId</summary>
        public Task ModuleZeroCancelAsync(int moduleId, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:MODule:ZERO:CANCel", moduleId.ToString()), ct);

        /// <summary>读取模块压力类型 —— PRESsure:MODule:PTYPe? moduleId（G=表压, A=绝压, D=差压）</summary>
        public Task<string> GetModulePressureTypeAsync(int moduleId, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:MODule:PTYPe", moduleId.ToString()), r => _codec.ExtractString(r), ct);

        /// <summary>读取模块量程 —— PRESsure:MODule:RANGe? moduleId</summary>
        public Task<string> GetModuleRangeAsync(int moduleId, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:MODule:RANGe", moduleId.ToString()), r => _codec.ExtractString(r), ct);

        /// <summary>读取可用量程列表 —— PRESsure:RANGe:LIST?</summary>
        public Task<string> GetRangeListAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:RANGe:LIST"), r => _codec.ExtractString(r), ct);

        /// <summary>读取当前量程索引 —— PRESsure:RANGe:INDEx?</summary>
        public Task<string> GetRangeIndexAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:RANGe:INDEx"), r => _codec.ExtractString(r), ct);

        /// <summary>设置当前量程索引 —— PRESsure:RANGe:INDEx index</summary>
        public Task SetRangeIndexAsync(string index, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:RANGe:INDEx", index), ct);

        /// <summary>读取模块多量程支持状态 —— PRESsure:MODule:MULTirange? moduleId（true=支持）</summary>
        public Task<bool> GetModuleMultiRangeAsync(int moduleId, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:MODule:MULTirange", moduleId.ToString()), r => IsOne(_codec.ExtractString(r)), ct);

        /// <summary>读取量程模式 —— PRESsure:RANGe:MODE?（0=手动, 1=自动）</summary>
        public Task<int> GetRangeModeAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:RANGe:MODE"), r => int.TryParse(_codec.ExtractString(r), out var v) ? v : 0, ct);

        /// <summary>设置量程模式 —— PRESsure:RANGe:MODE mode</summary>
        public Task SetRangeModeAsync(int mode, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:RANGe:MODE", mode.ToString()), ct);

        /// <summary>读取模块在线状态 —— PRESsure:MODule:ONLIne? moduleId（true=在线）</summary>
        public Task<bool> GetModuleOnlineAsync(int moduleId, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:MODule:ONLIne", moduleId.ToString()), r => IsOne(_codec.ExtractString(r)), ct);

        /// <summary>读取模块信息 —— PRESsure:MODule:INFO? moduleId</summary>
        public Task<ModuleInfo> GetModuleInfoAsync(int moduleId, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:MODule:INFO", moduleId.ToString()), r => new ModuleInfo
            {
                SerialNumber = _codec.ExtractField(r, ',', 0),
                Range = _codec.ExtractField(r, ',', 1),
                PressureType = _codec.ExtractField(r, ',', 2),
                Version = _codec.ExtractField(r, ',', 3),
                Accuracy = _codec.ExtractField(r, ',', 4)
            }, ct);

        /// <summary>读取模块滤波信息 —— PRESsure:MODule:FILTer? moduleId</summary>
        public Task<FilterInfo> GetModuleFilterAsync(int moduleId, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:MODule:FILTer", moduleId.ToString()), r => new FilterInfo
            {
                Enabled = _codec.ExtractField(r, ',', 0) == "1",
                FilterType = int.TryParse(_codec.ExtractField(r, ',', 1), out var ft) ? ft : 0,
                Value = double.TryParse(_codec.ExtractField(r, ',', 2), NumberStyles.Any, CultureInfo.InvariantCulture, out var fv) ? fv : 0
            }, ct);

        /// <summary>设置模块滤波信息 —— PRESsure:MODule:FILTer moduleId,enable,filterType,value</summary>
        public Task SetModuleFilterAsync(int moduleId, bool enable, int filterType, double value, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:MODule:FILTer", moduleId.ToString(), enable ? "1" : "0", filterType.ToString(), value.ToString(CultureInfo.InvariantCulture)), ct);

        /// <summary>读取所有模块压力值 —— PRESsure:MODUle:VALUes?（返回 & 分隔的字符串数组）</summary>
        public Task<string[]> GetAllModulePressuresAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:MODUle:VALUes"), r => _codec.ExtractFields(r, '&'), ct);

        /// <summary>读取指定模块压力值 —— PRESsure:MODule:MEASure? moduleId</summary>
        public Task<PressureValue> GetModulePressureAsync(int moduleId, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:MODule:MEASure", moduleId.ToString()), ParsePV, ct);

        // ---- 压力输出控制 PRESsure ----------------------------------------------

        /// <summary>读取输出压力值 —— PRESsure?</summary>
        public Task<PressureValue> GetOutputPressureAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure"), ParsePV, ct);

        /// <summary>读取控制状态 —— PRESsure:MODule:CONTrol?</summary>
        public Task<string> GetModuleControlStateAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:MODule:CONTrol"), r => _codec.ExtractString(r), ct);

        /// <summary>设置控制状态 —— PRESsure:MODule:CONTrol state</summary>
        public Task SetModuleControlStateAsync(string state, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:MODule:CONTrol", state), ct);

        /// <summary>读取控制模式 —— PRESsure:MODE?</summary>
        public Task<string> GetControlModeAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:MODE"), r => _codec.ExtractString(r), ct);

        /// <summary>设置控制模式 —— PRESsure:MODE mode</summary>
        public Task SetControlModeAsync(string mode, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:MODE", mode), ct);

        /// <summary>读取目标值范围 —— PRESsure:TARGet:RANGe?</summary>
        public Task<TargetPressureRange> GetTargetPressureRangeAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:TARGet:RANGe"), ParseTargetPressureRange, ct);

        /// <summary>读取目标压力值 —— PRESsure:TARGet?</summary>
        public Task<PressureValue> GetTargetPressureAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:TARGet"), ParsePV, ct);

        /// <summary>设置目标压力值 —— PRESsure:TARGet value</summary>
        public Task SetTargetPressureAsync(double value, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:TARGet", value.ToString(CultureInfo.InvariantCulture)), ct);

        /// <summary>读取当前量程 —— PRESsure:RANGe?</summary>
        public Task<string> GetPressureRangeAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:RANGe"), r => _codec.ExtractString(r), ct);

        /// <summary>读取当前控制模块 ID —— PRESsure:MODule?</summary>
        public Task<int> GetControlModuleAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:MODule"), r => int.TryParse(_codec.ExtractString(r), out var v) ? v : -1, ct);

        /// <summary>设置当前控制模块 —— PRESsure:MODule moduleId</summary>
        public Task SetControlModuleAsync(int moduleId, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:MODule", moduleId.ToString()), ct);

        /// <summary>读取排空压力值 —— PRESsure:Vent?</summary>
        public Task<PressureValue> GetVentPressureAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:Vent"), ParsePV, ct);

        /// <summary>设置排空压力值 —— PRESsure:Vent value</summary>
        public Task SetVentPressureAsync(double value, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:Vent", value.ToString(CultureInfo.InvariantCulture)), ct);

        /// <summary>读取压力限值使能状态 —— PRESsure:PLIMit:ENABle?（true=启用）</summary>
        public Task<bool> GetPressureLimitEnableAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:PLIMit:ENABle"), r => IsOne(_codec.ExtractString(r)), ct);

        /// <summary>设置压力限值使能状态 —— PRESsure:PLIMit:ENABle state</summary>
        public Task SetPressureLimitEnableAsync(bool enable, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:PLIMit:ENABle", enable ? "1" : "0"), ct);

        /// <summary>读取压力限值 —— PRESsure:PLIMit?</summary>
        public Task<TargetPressureRange> GetPressureLimitAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:PLIMit"), ParseTargetPressureRange, ct);

        /// <summary>设置压力限值 —— PRESsure:PLIMit low,high</summary>
        public Task SetPressureLimitAsync(double low, double high, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:PLIMit", low.ToString(CultureInfo.InvariantCulture), high.ToString(CultureInfo.InvariantCulture)), ct);

        /// <summary>读取压力类型 —— PRESsure:TYPE?</summary>
        public Task<PressureTypeInfo> GetPressureTypeAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:TYPE"), r => new PressureTypeInfo
            {
                Type = _codec.ExtractField(r, ',', 0),
                CanSwitch = _codec.ExtractField(r, ',', 1) == "1"
            }, ct);

        /// <summary>设置压力类型 —— PRESsure:TYPE type</summary>
        public Task SetPressureTypeAsync(string type, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:TYPE", type), ct);

        /// <summary>读取步进值 —— PRESsure:STEP?</summary>
        public Task<double> GetStepValueAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:STEP"), r => double.TryParse(_codec.ExtractString(r), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : double.NaN, ct);

        /// <summary>设置步进值 —— PRESsure:STEP value</summary>
        public Task SetStepValueAsync(double value, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:STEP", value.ToString(CultureInfo.InvariantCulture)), ct);

        /// <summary>步进增加 —— PRESsure:STEP:UP</summary>
        public Task StepUpAsync(CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:STEP:UP"), ct);

        /// <summary>步进减少 —— PRESsure:STEP:DOWN</summary>
        public Task StepDownAsync(CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:STEP:DOWN"), ct);

        /// <summary>读取控制信息 —— PRESsure:CONTrol:INFO?</summary>
        public Task<ControlInfo> GetControlInfoAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:CONTrol:INFO"), ParseControlInfo, ct);

        /// <summary>读取控制模式类型 —— PRESsure:CONTrol:MODE?（0=快速, 1=标准, 2=自定义）</summary>
        public Task<int> GetControlModeTypeAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:CONTrol:MODE"), r => int.TryParse(_codec.ExtractString(r), out var v) ? v : -1, ct);

        /// <summary>设置控制模式类型 —— PRESsure:CONTrol:MODE mode</summary>
        public Task SetControlModeTypeAsync(int mode, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:CONTrol:MODE", mode.ToString()), ct);

        /// <summary>读取控制速率 —— PRESsure:CONTrol:SLEWrate?</summary>
        public Task<SlewRateInfo> GetSlewRateAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:CONTrol:SLEWrate"), r => new SlewRateInfo
            {
                Type = int.TryParse(_codec.ExtractField(r, ',', 0), out var ty) ? ty : 0,
                Value = _codec.ExtractField(r, ',', 1),
                Unit = _codec.ExtractField(r, ',', 2)
            }, ct);

        /// <summary>设置控制速率最大值 —— PRESsure:CONTrol:SLEWrate:MAX</summary>
        public Task SetSlewRateMaxAsync(CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:CONTrol:SLEWrate:MAX"), ct);

        /// <summary>设置控制速率限值 —— PRESsure:CONTrol:SLEWrate:LIMIt value</summary>
        public Task SetSlewRateLimitAsync(double value, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:CONTrol:SLEWrate:LIMIt", value.ToString(CultureInfo.InvariantCulture)), ct);

        /// <summary>读取判稳设置 —— PRESsure:CONTrol:STABility?</summary>
        public Task<StabilityInfo> GetStabilityAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:CONTrol:STABility"), r => new StabilityInfo
            {
                Type = int.TryParse(_codec.ExtractField(r, ',', 0), out var ty) ? ty : 0,
                Value = double.TryParse(_codec.ExtractField(r, ',', 1), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0,
                Unit = _codec.ExtractField(r, ',', 2),
                PercentValue = double.TryParse(_codec.ExtractField(r, ',', 3), NumberStyles.Any, CultureInfo.InvariantCulture, out var pv) ? pv : 0,
                PercentUnit = _codec.ExtractField(r, ',', 4),
                Seconds = int.TryParse(_codec.ExtractField(r, ',', 5), out var s) ? s : 0
            }, ct);

        /// <summary>设置判稳设置 —— PRESsure:CONTrol:STABility type,value,seconds</summary>
        public Task SetStabilityAsync(int type, double value, int seconds, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:CONTrol:STABility", type.ToString(), value.ToString(CultureInfo.InvariantCulture), seconds.ToString()), ct);

        /// <summary>读取高度差修正 —— PRESsure:CONTrol:HEIGht:CORRection?</summary>
        public Task<HeightCorrectionInfo> GetHeightCorrectionAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:CONTrol:HEIGht:CORRection"), r => new HeightCorrectionInfo
            {
                Enabled = _codec.ExtractField(r, ',', 0) == "1",
                UnitType = int.TryParse(_codec.ExtractField(r, ',', 1), out var ut) ? ut : 0,
                Height = double.TryParse(_codec.ExtractField(r, ',', 2), NumberStyles.Any, CultureInfo.InvariantCulture, out var h) ? h : 0,
                Density = double.TryParse(_codec.ExtractField(r, ',', 3), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0,
                Gravity = double.TryParse(_codec.ExtractField(r, ',', 4), NumberStyles.Any, CultureInfo.InvariantCulture, out var g) ? g : 0,
                Temperature = double.TryParse(_codec.ExtractField(r, ',', 5), NumberStyles.Any, CultureInfo.InvariantCulture, out var t) ? t : 0
            }, ct);

        /// <summary>设置高度差修正 —— PRESsure:CONTrol:HEIGht:CORRection</summary>
        public Task SetHeightCorrectionAsync(bool enable, int unitType, double height, double density, double gravity, double temperature, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:CONTrol:HEIGht:CORRection", enable ? "1" : "0", unitType.ToString(), height.ToString(CultureInfo.InvariantCulture), density.ToString(CultureInfo.InvariantCulture), gravity.ToString(CultureInfo.InvariantCulture), temperature.ToString(CultureInfo.InvariantCulture)), ct);

        /// <summary>读取去皮信息 —— PRESsure:TARE?</summary>
        public Task<TareInfo> GetTareAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:TARE"), r => new TareInfo
            {
                Enabled = _codec.ExtractField(r, ',', 0) == "1",
                Value = double.TryParse(_codec.ExtractField(r, ',', 1), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0
            }, ct);

        /// <summary>设置去皮 —— PRESsure:TARE enable[,value]</summary>
        public Task SetTareAsync(bool enable, double value, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:TARE", enable ? "1" : "0", value.ToString(CultureInfo.InvariantCulture)), ct);

        /// <summary>读取压力开关类型 —— PRESsure:SWITch:TYPE?（0=机械, 1=NPN, 2=PNP）</summary>
        public Task<int> GetSwitchTypeAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:SWITch:TYPE"), r => int.TryParse(_codec.ExtractString(r), out var v) ? v : -1, ct);

        /// <summary>设置压力开关类型 —— PRESsure:SWITch:TYPE type</summary>
        public Task SetSwitchTypeAsync(int type, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:SWITch:TYPE", type.ToString()), ct);

        /// <summary>读取压力开关动作值 —— PRESsure:SWITch:VALUe?</summary>
        public Task<SwitchValueInfo> GetSwitchValueAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:SWITch:VALUe"), ParseSwitchValue, ct);

        /// <summary>重置压力开关动作值 —— PRESsure:SWITch:VALUe:RESEt</summary>
        public Task ResetSwitchValueAsync(CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:SWITch:VALUe:RESEt"), ct);

        /// <summary>读取扩展接口状态 —— PRESsure:EXTEnd:INTErface:STATe?</summary>
        public Task<ExtendedInterfaceState> GetExtendedInterfaceStateAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:EXTEnd:INTErface:STATe"), r => new ExtendedInterfaceState
            {
                Cps = _codec.ExtractField(r, ',', 0) == "1",
                Drv1 = _codec.ExtractField(r, ',', 1) == "1",
                Drv2 = _codec.ExtractField(r, ',', 2) == "1",
                Do1 = _codec.ExtractField(r, ',', 3) == "1",
                Do2 = _codec.ExtractField(r, ',', 4) == "1",
                Do3 = _codec.ExtractField(r, ',', 5) == "1",
                Dc24 = _codec.ExtractField(r, ',', 6) == "1",
                Switch = _codec.ExtractField(r, ',', 7) == "1"
            }, ct);

        /// <summary>读取扩展接口模式 —— PRESsure:EXTEnd:INTErface:MODE? type</summary>
        public Task<ExtendedInterfaceModeInfo> GetExtendedInterfaceModeAsync(int type, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:EXTEnd:INTErface:MODE", type.ToString()), r =>
            {
                var groups = _codec.ExtractFields(r, '&');
                return new ExtendedInterfaceModeInfo
                {
                    CurrentMode = groups.Length >= 1 && int.TryParse(groups[0], out var m) ? m : 0,
                    AvailableModes = groups.Length >= 2 ? groups[1].Split(',').Select(s => int.TryParse(s.Trim(), out var v) ? v : 0).ToArray() : Array.Empty<int>()
                };
            }, ct);

        /// <summary>设置扩展接口模式 —— PRESsure:EXTEnd:INTErface:MODE type,mode</summary>
        public Task SetExtendedInterfaceModeAsync(int type, int mode, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:EXTEnd:INTErface:MODE", type.ToString(), mode.ToString()), ct);

        /// <summary>设置扩展接口远程控制 —— PRESsure:EXTEnd:INTErface:Remote type,value</summary>
        public Task SetExtendedInterfaceRemoteAsync(int type, bool value, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:EXTEnd:INTErface:Remote", type.ToString(), value ? "1" : "0"), ct);

        /// <summary>读取自动零点跟踪状态 —— PRESsure:AZERo?（true=开启）</summary>
        public Task<bool> GetAutoZeroAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:AZERo"), r => IsOne(_codec.ExtractString(r)), ct);

        /// <summary>设置自动零点跟踪 —— PRESsure:AZERo state</summary>
        public Task SetAutoZeroAsync(bool enable, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:AZERo", enable ? "1" : "0"), ct);

        /// <summary>读取零点策略 —— PRESsure:ZERO:POINt:STRAtegy?</summary>
        public Task<int> GetZeroPointStrategyAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:ZERO:POINt:STRAtegy"), r => int.TryParse(_codec.ExtractString(r), out var v) ? v : -1, ct);

        /// <summary>设置零点策略 —— PRESsure:ZERO:POINt:STRAtegy strategy</summary>
        public Task SetZeroPointStrategyAsync(int strategy, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:ZERO:POINt:STRAtegy", strategy.ToString()), ct);

        /// <summary>读取压力稳定状态 —— PRESsure:STABle?（true=稳定）</summary>
        public Task<bool> GetPressureStableAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:STABle"), r => IsOne(_codec.ExtractString(r)), ct);

        /// <summary>读取固定大气压 —— PRESsure:FIXEd:ATM?</summary>
        public Task<AtmosphericPressure> GetFixedAtmosphericPressureAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:FIXEd:ATM"), r =>
            {
                var value = _codec.ExtractField(r, ',', 0);
                var unit = _codec.ExtractField(r, ',', 1);
                return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)
                    ? new AtmosphericPressure { Value = v, Unit = unit } : new AtmosphericPressure();
            }, ct);

        /// <summary>设置固定大气压 —— PRESsure:FIXEd:ATM value</summary>
        public Task SetFixedAtmosphericPressureAsync(double value, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:FIXEd:ATM", value.ToString(CultureInfo.InvariantCulture)), ct);

        /// <summary>读取介质名称 —— PRESsure:MEDIum:NAME?</summary>
        public Task<int> GetMediumNameAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:MEDIum:NAME"), r => int.TryParse(_codec.ExtractString(r), out var v) ? v : -1, ct);

        /// <summary>设置介质名称 —— PRESsure:MEDIum:NAME medium</summary>
        public Task SetMediumNameAsync(int medium, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:MEDIum:NAME", medium.ToString()), ct);

        #endregion

        #region 电测指令

        /// <summary>读取测量功能 —— MEASure:FUNCtion? [all]</summary>
        public Task<int> GetMeasureFunctionAsync(string all = null, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("MEASure:FUNCtion", all), r => int.TryParse(_codec.ExtractString(r), out var v) ? v : -1, ct);

        /// <summary>设置测量功能 —— MEASure:FUNCtion function</summary>
        public Task SetMeasureFunctionAsync(int function, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("MEASure:FUNCtion", function.ToString()), ct);

        /// <summary>读取测量分辨率 —— MEASure:CONFig:RESOlution? sw</summary>
        public Task<int> GetMeasureResolutionAsync(int sw, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("MEASure:CONFig:RESOlution", sw.ToString()), r => int.TryParse(_codec.ExtractString(r), out var v) ? v : -1, ct);

        /// <summary>设置测量分辨率 —— MEASure:CONFig:RESOlution sw,digital</summary>
        public Task SetMeasureResolutionAsync(int sw, int digital, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("MEASure:CONFig:RESOlution", sw.ToString(), digital.ToString()), ct);

        /// <summary>读取测量值 —— MEASure?</summary>
        public Task<double> GetMeasureValueAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("MEASure"), r => double.TryParse(_codec.ExtractString(r), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : double.NaN, ct);

        /// <summary>测量清零 —— MEASure:ZERO</summary>
        public Task MeasureZeroAsync(CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("MEASure:ZERO"), ct);

        /// <summary>取消测量清零 —— MEASure:ZERO:CANCel</summary>
        public Task MeasureZeroCancelAsync(CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("MEASure:ZERO:CANCel"), ct);

        #endregion

        #region 私有解析方法

        private static double ParseDouble(string text) =>
            double.TryParse(text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : double.NaN;

        private static int ParseInt(string text) =>
            int.TryParse(text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : -1;

        private static bool IsOne(string text) => text.Trim() == "1";

        private PressureValue ParsePV(byte[] raw)
        {
            var value = _codec.ExtractField(raw, ',', 0);
            var unit = _codec.ExtractField(raw, ',', 1);
            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)
                ? new PressureValue { Value = v, Unit = unit } : new PressureValue();
        }

        private TargetPressureRange ParseTargetPressureRange(byte[] raw)
        {
            var lo = _codec.ExtractField(raw, ',', 0);
            var hi = _codec.ExtractField(raw, ',', 1);
            var unit = _codec.ExtractField(raw, ',', 2);
            return double.TryParse(lo, NumberStyles.Any, CultureInfo.InvariantCulture, out var low)
                && double.TryParse(hi, NumberStyles.Any, CultureInfo.InvariantCulture, out var high)
                ? new TargetPressureRange { Low = low, High = high, Unit = unit } : new TargetPressureRange();
        }

        private ControlInfo ParseControlInfo(byte[] raw) =>
            new ControlInfo
            {
                Value = double.TryParse(_codec.ExtractField(raw, ',', 0), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : double.NaN,
                Target = double.TryParse(_codec.ExtractField(raw, ',', 1), NumberStyles.Any, CultureInfo.InvariantCulture, out var ta) ? ta : double.NaN,
                Unit = _codec.ExtractField(raw, ',', 2),
                Range = _codec.ExtractField(raw, ',', 3),
                PressureType = _codec.ExtractField(raw, ',', 4),
                IsStable = _codec.ExtractField(raw, ',', 5) == "1",
                State = _codec.ExtractField(raw, ',', 6),
                ExtendInfo = _codec.ExtractField(raw, ',', 7)
            };

        private SwitchValueInfo ParseSwitchValue(byte[] raw)
        {
            var groups = _codec.ExtractFields(raw, '&');
            var res = new SwitchValueInfo();
            if (groups.Length >= 1)
            {
                var closeParts = groups[0].Split(',');
                if (closeParts.Length >= 2)
                {
                    res.CloseValue = double.TryParse(closeParts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var cv) ? cv : 0;
                    res.CloseUnit = closeParts[1].Trim();
                }
            }
            if (groups.Length >= 2)
            {
                var openParts = groups[1].Split(',');
                if (openParts.Length >= 2)
                {
                    res.OpenValue = double.TryParse(openParts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var ov) ? ov : 0;
                    res.OpenUnit = openParts[1].Trim();
                }
            }
            return res;
        }

        #endregion
    }
}
