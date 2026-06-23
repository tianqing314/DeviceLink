using DeviceLink.DataLink;
using DeviceLink.Device.ConST171A;
using DeviceLink.DeviceBase;
using DeviceLink.Protocol;
using DeviceLink.Session;
using DeviceLink.Transport;
using System.IO.Ports;
using System.Threading.Tasks;
using Xunit;

using ConST171ADevice = DeviceLink.Device.ConST171A.ConST171Base;

namespace DeviceLink.Tests.ConST171A
{
    /// <summary>
    /// ConST171A 压力控制器设备测试
    /// 
    /// 注意：这些测试需要实际的 ConST171A 设备连接到串口。
    /// 测试使用串口连接，串口参数为 COM1, 115200, 8, 1, None。
    /// 如果设备未连接，测试将失败。
    /// </summary>
    public class ConST171ATests
    {
        private const string TestPortName = "COM1";
        private const int TestBaudRate = 115200;
        private const int TestDataBits = 8;
        private const StopBits TestStopBits = StopBits.One;
        private const Parity TestParity = Parity.None;

        private ConST171ADevice CreateDevice()
        {
            var transport = new SerialPortTransport(TestPortName, TestBaudRate, TestDataBits, TestStopBits, TestParity);
            var frameStrategy = new DelimiterFrameStrategy(new byte[] { 0x0D, 0x0A });
            var dataLink = new DirectDataLink(transport, frameStrategy);
            var session = new DirectSession(dataLink);
            var codec = new ScpiCodec("\r\n");
            return new ConST171ADevice(session, codec);
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
            var identification = await device.GetIdentificationAsync();

            // Assert
            Assert.NotNull(identification);
            Assert.NotEmpty(identification);
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
        public async Task GetPressureAsync_ShouldReturnPressure()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var result = await device.GetPressureAsync();

            // Assert
            Assert.False(double.IsNaN(result.Value));
            Assert.NotNull(result.Unit);
            Assert.NotEmpty(result.Unit);
        }

        [Fact]
        public async Task GetPressureAsync_WithSource_ShouldReturnPressure()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var result = await device.GetPressureAsync("1");

            // Assert
            Assert.False(double.IsNaN(result.Value));
            Assert.NotNull(result.Unit);
            Assert.NotEmpty(result.Unit);
        }

        [Fact]
        public async Task GetPressureUnitAsync_ShouldReturnUnit()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var unit = await device.GetPressureUnitAsync("1");

            // Assert
            Assert.NotNull(unit);
            Assert.NotEmpty(unit);
        }

        [Fact]
        public async Task GetPressureRangeAsync_ShouldReturnRange()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var range = await device.GetPressureRangeAsync("1");

            // Assert
            Assert.NotNull(range);
            Assert.NotEmpty(range);
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
        public async Task GetVersionAsync_ShouldReturnVersion()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var version = await device.GetVersionAsync();

            // Assert
            Assert.NotNull(version);
            Assert.NotEmpty(version);
        }

        [Fact]
        public async Task GetVersionAsync_WithModule_ShouldReturnVersion()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var version = await device.GetVersionAsync("MCU");

            // Assert
            Assert.NotNull(version);
            Assert.NotEmpty(version);
        }

        [Fact]
        public async Task GetRs232InfoAsync_ShouldReturnRs232Info()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var rs232Info = await device.GetRs232InfoAsync();

            // Assert
            Assert.NotNull(rs232Info);
            Assert.NotEmpty(rs232Info);
        }

        [Fact]
        public async Task GetErrorAsync_ShouldReturnError()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var error = await device.GetErrorAsync();

            // Assert
            Assert.NotNull(error);
            Assert.NotEmpty(error);
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
            Assert.True(brightness >= 0 && brightness <= 100);
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
            Assert.True(logo >= 0);
        }

        [Fact]
        public async Task GetFanSpeedAsync_ShouldReturnFanSpeed()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var fanSpeed = await device.GetFanSpeedAsync("FAN1");

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
            var valve = await device.GetValveAsync(1);

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
            Assert.NotNull(temperature);
            Assert.NotEmpty(temperature);
        }

        [Fact]
        public async Task GetBoardVoltageAsync_ShouldReturnVoltage()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var voltage = await device.GetBoardVoltageAsync();

            // Assert
            Assert.NotNull(voltage);
            Assert.NotEmpty(voltage);
        }

        [Fact]
        public async Task GetPumpStateAsync_ShouldReturnPumpState()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var pumpState = await device.GetPumpStateAsync("PUMP1");

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
            Assert.NotEmpty(focState);
        }

        [Fact]
        public async Task GetPumpTemperatureAsync_ShouldReturnTemperature()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var temperature = await device.GetPumpTemperatureAsync();

            // Assert
            Assert.NotNull(temperature);
            Assert.NotEmpty(temperature);
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
            Assert.NotEmpty(current);
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
            var result = await device.GetRawPressureAsync("1");

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
        public async Task GetCalibrationDataAsync_ShouldReturnCalibrationData()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var calibrationData = await device.GetCalibrationDataAsync("1", "123456", 0);

            // Assert
            Assert.NotNull(calibrationData);
            Assert.NotEmpty(calibrationData);
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
            Assert.True(blowTestState >= 0);
        }

        [Fact]
        public async Task GetScreenTestResultAsync_ShouldReturnScreenTestResult()
        {
            // Arrange
            using var device = CreateDevice();
            await device.OpenAsync();

            // Act
            var screenTestResult = await device.GetScreenTestResultAsync(1);

            // Assert
            Assert.True(screenTestResult >= 0);
        }
    }
}