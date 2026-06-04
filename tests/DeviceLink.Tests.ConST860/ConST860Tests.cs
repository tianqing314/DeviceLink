using DeviceLink.DataLink;
using DeviceLink.Device.ConST860;
using DeviceLink.DeviceBase;
using DeviceLink.Protocol;
using DeviceLink.Session;
using DeviceLink.Transport;
using System.Threading.Tasks;
using Xunit;

using ConST860Device = DeviceLink.Device.ConST860.ConST860;

namespace DeviceLink.Tests.ConST860
{
    /// <summary>
    /// ConST860 设备测试
    /// 
    /// 注意：这些测试需要实际的 ConST860 设备连接到网络。
    /// 测试使用 TCP 连接，IP 地址统一为 192.168.1.100，端口为 8000。
    /// 如果设备未连接，测试将失败。
    /// </summary>
    public class ConST860Tests
    {
        private const string TestIpAddress = "192.168.1.100";
        private const int TestPort = 8000;

        private ConST860Device CreateDevice()
        {
            var transport = new TcpTransport(TestIpAddress, TestPort);
            var frameStrategy = new DelimiterFrameStrategy(new byte[] { 0 });
            var dataLink = new DirectDataLink(transport, frameStrategy);
            var session = new DirectSession(dataLink);
            var codec = new ScpiCodec("\n");
            return new ConST860Device(session, codec);
        }

        [Fact]
        public async Task GetIdentificationAsync_ShouldReturnIdentification()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var identification = await const860.GetIdentificationAsync();

            // Assert
            Assert.NotNull(identification);
            Assert.NotEmpty(identification);
        }

        [Fact]
        public async Task GetOutputPressureAsync_ShouldReturnPressure()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var result = await const860.GetOutputPressureAsync();

            // Assert
            Assert.False(double.IsNaN(result.Value));
            Assert.NotNull(result.Unit);
            Assert.NotEmpty(result.Unit);
        }

        [Fact]
        public async Task GetModulePressureAsync_ShouldReturnPressure()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act - 读取内部高压模块压力值
            var result = await const860.GetModulePressureAsync(2);

            // Assert
            Assert.False(double.IsNaN(result.Value));
            Assert.NotNull(result.Unit);
            Assert.NotEmpty(result.Unit);
        }

        [Fact]
        public async Task SetTargetPressureAsync_ShouldSetTarget()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            await const860.SetTargetPressureAsync(100.0);

            // Assert - 验证目标值已设置
            var result = await const860.GetTargetPressureAsync();
            Assert.Equal(100.0, result.Value, 2);
        }

        [Fact]
        public async Task SetControlModeAsync_ShouldSetMode()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            await const860.SetControlModeAsync("MEASURE");

            // Assert
            var mode = await const860.GetControlModeAsync();
            Assert.Equal("MEASURE", mode);
        }

        [Fact]
        public async Task GetVersionAsync_ShouldReturnVersion()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var version = await const860.GetVersionAsync();

            // Assert
            Assert.NotNull(version);
            Assert.NotEmpty(version);
        }

        [Fact]
        public async Task GetControlInfoAsync_ShouldReturnControlInfo()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var controlInfo = await const860.GetControlInfoAsync();

            // Assert
            Assert.NotNull(controlInfo);
            Assert.False(double.IsNaN(controlInfo.Value));
            Assert.NotNull(controlInfo.Unit);
            Assert.NotEmpty(controlInfo.Unit);
        }

        [Fact]
        public async Task GetMeasureValueAsync_ShouldReturnMeasureValue()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var measureValue = await const860.GetMeasureValueAsync();

            // Assert
            Assert.False(double.IsNaN(measureValue));
        }

        [Fact]
        public async Task GetPressureTypeAsync_ShouldReturnType()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var result = await const860.GetPressureTypeAsync();

            // Assert
            Assert.NotNull(result.Type);
            Assert.NotEmpty(result.Type);
            Assert.True(result.Type == "G" || result.Type == "A" || result.Type == "D");
        }

        [Fact]
        public async Task GetEthernetAddressAsync_ShouldReturnAddress()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var address = await const860.GetEthernetAddressAsync();

            // Assert
            Assert.NotNull(address);
            Assert.NotEmpty(address);
        }

        [Fact]
        public async Task GetErrorAsync_ShouldReturnError()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var error = await const860.GetErrorAsync();

            // Assert - 如果没有错误，应该返回 "0,No error"
            Assert.NotNull(error);
            Assert.NotEmpty(error);
        }

        [Fact]
        public async Task ClearErrorsAsync_ShouldClearErrors()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            await const860.ClearErrorsAsync();

            // Assert - 清除错误后，查询错误应该返回 "0,No error"
            var error = await const860.GetErrorAsync();
            Assert.Contains("0", error);
        }

        [Fact]
        public async Task GetAllModulePressuresAsync_ShouldReturnPressures()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var pressures = await const860.GetAllModulePressuresAsync();

            // Assert
            Assert.NotNull(pressures);
            Assert.NotEmpty(pressures);
        }

        [Fact]
        public async Task GetModuleUnitAsync_ShouldReturnUnit()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act - 读取内部高压模块压力单位
            var unit = await const860.GetModuleUnitAsync(2);

            // Assert
            Assert.NotNull(unit);
            Assert.NotEmpty(unit);
        }

        [Fact]
        public async Task GetSystemTimeAsync_ShouldReturnTime()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var time = await const860.GetSystemTimeAsync();

            // Assert
            Assert.NotNull(time);
            Assert.NotEmpty(time);
        }

        [Fact]
        public async Task GetSystemDateAsync_ShouldReturnDate()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var date = await const860.GetSystemDateAsync();

            // Assert
            Assert.NotNull(date);
            Assert.NotEmpty(date);
        }

        [Fact]
        public async Task GetMeasureFunctionAsync_ShouldReturnFunction()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var function = await const860.GetMeasureFunctionAsync();

            // Assert
            Assert.True(function >= 1 && function <= 4);
        }

        [Fact]
        public async Task GetEthernetDhcpAsync_ShouldReturnDhcpState()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var dhcp = await const860.GetEthernetDhcpAsync();

            // Assert - DHCP 状态为 true 或 false 都可以
            Assert.IsType<bool>(dhcp);
        }

        [Fact]
        public async Task ResetAsync_ShouldNotThrow()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act & Assert
            await const860.ResetAsync();
        }

        // === 新增测试：覆盖更多方法 ===

        [Fact]
        public async Task GetModuleResolutionAsync_ShouldReturnResolution()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act - 读取内部高压模块分辨率
            var resolution = await const860.GetModuleResolutionAsync(2);

            // Assert
            Assert.True(resolution >= 0);
        }

        [Fact]
        public async Task GetModulePressureTypeAsync_ShouldReturnType()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act - 读取内部高压模块压力类型
            var type = await const860.GetModulePressureTypeAsync(2);

            // Assert
            Assert.NotNull(type);
            Assert.NotEmpty(type);
        }

        [Fact]
        public async Task GetModuleRangeAsync_ShouldReturnRange()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act - 读取内部高压模块量程
            var range = await const860.GetModuleRangeAsync(2);

            // Assert
            Assert.NotNull(range);
            Assert.NotEmpty(range);
        }

        [Fact]
        public async Task GetModuleOnlineAsync_ShouldReturnOnlineState()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act - 读取内部高压模块在线状态
            var online = await const860.GetModuleOnlineAsync(2);

            // Assert - 在线状态为 true 或 false 都可以
            Assert.IsType<bool>(online);
        }

        [Fact]
        public async Task GetModuleInfoAsync_ShouldReturnModuleInfo()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act - 读取内部高压模块信息
            var moduleInfo = await const860.GetModuleInfoAsync(2);

            // Assert
            Assert.NotNull(moduleInfo);
            Assert.NotEmpty(moduleInfo.SerialNumber);
        }

        [Fact]
        public async Task GetModuleFilterAsync_ShouldReturnFilterInfo()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act - 读取内部高压模块滤波设置
            var filterInfo = await const860.GetModuleFilterAsync(2);

            // Assert
            Assert.NotNull(filterInfo);
            Assert.True(filterInfo.FilterType >= 0 && filterInfo.FilterType <= 1);
        }

        [Fact]
        public async Task GetControlModeAsync_ShouldReturnMode()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var mode = await const860.GetControlModeAsync();

            // Assert
            Assert.NotNull(mode);
            Assert.NotEmpty(mode);
            Assert.True(mode == "VENT" || mode == "MEASURE" || mode == "CONTROL");
        }

        [Fact]
        public async Task GetTargetPressureRangeAsync_ShouldReturnRange()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var range = await const860.GetTargetPressureRangeAsync();

            // Assert
            Assert.NotNull(range);
            Assert.False(double.IsNaN(range.Low));
            Assert.False(double.IsNaN(range.High));
            Assert.NotNull(range.Unit);
            Assert.NotEmpty(range.Unit);
        }

        [Fact]
        public async Task GetControlModuleAsync_ShouldReturnModuleId()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var moduleId = await const860.GetControlModuleAsync();

            // Assert
            Assert.True(moduleId >= 1);
        }

        [Fact]
        public async Task GetVentPressureAsync_ShouldReturnPressure()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var result = await const860.GetVentPressureAsync();

            // Assert
            Assert.NotNull(result);
            Assert.False(double.IsNaN(result.Value));
            Assert.NotNull(result.Unit);
            Assert.NotEmpty(result.Unit);
        }

        [Fact]
        public async Task GetPressureLimitEnableAsync_ShouldReturnState()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var enabled = await const860.GetPressureLimitEnableAsync();

            // Assert - 使能状态为 true 或 false 都可以
            Assert.IsType<bool>(enabled);
        }

        [Fact]
        public async Task GetPressureLimitAsync_ShouldReturnRange()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var range = await const860.GetPressureLimitAsync();

            // Assert
            Assert.NotNull(range);
            Assert.False(double.IsNaN(range.Low));
            Assert.False(double.IsNaN(range.High));
            Assert.NotNull(range.Unit);
            Assert.NotEmpty(range.Unit);
        }

        [Fact]
        public async Task GetStepValueAsync_ShouldReturnStep()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var step = await const860.GetStepValueAsync();

            // Assert
            Assert.False(double.IsNaN(step));
            Assert.True(step > 0);
        }

        [Fact]
        public async Task GetControlModeTypeAsync_ShouldReturnType()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var modeType = await const860.GetControlModeTypeAsync();

            // Assert
            Assert.True(modeType >= 0 && modeType <= 2);
        }

        [Fact]
        public async Task GetSlewRateAsync_ShouldReturnSlewRateInfo()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var slewRate = await const860.GetSlewRateAsync();

            // Assert
            Assert.NotNull(slewRate);
            Assert.True(slewRate.Type >= 0 && slewRate.Type <= 1);
            Assert.NotNull(slewRate.Value);
            Assert.NotEmpty(slewRate.Value);
            Assert.NotNull(slewRate.Unit);
            Assert.NotEmpty(slewRate.Unit);
        }

        [Fact]
        public async Task GetStabilityAsync_ShouldReturnStabilityInfo()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var stability = await const860.GetStabilityAsync();

            // Assert
            Assert.NotNull(stability);
            Assert.True(stability.Type >= 0 && stability.Type <= 1);
            Assert.True(stability.Seconds >= 0);
        }

        [Fact]
        public async Task GetHeightCorrectionAsync_ShouldReturnHeightCorrectionInfo()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var heightCorrection = await const860.GetHeightCorrectionAsync();

            // Assert
            Assert.NotNull(heightCorrection);
            Assert.True(heightCorrection.UnitType >= 0 && heightCorrection.UnitType <= 1);
        }

        [Fact]
        public async Task GetTareAsync_ShouldReturnTareInfo()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var tare = await const860.GetTareAsync();

            // Assert
            Assert.NotNull(tare);
        }

        [Fact]
        public async Task GetSwitchTypeAsync_ShouldReturnType()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var switchType = await const860.GetSwitchTypeAsync();

            // Assert
            Assert.True(switchType >= 0 && switchType <= 2);
        }

        [Fact]
        public async Task GetSwitchValueAsync_ShouldReturnSwitchValueInfo()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var switchValue = await const860.GetSwitchValueAsync();

            // Assert
            Assert.NotNull(switchValue);
            Assert.False(double.IsNaN(switchValue.CloseValue));
            Assert.False(double.IsNaN(switchValue.OpenValue));
            Assert.NotNull(switchValue.CloseUnit);
            Assert.NotEmpty(switchValue.CloseUnit);
            Assert.NotNull(switchValue.OpenUnit);
            Assert.NotEmpty(switchValue.OpenUnit);
        }

        [Fact]
        public async Task GetExtendedInterfaceStateAsync_ShouldReturnState()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var state = await const860.GetExtendedInterfaceStateAsync();

            // Assert
            Assert.NotNull(state);
        }

        [Fact]
        public async Task GetExtendedInterfaceModeAsync_ShouldReturnModeInfo()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act - 读取 CPS 输出模式
            var modeInfo = await const860.GetExtendedInterfaceModeAsync(0);

            // Assert
            Assert.NotNull(modeInfo);
            Assert.True(modeInfo.CurrentMode >= 0);
            Assert.NotNull(modeInfo.AvailableModes);
            Assert.NotEmpty(modeInfo.AvailableModes);
        }

        [Fact]
        public async Task GetAutoZeroAsync_ShouldReturnState()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var autoZero = await const860.GetAutoZeroAsync();

            // Assert - 自动调零状态为 true 或 false 都可以
            Assert.IsType<bool>(autoZero);
        }

        [Fact]
        public async Task GetZeroPointStrategyAsync_ShouldReturnStrategy()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var strategy = await const860.GetZeroPointStrategyAsync();

            // Assert
            Assert.True(strategy >= 0 && strategy <= 1);
        }

        [Fact]
        public async Task GetPressureStableAsync_ShouldReturnStableState()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var stable = await const860.GetPressureStableAsync();

            // Assert - 稳定状态为 true 或 false 都可以
            Assert.IsType<bool>(stable);
        }

        [Fact]
        public async Task GetFixedAtmosphericPressureAsync_ShouldReturnPressure()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var atm = await const860.GetFixedAtmosphericPressureAsync();

            // Assert
            Assert.NotNull(atm);
            Assert.False(double.IsNaN(atm.Value));
            Assert.NotNull(atm.Unit);
            Assert.NotEmpty(atm.Unit);
        }

        [Fact]
        public async Task GetMediumNameAsync_ShouldReturnMedium()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var medium = await const860.GetMediumNameAsync();

            // Assert
            Assert.True(medium >= 0 && medium <= 2);
        }

        [Fact]
        public async Task GetLockAsync_ShouldReturnLockState()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var locked = await const860.GetLockAsync();

            // Assert - 锁定状态为 true 或 false 都可以
            Assert.IsType<bool>(locked);
        }

        [Fact]
        public async Task GetWlanStateAsync_ShouldReturnWlanState()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var wlanState = await const860.GetWlanStateAsync();

            // Assert - WLAN 状态为 true 或 false 都可以
            Assert.IsType<bool>(wlanState);
        }

        [Fact]
        public async Task GetWlanAddressAsync_ShouldReturnAddress()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var address = await const860.GetWlanAddressAsync();

            // Assert
            Assert.NotNull(address);
            Assert.NotEmpty(address);
        }

        [Fact]
        public async Task GetWlanMaskAsync_ShouldReturnMask()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var mask = await const860.GetWlanMaskAsync();

            // Assert
            Assert.NotNull(mask);
            Assert.NotEmpty(mask);
        }

        [Fact]
        public async Task GetWlanGatewayAsync_ShouldReturnGateway()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var gateway = await const860.GetWlanGatewayAsync();

            // Assert
            Assert.NotNull(gateway);
            Assert.NotEmpty(gateway);
        }

        [Fact]
        public async Task GetWlanDhcpAsync_ShouldReturnDhcpState()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var dhcp = await const860.GetWlanDhcpAsync();

            // Assert - DHCP 状态为 true 或 false 都可以
            Assert.IsType<bool>(dhcp);
        }

        [Fact]
        public async Task GetWlanMacAsync_ShouldReturnMac()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var mac = await const860.GetWlanMacAsync();

            // Assert
            Assert.NotNull(mac);
            Assert.NotEmpty(mac);
        }

        [Fact]
        public async Task GetEthernetMaskAsync_ShouldReturnMask()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var mask = await const860.GetEthernetMaskAsync();

            // Assert
            Assert.NotNull(mask);
            Assert.NotEmpty(mask);
        }

        [Fact]
        public async Task GetEthernetGatewayAsync_ShouldReturnGateway()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var gateway = await const860.GetEthernetGatewayAsync();

            // Assert
            Assert.NotNull(gateway);
            Assert.NotEmpty(gateway);
        }

        [Fact]
        public async Task GetEthernetMacAsync_ShouldReturnMac()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var mac = await const860.GetEthernetMacAsync();

            // Assert
            Assert.NotNull(mac);
            Assert.NotEmpty(mac);
        }

        [Fact]
        public async Task GetRs232InfoAsync_ShouldReturnRs232Info()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var rs232Info = await const860.GetRs232InfoAsync();

            // Assert
            Assert.NotNull(rs232Info);
            Assert.True(rs232Info.BaudRate > 0);
            Assert.True(rs232Info.DataBits > 0);
            Assert.NotNull(rs232Info.StopBits);
            Assert.NotEmpty(rs232Info.StopBits);
            Assert.NotNull(rs232Info.Parity);
            Assert.NotEmpty(rs232Info.Parity);
        }

        [Fact]
        public async Task GetTimeFormatAsync_ShouldReturnFormat()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var format = await const860.GetTimeFormatAsync();

            // Assert
            Assert.True(format >= 0 && format <= 1);
        }

        [Fact]
        public async Task GetDateFormatAsync_ShouldReturnFormat()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var format = await const860.GetDateFormatAsync();

            // Assert
            Assert.True(format >= 0 && format <= 2);
        }

        [Fact]
        public async Task GetDateSeparatorAsync_ShouldReturnSeparator()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var separator = await const860.GetDateSeparatorAsync();

            // Assert
            Assert.NotNull(separator);
            Assert.NotEmpty(separator);
            Assert.True(separator == "/" || separator == "-" || separator == ".");
        }

        [Fact]
        public async Task GetVolumeAsync_ShouldReturnVolume()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var volume = await const860.GetVolumeAsync();

            // Assert
            Assert.True(volume >= 0 && volume <= 100);
        }

        [Fact]
        public async Task GetTouchSoundAsync_ShouldReturnState()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var touchSound = await const860.GetTouchSoundAsync();

            // Assert - 触摸音状态为 true 或 false 都可以
            Assert.IsType<bool>(touchSound);
        }

        [Fact]
        public async Task GetPromptSoundAsync_ShouldReturnState()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var promptSound = await const860.GetPromptSoundAsync();

            // Assert - 提示音状态为 true 或 false 都可以
            Assert.IsType<bool>(promptSound);
        }

        [Fact]
        public async Task GetOverrangeSoundAsync_ShouldReturnState()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var overrangeSound = await const860.GetOverrangeSoundAsync();

            // Assert - 超量程音状态为 true 或 false 都可以
            Assert.IsType<bool>(overrangeSound);
        }

        [Fact]
        public async Task GetBrightnessAsync_ShouldReturnBrightness()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var brightness = await const860.GetBrightnessAsync();

            // Assert
            Assert.True(brightness >= 0 && brightness <= 100);
        }

        [Fact]
        public async Task GetLanguageAsync_ShouldReturnLanguage()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var language = await const860.GetLanguageAsync();

            // Assert
            Assert.NotNull(language);
            Assert.NotEmpty(language);
        }

        [Fact]
        public async Task GetMeasureResolutionAsync_ShouldReturnResolution()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act - 读取电测分辨率（开关1）
            var resolution = await const860.GetMeasureResolutionAsync(1);

            // Assert
            Assert.True(resolution >= 0 && resolution <= 6);
        }

        [Fact]
        public async Task GetRangeListAsync_ShouldReturnRangeList()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var rangeList = await const860.GetRangeListAsync();

            // Assert
            Assert.NotNull(rangeList);
            Assert.NotEmpty(rangeList);
        }

        [Fact]
        public async Task GetRangeIndexAsync_ShouldReturnIndex()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var index = await const860.GetRangeIndexAsync();

            // Assert
            Assert.NotNull(index);
            Assert.NotEmpty(index);
        }

        [Fact]
        public async Task GetRangeModeAsync_ShouldReturnMode()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var mode = await const860.GetRangeModeAsync();

            // Assert
            Assert.True(mode >= 0 && mode <= 1);
        }

        [Fact]
        public async Task GetModuleMultiRangeAsync_ShouldReturnMultiRangeState()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act - 读取内部高压模块多量程状态
            var multiRange = await const860.GetModuleMultiRangeAsync(2);

            // Assert - 多量程状态为 true 或 false 都可以
            Assert.IsType<bool>(multiRange);
        }

        [Fact]
        public async Task GetPressureRangeAsync_ShouldReturnRange()
        {
            // Arrange
            using var const860 = CreateDevice();
            await const860.OpenAsync();

            // Act
            var range = await const860.GetPressureRangeAsync();

            // Assert
            Assert.NotNull(range);
            Assert.NotEmpty(range);
        }
    }
}