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
    public class ConST860 : DeviceLink.DeviceBase.DeviceBase
    {
        private readonly ScpiCodec _codec;
        private static readonly byte[] CrlfDelimiter = new byte[] { 0x0D, 0x0A };

        public ConST860(ISession session, ScpiCodec codec) : base(session, codec) { _codec = codec; }
        public ConST860(IPAddress ipAddress, int port) : base(ipAddress, port, new ScpiCodec("\r\n")) { _codec = (ScpiCodec)Codec; }
        public ConST860(string ipAddress, int port) : base(IPAddress.Parse(ipAddress), port, new ScpiCodec("\r\n")) { _codec = (ScpiCodec)Codec; }
        public ConST860(DeviceCommSettings settings) : base(settings, new ScpiCodec("\r\n")) { _codec = (ScpiCodec)Codec; }

        /// <summary>
        /// 串口通信构造函数（自动配置 SCPI 帧分隔符为 \r\n）
        /// </summary>
        public ConST860(string portName, int baudRate = 9600, int dataBits = 8,
            System.IO.Ports.StopBits stopBits = System.IO.Ports.StopBits.One,
            System.IO.Ports.Parity parity = System.IO.Ports.Parity.None)
            : base(portName, baudRate, dataBits, stopBits, parity, new ScpiCodec("\r\n"), CrlfDelimiter)
        { _codec = (ScpiCodec)Codec; }

        protected override void ConstructDefaultInfo() { base.ConstructDefaultInfo(); Name = "ConST860"; }

        private PressureValue ParsePV(byte[] raw)
        {
            var value = _codec.ExtractField(raw, ',', 0);
            var unit = _codec.ExtractField(raw, ',', 1);
            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)
                ? new PressureValue { Value = v, Unit = unit } : new PressureValue();
        }
        private bool PB(string t) => t.Trim() == "1";

        // === 2.1 IEEE488.2 ===
        public Task<string> GetIdentificationAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("*IDN"), r => _codec.ExtractString(r), ct);
        public Task ClearErrorsAsync(CancellationToken ct = default) => SendNonQueryAsync(Command.Write("*CLS"), ct);
        public Task ResetAsync(CancellationToken ct = default) => SendNonQueryAsync(Command.Write("*RST"), ct);

        // === 2.2 压力通用 ===
        public Task<string> GetModuleUnitAsync(int moduleId, CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:MODule:UNIT", moduleId.ToString()), r => _codec.ExtractString(r), ct);
        public Task SetModuleUnitAsync(int moduleId, string unit, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:MODule:UNIT", moduleId.ToString(), unit), ct);
        public Task<string> GetModuleUnitListAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:MODule:UNIT:LIST"), r => _codec.ExtractString(r), ct);
        public Task<int> GetModuleResolutionAsync(int moduleId, CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:MODule:RESOlution", moduleId.ToString()), r => { var t = _codec.ExtractString(r); return int.TryParse(t, out var v) ? v : -1; }, ct);
        public Task SetModuleResolutionAsync(int moduleId, int value, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:MODule:RESOlution", moduleId.ToString(), value.ToString()), ct);
        public Task ModuleZeroAsync(int moduleId, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:MODule:ZERO", moduleId.ToString()), ct);
        public Task ModuleZeroCancelAsync(int moduleId, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:MODule:ZERO:CANCel", moduleId.ToString()), ct);
        public Task<string> GetModulePressureTypeAsync(int moduleId, CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:MODule:PTYPe", moduleId.ToString()), r => _codec.ExtractString(r), ct);
        public Task<string> GetModuleRangeAsync(int moduleId, CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:MODule:RANGe", moduleId.ToString()), r => _codec.ExtractString(r), ct);
        public Task<string> GetRangeListAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:RANGe:LIST"), r => _codec.ExtractString(r), ct);
        public Task<string> GetRangeIndexAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:RANGe:INDEx"), r => _codec.ExtractString(r), ct);
        public Task SetRangeIndexAsync(string index, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:RANGe:INDEx", index), ct);
        public Task<bool> GetModuleMultiRangeAsync(int moduleId, CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:MODule:MULTirange", moduleId.ToString()), r => PB(_codec.ExtractString(r)), ct);
        public Task<int> GetRangeModeAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:RANGe:MODE"), r => { var t = _codec.ExtractString(r); return int.TryParse(t, out var v) ? v : 0; }, ct);
        public Task SetRangeModeAsync(int mode, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:RANGe:MODE", mode.ToString()), ct);
        public Task<bool> GetModuleOnlineAsync(int moduleId, CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:MODule:ONLIne", moduleId.ToString()), r => PB(_codec.ExtractString(r)), ct);
        public Task<ModuleInfo> GetModuleInfoAsync(int moduleId, CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:MODule:INFO", moduleId.ToString()), r => new ModuleInfo { SerialNumber = _codec.ExtractField(r, ',', 0), Range = _codec.ExtractField(r, ',', 1), PressureType = _codec.ExtractField(r, ',', 2), Version = _codec.ExtractField(r, ',', 3), Accuracy = _codec.ExtractField(r, ',', 4) }, ct);
        public Task<FilterInfo> GetModuleFilterAsync(int moduleId, CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:MODule:FILTer", moduleId.ToString()), r => new FilterInfo { Enabled = _codec.ExtractField(r, ',', 0) == "1", FilterType = int.TryParse(_codec.ExtractField(r, ',', 1), out var ft) ? ft : 0, Value = double.TryParse(_codec.ExtractField(r, ',', 2), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0 }, ct);
        public Task SetModuleFilterAsync(int moduleId, bool enable, int filterType, double value, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:MODule:FILTer", moduleId.ToString(), enable ? "1" : "0", filterType.ToString(), value.ToString(CultureInfo.InvariantCulture)), ct);
        public Task<string[]> GetAllModulePressuresAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:MODUle:VALUes"), r => _codec.ExtractFields(r, '&'), ct);

        // === 2.3 压力测量 ===
        public Task<PressureValue> GetModulePressureAsync(int moduleId, CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:MODule:MEASure", moduleId.ToString()), r => ParsePV(r), ct);

        // === 2.4 压力输出 ===
        public Task<PressureValue> GetOutputPressureAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure"), r => ParsePV(r), ct);
        public Task<string> GetModuleControlStateAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:MODule:CONTrol"), r => _codec.ExtractString(r), ct);
        public Task SetModuleControlStateAsync(string state, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:MODule:CONTrol", state), ct);
        public Task<string> GetControlModeAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:MODE"), r => _codec.ExtractString(r), ct);
        public Task SetControlModeAsync(string mode, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:MODE", mode), ct);
        public Task<TargetPressureRange> GetTargetPressureRangeAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:TARGet:RANGe"), r => { var lo = _codec.ExtractField(r, ',', 0); var hi = _codec.ExtractField(r, ',', 1); var unit = _codec.ExtractField(r, ',', 2); return double.TryParse(lo, NumberStyles.Any, CultureInfo.InvariantCulture, out var low) && double.TryParse(hi, NumberStyles.Any, CultureInfo.InvariantCulture, out var high) ? new TargetPressureRange { Low = low, High = high, Unit = unit } : new TargetPressureRange(); }, ct);
        public Task<PressureValue> GetTargetPressureAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:TARGet"), r => ParsePV(r), ct);
        public Task SetTargetPressureAsync(double value, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:TARGet", value.ToString(CultureInfo.InvariantCulture)), ct);
        public Task<string> GetPressureRangeAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:RANGe"), r => _codec.ExtractString(r), ct);
        public Task<int> GetControlModuleAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:MODule"), r => { var t = _codec.ExtractString(r); return int.TryParse(t, out var v) ? v : -1; }, ct);
        public Task SetControlModuleAsync(int moduleId, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:MODule", moduleId.ToString()), ct);
        public Task<PressureValue> GetVentPressureAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:Vent"), r => ParsePV(r), ct);
        public Task SetVentPressureAsync(double value, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:Vent", value.ToString(CultureInfo.InvariantCulture)), ct);
        public Task<bool> GetPressureLimitEnableAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:PLIMit:ENABle"), r => PB(_codec.ExtractString(r)), ct);
        public Task SetPressureLimitEnableAsync(bool enable, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:PLIMit:ENABle", enable ? "1" : "0"), ct);
        public Task<TargetPressureRange> GetPressureLimitAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:PLIMit"), r => { var lo = _codec.ExtractField(r, ',', 0); var hi = _codec.ExtractField(r, ',', 1); var unit = _codec.ExtractField(r, ',', 2); return double.TryParse(lo, NumberStyles.Any, CultureInfo.InvariantCulture, out var low) && double.TryParse(hi, NumberStyles.Any, CultureInfo.InvariantCulture, out var high) ? new TargetPressureRange { Low = low, High = high, Unit = unit } : new TargetPressureRange(); }, ct);
        public Task SetPressureLimitAsync(double low, double high, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:PLIMit", low.ToString(CultureInfo.InvariantCulture), high.ToString(CultureInfo.InvariantCulture)), ct);
        public Task<PressureTypeInfo> GetPressureTypeAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:TYPE"), r => new PressureTypeInfo { Type = _codec.ExtractField(r, ',', 0), CanSwitch = _codec.ExtractField(r, ',', 1) == "1" }, ct);
        public Task SetPressureTypeAsync(string type, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:TYPE", type), ct);
        public Task<double> GetStepValueAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:STEP"), r => { var t = _codec.ExtractString(r); return double.TryParse(t, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : double.NaN; }, ct);
        public Task SetStepValueAsync(double value, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:STEP", value.ToString(CultureInfo.InvariantCulture)), ct);
        public Task StepUpAsync(CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:STEP:UP"), ct);
        public Task StepDownAsync(CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:STEP:DOWN"), ct);
        public Task<ControlInfo> GetControlInfoAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:CONTrol:INFO"), r => new ControlInfo { Value = double.TryParse(_codec.ExtractField(r, ',', 0), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : double.NaN, Target = double.TryParse(_codec.ExtractField(r, ',', 1), NumberStyles.Any, CultureInfo.InvariantCulture, out var ta) ? ta : double.NaN, Unit = _codec.ExtractField(r, ',', 2), Range = _codec.ExtractField(r, ',', 3), PressureType = _codec.ExtractField(r, ',', 4), IsStable = _codec.ExtractField(r, ',', 5) == "1", State = _codec.ExtractField(r, ',', 6), ExtendInfo = _codec.ExtractField(r, ',', 7) }, ct);
        public Task<int> GetControlModeTypeAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:CONTrol:MODE"), r => { var t = _codec.ExtractString(r); return int.TryParse(t, out var v) ? v : -1; }, ct);
        public Task SetControlModeTypeAsync(int mode, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:CONTrol:MODE", mode.ToString()), ct);
        public Task<SlewRateInfo> GetSlewRateAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:CONTrol:SLEWrate"), r => new SlewRateInfo { Type = int.TryParse(_codec.ExtractField(r, ',', 0), out var ty) ? ty : 0, Value = _codec.ExtractField(r, ',', 1), Unit = _codec.ExtractField(r, ',', 2) }, ct);
        public Task SetSlewRateMaxAsync(CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:CONTrol:SLEWrate:MAX"), ct);
        public Task SetSlewRateLimitAsync(double value, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:CONTrol:SLEWrate:LIMIt", value.ToString(CultureInfo.InvariantCulture)), ct);
        public Task<StabilityInfo> GetStabilityAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:CONTrol:STABility"), r => new StabilityInfo { Type = int.TryParse(_codec.ExtractField(r, ',', 0), out var ty) ? ty : 0, Value = double.TryParse(_codec.ExtractField(r, ',', 1), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0, Unit = _codec.ExtractField(r, ',', 2), PercentValue = double.TryParse(_codec.ExtractField(r, ',', 3), NumberStyles.Any, CultureInfo.InvariantCulture, out var pv) ? pv : 0, PercentUnit = _codec.ExtractField(r, ',', 4), Seconds = int.TryParse(_codec.ExtractField(r, ',', 5), out var s) ? s : 0 }, ct);
        public Task SetStabilityAsync(int type, double value, int seconds, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:CONTrol:STABility", type.ToString(), value.ToString(CultureInfo.InvariantCulture), seconds.ToString()), ct);
        public Task<HeightCorrectionInfo> GetHeightCorrectionAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:CONTrol:HEIGht:CORRection"), r => new HeightCorrectionInfo { Enabled = _codec.ExtractField(r, ',', 0) == "1", UnitType = int.TryParse(_codec.ExtractField(r, ',', 1), out var ut) ? ut : 0, Height = double.TryParse(_codec.ExtractField(r, ',', 2), NumberStyles.Any, CultureInfo.InvariantCulture, out var h) ? h : 0, Density = double.TryParse(_codec.ExtractField(r, ',', 3), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0, Gravity = double.TryParse(_codec.ExtractField(r, ',', 4), NumberStyles.Any, CultureInfo.InvariantCulture, out var g) ? g : 0, Temperature = double.TryParse(_codec.ExtractField(r, ',', 5), NumberStyles.Any, CultureInfo.InvariantCulture, out var te) ? te : 0 }, ct);
        public Task SetHeightCorrectionAsync(bool enable, int unitType, double height, double density, double gravity, double temperature, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:CONTrol:HEIGht:CORRection", enable ? "1" : "0", unitType.ToString(), height.ToString(CultureInfo.InvariantCulture), density.ToString(CultureInfo.InvariantCulture), gravity.ToString(CultureInfo.InvariantCulture), temperature.ToString(CultureInfo.InvariantCulture)), ct);
        public Task<TareInfo> GetTareAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:CONTrol:TARE"), r => new TareInfo { Enabled = _codec.ExtractField(r, ',', 0) == "1", Value = double.TryParse(_codec.ExtractField(r, ',', 1), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0 }, ct);
        public Task SetTareAsync(bool enable, double value, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:CONTrol:TARE", enable ? "1" : "0", value.ToString(CultureInfo.InvariantCulture)), ct);
        public Task<int> GetSwitchTypeAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:SWITch:TYPE"), r => { var t = _codec.ExtractString(r); return int.TryParse(t, out var v) ? v : -1; }, ct);
        public Task SetSwitchTypeAsync(int type, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:SWITch:TYPE", type.ToString()), ct);
        public Task<SwitchValueInfo> GetSwitchValueAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:SWITch:VALUe"), r => { var groups = _codec.ExtractFields(r, '&'); var res = new SwitchValueInfo(); if (groups.Length >= 1) { var closeParts = groups[0].Split(','); if (closeParts.Length >= 2) { res.CloseValue = double.TryParse(closeParts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var cv) ? cv : 0; res.CloseUnit = closeParts[1].Trim(); } } if (groups.Length >= 2) { var openParts = groups[1].Split(','); if (openParts.Length >= 2) { res.OpenValue = double.TryParse(openParts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var ov) ? ov : 0; res.OpenUnit = openParts[1].Trim(); } } return res; }, ct);
        public Task ResetSwitchValueAsync(CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:SWITch:VALUe:RESEt"), ct);
        public Task<ExtendedInterfaceState> GetExtendedInterfaceStateAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:EXTEnd:INTErface:STATe"), r => new ExtendedInterfaceState { Cps = _codec.ExtractField(r, ',', 0) == "1", Drv1 = _codec.ExtractField(r, ',', 1) == "1", Drv2 = _codec.ExtractField(r, ',', 2) == "1", Do1 = _codec.ExtractField(r, ',', 3) == "1", Do2 = _codec.ExtractField(r, ',', 4) == "1", Do3 = _codec.ExtractField(r, ',', 5) == "1", Dc24 = _codec.ExtractField(r, ',', 6) == "1", Switch = _codec.ExtractField(r, ',', 7) == "1" }, ct);
        public Task<ExtendedInterfaceModeInfo> GetExtendedInterfaceModeAsync(int type, CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:EXTEnd:INTErface:MODE", type.ToString()), r => { var groups = _codec.ExtractFields(r, '&'); var res = new ExtendedInterfaceModeInfo(); if (groups.Length >= 1) res.CurrentMode = int.TryParse(groups[0], out var m) ? m : 0; if (groups.Length >= 2) res.AvailableModes = groups[1].Split(',').Select(s => int.TryParse(s.Trim(), out var v) ? v : 0).ToArray(); return res; }, ct);
        public Task SetExtendedInterfaceModeAsync(int type, int mode, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:EXTEnd:INTErface:MODE", type.ToString(), mode.ToString()), ct);
        public Task SetExtendedInterfaceRemoteAsync(int type, bool value, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:EXTEnd:INTErface:Remote", type.ToString(), value ? "1" : "0"), ct);
        public Task<bool> GetAutoZeroAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:AZERo"), r => PB(_codec.ExtractString(r)), ct);
        public Task SetAutoZeroAsync(bool enable, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:AZERo", enable ? "1" : "0"), ct);
        public Task<int> GetZeroPointStrategyAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:ZERO:POINt:STRAtegy"), r => { var t = _codec.ExtractString(r); return int.TryParse(t, out var v) ? v : -1; }, ct);
        public Task SetZeroPointStrategyAsync(int strategy, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:ZERO:POINt:STRAtegy", strategy.ToString()), ct);
        public Task<bool> GetPressureStableAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:STABle"), r => PB(_codec.ExtractString(r)), ct);
        public Task<AtmosphericPressure> GetFixedAtmosphericPressureAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:FIXEd:ATM"), r => { var value = _codec.ExtractField(r, ',', 0); var unit = _codec.ExtractField(r, ',', 1); return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? new AtmosphericPressure { Value = v, Unit = unit } : new AtmosphericPressure(); }, ct);
        public Task SetFixedAtmosphericPressureAsync(double value, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:FIXEd:ATM", value.ToString(CultureInfo.InvariantCulture)), ct);
        public Task<int> GetMediumNameAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("PRESsure:MEDIum:NAME"), r => { var t = _codec.ExtractString(r); return int.TryParse(t, out var v) ? v : -1; }, ct);
        public Task SetMediumNameAsync(int medium, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("PRESsure:MEDIum:NAME", medium.ToString()), ct);

        // === 2.5 界面 ===
        public Task GoHomeAsync(CancellationToken ct = default) => SendNonQueryAsync(Command.Write("SYSTem:HOME"), ct);
        public Task<bool> GetLockAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("SYSTem:LOCK"), r => PB(_codec.ExtractString(r)), ct);
        public Task SetLockAsync(bool enable, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("SYSTem:LOCK", enable ? "1" : "0"), ct);

        // === 2.6 通讯 ===
        public Task<bool> GetWlanStateAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("SYSTem:WLAN:STATe"), r => PB(_codec.ExtractString(r)), ct);
        public Task SetWlanStateAsync(bool enable, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("SYSTem:WLAN:STATe", enable ? "1" : "0"), ct);
        public Task<string> GetWlanAddressAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("SYSTem:WLAN:ADDRess"), r => _codec.ExtractString(r), ct);
        public Task SetWlanAddressAsync(string ip, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("SYSTem:WLAN:ADDRess", ip), ct);
        public Task<string> GetWlanMaskAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("SYSTem:WLAN:MASK"), r => _codec.ExtractString(r), ct);
        public Task SetWlanMaskAsync(string mask, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("SYSTem:WLAN:MASK", mask), ct);
        public Task<string> GetWlanGatewayAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("SYSTem:WLAN:GATeway"), r => _codec.ExtractString(r), ct);
        public Task SetWlanGatewayAsync(string gateway, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("SYSTem:WLAN:GATeway", gateway), ct);
        public Task<bool> GetWlanDhcpAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("SYSTem:WLAN:DHCP"), r => PB(_codec.ExtractString(r)), ct);
        public Task SetWlanDhcpAsync(bool enable, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("SYSTem:WLAN:DHCP", enable ? "1" : "0"), ct);
        public Task<string> GetWlanMacAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("SYSTem:WLAN:MAC"), r => _codec.ExtractString(r), ct);
        public Task<string> GetWlanSsidAsync(string all = null, CancellationToken ct = default) => SendForResultAsync(Command.Read("SYSTem:WLAN:SSID", all), r => _codec.ExtractString(r), ct);
        public Task ConnectWlanAsync(string name, string password = null, CancellationToken ct = default) => SendNonQueryAsync(password != null ? Command.Write("SYSTem:WLAN:CONNect", name, password) : Command.Write("SYSTem:WLAN:CONNect", name), ct);
        public Task DisconnectWlanAsync(CancellationToken ct = default) => SendNonQueryAsync(Command.Write("SYSTem:WLAN:DISConnect"), ct);
        public Task<string> GetEthernetAddressAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("SYSTem:ETHernet:ADDRess"), r => _codec.ExtractString(r), ct);
        public Task SetEthernetAddressAsync(string ip, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("SYSTem:ETHernet:ADDRess", ip), ct);
        public Task<string> GetEthernetMaskAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("SYSTem:ETHernet:MASK"), r => _codec.ExtractString(r), ct);
        public Task SetEthernetMaskAsync(string mask, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("SYSTem:ETHernet:MASK", mask), ct);
        public Task<string> GetEthernetGatewayAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("SYSTem:ETHernet:GATeway"), r => _codec.ExtractString(r), ct);
        public Task SetEthernetGatewayAsync(string gateway, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("SYSTem:ETHernet:GATeway", gateway), ct);
        public Task<bool> GetEthernetDhcpAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("SYSTem:ETHernet:DHCP"), r => PB(_codec.ExtractString(r)), ct);
        public Task SetEthernetDhcpAsync(bool enable, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("SYSTem:ETHernet:DHCP", enable ? "1" : "0"), ct);
        public Task<string> GetEthernetMacAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("SYSTem:ETHernet:MAC"), r => _codec.ExtractString(r), ct);
        public Task<Rs232Info> GetRs232InfoAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("SYSTem:RS232:Info"), r => new Rs232Info { BaudRate = int.TryParse(_codec.ExtractField(r, ',', 0), out var br) ? br : 0, DataBits = int.TryParse(_codec.ExtractField(r, ',', 1), out var db) ? db : 0, StopBits = _codec.ExtractField(r, ',', 2), Parity = _codec.ExtractField(r, ',', 3) }, ct);
        public Task SetRs232InfoAsync(int baudRate, int dataBits, string stopBits, string parity, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("SYSTem:RS232:Info", baudRate.ToString(), dataBits.ToString(), stopBits, parity), ct);

        // === 2.7 系统指令 ===
        public Task<string> GetSystemTimeAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("SYSTem:TIME"), r => _codec.ExtractString(r), ct);
        public Task SetSystemTimeAsync(int hour, int minute, int second, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("SYSTem:TIME", hour.ToString(), minute.ToString(), second.ToString()), ct);
        public Task<string> GetSystemDateAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("SYSTem:DATE"), r => _codec.ExtractString(r), ct);
        public Task SetSystemDateAsync(int year, int month, int day, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("SYSTem:DATE", year.ToString(), month.ToString(), day.ToString()), ct);
        public Task<int> GetTimeFormatAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("SYSTem:TIME:FORMat"), r => { var t = _codec.ExtractString(r); return int.TryParse(t, out var v) ? v : -1; }, ct);
        public Task SetTimeFormatAsync(int format, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("SYSTem:TIME:FORMat", format.ToString()), ct);
        public Task<int> GetDateFormatAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("SYSTem:DATE:FORMat"), r => { var t = _codec.ExtractString(r); return int.TryParse(t, out var v) ? v : -1; }, ct);
        public Task SetDateFormatAsync(int format, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("SYSTem:DATE:FORMat", format.ToString()), ct);
        public Task<string> GetDateSeparatorAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("SYSTem:DATE:SEParator"), r => _codec.ExtractString(r), ct);
        public Task SetDateSeparatorAsync(int separator, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("SYSTem:DATE:SEParator", separator.ToString()), ct);
        public Task<int> GetVolumeAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("SYSTem:VOLUme"), r => { var t = _codec.ExtractString(r); return int.TryParse(t, out var v) ? v : -1; }, ct);
        public Task SetVolumeAsync(int volume, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("SYSTem:VOLUme", volume.ToString()), ct);
        public Task<bool> GetTouchSoundAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("SYSTem:VOLUme:TOUCH"), r => PB(_codec.ExtractString(r)), ct);
        public Task SetTouchSoundAsync(bool enable, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("SYSTem:VOLUme:TOUCH", enable ? "1" : "0"), ct);
        public Task<bool> GetPromptSoundAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("SYSTem:VOLUme:PROMpt"), r => PB(_codec.ExtractString(r)), ct);
        public Task SetPromptSoundAsync(bool enable, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("SYSTem:VOLUme:PROMpt", enable ? "1" : "0"), ct);
        public Task<bool> GetOverrangeSoundAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("SYSTem:VOLUme:OVERrange"), r => PB(_codec.ExtractString(r)), ct);
        public Task SetOverrangeSoundAsync(bool enable, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("SYSTem:VOLUme:OVERrange", enable ? "1" : "0"), ct);
        public Task<int> GetBrightnessAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("SYSTem:BRIGhtness"), r => { var t = _codec.ExtractString(r); return int.TryParse(t, out var v) ? v : -1; }, ct);
        public Task SetBrightnessAsync(int brightness, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("SYSTem:BRIGhtness", brightness.ToString()), ct);
        public Task<string> GetVersionAsync(string module = "APPLication", CancellationToken ct = default) => SendForResultAsync(Command.Read("SYSTem:VERSion", module), r => _codec.ExtractString(r), ct);
        public Task<string> GetLanguageAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("SYSTem:LANGuage"), r => _codec.ExtractString(r), ct);
        public Task SetLanguageAsync(string language, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("SYSTem:LANGuage", language), ct);

        // === 2.8 电测指令 ===
        public Task<int> GetMeasureFunctionAsync(string all = null, CancellationToken ct = default) => SendForResultAsync(Command.Read("MEASure:FUNCtion", all), r => { var t = _codec.ExtractString(r); return int.TryParse(t, out var v) ? v : -1; }, ct);
        public Task SetMeasureFunctionAsync(int function, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("MEASure:FUNCtion", function.ToString()), ct);
        public Task<int> GetMeasureResolutionAsync(int sw, CancellationToken ct = default) => SendForResultAsync(Command.Read("MEASure:CONFig:RESOlution", sw.ToString()), r => { var t = _codec.ExtractString(r); return int.TryParse(t, out var v) ? v : -1; }, ct);
        public Task SetMeasureResolutionAsync(int sw, int digital, CancellationToken ct = default) => SendNonQueryAsync(Command.Write("MEASure:CONFig:RESOlution", sw.ToString(), digital.ToString()), ct);
        public Task<double> GetMeasureValueAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("MEASure"), r => { var t = _codec.ExtractString(r); return double.TryParse(t, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : double.NaN; }, ct);
        public Task MeasureZeroAsync(CancellationToken ct = default) => SendNonQueryAsync(Command.Write("MEASure:ZERO"), ct);
        public Task MeasureZeroCancelAsync(CancellationToken ct = default) => SendNonQueryAsync(Command.Write("MEASure:ZERO:CANCel"), ct);

        // === 错误查询 ===
        public Task<string> GetErrorAsync(CancellationToken ct = default) => SendForResultAsync(Command.Read("SYSTem:ERRor"), r => _codec.ExtractString(r), ct);
    }
}