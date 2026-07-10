using DeviceLink.Device.ConST171A;
using DeviceLink.DeviceBase;
using System.IO.Ports;
using Xunit;

using ConST171ADevice = DeviceLink.Device.ConST171A.ConST171Base;

namespace DeviceLink.Tests.ConST171A
{
    /// <summary>
    /// ConST171A 压力控制器设备测试
    /// 
    /// 注意：这些测试需要实际的 ConST171A 设备连接到串口。
    /// 测试使用串口连接，串口参数为 COM7, 115200, 8, 1, None。
    /// 如果设备未连接，测试将失败。
    /// 
    /// 气源参数遵循文档规范：
    ///   Pressure = 正压气源
    ///   Vacuum   = 真空气源
    ///   Pre      = 前级泵
    /// </summary>
    public class ConST171ATests
    {
        private const string TestPortName = "COM7";
        private const int TestBaudRate = 115200;
        private const int TestDataBits = 8;
        private const StopBits TestStopBits = StopBits.Two;
        private const Parity TestParity = Parity.None;

        private ConST171ADevice CreateDevice()
        {
            return new DeviceLink.Device.ConST171A.ConST171Base(TestPortName, TestBaudRate, TestDataBits, TestStopBits, TestParity);
        }

        /// <summary>使用 SerialPortSettings 创建设备（参考对比用）/// </summary>
        private ConST171ADevice CreateDeviceWithSettings()
        {
            var settings = new SerialPortSettings(TestPortName, TestBaudRate, TestDataBits, TestStopBits, TestParity)
            {
                ReceiveTimeoutMs = 15000,
                ReceiveIdleTimeoutMs = 100,
                MaxRetryCount = 2,
                RetryDelayMs = 500
            };
            return new ConST171ADevice(settings);
        }

        // ============================================================
        // IEEE488.2 共同指令测试
        // ============================================================

        [Fact]
        public async Task GetIdentificationAsync_ShouldReturnIdentification()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var id = await device.GetIdentificationAsync();

            // Assert
            Assert.NotNull(id);
            Assert.NotEmpty(id.Manufacturer);
            Assert.NotEmpty(id.Model);
        }

        [Fact]
        public async Task ClearErrorsAsync_ShouldNotThrow()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act & Assert
            await device.ClearErrorsAsync();
        }

        [Fact]
        public async Task ResetAsync_ShouldNotThrow()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act & Assert
            await device.ResetAsync();
        }

        // ============================================================
        // 压力控制测试
        // ============================================================

        [Fact]
        public async Task GetPressureAsync_ShouldReturnDualPressure()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var result = await device.GetPressureAsync();

            // Assert
            // PRESsure? 无参数返回正压+真空双路值：正压值,正压单位,真空值,真空单位
            Assert.True(result.IsValid, "应返回有效的双气源压力值");
            Assert.False(double.IsNaN(result.PositiveValue), "正压值应有效");
            Assert.False(double.IsNaN(result.VacuumValue), "真空值应有效");
            Assert.NotEmpty(result.PositiveUnit);
            Assert.NotEmpty(result.VacuumUnit);
        }

        [Fact]
        public async Task GetPressureAsync_WithSourcePressure_ShouldReturnPressure()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var result = await device.GetPressureAsync(SourceModule.Pressure);

            // Assert
            Assert.False(double.IsNaN(result.Value));
            Assert.NotNull(result.Unit);
            Assert.NotEmpty(result.Unit);
        }

        [Fact]
        public async Task GetPressureAsync_WithSourceVacuum_ShouldReturnPressure()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var result = await device.GetPressureAsync(SourceModule.Vacuum);

            // Assert
            Assert.False(double.IsNaN(result.Value));
            Assert.NotNull(result.Unit);
            Assert.NotEmpty(result.Unit);
        }

        [Fact]
        public async Task GetPressureUnitAsync_WithPressureSource_ShouldReturnUnit()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var unit = await device.GetPressureUnitAsync(SourceModule.Pressure);

            // Assert
            Assert.NotNull(unit);
            Assert.NotEmpty(unit);
        }

        [Fact]
        public async Task GetPressureUnitAsync_WithVacuumSource_ShouldReturnUnit()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var unit = await device.GetPressureUnitAsync(SourceModule.Vacuum);

            // Assert
            Assert.NotNull(unit);
            Assert.NotEmpty(unit);
        }

        [Fact]
        public async Task GetPressureRangeAsync_ShouldReturnPressureRange()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var range = await device.GetPressureRangeAsync(SourceModule.Pressure);

            // Assert
            Assert.NotNull(range);
            Assert.True(range.IsValid, "压力范围应有效");
            Assert.True(range.Max >= range.Min, "上限应大于等于下限");
        }

        [Fact]
        public async Task GetPressureControlStateAsync_ShouldReturnBool()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var state = await device.GetPressureControlStateAsync(SourceModule.Pressure);

            // Assert
            Assert.IsType<bool>(state);
        }

        // ============================================================
        // 系统指令测试
        // ============================================================

        [Fact]
        public async Task GetManufacturerAsync_ShouldReturnManufacturer()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var manufacturer = await device.GetManufacturerAsync();

            // Assert
            Assert.NotNull(manufacturer);
            Assert.NotEmpty(manufacturer);
        }

        [Fact]
        public async Task GetModelAsync_ShouldReturnModel()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var model = await device.GetModelAsync();

            // Assert
            Assert.NotNull(model);
            Assert.NotEmpty(model);
        }
        [Fact]
        public async Task IsExistVerify()
        {
            // Arrange
            using var device = CreateDevice();

            // Act
            await device.OpenAsync();

            // 分步诊断 IsExistAsync
            var isOpen = device.IsOpen;
            Assert.True(isOpen, "OpenAsync 后 IsOpen 应为 true");

            try
            {
                var version = await device.GetVersionAsync();
                var isConST171 = version.IsValid &&
                    (version.Firmware.ToUpperInvariant().Contains("EPU-LP") ||
                     version.Hardware.ToUpperInvariant().Contains("EPU-LP"));

                var result = await device.IsExistAsync();
                Assert.True(isConST171,
                    $"版本信息应包含 EPU-LP。Firmware='{version.Firmware}', Hardware='{version.Hardware}', IsValid={version.IsValid}");
            }
            catch (Exception ex)
            {
                Assert.Fail($"GetVersionAsync 抛出异常: {ex.GetType().Name}: {ex.Message}");
            }
        }

        [Fact]
        public async Task GetSerialNumberAsync_ShouldReturnSerialNumber()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var serialNumber = await device.GetSerialNumberAsync();

            // Assert
            Assert.NotNull(serialNumber);
            Assert.NotEmpty(serialNumber);
        }

        [Fact]
        public async Task GetVersionAsync_ShouldReturnVersionInfo()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var version = await device.GetVersionAsync();

            // Assert
            Assert.NotNull(version);
            Assert.NotEmpty(version.Firmware);
        }

        [Fact]
        public async Task GetVersionAsync_WithBootModule_ShouldReturnBootVersion()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var version = await device.GetVersionAsync(VersionModules.Boot);

            // Assert
            Assert.NotNull(version);
            Assert.NotEmpty(version);
        }

        [Fact]
        public async Task GetVersionAsync_WithFirmModule_ShouldReturnFirmwareVersion()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var version = await device.GetVersionAsync(VersionModules.Firmware);

            // Assert
            Assert.NotNull(version);
            Assert.NotEmpty(version);
        }

        [Fact]
        public async Task GetRs232InfoAsync_ShouldReturnRs232Settings()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var rs232 = await device.GetRs232InfoAsync();

            // Assert
            Assert.NotNull(rs232);
            Assert.True(rs232.BaudRate > 0, "波特率应大于0");
            Assert.True(rs232.DataBits >= 7, "数据位应 >= 7");
            Assert.NotEmpty(rs232.StopBits);
            Assert.NotEmpty(rs232.Parity);
        }

        [Fact]
        public async Task GetErrorAsync_ShouldReturnScpiError()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var error = await device.GetErrorAsync();

            // Assert
            Assert.NotNull(error);
            Assert.IsType<int>(error.Code);
            Assert.NotNull(error.Message);
        }

        [Fact]
        public async Task GetLockAsync_ShouldReturnLockState()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var locked = await device.GetLockAsync();

            // Assert
            Assert.IsType<bool>(locked);
        }

        [Fact]
        public async Task GetSoundAsync_ShouldReturnSoundState()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var sound = await device.GetSoundAsync();

            // Assert
            Assert.IsType<bool>(sound);
        }

        [Fact]
        public async Task GetBrightnessAsync_ShouldReturnBrightness()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var brightness = await device.GetBrightnessAsync();

            // Assert
            Assert.True(brightness >= 0 && brightness <= 100,
                $"亮度值应在 0-100 范围内，实际值: {brightness}");
        }

        [Fact]
        public async Task GetLanguageAsync_ShouldReturnLanguage()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var language = await device.GetLanguageAsync();

            // Assert
            Assert.NotNull(language);
            Assert.NotEmpty(language);
        }

        // ============================================================
        // 诊断指令测试
        // ============================================================

        [Fact]
        public async Task GetDiagSerialNumberAsync_ShouldReturnSerialNumber()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var serialNumber = await device.GetDiagSerialNumberAsync();

            // Assert
            Assert.NotNull(serialNumber);
            Assert.NotEmpty(serialNumber);
        }

        [Fact]
        public async Task GetDiagModelAsync_ShouldReturnModel()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var model = await device.GetDiagModelAsync();

            // Assert
            Assert.NotNull(model);
            Assert.NotEmpty(model);
        }

        [Fact]
        public async Task GetDiagManufacturerAsync_ShouldReturnManufacturer()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var manufacturer = await device.GetDiagManufacturerAsync();

            // Assert
            Assert.NotNull(manufacturer);
            Assert.NotEmpty(manufacturer);
        }

        [Fact]
        public async Task GetManufactureDateAsync_ShouldReturnDate()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var date = await device.GetManufactureDateAsync();

            // Assert
            Assert.NotNull(date);
            Assert.NotEmpty(date);
        }

        [Fact]
        public async Task GetLogoAsync_ShouldReturnLogo()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var logo = await device.GetLogoAsync();

            // Assert
            Assert.True(logo >= 0, $"LOGO 值应 >= 0，实际值: {logo}");
        }

        [Fact]
        public async Task GetFanSpeedAsync_WithPressure_ShouldReturnFanSpeed()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var fanSpeed = await device.GetFanSpeedAsync(SourceModule.Pressure);

            // Assert
            Assert.True(fanSpeed >= 0, $"风扇转速应 >= 0，实际值: {fanSpeed}");
        }

        [Fact]
        public async Task GetFanSpeedAsync_WithVacuum_ShouldReturnFanSpeed()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var fanSpeed = await device.GetFanSpeedAsync(SourceModule.Vacuum);

            // Assert
            Assert.True(fanSpeed >= 0);
        }

        [Fact]
        public async Task GetVentAsync_ShouldReturnVentState()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var vent = await device.GetVentAsync();

            // Assert
            Assert.IsType<bool>(vent);
        }

        [Fact]
        public async Task GetValveAsync_ShouldReturnValveState()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var valve = await device.GetValveAsync(ValveIds.BoostV1);

            // Assert
            Assert.IsType<bool>(valve);
        }

        [Fact]
        public async Task GetBoardTemperatureAsync_ShouldReturnTemperature()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var temperature = await device.GetBoardTemperatureAsync();

            // Assert
            Assert.False(double.IsNaN(temperature), "主板温度应有效");
        }

        [Fact]
        public async Task GetBoardVoltageAsync_ShouldReturnBoardVoltage()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var voltage = await device.GetBoardVoltageAsync();

            // Assert
            Assert.NotNull(voltage);
            Assert.False(double.IsNaN(voltage.Voltage24V), "24V 电压应有效");
            Assert.False(double.IsNaN(voltage.BoostSensorVoltage), "Boost 传感器电压应有效");
            Assert.False(double.IsNaN(voltage.VacuumSensorVoltage), "Vacuum 传感器电压应有效");
        }

        [Fact]
        public async Task GetPumpStateAsync_WithPressure_ShouldReturnPumpState()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var pumpState = await device.GetPumpStateAsync(SourceModule.Pressure);

            // Assert
            Assert.IsType<bool>(pumpState);
        }

        [Fact]
        public async Task GetFocStateAsync_ShouldReturnFocState()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var focState = await device.GetFocStateAsync();

            // Assert
            Assert.NotNull(focState);
            Assert.IsType<bool>(focState.PreStageOk);
            Assert.IsType<bool>(focState.BoostOk);
        }

        [Fact]
        public async Task GetPumpTemperatureAsync_ShouldReturnTemperatures()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var temps = await device.GetPumpTemperatureAsync();

            // Assert
            Assert.NotNull(temps);
            Assert.False(double.IsNaN(temps.PreStagePump), "前级泵温度应有效");
            Assert.False(double.IsNaN(temps.BoostPump), "增压泵温度应有效");
        }

        [Fact]
        public async Task GetPumpCurrentAsync_ShouldReturnCurrent()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var current = await device.GetPumpCurrentAsync();

            // Assert
            Assert.NotNull(current);
            Assert.False(double.IsNaN(current.PreStagePump), "前级泵电流应有效");
            Assert.False(double.IsNaN(current.BoostPump), "增压泵电流应有效");
        }

        // ============================================================
        // 内部指令 - 压力配置测试
        // ============================================================

        [Fact]
        public async Task GetRawPressureAsync_ShouldReturnPressure()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var result = await device.GetRawPressureAsync(SourceModule.Pressure);

            // Assert
            Assert.False(double.IsNaN(result.Value));
            Assert.NotNull(result.Unit);
            Assert.NotEmpty(result.Unit);
        }

        [Fact]
        public async Task GetMuteAsync_ShouldReturnMuteState()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var mute = await device.GetMuteAsync();

            // Assert
            Assert.IsType<bool>(mute);
        }

        [Fact]
        public async Task GetAdjAsync_ShouldReturnAdjState()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var adj = await device.GetAdjAsync();

            // Assert
            Assert.IsType<bool>(adj);
        }

        [Fact]
        public async Task GetVacuumVentAsync_ShouldReturnVacuumVentState()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var vacuumVent = await device.GetVacuumVentAsync();

            // Assert
            Assert.IsType<bool>(vacuumVent);
        }

        // ============================================================
        // 校准指令测试
        // ============================================================

        [Fact]
        public async Task GetCalibrationDataAsync_ShouldReturnCalibrationRecord()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var calibrationData = await device.GetCalibrationDataAsync(SourceModule.Pressure, "123456", 0);

            // Assert
            Assert.NotNull(calibrationData);
            Assert.True(calibrationData.StandardValues.Length > 0, "应有校准标准值");
            Assert.True(calibrationData.RawValues.Length > 0, "应有原始值");
            Assert.True(calibrationData.Year > 2000, $"校准年份应 > 2000，实际值: {calibrationData.Year}");
        }

        // ============================================================
        // 测试指令测试
        // ============================================================

        [Fact]
        public async Task GetTestModeAsync_ShouldReturnTestMode()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var testMode = await device.GetTestModeAsync();

            // Assert
            Assert.IsType<bool>(testMode);
        }

        [Fact]
        public async Task GetBlowTestStateAsync_ShouldReturnBlowTestState()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var blowTestState = await device.GetBlowTestStateAsync();

            // Assert
            Assert.True(blowTestState >= 0, $"吹扫测试状态应 >= 0，实际值: {blowTestState}");
        }

        [Fact]
        public async Task GetScreenTestResultAsync_ShouldReturnScreenTestResult()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var screenTestResult = await device.GetScreenTestResultAsync(0);

            // Assert
            Assert.True(screenTestResult >= 0, $"屏幕测试结果应 >= 0，实际值: {screenTestResult}");
        }
    }
}
