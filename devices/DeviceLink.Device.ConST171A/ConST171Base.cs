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
    /// 基于P27 SCPI通讯指令集实现，严格遵守OSI七层模型架构
    /// 
    /// OSI通信栈配置：
    /// ┌─────────────────────────────────────────┐
    /// │ 应用层: ConST171Base                      │
    /// ├─────────────────────────────────────────┤
    /// │ 协议层: ScpiCodec (CRLF分隔)             │
    /// ├─────────────────────────────────────────┤
    /// │ 数据链路层: DelimiterFrameStrategy(\r\n) │
    /// ├─────────────────────────────────────────┤
    /// │ 物理层: SerialPortTransport / TcpTransport│
    /// └─────────────────────────────────────────┘
    /// </summary>
    public class ConST171Base : DeviceLink.DeviceBase.DeviceBase
    {
        private readonly ScpiCodec _codec;
        private static readonly byte[] CrlfDelimiter = new byte[] { 0x0D, 0x0A };

        #region 构造函数

        /// <summary>会话层注入（测试/MQTT等场景）</summary>
        public ConST171Base(ISession session, ScpiCodec codec) : base(session, codec) { _codec = codec; }

        /// <summary>TCP/IP连接</summary>
        public ConST171Base(IPAddress ipAddress, int port) : base(ipAddress, port, new ScpiCodec("\r\n")) { _codec = (ScpiCodec)Codec; }

        /// <summary>TCP/IP连接（字符串IP）</summary>
        public ConST171Base(string ipAddress, int port) : base(IPAddress.Parse(ipAddress), port, new ScpiCodec("\r\n")) { _codec = (ScpiCodec)Codec; }

        /// <summary>通信配置</summary>
        public ConST171Base(DeviceCommSettings settings) : base(settings, new ScpiCodec("\r\n")) { _codec = (ScpiCodec)Codec; }

        /// <summary>串口通信（默认9600,8,1,None）</summary>
        public ConST171Base(string portName, int baudRate = 9600, int dataBits = 8,
            System.IO.Ports.StopBits stopBits = System.IO.Ports.StopBits.One,
            System.IO.Ports.Parity parity = System.IO.Ports.Parity.None)
            : base(portName, baudRate, dataBits, stopBits, parity, new ScpiCodec("\r\n"), CrlfDelimiter)
        { _codec = (ScpiCodec)Codec; }

        #endregion

        /// <summary>构造默认设备信息</summary>
        protected override void ConstructDefaultInfo() { base.ConstructDefaultInfo(); Name = "ConST171A"; }

        #region 辅助方法

        private PressureValue ParsePV(byte[] raw)
        {
            var value = _codec.ExtractField(raw, ',', 0);
            var unit = _codec.ExtractField(raw, ',', 1);
            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)
                ? new PressureValue { Value = v, Unit = unit } : new PressureValue();
        }

        private bool PB(string t) => t.Trim() == "1";

        #endregion

        // ============================================================
        // 2.1 IEEE488.2 共同指令
        // ============================================================

        /// <summary>仪器标识查询 *IDN?</summary>
        public Task<string> GetIdentificationAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("*IDN"), r => _codec.ExtractString(r), ct);

        /// <summary>清除寄存器标志 *CLS</summary>
        public Task ClearErrorsAsync(CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("*CLS"), ct);

        /// <summary>状态复位 *RST</summary>
        public Task ResetAsync(CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("*RST"), ct);

        // ============================================================
        // 2.2 压力控制
        // ============================================================

        /// <summary>读取实时压力值 PRESsure?</summary>
        public Task<PressureValue> GetPressureAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure"), r => ParsePV(r), ct);

        /// <summary>读取指定气源实时压力值 PRESsure?</summary>
        public Task<PressureValue> GetPressureAsync(string source, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure", source), r => ParsePV(r), ct);

        /// <summary>获取指定气源控制状态 PRESsure:CONTrol?</summary>
        public Task<bool> GetPressureControlStateAsync(string source, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:CONTrol", source), r => PB(_codec.ExtractString(r)), ct);

        /// <summary>设置指定气源控制状态 PRESsure:CONTrol</summary>
        public Task SetPressureControlStateAsync(string source, bool running, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:CONTrol", source, running ? "1" : "0"), ct);

        /// <summary>获取指定气源压力单位 UNIT?</summary>
        public Task<string> GetPressureUnitAsync(string source, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("UNIT", source), r => _codec.ExtractString(r), ct);

        /// <summary>设置指定气源压力单位 UNIT</summary>
        public Task SetPressureUnitAsync(string source, string unit, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("UNIT", source, unit), ct);

        /// <summary>获取指定气源压力范围 PRESsure:RANGe?</summary>
        public Task<string> GetPressureRangeAsync(string source, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:RANGe", source), r => _codec.ExtractString(r), ct);

        /// <summary>设置指定气源压力范围 PRESsure:RANGe</summary>
        public Task SetPressureRangeAsync(string source, double min, double max, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:RANGe", source, $"{min}:{max}"), ct);

        // ============================================================
        // 内部指令 - 压力配置
        // ============================================================

        /// <summary>读取指定泵原始压力值 PRESsure:RAW?</summary>
        public Task<PressureValue> GetRawPressureAsync(string source, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:RAW", source), r => ParsePV(r), ct);

        /// <summary>获取正压气源静音模式 PRESsure:MUTE?</summary>
        public Task<bool> GetMuteAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:MUTE"), r => PB(_codec.ExtractString(r)), ct);

        /// <summary>设置正压气源静音模式 PRESsure:MUTE</summary>
        public Task SetMuteAsync(bool enable, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:MUTE", enable ? "1" : "0"), ct);

        /// <summary>获取正压上限设定点模式 PRESsure:ADJ?</summary>
        public Task<bool> GetAdjAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:ADJ"), r => PB(_codec.ExtractString(r)), ct);

        /// <summary>设置正压上限设定点模式 PRESsure:ADJ</summary>
        public Task SetAdjAsync(bool activeRelief, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:ADJ", activeRelief ? "1" : "0"), ct);

        /// <summary>获取真空气源开机排水模式 PRESsure:VACUum:VENT?</summary>
        public Task<bool> GetVacuumVentAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("PRESsure:VACUum:VENT"), r => PB(_codec.ExtractString(r)), ct);

        /// <summary>设置真空气源开机排水模式 PRESsure:VACUum:VENT</summary>
        public Task SetVacuumVentAsync(bool enable, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("PRESsure:VACUum:VENT", enable ? "1" : "0"), ct);

        // ============================================================
        // 2.3 校准指令
        // ============================================================

        /// <summary>读取指定气源传感器的校准记录 CALibration:DATA:VALue?</summary>
        public Task<string> GetCalibrationDataAsync(string source, string password, int type, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("CALibration:DATA:VALue", source, password, type.ToString()), r => _codec.ExtractString(r), ct);

        /// <summary>写入指定气源传感器的校准数据 CALibration:DATA:VALue</summary>
        public Task SetCalibrationDataAsync(string source, string password, int count, string points, string values, int year, int month, int day, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("CALibration:DATA:VALue", source, password, count.ToString(), points, values, year.ToString(), month.ToString(), day.ToString()), ct);

        /// <summary>重置校准数据 CALibration:DATA:RESet?</summary>
        public Task<string> ResetCalibrationDataAsync(string source, string password, int type, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("CALibration:DATA:RESet", source, password, type.ToString()), r => _codec.ExtractString(r), ct);

        // 内部校准指令
        /// <summary>进入校准状态 CALibration:METHod:STARt</summary>
        public Task StartCalibrationAsync(string source, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("CALibration:METHod:STARt", source), ct);

        /// <summary>退出校准状态 CALibration:METHod:STOP</summary>
        public Task StopCalibrationAsync(string source, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("CALibration:METHod:STOP", source), ct);

        /// <summary>控制泵达到目标压力值 CALibration:METHod:RUN</summary>
        public Task RunCalibrationAsync(string source, double value, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("CALibration:METHod:RUN", source, value.ToString(CultureInfo.InvariantCulture)), ct);

        // ============================================================
        // 2.4 系统指令 SYSTem
        // ============================================================

        /// <summary>读取设备生产厂家 SYSTem:MFR?</summary>
        public Task<string> GetManufacturerAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:MFR"), r => _codec.ExtractString(r), ct);

        /// <summary>写入设备生产厂家 SYSTem:MFR</summary>
        public Task SetManufacturerAsync(string value, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:MFR", value), ct);

        /// <summary>读取设备型号 SYSTem:MODel?</summary>
        public Task<string> GetModelAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:MODel"), r => _codec.ExtractString(r), ct);

        /// <summary>写入设备型号 SYSTem:MODel</summary>
        public Task SetModelAsync(string value, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:MODel", value), ct);

        /// <summary>读取设备序列号 SYSTem:SN?</summary>
        public Task<string> GetSerialNumberAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:SN"), r => _codec.ExtractString(r), ct);

        /// <summary>写入设备序列号 SYSTem:SN</summary>
        public Task SetSerialNumberAsync(string value, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:SN", value), ct);

        /// <summary>读取设备版本 SYSTem:VERSion?</summary>
        public Task<string> GetVersionAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:VERSion"), r => _codec.ExtractString(r), ct);

        /// <summary>读取指定模块版本 SYSTem:VERSion?</summary>
        public Task<string> GetVersionAsync(string module, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:VERSion", module), r => _codec.ExtractString(r), ct);

        /// <summary>读取MCU与PC串口参数 SYSTem:RS232:INFo?</summary>
        public Task<string> GetRs232InfoAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:RS232:INFo"), r => _codec.ExtractString(r), ct);

        /// <summary>设置MCU与PC串口参数 SYSTem:RS232:INFo</summary>
        public Task SetRs232InfoAsync(int baudRate, int dataBits, string stopBits, string parity, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:RS232:INFo", baudRate.ToString(), dataBits.ToString(), stopBits, parity), ct);

        /// <summary>读取SCPI指令错误内容 SYSTem:ERRor?</summary>
        public Task<string> GetErrorAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:ERRor"), r => _codec.ExtractString(r), ct);

        /// <summary>切换到主界面 SYSTem:HOMe</summary>
        public Task GoHomeAsync(CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:HOMe"), ct);

        /// <summary>查询锁屏状态 SYSTem:LOCK?</summary>
        public Task<bool> GetLockAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:LOCK"), r => PB(_codec.ExtractString(r)), ct);

        /// <summary>设置锁屏状态 SYSTem:LOCK</summary>
        public Task SetLockAsync(bool enable, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:LOCK", enable ? "1" : "0"), ct);

        /// <summary>设备重启 SYSTem:RESTart</summary>
        public Task RestartAsync(CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:RESTart"), ct);

        /// <summary>恢复出厂设置 SYSTem:RESet</summary>
        public Task FactoryResetAsync(string password = "123456", CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:RESet", password), ct);

        /// <summary>查询系统声音状态 SYSTem:SOUNd?</summary>
        public Task<bool> GetSoundAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:SOUNd"), r => PB(_codec.ExtractString(r)), ct);

        /// <summary>设置系统声音状态 SYSTem:SOUNd</summary>
        public Task SetSoundAsync(bool enable, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:SOUNd", enable ? "1" : "0"), ct);

        /// <summary>查询系统亮度 SYSTem:BRIG?</summary>
        public Task<int> GetBrightnessAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:BRIG"), r => { var t = _codec.ExtractString(r); return int.TryParse(t, out var v) ? v : -1; }, ct);

        /// <summary>设置系统亮度 SYSTem:BRIG</summary>
        public Task SetBrightnessAsync(int value, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:BRIG", value.ToString()), ct);

        /// <summary>查询语言 SYSTem:LANGuage?</summary>
        public Task<string> GetLanguageAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("SYSTem:LANGuage"), r => _codec.ExtractString(r), ct);

        /// <summary>设置语言 SYSTem:LANGuage</summary>
        public Task SetLanguageAsync(string language, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:LANGuage", language), ct);

        // 内部系统指令
        /// <summary>更改MCU串口波特率 SYSTem:MCUBaudrate</summary>
        public Task SetMcuBaudrateAsync(int baudRate, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("SYSTem:MCUBaudrate", baudRate.ToString()), ct);

        // ============================================================
        // 2.4（补充）诊断指令 DIAGnostic
        // ============================================================

        /// <summary>软重启指令 DIAGnostic:SOFTreboot</summary>
        public Task SoftRebootAsync(int waitTimeMs = -1, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("DIAGnostic:SOFTreboot", waitTimeMs.ToString()), ct);

        /// <summary>读取序列号 DIAGnostic:SN?</summary>
        public Task<string> GetDiagSerialNumberAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("DIAGnostic:SN"), r => _codec.ExtractString(r), ct);

        /// <summary>设置序列号 DIAGnostic:SN</summary>
        public Task SetDiagSerialNumberAsync(string value, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("DIAGnostic:SN", value), ct);

        /// <summary>读取型号 DIAGnostic:MODel?</summary>
        public Task<string> GetDiagModelAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("DIAGnostic:MODel"), r => _codec.ExtractString(r), ct);

        /// <summary>设置型号 DIAGnostic:MODel</summary>
        public Task SetDiagModelAsync(string value, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("DIAGnostic:MODel", value), ct);

        /// <summary>读取厂家 DIAGnostic:MFR?</summary>
        public Task<string> GetDiagManufacturerAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("DIAGnostic:MFR"), r => _codec.ExtractString(r), ct);

        /// <summary>设置厂家 DIAGnostic:MFR</summary>
        public Task SetDiagManufacturerAsync(string value, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("DIAGnostic:MFR", value), ct);

        /// <summary>读取生产日期 DIAGnostic:MFRDate?</summary>
        public Task<string> GetManufactureDateAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("DIAGnostic:MFRDate"), r => _codec.ExtractString(r), ct);

        /// <summary>设置生产日期 DIAGnostic:MFRDate</summary>
        public Task SetManufactureDateAsync(string date, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("DIAGnostic:MFRDate", date), ct);

        /// <summary>读取开机LOGO DIAGnostic:LOGO?</summary>
        public Task<int> GetLogoAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("DIAGnostic:LOGO"), r => { var t = _codec.ExtractString(r); return int.TryParse(t, out var v) ? v : -1; }, ct);

        /// <summary>设置开机LOGO DIAGnostic:LOGO</summary>
        public Task SetLogoAsync(int value, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("DIAGnostic:LOGO", value.ToString()), ct);

        /// <summary>重启LCD DIAGnostic:LCD:REBoot</summary>
        public Task RebootLcdAsync(CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("DIAGnostic:LCD:REBoot"), ct);

        /// <summary>LCD握手 DIAGnostic:LCD:HANDshake</summary>
        public Task LcdHandshakeAsync(CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("DIAGnostic:LCD:HANDshake"), ct);

        /// <summary>读取风扇转速 DIAGnostic:FAN?</summary>
        public Task<int> GetFanSpeedAsync(string name, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("DIAGnostic:FAN", name), r => { var t = _codec.ExtractString(r); return int.TryParse(t, out var v) ? v : -1; }, ct);

        /// <summary>设置风扇转速 DIAGnostic:FAN</summary>
        public Task SetFanSpeedAsync(string name, double pwm, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("DIAGnostic:FAN", name, pwm.ToString(CultureInfo.InvariantCulture)), ct);

        /// <summary>读取常开(泄压阀)控制状态 DIAGnostic:VENT?</summary>
        public Task<bool> GetVentAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("DIAGnostic:VENT"), r => PB(_codec.ExtractString(r)), ct);

        /// <summary>设置常开(泄压阀)控制状态 DIAGnostic:VENT</summary>
        public Task SetVentAsync(bool open, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("DIAGnostic:VENT", open ? "1" : "0"), ct);

        /// <summary>读取阀门状态 DIAGnostic:VALVe?</summary>
        public Task<bool> GetValveAsync(int valveId, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("DIAGnostic:VALVe", valveId.ToString()), r => PB(_codec.ExtractString(r)), ct);

        /// <summary>设置阀门开关 DIAGnostic:VALVe</summary>
        public Task SetValveAsync(int valveId, bool open, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("DIAGnostic:VALVe", valveId.ToString(), open ? "1" : "0"), ct);

        /// <summary>查询主板温度 DIAGnostic:BOARd:TEMP?</summary>
        public Task<string> GetBoardTemperatureAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("DIAGnostic:BOARd:TEMP"), r => _codec.ExtractString(r), ct);

        /// <summary>查询主板电压 DIAGnostic:BOARd:VOLTage?</summary>
        public Task<string> GetBoardVoltageAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("DIAGnostic:BOARd:VOLTage"), r => _codec.ExtractString(r), ct);

        /// <summary>查询泵状态 DIAGnostic:PUMP?</summary>
        public Task<bool> GetPumpStateAsync(string name, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("DIAGnostic:PUMP", name), r => PB(_codec.ExtractString(r)), ct);

        /// <summary>设置泵运行状态 DIAGnostic:PUMP</summary>
        public Task SetPumpStateAsync(string name, bool running, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("DIAGnostic:PUMP", name, running ? "1" : "0"), ct);

        /// <summary>查询FOC状态 DIAGnostic:FOC?</summary>
        public Task<string> GetFocStateAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("DIAGnostic:FOC"), r => _codec.ExtractString(r), ct);

        /// <summary>查询泵温度 DIAGnostic:PUMP:TEMP?</summary>
        public Task<string> GetPumpTemperatureAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("DIAGnostic:PUMP:TEMP"), r => _codec.ExtractString(r), ct);

        /// <summary>查询泵电流 DIAGnostic:PUMP:CURRent?</summary>
        public Task<string> GetPumpCurrentAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("DIAGnostic:PUMP:CURRent"), r => _codec.ExtractString(r), ct);

        // ============================================================
        // 2.5 测试指令 TEST
        // ============================================================

        /// <summary>设置整机功能测试模式 TEST:FUNCtion</summary>
        public Task SetTestModeAsync(bool enter, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("TEST:FUNCtion", enter ? "1" : "0"), ct);

        /// <summary>查询整机测试状态 TEST:FUNCtion?</summary>
        public Task<bool> GetTestModeAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("TEST:FUNCtion"), r => PB(_codec.ExtractString(r)), ct);

        /// <summary>蜂鸣器测试 TEST:BEEP</summary>
        public Task SetBeepAsync(bool enable, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("TEST:BEEP", enable ? "1" : "0"), ct);

        /// <summary>启动屏幕测试 TEST:SCReen</summary>
        public Task StartScreenTestAsync(int item, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("TEST:SCReen", item.ToString()), ct);

        /// <summary>读取屏幕测试结果 TEST:SCReen:RESUlt?</summary>
        public Task<int> GetScreenTestResultAsync(int item, CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("TEST:SCReen:RESUlt", item.ToString()), r => { var t = _codec.ExtractString(r); return int.TryParse(t, out var v) ? v : -1; }, ct);

        /// <summary>读取吹扫测试状态 TEST:BLOW?</summary>
        public Task<int> GetBlowTestStateAsync(CancellationToken ct = default) =>
            SendForResultAsync(Command.Read("TEST:BLOW"), r => { var t = _codec.ExtractString(r); return int.TryParse(t, out var v) ? v : -1; }, ct);

        /// <summary>吹扫测试 TEST:BLOW</summary>
        public Task StartBlowTestAsync(CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("TEST:BLOW", "1"), ct);

        /// <summary>跳转到触摸测试屏幕 TEST:DST:TOUCh</summary>
        public Task GoToTouchTestAsync(CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("TEST:DST:TOUCh"), ct);

        /// <summary>跳转到刷新测试屏幕 TEST:DST:REFResh</summary>
        public Task GoToRefreshTestAsync(CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("TEST:DST:REFResh"), ct);

        /// <summary>设置测试屏幕刷新数据 TEST:REFResh</summary>
        public Task SetRefreshTestDataAsync(uint data, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("TEST:REFResh", data.ToString()), ct);

        // ============================================================
        // 2.6 OTA升级指令
        // ============================================================

        /// <summary>建立OTA连接 OTAUpdate:CONNect</summary>
        public Task OtaConnectAsync(string base64Config, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("OTAUpdate:CONNect", base64Config), ct);

        /// <summary>传输升级数据 OTAUpdate:TRANs</summary>
        public Task OtaTransferAsync(int sequenceNumber, string data, CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("OTAUpdate:TRANs", sequenceNumber.ToString(), data), ct);

        /// <summary>执行升级 OTAUpdate:UPDAte</summary>
        public Task OtaUpdateAsync(CancellationToken ct = default) =>
            SendNonQueryAsync(Command.Write("OTAUpdate:UPDAte"), ct);
    }
}
