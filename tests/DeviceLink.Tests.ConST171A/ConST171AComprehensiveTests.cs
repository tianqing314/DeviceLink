using DeviceLink.Device.ConST171A;
using DeviceLink.DeviceBase;
using System.IO.Ports;
using System.Threading.Tasks;
using Xunit;

using ConST171ADevice = DeviceLink.Device.ConST171A.ConST171Base;

namespace DeviceLink.Tests.ConST171A
{
    /// <summary>
    /// ConST171A 全指令集成测试
    /// 
    /// 依赖真实硬件设备，串口参数见下方常量。
    /// 运行方式：dotnet test （一条命令验证全部指令）
    /// 
    /// ⚠ SET 类指令执行后不会自动恢复设备状态，
    ///    Sweep 测试（SetXxx → GetXxx 配对）会在测试内恢复原值。
    /// </summary>
    public class ConST171AComprehensiveTests
    {
        private const string TestPortName = "COM7";
        private const int TestBaudRate = 115200;
        private const int TestDataBits = 8;
        private const StopBits TestStopBits = StopBits.One;
        private const Parity TestParity = Parity.None;

        private ConST171ADevice CreateDevice()
        {
            return new ConST171ADevice(TestPortName, TestBaudRate, TestDataBits, TestStopBits, TestParity);
        }

        #region 通用指令 —— IEEE488.2 共同指令

        [Fact]
        public async Task GetIdentificationAsync_ShouldReturnValidIdentification()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var id = await dev.GetIdentificationAsync();
            Assert.True(id.IsValid);
            Assert.NotEmpty(id.Manufacturer);
            Assert.NotEmpty(id.Model);
        }

        [Fact]
        public async Task ClearErrorsAsync_ShouldNotThrow()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            await dev.ClearErrorsAsync();
        }

        [Fact]
        public async Task ResetAsync_ShouldNotThrow()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            await dev.ResetAsync();
        }

        #endregion

        #region 通用指令 —— SYSTem 系统指令

        [Fact]
        public async Task GetManufacturerAsync_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var val = await dev.GetManufacturerAsync();
            Assert.NotEmpty(val);
        }

        [Fact]
        public async Task Sweep_Manufacturer_SetAndGet()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var original = await dev.GetManufacturerAsync();
            await dev.SetManufacturerAsync("TestMFR");
            var readback = await dev.GetManufacturerAsync();
            Assert.Equal("TestMFR", readback);
            await dev.SetManufacturerAsync(original);
        }

        [Fact]
        public async Task GetModelAsync_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var val = await dev.GetModelAsync();
            Assert.NotEmpty(val);
        }

        [Fact]
        public async Task Sweep_Model_SetAndGet()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var original = await dev.GetModelAsync();
            await dev.SetModelAsync("TestModel");
            var readback = await dev.GetModelAsync();
            Assert.Equal("TestModel", readback);
            await dev.SetModelAsync(original);
        }

        [Fact]
        public async Task GetSerialNumberAsync_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var val = await dev.GetSerialNumberAsync();
            Assert.NotEmpty(val);
        }

        [Fact]
        public async Task Sweep_SerialNumber_SetAndGet()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var original = await dev.GetSerialNumberAsync();
            await dev.SetSerialNumberAsync("TestSN");
            var readback = await dev.GetSerialNumberAsync();
            Assert.Equal("TestSN", readback);
            await dev.SetSerialNumberAsync(original);
        }

        [Fact]
        public async Task GetVersionAsync_ShouldReturnAllModules()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var ver = await dev.GetVersionAsync();
            Assert.NotEmpty(ver.Firmware);
        }

        [Fact]
        public async Task GetVersionAsync_WithBootModule_ShouldReturnVersion()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var ver = await dev.GetVersionAsync(VersionModules.Boot);
            Assert.NotEmpty(ver);
        }

        [Fact]
        public async Task GetVersionAsync_WithFirmModule_ShouldReturnVersion()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var ver = await dev.GetVersionAsync(VersionModules.Firmware);
            Assert.NotEmpty(ver);
        }

        [Fact]
        public async Task GetVersionAsync_WithHardModule_ShouldReturnVersion()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var ver = await dev.GetVersionAsync(VersionModules.Hardware);
            Assert.NotEmpty(ver);
        }

        [Fact]
        public async Task GetVersionAsync_WithDmModule_ShouldReturnVersion()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var ver = await dev.GetVersionAsync(VersionModules.DisplayModule);
            Assert.NotEmpty(ver);
        }

        [Fact]
        public async Task GetRs232InfoAsync_ShouldReturnValidSettings()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var rs232 = await dev.GetRs232InfoAsync();
            Assert.True(rs232.BaudRate > 0);
            Assert.True(rs232.DataBits >= 7);
            Assert.NotEmpty(rs232.StopBits);
            Assert.NotEmpty(rs232.Parity);
        }

        [Fact]
        public async Task GetErrorAsync_ShouldReturnNoError()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var err = await dev.GetErrorAsync();
            Assert.NotNull(err);
            Assert.False(err.IsError, $"设备报告错误: {err}");
        }

        [Fact]
        public async Task GoHomeAsync_ShouldNotThrow()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            await dev.GoHomeAsync();
        }

        [Fact]
        public async Task GetLockAsync_ShouldReturnBool()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var locked = await dev.GetLockAsync();
            Assert.IsType<bool>(locked);
        }

        [Fact]
        public async Task Sweep_Lock_SetAndGet()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var original = await dev.GetLockAsync();
            await dev.SetLockAsync(true);
            Assert.True(await dev.GetLockAsync());
            await dev.SetLockAsync(original);
        }

        [Fact]
        public async Task GetSoundAsync_ShouldReturnBool()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var sound = await dev.GetSoundAsync();
            Assert.IsType<bool>(sound);
        }

        [Fact]
        public async Task Sweep_Sound_SetAndGet()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var original = await dev.GetSoundAsync();
            await dev.SetSoundAsync(true);
            Assert.True(await dev.GetSoundAsync());
            await dev.SetSoundAsync(original);
        }

        [Fact]
        public async Task GetBrightnessAsync_ShouldBeInRange()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var brightness = await dev.GetBrightnessAsync();
            Assert.InRange(brightness, 0, 100);
        }

        [Fact]
        public async Task Sweep_Brightness_SetAndGet()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var original = await dev.GetBrightnessAsync();
            await dev.SetBrightnessAsync(50);
            Assert.Equal(50, await dev.GetBrightnessAsync());
            await dev.SetBrightnessAsync(original);
        }

        [Fact]
        public async Task GetLanguageAsync_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var lang = await dev.GetLanguageAsync();
            Assert.NotEmpty(lang);
        }

        [Fact]
        public async Task Sweep_Language_SetAndGet()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var original = await dev.GetLanguageAsync();
            await dev.SetLanguageAsync("en-US");
            Assert.Equal("en-US", await dev.GetLanguageAsync());
            await dev.SetLanguageAsync(original);
        }

        #endregion

        #region 压力控制指令

        [Fact]
        public async Task GetPressureAsync_ShouldReturnDualPressure()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var val = await dev.GetPressureAsync();
            Assert.True(val.IsValid, "双气源压力值应有效");
            Assert.False(double.IsNaN(val.PositiveValue), "正压值应有效");
            Assert.False(double.IsNaN(val.VacuumValue), "真空值应有效");
            Assert.NotEmpty(val.PositiveUnit);
            Assert.NotEmpty(val.VacuumUnit);
        }

        [Fact]
        public async Task GetPressureAsync_WithPressureSource_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var val = await dev.GetPressureAsync(SourceModule.Pressure);
            Assert.False(double.IsNaN(val.Value));
            Assert.NotEmpty(val.Unit);
        }

        [Fact]
        public async Task GetPressureAsync_WithVacuumSource_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var val = await dev.GetPressureAsync(SourceModule.Vacuum);
            Assert.False(double.IsNaN(val.Value));
            Assert.NotEmpty(val.Unit);
        }

        [Fact]
        public async Task GetPressureControlStateAsync_ShouldReturnBool()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var state = await dev.GetPressureControlStateAsync(SourceModule.Pressure);
            Assert.IsType<bool>(state);
        }

        [Fact]
        public async Task GetPressureUnitAsync_WithPressureSource_ShouldReturnUnit()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var unit = await dev.GetPressureUnitAsync(SourceModule.Pressure);
            Assert.NotEmpty(unit);
        }

        [Fact]
        public async Task GetPressureUnitAsync_WithVacuumSource_ShouldReturnUnit()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var unit = await dev.GetPressureUnitAsync(SourceModule.Vacuum);
            Assert.NotEmpty(unit);
        }

        [Fact]
        public async Task Sweep_PressureUnit_SetAndGet()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var original = await dev.GetPressureUnitAsync(SourceModule.Pressure);
            await dev.SetPressureUnitAsync(SourceModule.Pressure, "kPa");
            Assert.Equal("kPa", await dev.GetPressureUnitAsync(SourceModule.Pressure));
            await dev.SetPressureUnitAsync(SourceModule.Pressure, original);
        }

        [Fact]
        public async Task GetPressureRangeAsync_ShouldReturnValidRange()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var range = await dev.GetPressureRangeAsync(SourceModule.Pressure);
            Assert.True(range.IsValid);
            Assert.True(range.Max >= range.Min);
        }

        [Fact]
        public async Task Sweep_PressureRange_SetAndGet()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var original = await dev.GetPressureRangeAsync(SourceModule.Pressure);
            await dev.SetPressureRangeAsync(SourceModule.Pressure, 0, 1000);
            var readback = await dev.GetPressureRangeAsync(SourceModule.Pressure);
            Assert.Equal(0, readback.Min, 1);
            Assert.Equal(1000, readback.Max, 1);
            await dev.SetPressureRangeAsync(SourceModule.Pressure, original.Min, original.Max);
        }

        // ---- 内部 - 压力配置

        [Fact]
        public async Task GetRawPressureAsync_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var val = await dev.GetRawPressureAsync(SourceModule.Pressure);
            Assert.False(double.IsNaN(val.Value));
            Assert.NotEmpty(val.Unit);
        }

        [Fact]
        public async Task GetMuteAsync_ShouldReturnBool()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            Assert.IsType<bool>(await dev.GetMuteAsync());
        }

        [Fact]
        public async Task Sweep_Mute_SetAndGet()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var original = await dev.GetMuteAsync();
            await dev.SetMuteAsync(true);
            Assert.True(await dev.GetMuteAsync());
            await dev.SetMuteAsync(original);
        }

        [Fact]
        public async Task GetAdjAsync_ShouldReturnBool()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            Assert.IsType<bool>(await dev.GetAdjAsync());
        }

        [Fact]
        public async Task Sweep_Adj_SetAndGet()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var original = await dev.GetAdjAsync();
            await dev.SetAdjAsync(true);
            Assert.True(await dev.GetAdjAsync());
            await dev.SetAdjAsync(original);
        }

        [Fact]
        public async Task GetVacuumVentAsync_ShouldReturnBool()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            Assert.IsType<bool>(await dev.GetVacuumVentAsync());
        }

        [Fact]
        public async Task Sweep_VacuumVent_SetAndGet()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var original = await dev.GetVacuumVentAsync();
            await dev.SetVacuumVentAsync(true);
            Assert.True(await dev.GetVacuumVentAsync());
            await dev.SetVacuumVentAsync(original);
        }

        #endregion

        #region 私有指令 —— 校准

        [Fact]
        public async Task GetCalibrationDataAsync_ShouldReturnRecord()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var cal = await dev.GetCalibrationDataAsync(SourceModule.Pressure, "123456", 0);
            Assert.NotNull(cal);
            Assert.True(cal.StandardValues.Length > 0);
        }

        [Fact]
        public async Task GetCalibrationDataAsync_MultiPoint_ShouldReturnRecord()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var cal = await dev.GetCalibrationDataAsync(SourceModule.Pressure, "123456", 1);
            Assert.NotNull(cal);
            Assert.True(cal.StandardValues.Length > 0);
        }

        #endregion

        #region 私有指令 —— 诊断 DIAGnostic

        [Fact]
        public async Task GetDiagSerialNumberAsync_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            Assert.NotEmpty(await dev.GetDiagSerialNumberAsync());
        }

        [Fact]
        public async Task GetDiagModelAsync_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            Assert.NotEmpty(await dev.GetDiagModelAsync());
        }

        [Fact]
        public async Task GetDiagManufacturerAsync_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            Assert.NotEmpty(await dev.GetDiagManufacturerAsync());
        }

        [Fact]
        public async Task GetManufactureDateAsync_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            Assert.NotEmpty(await dev.GetManufactureDateAsync());
        }

        [Fact]
        public async Task GetLogoAsync_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var logo = await dev.GetLogoAsync();
            Assert.True(logo >= 0);
        }

        [Fact]
        public async Task GetFanSpeedAsync_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var speed = await dev.GetFanSpeedAsync(SourceModule.Pressure);
            Assert.True(speed >= 0);
        }

        [Fact]
        public async Task GetFanSpeedAsync_WithVacuum_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var speed = await dev.GetFanSpeedAsync(SourceModule.Vacuum);
            Assert.True(speed >= 0);
        }

        [Fact]
        public async Task GetVentAsync_ShouldReturnBool()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            Assert.IsType<bool>(await dev.GetVentAsync());
        }

        [Fact]
        public async Task GetValveAsync_ShouldReturnBool()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            Assert.IsType<bool>(await dev.GetValveAsync(ValveIds.BoostV1));
        }

        [Fact]
        public async Task GetBoardTemperatureAsync_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var temp = await dev.GetBoardTemperatureAsync();
            Assert.False(double.IsNaN(temp));
            Assert.InRange(temp, -10, 100);
        }

        [Fact]
        public async Task GetBoardVoltageAsync_ShouldReturnValidVoltage()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var v = await dev.GetBoardVoltageAsync();
            Assert.False(double.IsNaN(v.Voltage24V));
            Assert.False(double.IsNaN(v.BoostSensorVoltage));
            Assert.False(double.IsNaN(v.VacuumSensorVoltage));
        }

        [Fact]
        public async Task GetPumpStateAsync_ShouldReturnBool()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            Assert.IsType<bool>(await dev.GetPumpStateAsync(SourceModule.Pressure));
        }

        [Fact]
        public async Task GetFocStateAsync_ShouldReturnState()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var foc = await dev.GetFocStateAsync();
            Assert.NotNull(foc);
            Assert.IsType<bool>(foc.PreStageOk);
            Assert.IsType<bool>(foc.BoostOk);
        }

        [Fact]
        public async Task GetPumpTemperatureAsync_ShouldReturnValues()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var t = await dev.GetPumpTemperatureAsync();
            Assert.False(double.IsNaN(t.PreStagePump));
            Assert.False(double.IsNaN(t.BoostPump));
        }

        [Fact]
        public async Task GetPumpCurrentAsync_ShouldReturnValues()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var c = await dev.GetPumpCurrentAsync();
            Assert.False(double.IsNaN(c.PreStagePump));
            Assert.False(double.IsNaN(c.BoostPump));
        }

        #endregion

        #region 私有指令 —— 测试 TEST

        [Fact]
        public async Task Sweep_TestMode_SetAndGet()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var original = await dev.GetTestModeAsync();
            await dev.SetTestModeAsync(true);
            Assert.True(await dev.GetTestModeAsync());
            await dev.SetTestModeAsync(original);
        }

        [Fact]
        public async Task Sweep_Beep_SetAndGet()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            // 蜂鸣器没有独立的查询指令，仅验证 Set 不抛异常
            await dev.SetBeepAsync(true);
            await dev.SetBeepAsync(false);
        }

        [Fact]
        public async Task GetBlowTestStateAsync_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var state = await dev.GetBlowTestStateAsync();
            Assert.InRange(state, 0, 2);
        }

        #endregion

        #region 危险操作（默认跳过，需手动启用）

        [Fact(Skip = "⚠ 会重启设备，手动启用")]
        public async Task RestartAsync_RebootsDevice()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            await dev.RestartAsync();
        }

        [Fact(Skip = "⚠ 会恢复出厂设置，手动启用")]
        public async Task FactoryResetAsync_ResetsDevice()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            await dev.FactoryResetAsync();
        }

        [Fact(Skip = "⚠ 会改变串口参数导致通信中断，手动启用")]
        public async Task SetRs232InfoAsync_ChangesBaudRate()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            await dev.SetRs232InfoAsync(9600, 8, "One", "None");
        }

        [Fact(Skip = "⚠ 会改变 MCU 波特率，手动启用")]
        public async Task SetMcuBaudrateAsync_ChangesBaudRate()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            await dev.SetMcuBaudrateAsync(115200);
        }

        [Fact(Skip = "⚠ 会执行软重启，手动启用")]
        public async Task SoftRebootAsync_RebootsDevice()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            await dev.SoftRebootAsync(1000);
        }

        [Fact(Skip = "⚠ 校准操作会改变设备数据，手动启用")]
        public async Task StartStopCalibrationAsync_ChangesState()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            await dev.StartCalibrationAsync(SourceModule.Pressure);
            await dev.StopCalibrationAsync(SourceModule.Pressure);
        }

        [Fact(Skip = "⚠ 校准写入会修改设备校准数据，手动启用")]
        public async Task SetCalibrationDataAsync_WritesData()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            await dev.SetCalibrationDataAsync(
                SourceModule.Pressure, "123456", 1, "0", "0.01", 2026, 1, 1);
        }

        [Fact(Skip = "⚠ 会清除校准数据，手动启用")]
        public async Task ResetCalibrationDataAsync_ResetsData()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var result = await dev.ResetCalibrationDataAsync(SourceModule.Pressure, "123456", 0);
            Assert.NotNull(result);
        }

        [Fact(Skip = "⚠ 会启动吹扫测试（物理动作），手动启用")]
        public async Task StartBlowTestAsync_StartsBlow()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            await dev.StartBlowTestAsync(true);
        }

        [Fact(Skip = "⚠ 会跳转测试屏幕，手动启用")]
        public async Task ScreenTestCommands_Work()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            await dev.StartScreenTestAsync(0);
            var result = await dev.GetScreenTestResultAsync(0);
            Assert.InRange(result, 0, 3);
        }

        [Fact(Skip = "⚠ OTA 升级操作，手动启用")]
        public async Task OtaCommands_Work()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            await dev.OtaConnectAsync("eyJiYXVkIjo5NjAwfQ==");
            await dev.OtaTransferAsync(1, "data");
            // 不执行 OtaUpdateAsync 以免真正升级
        }

        #endregion
    }
}
