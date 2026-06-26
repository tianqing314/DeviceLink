using System;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DeviceLink.DeviceBase;
using DeviceLink.Protocol;
using DeviceLink.Session;

namespace DeviceLink.Device.ConST171A
{
    /// <summary>
    /// ConST171A 压力控制器设备类
    /// 基于 P27 SCPI 通讯指令集实现，严格遵守 OSI 七层模型架构
    /// 
    /// OSI 通信栈配置：
    /// ┌─────────────────────────────────────────┐
    /// │ 应用层: ConST171Base                      │
    /// ├─────────────────────────────────────────┤
    /// │ 协议层: ScpiCodec (CRLF 分隔)            │
    /// ├─────────────────────────────────────────┤
    /// │ 数据链路层: DelimiterFrameStrategy(\r\n) │
    /// ├─────────────────────────────────────────┤
    /// │ 物理层: SerialPortTransport / TcpTransport│
    /// └─────────────────────────────────────────┘
    /// 
    /// 指令文档：docs/P27 SCPI通讯指令集.md
    ///           docs/P27 SCPI通讯指令集（内部指令补充）.md
    /// </summary>
    public class ConST171Base : DeviceLink.DeviceBase.DeviceBase
    {
        #region 属性字段

        private readonly ScpiCodec _codec;
        private static readonly byte[] CrlfDelimiter = new byte[] { 0x0D, 0x0A };

        #endregion

        #region 构造函数

        /// <summary>TCP/IP 连接</summary>
        public ConST171Base(IPAddress ipAddress, int port) : base(ipAddress, port, new ScpiCodec("\r\n")) { _codec = (ScpiCodec)Codec; }

        /// <summary>TCP/IP 连接（字符串 IP）</summary>
        public ConST171Base(string ipAddress, int port) : base(IPAddress.Parse(ipAddress), port, new ScpiCodec("\r\n")) { _codec = (ScpiCodec)Codec; }

        /// <summary>通信配置</summary>
        public ConST171Base(DeviceCommSettings settings) : base(settings, new ScpiCodec("\r\n")) { _codec = (ScpiCodec)Codec; }

        /// <summary>串口通信（默认 9600,8,1,None）</summary>
        public ConST171Base(string portName, int baudRate = 9600, int dataBits = 8,
            System.IO.Ports.StopBits stopBits = System.IO.Ports.StopBits.One,
            System.IO.Ports.Parity parity = System.IO.Ports.Parity.None)
            : base(portName, baudRate, dataBits, stopBits, parity, new ScpiCodec("\r\n"), CrlfDelimiter)
        { _codec = (ScpiCodec)Codec; }

        /// <summary>构造默认设备信息</summary>
        protected override void ConstructDefaultInfo() { base.ConstructDefaultInfo(); Name = "ConST171A"; }

        #endregion

        #region 通用指令

        /// <summary>仪器标识查询 —— *IDN?（返回 厂家,型号,序列号,固件版本）</summary>
        public Task<DeviceIdentification> GetIdentificationAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("*IDN"), ParseIdentification, ct);

        /// <summary>判断当前连接的设备是否为 ConST171A —— 通过 SYSTem:VERSion? 检查固件是否包含 EPU-LP
        /// 返回 true=是 ConST171A 设备, false=非 ConST171A 设备</summary>
        public async Task<bool> IsExistAsync(CancellationToken ct = default)
        {
            try
            {
                if (!IsOpen) return false;

                var version = await GetVersionAsync(ct);
                return version.IsValid &&
                    (version.Firmware.ToUpperInvariant().Contains("EPU-LP".ToUpperInvariant()) ||
                     version.Hardware.ToUpperInvariant().Contains("EPU-LP".ToUpperInvariant()));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>清除寄存器标志（清除错误队列）—— *CLS</summary>
        public Task ClearErrorsAsync(CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("*CLS"), ct);

        /// <summary>状态复位（恢复出厂默认状态）—— *RST</summary>
        public Task ResetAsync(CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("*RST"), ct);

        /// <summary>读取设备生产厂家 —— SYSTem:MFR?</summary>
        public Task<string> GetManufacturerAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:MFR"), r => _codec.ExtractString(r), ct);

        /// <summary>写入设备生产厂家 —— SYSTem:MFR value</summary>
        public Task SetManufacturerAsync(string value, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:MFR", value), ct);

        /// <summary>读取设备型号 —— SYSTem:MODel?</summary>
        public Task<string> GetModelAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:MODel"), r => _codec.ExtractString(r), ct);

        /// <summary>写入设备型号 —— SYSTem:MODel value</summary>
        public Task SetModelAsync(string value, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:MODel", value), ct);

        /// <summary>读取设备序列号 —— SYSTem:SN?</summary>
        public Task<string> GetSerialNumberAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:SN"), r => _codec.ExtractString(r), ct);

        /// <summary>写入设备序列号 —— SYSTem:SN value</summary>
        public Task SetSerialNumberAsync(string value, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:SN", value), ct);

        /// <summary>读取全部模块版本信息 —— SYSTem:VERSion?
        /// 返回 Bootloader / 显示模块 / 硬件 / 固件 版本</summary>
        public Task<DeviceVersionInfo> GetVersionAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:VERSion"), ParseVersionInfo, ct);

        /// <summary>读取指定模块版本 —— SYSTem:VERSion? module</summary>
        public Task<string> GetVersionAsync(string module, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:VERSion", module), r => _codec.ExtractString(r), ct);

        /// <summary>读取 MCU 与 PC 串口参数 —— SYSTem:RS232:INFo?
        /// 返回 波特率,数据位,停止位,校验位</summary>
        public Task<Rs232Settings> GetRs232InfoAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:RS232:INFo"), ParseRs232Settings, ct);

        /// <summary>设置 MCU 与 PC 串口参数 —— SYSTem:RS232:INFo BaudRate,DataBits,StopBits,Parity</summary>
        public Task SetRs232InfoAsync(int baudRate, int dataBits, string stopBits, string parity, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:RS232:INFo", baudRate.ToString(), dataBits.ToString(), stopBits, parity), ct);

        /// <summary>读取 SCPI 指令错误内容 —— SYSTem:ERRor?
        /// 返回 错误码,"错误描述"（无错误时返回 0,"No error"）</summary>
        public Task<ScpiError> GetErrorAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:ERRor"), ParseScpiError, ct);

        /// <summary>切换到主界面 —— SYSTem:HOMe（请确保设备处于空闲状态）</summary>
        public Task GoHomeAsync(CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:HOMe"), ct);

        /// <summary>查询锁屏状态 —— SYSTem:LOCK?（true=已启用锁屏）</summary>
        public Task<bool> GetLockAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:LOCK"), r => IsOne(_codec.ExtractString(r)), ct);

        /// <summary>设置锁屏状态 —— SYSTem:LOCK state</summary>
        public Task SetLockAsync(bool enable, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:LOCK", enable ? "1" : "0"), ct);

        /// <summary>设备重启 —— SYSTem:RESTart</summary>
        public Task RestartAsync(CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:RESTart"), ct);

        /// <summary>恢复出厂设置 —— SYSTem:RESet password（默认密码 123456）</summary>
        public Task FactoryResetAsync(string password = "123456", CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:RESet", password), ct);

        /// <summary>查询系统声音启用状态 —— SYSTem:SOUNd?（true=启用）</summary>
        public Task<bool> GetSoundAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:SOUNd"), r => IsOne(_codec.ExtractString(r)), ct);

        /// <summary>设置系统声音启用状态 —— SYSTem:SOUNd value</summary>
        public Task SetSoundAsync(bool enable, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:SOUNd", enable ? "1" : "0"), ct);

        /// <summary>查询系统亮度 —— SYSTem:BRIG?（返回值 0-100）</summary>
        public Task<int> GetBrightnessAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:BRIG"), r => ParseInt(_codec.ExtractString(r)), ct);

        /// <summary>设置系统亮度 —— SYSTem:BRIG value（取值范围 0-100）</summary>
        public Task SetBrightnessAsync(int value, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:BRIG", value.ToString()), ct);

        /// <summary>查询当前语言 —— SYSTem:LANGuage?（如 zh-CN / en-US）</summary>
        public Task<string> GetLanguageAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:LANGuage"), r => _codec.ExtractString(r), ct);

        /// <summary>设置当前语言 —— SYSTem:LANGuage language</summary>
        public Task SetLanguageAsync(string language, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:LANGuage", language), ct);

        #endregion

        #region 压力控制指令

        /// <summary>读取双气源实时压力值 —— PRESsure?
        /// 实际返回 正压值,kPa,真空值,kPa,前级值,kPa（如 44.273,kPa,100.428,kPa,24.498,kPa）</summary>
        public Task<DualPressureValue> GetPressureAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure"), ParseDualPressureValue, ct);

        /// <summary>读取指定气源实时压力值 —— PRESsure? source
        /// 返回 压力值,单位（如 5.655,kPa）</summary>
        public Task<PressureValue> GetPressureAsync(SourceModule module, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure", module.ToScpiString()), ParsePressureValue, ct);

        /// <summary>获取指定气源控制状态 —— PRESsure:CONTrol? source（true=运行中）</summary>
        public Task<bool> GetPressureControlStateAsync(SourceModule module, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:CONTrol", module.ToScpiString()), r => IsOne(_codec.ExtractString(r)), ct);

        /// <summary>设置指定气源控制状态 —— PRESsure:CONTrol source,state</summary>
        public Task SetPressureControlStateAsync(SourceModule module, bool running, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:CONTrol", module.ToScpiString(), running ? "1" : "0"), ct);

        /// <summary>获取指定气源压力单位 —— UNIT? source</summary>
        public Task<string> GetPressureUnitAsync(SourceModule module, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("UNIT", module.ToScpiString()), r => _codec.ExtractString(r), ct);

        /// <summary>设置指定气源压力单位 —— UNIT source,unit</summary>
        public Task SetPressureUnitAsync(SourceModule module, string unit, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("UNIT", module.ToScpiString(), unit), ct);

        /// <summary>获取指定气源压力控制范围 —— PRESsure:RANGe? source（返回 下限:上限 kPa）</summary>
        public Task<PressureRange> GetPressureRangeAsync(SourceModule module, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:RANGe", module.ToScpiString()), ParsePressureRange, ct);

        /// <summary>设置指定气源压力控制范围 —— PRESsure:RANGe source,min:max</summary>
        public Task SetPressureRangeAsync(SourceModule module, double min, double max, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:RANGe", module.ToScpiString(), $"{min:F2}:{max:F2}"), ct);

        // ---- 内部 - 压力配置 ----------------------------------------------------
        // 指令文档：P27 SCPI通讯指令集（内部指令补充）.md → 2.1 压力配置

        /// <summary>读取指定泵原始压力值 —— PRESsure:RAW? source（内部指令）</summary>
        public Task<PressureValue> GetRawPressureAsync(SourceModule module, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:RAW", module.ToScpiString()), ParsePressureValue, ct);

        /// <summary>获取正压气源静音模式 —— PRESsure:MUTE?（内部指令）</summary>
        public Task<bool> GetMuteAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:MUTE"), r => IsOne(_codec.ExtractString(r)), ct);

        /// <summary>设置正压气源静音模式 —— PRESsure:MUTE value（内部指令）</summary>
        public Task SetMuteAsync(bool enable, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:MUTE", enable ? "1" : "0"), ct);

        /// <summary>获取正压上限设定点模式 —— PRESsure:ADJ?（内部指令）</summary>
        public Task<bool> GetAdjAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:ADJ"), r => IsOne(_codec.ExtractString(r)), ct);

        /// <summary>设置正压上限设定点模式 —— PRESsure:ADJ value（内部指令）</summary>
        public Task SetAdjAsync(bool activeRelief, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:ADJ", activeRelief ? "1" : "0"), ct);

        /// <summary>获取真空气源开机排水模式 —— PRESsure:VACUum:VENT?（内部指令）</summary>
        public Task<bool> GetVacuumVentAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:VACUum:VENT"), r => IsOne(_codec.ExtractString(r)), ct);

        /// <summary>设置真空气源开机排水模式 —— PRESsure:VACUum:VENT value（内部指令）</summary>
        public Task SetVacuumVentAsync(bool enable, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:VACUum:VENT", enable ? "1" : "0"), ct);

        #endregion

        #region 私有指令

        /// <summary>读取指定气源传感器的校准记录 —— CALibration:DATA:VALue? ModuleID,password,type</summary>
        public Task<CalibrationRecord> GetCalibrationDataAsync(SourceModule module, string password, int type, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("CALibration:DATA:VALue", module.ToScpiString(), password, type.ToString()), ParseCalibrationRecord, ct);

        /// <summary>写入指定气源传感器的校准数据 —— CALibration:DATA:VALue</summary>
        public Task SetCalibrationDataAsync(SourceModule module, string password, int count, string points, string values, int year, int month, int day, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("CALibration:DATA:VALue", module.ToScpiString(), password, count.ToString(), points, values, year.ToString(), month.ToString(), day.ToString()), ct);

        /// <summary>重置校准数据（返回被重置前的校准记录）—— CALibration:DATA:RESet? ModuleID,password,type</summary>
        public Task<CalibrationRecord> ResetCalibrationDataAsync(SourceModule module, string password, int type, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("CALibration:DATA:RESet", module.ToScpiString(), password, type.ToString()), ParseCalibrationRecord, ct);

        /// <summary>控制【增压/真空泵】进入校准状态 —— CALibration:METHod:STARt ModuleID（内部，不对外开放）</summary>
        public Task StartCalibrationAsync(SourceModule module, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("CALibration:METHod:STARt", module.ToScpiString()), ct);

        /// <summary>控制【增压/真空泵】退出校准状态 —— CALibration:METHod:STOP ModuleID（内部，不对外开放）</summary>
        public Task StopCalibrationAsync(SourceModule module, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("CALibration:METHod:STOP", module.ToScpiString()), ct);

        /// <summary>控制【增压/真空泵】达到目标压力值 —— CALibration:METHod:RUN ModuleID,value（内部，不对外开放）</summary>
        public Task RunCalibrationAsync(SourceModule module, double value, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("CALibration:METHod:RUN", module.ToScpiString(), value.ToString("F2", CultureInfo.InvariantCulture)), ct);

        /// <summary>更改设备 RS232 串口波特率 —— SYSTem:MCUBaudrate value（内部指令）</summary>
        public Task SetMcuBaudrateAsync(int baudRate, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:MCUBaudrate", baudRate.ToString()), ct);

        /// <summary>软重启指令（用于升级）—— DIAGnostic:SOFTreboot waitTimeMs</summary>
        public Task SoftRebootAsync(int waitTimeMs = -1, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("DIAGnostic:SOFTreboot", waitTimeMs.ToString()), ct);

        /// <summary>读取序列号（诊断）—— DIAGnostic:SN?</summary>
        public Task<string> GetDiagSerialNumberAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("DIAGnostic:SN"), r => _codec.ExtractString(r), ct);

        /// <summary>设置序列号（诊断）—— DIAGnostic:SN value</summary>
        public Task SetDiagSerialNumberAsync(string value, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("DIAGnostic:SN", value), ct);

        /// <summary>读取型号（诊断）—— DIAGnostic:MODel?</summary>
        public Task<string> GetDiagModelAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("DIAGnostic:MODel"), r => _codec.ExtractString(r), ct);

        /// <summary>设置型号（诊断）—— DIAGnostic:MODel value</summary>
        public Task SetDiagModelAsync(string value, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("DIAGnostic:MODel", value), ct);

        /// <summary>读取生产厂家（诊断）—— DIAGnostic:MFR?</summary>
        public Task<string> GetDiagManufacturerAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("DIAGnostic:MFR"), r => _codec.ExtractString(r), ct);

        /// <summary>设置生产厂家（诊断）—— DIAGnostic:MFR value</summary>
        public Task SetDiagManufacturerAsync(string value, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("DIAGnostic:MFR", value), ct);

        /// <summary>读取生产日期 —— DIAGnostic:MFRDate?（如 2026.1.1）</summary>
        public Task<string> GetManufactureDateAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("DIAGnostic:MFRDate"), r => _codec.ExtractString(r), ct);

        /// <summary>设置生产日期 —— DIAGnostic:MFRDate value（年月日以.分隔）</summary>
        public Task SetManufactureDateAsync(string date, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("DIAGnostic:MFRDate", date), ct);

        /// <summary>读取开机 LOGO —— DIAGnostic:LOGO?（0=ConST, 1=Additel, 2=定制）</summary>
        public Task<int> GetLogoAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("DIAGnostic:LOGO"), r => ParseInt(_codec.ExtractString(r)), ct);

        /// <summary>设置开机 LOGO —— DIAGnostic:LOGO value</summary>
        public Task SetLogoAsync(int value, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("DIAGnostic:LOGO", value.ToString()), ct);

        /// <summary>重启 LCD 显示模块 —— DIAGnostic:LCD:REBoot（解决屏幕卡死或显示异常）</summary>
        public Task RebootLcdAsync(CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("DIAGnostic:LCD:REBoot"), ct);

        /// <summary>LCD 握手 —— DIAGnostic:LCD:HANDshake（检测通信链路是否正常）</summary>
        public Task LcdHandshakeAsync(CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("DIAGnostic:LCD:HANDshake"), ct);

        /// <summary>读取风扇转速 —— DIAGnostic:FAN? name</summary>
        public Task<int> GetFanSpeedAsync(SourceModule module, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("DIAGnostic:FAN", module.ToScpiString()), r => ParseInt(_codec.ExtractString(r)), ct);

        /// <summary>设置风扇转速 —— DIAGnostic:FAN name,pwm（pwm 范围 0~1）</summary>
        public Task SetFanSpeedAsync(SourceModule module, double pwm, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("DIAGnostic:FAN", module.ToScpiString(), pwm.ToString("F2", CultureInfo.InvariantCulture)), ct);

        /// <summary>读取常开（泄压阀）控制指令状态 —— DIAGnostic:VENT?（true=打开）</summary>
        public Task<bool> GetVentAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("DIAGnostic:VENT"), r => IsOne(_codec.ExtractString(r)), ct);

        /// <summary>设置常开（泄压阀）控制指令状态 —— DIAGnostic:VENT val</summary>
        public Task SetVentAsync(bool open, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("DIAGnostic:VENT", open ? "1" : "0"), ct);

        /// <summary>读取阀门状态 —— DIAGnostic:VALVe? name（valveId 1~4）</summary>
        public Task<bool> GetValveAsync(int valveId, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("DIAGnostic:VALVe", valveId.ToString()), r => IsOne(_codec.ExtractString(r)), ct);

        /// <summary>设置阀门开关状态 —— DIAGnostic:VALVe name,val</summary>
        public Task SetValveAsync(int valveId, bool open, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("DIAGnostic:VALVe", valveId.ToString(), open ? "1" : "0"), ct);

        /// <summary>查询主板温度 —— DIAGnostic:BOARd:TEMP?（返回 ℃）</summary>
        public Task<double> GetBoardTemperatureAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("DIAGnostic:BOARd:TEMP"), r =>
            {
                var text = _codec.ExtractString(r)
                    .Replace("°C", "").Replace("℃", "").Replace("°", "").Trim();
                return ParseDouble(text);
            }, ct);

        /// <summary>查询主板电压 —— DIAGnostic:BOARd:VOLTage?（24V, Boost传感器, Vacuum传感器）</summary>
        public Task<BoardVoltage> GetBoardVoltageAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("DIAGnostic:BOARd:VOLTage"), ParseBoardVoltage, ct);

        /// <summary>查询泵运行状态 —— DIAGnostic:PUMP? name（true=运行中）</summary>
        public Task<bool> GetPumpStateAsync(SourceModule module, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("DIAGnostic:PUMP", module.ToScpiString()), r => IsOne(_codec.ExtractString(r)), ct);

        /// <summary>设置泵运行状态 —— DIAGnostic:PUMP name,val</summary>
        public Task SetPumpStateAsync(SourceModule module, bool running, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("DIAGnostic:PUMP", module.ToScpiString(), running ? "1" : "0"), ct);

        /// <summary>查询 FOC 状态 —— DIAGnostic:FOC?（前级FOC和增压FOC正常/错误）</summary>
        public Task<FocState> GetFocStateAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("DIAGnostic:FOC"), ParseFocState, ct);

        /// <summary>查询泵温度 —— DIAGnostic:PUMP:TEMP?（前级泵温度, 增压泵温度 ℃）</summary>
        public Task<PumpTemperatures> GetPumpTemperatureAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("DIAGnostic:PUMP:TEMP"), ParsePumpTemperatures, ct);

        /// <summary>查询泵电流 —— DIAGnostic:PUMP:CURRent?（前级泵电流, 增压泵电流 A）</summary>
        public Task<PumpCurrents> GetPumpCurrentAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("DIAGnostic:PUMP:CURRent"), ParsePumpCurrents, ct);

        /// <summary>设置整机功能测试模式 —— TEST:FUNCtion val</summary>
        public Task SetTestModeAsync(bool enter, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("TEST:FUNCtion", enter ? "1" : "0"), ct);

        /// <summary>查询整机测试状态 —— TEST:FUNCtion?（true=正在进行测试）</summary>
        public Task<bool> GetTestModeAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("TEST:FUNCtion"), r => IsOne(_codec.ExtractString(r)), ct);

        /// <summary>蜂鸣器测试 —— TEST:BEEP value</summary>
        public Task SetBeepAsync(bool enable, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("TEST:BEEP", enable ? "1" : "0"), ct);

        /// <summary>启动屏幕测试 —— TEST:SCReen item（0=坏点,1=触摸,2=亮度,3=蜂鸣器）</summary>
        public Task StartScreenTestAsync(int item, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("TEST:SCReen", item.ToString()), ct);

        /// <summary>读取屏幕测试结果 —— TEST:SCReen:RESUlt? item（0=未进行,1=进行中,2=失败,3=通过）</summary>
        public Task<int> GetScreenTestResultAsync(int item, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("TEST:SCReen:RESUlt", item.ToString()), r => ParseInt(_codec.ExtractString(r)), ct);

        /// <summary>读取吹扫测试状态 —— TEST:BLOW?（0=未进行,1=进行中,2=吹扫结束）</summary>
        public Task<int> GetBlowTestStateAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("TEST:BLOW"), r => ParseInt(_codec.ExtractString(r)), ct);

        /// <summary>吹扫测试 —— TEST:BLOW val</summary>
        public Task StartBlowTestAsync(bool start = true, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("TEST:BLOW", start ? "1" : "0"), ct);

        /// <summary>跳转到触摸测试屏幕 —— TEST:DST:TOUCh</summary>
        public Task GoToTouchTestAsync(CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("TEST:DST:TOUCh"), ct);

        /// <summary>跳转到刷新测试屏幕 —— TEST:DST:REFResh</summary>
        public Task GoToRefreshTestAsync(CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("TEST:DST:REFResh"), ct);

        /// <summary>设置测试屏幕要刷新的数据 —— TEST:REFResh data（uint32 范围内）</summary>
        public Task SetRefreshTestDataAsync(uint data, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("TEST:REFResh", data.ToString()), ct);

        /// <summary>建立 OTA 连接 —— OTAUpdate:CONNect base64Config</summary>
        public Task OtaConnectAsync(string base64Config, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("OTAUpdate:CONNect", base64Config), ct);

        /// <summary>传输升级数据 —— OTAUpdate:TRANs sn,data</summary>
        public Task OtaTransferAsync(int sequenceNumber, string data, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("OTAUpdate:TRANs", sequenceNumber.ToString(), data), ct);

        /// <summary>执行升级 —— OTAUpdate:UPDAte（传输完成后触发固件校验升级）</summary>
        public Task OtaUpdateAsync(CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("OTAUpdate:UPDAte"), ct);

        // ---- 私有解析方法 ------------------------------------------------------

        private static double ParseDouble(string text) =>
            double.TryParse(text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : double.NaN;

        private static int ParseInt(string text) =>
            int.TryParse(text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : -1;

        private static bool IsOne(string text) => text.Trim() == "1";

        private PressureValue ParsePressureValue(byte[] raw)
        {
            var text = _codec.DecodeText(raw);
            var parts = text.Split(',');
            if (parts.Length >= 2)
            {
                return new PressureValue
                {
                    Value = ParseDouble(parts[0]),
                    Unit = parts[1].Trim()
                };
            }
            return new PressureValue();
        }

        private DualPressureValue ParseDualPressureValue(byte[] raw)
        {
            var text = _codec.DecodeText(raw);
            var parts = text.Split(',');
            if (parts.Length >= 6)
            {
                return new DualPressureValue
                {
                    PositiveValue = ParseDouble(parts[0]),
                    PositiveUnit = parts[1].Trim(),
                    VacuumValue = ParseDouble(parts[2]),
                    VacuumUnit = parts[3].Trim(),
                    PreValue = ParseDouble(parts[4]),
                    PreUnit = parts[5].Trim()
                };
            }
            if (parts.Length >= 4)
            {
                return new DualPressureValue
                {
                    PositiveValue = ParseDouble(parts[0]),
                    PositiveUnit = parts[1].Trim(),
                    VacuumValue = ParseDouble(parts[2]),
                    VacuumUnit = parts[3].Trim()
                };
            }
            return new DualPressureValue();
        }

        private PressureRange ParsePressureRange(byte[] raw)
        {
            var text = _codec.DecodeText(raw);
            var parts = text.Split(':');
            if (parts.Length >= 2)
            {
                return new PressureRange
                {
                    Min = ParseDouble(parts[0]),
                    Max = ParseDouble(parts[1])
                };
            }
            return new PressureRange();
        }

        private DeviceIdentification ParseIdentification(byte[] raw)
        {
            var text = _codec.DecodeText(raw);
            var parts = text.Split(',');
            return new DeviceIdentification
            {
                Manufacturer = parts.Length >= 1 ? parts[0].Trim() : string.Empty,
                Model = parts.Length >= 2 ? parts[1].Trim() : string.Empty,
                SerialNumber = parts.Length >= 3 ? parts[2].Trim() : string.Empty,
                FirmwareVersion = parts.Length >= 4 ? parts[3].Trim() : string.Empty
            };
        }

        private CalibrationRecord ParseCalibrationRecord(byte[] raw)
        {
            var text = _codec.DecodeText(raw);
            var parts = text.Split(',');
            if (parts.Length >= 3)
            {
                var standardParts = parts[0].Split(':');
                var rawParts = parts[1].Split(':');
                var standards = new double[standardParts.Length];
                var raws = new double[rawParts.Length];
                for (int i = 0; i < standardParts.Length; i++)
                    standards[i] = ParseDouble(standardParts[i]);
                for (int i = 0; i < rawParts.Length; i++)
                    raws[i] = ParseDouble(rawParts[i]);

                return new CalibrationRecord
                {
                    StandardValues = standards,
                    RawValues = raws,
                    Year = ParseInt(parts[parts.Length - 3]),
                    Month = ParseInt(parts[parts.Length - 2]),
                    Day = ParseInt(parts[parts.Length - 1])
                };
            }
            return new CalibrationRecord();
        }

        private DeviceVersionInfo ParseVersionInfo(byte[] raw)
        {
            var text = _codec.DecodeText(raw);
            var parts = text.Split(new[] { '，', ',' });
            var info = new DeviceVersionInfo();

            for (int i = 0; i < parts.Length; i++)
            {
                var partStr = parts[i].Trim();
                if (partStr.StartsWith("BOOT=", StringComparison.OrdinalIgnoreCase))
                    info.Bootloader = partStr.Substring(5).Trim();
                else if (partStr.StartsWith("EPU_DM_", StringComparison.OrdinalIgnoreCase))
                    info.DisplayModule = partStr.Trim();
                else if (partStr.StartsWith("EPU-LP", StringComparison.OrdinalIgnoreCase) ||
                         partStr.StartsWith("HARD=", StringComparison.OrdinalIgnoreCase))
                    info.Hardware = partStr.Trim();
                else if (partStr.StartsWith("EPU_LP_", StringComparison.OrdinalIgnoreCase) ||
                         partStr.StartsWith("FIRM=", StringComparison.OrdinalIgnoreCase))
                    info.Firmware = partStr.Trim();
                else if (!string.IsNullOrEmpty(partStr) && string.IsNullOrEmpty(info.Firmware))
                    info.Firmware = partStr.Trim();
            }

            return info;
        }

        private Rs232Settings ParseRs232Settings(byte[] raw)
        {
            var text = _codec.DecodeText(raw);
            var parts = text.Split(',');
            return new Rs232Settings
            {
                BaudRate = parts.Length >= 1 ? ParseInt(parts[0]) : 0,
                DataBits = parts.Length >= 2 ? ParseInt(parts[1]) : 0,
                StopBits = parts.Length >= 3 ? parts[2].Trim() : string.Empty,
                Parity = parts.Length >= 4 ? parts[3].Trim() : string.Empty
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

        private BoardVoltage ParseBoardVoltage(byte[] raw)
        {
            var text = _codec.DecodeText(raw);
            var parts = text.Split(new[] { '，', ',' });
            for (int i = 0; i < parts.Length; i++) parts[i] = parts[i].Trim();

            double ExtractVoltage(string s)
            {
                s = s.TrimEnd('V', 'v', ' ');
                return ParseDouble(s);
            }

            return new BoardVoltage
            {
                Voltage24V = parts.Length >= 1 ? ExtractVoltage(parts[0]) : double.NaN,
                BoostSensorVoltage = parts.Length >= 2 ? ExtractVoltage(parts[1]) : double.NaN,
                VacuumSensorVoltage = parts.Length >= 3 ? ExtractVoltage(parts[2]) : double.NaN
            };
        }

        private PumpTemperatures ParsePumpTemperatures(byte[] raw)
        {
            var text = _codec.DecodeText(raw);
            var parts = text.Split(new[] { '，', ',' });
            for (int i = 0; i < parts.Length; i++) parts[i] = parts[i].Trim();

            static double ExtractTemperature(string s)
            {
                s = s.Replace("°C", "").Replace("℃", "").Replace("°", "").Trim();
                return ParseDouble(s);
            }

            return new PumpTemperatures
            {
                PreStagePump = parts.Length >= 1 ? ExtractTemperature(parts[0]) : double.NaN,
                BoostPump = parts.Length >= 2 ? ExtractTemperature(parts[1]) : double.NaN
            };
        }

        private PumpCurrents ParsePumpCurrents(byte[] raw)
        {
            var text = _codec.DecodeText(raw);
            var parts = text.Split(new[] { '，', ',' });
            for (int i = 0; i < parts.Length; i++) parts[i] = parts[i].Trim();

            static double ExtractCurrent(string s)
            {
                s = s.TrimEnd('A', 'a', ' ');
                return ParseDouble(s);
            }

            return new PumpCurrents
            {
                PreStagePump = parts.Length >= 1 ? ExtractCurrent(parts[0]) : double.NaN,
                BoostPump = parts.Length >= 2 ? ExtractCurrent(parts[1]) : double.NaN
            };
        }

        private FocState ParseFocState(byte[] raw)
        {
            var text = _codec.DecodeText(raw);
            var parts = text.Split(new[] { '，', ',' });
            for (int i = 0; i < parts.Length; i++) parts[i] = parts[i].Trim();
            return new FocState
            {
                PreStageOk = parts.Length < 1 || parts[0] != "1",
                BoostOk = parts.Length < 2 || parts[1] != "1"
            };
        }

        #endregion
    }
}
