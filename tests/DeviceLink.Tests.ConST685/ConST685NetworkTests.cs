using DeviceLink.Device.ConST685;
using System.Net;
using Xunit;

namespace DeviceLink.Tests.ConST685
{
    /// <summary>
    /// ConST685 多路温场测量/校准设备网口集成测试
    /// 
    /// 依赖真实硬件设备，网口参数见下方常量。
    /// 运行方式：dotnet test
    /// 
    /// ⚠ SET 类指令执行后不会自动恢复设备状态，
    ///    Sweep 测试（SetXxx → GetXxx 配对）会在测试内恢复原值。
    /// 
    /// 使用前请修改 TestIpAddress 和 TestPort 为实际设备地址。
    /// </summary>
    public class ConST685NetworkTests
    {
        // ======================== 配置参数 ========================
        private const string TestIpAddress = "192.168.143.245";
        private const int TestPort = 8000;

        private ConST685Base CreateDevice()
        {
            return new ConST685Base(IPAddress.Parse(TestIpAddress), TestPort);
        }

        #region 通用指令 —— IEEE488.2

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
        public async Task GetStandardEventEnableAsync_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var val = await dev.GetStandardEventEnableAsync();
            Assert.True(val >= 0);
        }

        [Fact]
        public async Task GetStatusByteAsync_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var val = await dev.GetStatusByteAsync();
            Assert.True(val >= 0);
        }

        [Fact]
        public async Task Sweep_StandardEventEnable_SetAndGet()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var original = await dev.GetStandardEventEnableAsync();
            await dev.SetStandardEventEnableAsync(0);
            var readback = await dev.GetStandardEventEnableAsync();
            Assert.Equal(0, readback);
            await dev.SetStandardEventEnableAsync(original);
        }

        #endregion

        #region 系统指令 —— SYSTem

        [Fact]
        public async Task GetVersionAsync_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var version = await dev.GetVersionAsync();
            Assert.NotEmpty(version);
        }

        [Fact]
        public async Task GetVersionAsync_Application_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var appVer = await dev.GetVersionAsync("APPLication");
            Assert.NotEmpty(appVer);
        }

        [Fact]
        public async Task GetDateAsync_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var date = await dev.GetDateAsync();
            Assert.NotEmpty(date);
        }

        [Fact]
        public async Task Sweep_Date_SetAndGet()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var original = await dev.GetDateAsync();
            await dev.SetDateAsync(2022, 1, 1);
            var readback = await dev.GetDateAsync();
            Assert.Contains("2022", readback);
            var parts = original.Split(',');
            if (parts.Length == 3 &&
                int.TryParse(parts[0].Trim(), out var y) &&
                int.TryParse(parts[1].Trim(), out var m) &&
                int.TryParse(parts[2].Trim(), out var d))
            {
                await dev.SetDateAsync(y, m, d);
            }
        }

        [Fact]
        public async Task GetTimeAsync_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var time = await dev.GetTimeAsync();
            Assert.NotEmpty(time);
        }

        [Fact]
        public async Task Sweep_Time_SetAndGet()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var original = await dev.GetTimeAsync();
            await dev.SetTimeAsync(12, 0, 0);
            var readback = await dev.GetTimeAsync();
            Assert.Contains("12", readback);
            var parts = original.Split(',');
            if (parts.Length >= 3)
            {
                var h = int.TryParse(parts[0].Trim(), out var hh) ? hh : 0;
                var mm = int.TryParse(parts[1].Trim(), out var min) ? min : 0;
                var s = int.TryParse(parts[2].Trim(), out var ss) ? ss : 0;
                await dev.SetTimeAsync(h, mm, s);
            }
        }

        #endregion

        #region 测量指令 —— MODule / SCAN / CHANnel

        [Fact]
        public async Task GetModuleInfoListAsync_ShouldReturnValidList()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var list = await dev.GetModuleInfoListAsync();
            Assert.NotEmpty(list);
        }

        [Fact]
        public async Task GetScanConfigAsync_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var config = await dev.GetScanConfigAsync();
            Assert.NotNull(config);
        }

        [Fact(Skip = "⚠ 扫描操作需确认设备通道状态")]
        public async Task Scan_StartStop_ShouldNotThrow()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            await dev.StartScanAsync(new ScanInfo { ChannelName = "Ch1", NPLC = 100 });
            await Task.Delay(500);
            await dev.StopScanAsync();
        }

        [Fact]
        public async Task GetScanDataAsync_ShouldReturnReadings()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var readings = await dev.GetScanDataAsync(1);
            Assert.NotNull(readings);
            if (readings.Count > 0)
            {
                var first = readings[0];
                Assert.False(string.IsNullOrEmpty(first.ChannelName));
                Assert.NotEmpty(first.Values);
            }
        }

        #endregion

        #region 校准指令

        [Fact]
        public async Task GetCalibrationScanAsync_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var data = await dev.GetCalibrationScanAsync();
            Assert.NotNull(data);
        }

        [Fact]
        public async Task GetCalibrationScanResultAsync_ShouldReturnValidResult()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var result = await dev.GetCalibrationScanResultAsync();
            Assert.NotNull(result);
        }

        #endregion

        #region 存储指令

        [Fact]
        public async Task GetMemoryFreeAsync_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var info = await dev.GetMemoryFreeAsync();
            Assert.True(info.IsValid);
            Assert.True(info.FreeBytes >= 0);
        }

        #endregion

        #region 诊断指令

        [Fact]
        public async Task GetDiagnosticSerialNumberAsync_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var sn = await dev.GetDiagnosticSerialNumberAsync();
            Assert.NotEmpty(sn);
        }

        [Fact]
        public async Task GetDiagnosticModelAsync_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var model = await dev.GetDiagnosticModelAsync();
            Assert.NotEmpty(model);
        }

        [Fact]
        public async Task GetDiagnosticNameAsync_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var name = await dev.GetDiagnosticNameAsync();
            Assert.NotEmpty(name);
        }

        [Fact]
        public async Task GetSystemRuntimeAsync_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var runtime = await dev.GetSystemRuntimeAsync();
            Assert.True(runtime >= 0);
        }

        [Fact]
        public async Task GetSystemVoltagesAsync_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var voltages = await dev.GetSystemVoltagesAsync();
            Assert.True(voltages.IsValid);
        }

        [Fact]
        public async Task GetElectricitySerialNumberAsync_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var sn = await dev.GetElectricitySerialNumberAsync();
            Assert.NotNull(sn);
        }

        [Fact]
        public async Task GetSystemVpidAsync_ShouldReturnValue()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var vpid = await dev.GetSystemVpidAsync();
            Assert.NotEmpty(vpid);
        }

        #endregion

        #region 错误处理

        [Fact]
        public async Task GetErrorAsync_ShouldNotThrow()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();
            var error = await dev.GetErrorAsync();
            Assert.NotNull(error);
        }

        [Fact(Skip = "⚠ 完整连通性测试，确认设备在线后取消 Skip 运行")]
        public async Task Comprehensive_AllGetCommands_ShouldNotThrow()
        {
            using var dev = CreateDevice();
            await dev.OpenAsync();

            // IEEE488.2
            var id = await dev.GetIdentificationAsync();
            Assert.True(id.IsValid);
            var ese = await dev.GetStandardEventEnableAsync();
            Assert.True(ese >= 0);
            var stb = await dev.GetStatusByteAsync();
            Assert.True(stb >= 0);

            // SYSTem
            var version = await dev.GetVersionAsync();
            Assert.NotEmpty(version);
            var appVer = await dev.GetVersionAsync("APPLication");
            Assert.NotEmpty(appVer);
            var date = await dev.GetDateAsync();
            Assert.NotEmpty(date);
            var time = await dev.GetTimeAsync();
            Assert.NotEmpty(time);
            var error = await dev.GetErrorAsync();
            Assert.NotNull(error);

            // MEASure
            var modInfo = await dev.GetModuleInfoListAsync();
            Assert.NotEmpty(modInfo);

            // MMEMory
            var memInfo = await dev.GetMemoryFreeAsync();
            Assert.True(memInfo.IsValid);

            // DIAGnostic
            var sn = await dev.GetDiagnosticSerialNumberAsync();
            Assert.NotEmpty(sn);
            var model = await dev.GetDiagnosticModelAsync();
            Assert.NotEmpty(model);
            var runtime = await dev.GetSystemRuntimeAsync();
            Assert.True(runtime >= 0);
            var vpid = await dev.GetSystemVpidAsync();
            Assert.NotEmpty(vpid);
        }

        #endregion
    }
}
