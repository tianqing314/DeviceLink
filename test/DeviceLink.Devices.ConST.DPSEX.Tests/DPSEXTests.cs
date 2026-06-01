using DeviceLink.Core.Channel;
using DeviceLink.Core.Framing;
using DeviceLink.Testing;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DeviceLink.Devices.ConST.DPSEX.Tests
{
    /// <summary>
    /// DPSEX 设备测试 —— 验证框架全链路（Transport → Framing → Channel → Codec → DeviceBase → DPSEX）
    /// </summary>
    public class DPSEXTests
    {
        #region ScenarioChannel 测试（不依赖任何硬件）
        [Fact]
        public async Task GetPressureAsync_ReturnsCorrectValue()
        {
            // Arrange: 模拟 DPSEX 响应 ConST 协议
            var scenario = new ChannelScenario();
            scenario.When(req => Encoding.ASCII.GetString(req).Contains("R:MRMD"))
                    .Respond(Encoding.ASCII.GetBytes("1:F:MRMD:1.23456:kPa\0"))
                    .Named("读取压力")
                    .Add();

            var channel = new ScenarioChannel(scenario) { Name = "DPSEX-Test" };
            var dpsex = new DPSEX(channel);

            // Act
            var result = await dpsex.GetPressureAsync();

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1.23456, result.Value, 5);
        }

        [Fact]
        public async Task PressureZeroAsync_SendsCorrectCommand()
        {
            var scenario = new ChannelScenario();
            scenario.When(req =>
                    {
                        var text = Encoding.ASCII.GetString(req);
                        return text.Contains("W:OZERO");
                    })
                    .Respond(Encoding.ASCII.GetBytes("1:F:OZERO:\0"))
                    .Named("压力清零")
                    .Add();

            var channel = new ScenarioChannel(scenario) { Name = "DPSEX-Test" };
            var dpsex = new DPSEX(channel);

            var result = await dpsex.PressureZeroAsync();

            Assert.True(result.Success);
        }

        [Fact]
        public async Task GetVersionAsync_ReturnsVersionString()
        {
            var scenario = new ChannelScenario();
            scenario.When(req => Encoding.ASCII.GetString(req).Contains("R:OVER"))
                    .Respond(Encoding.ASCII.GetBytes("1:F:OVER:DPS-EX-2.1.0\0"))
                    .Named("读取版本")
                    .Add();

            var channel = new ScenarioChannel(scenario) { Name = "DPSEX-Test" };
            var dpsex = new DPSEX(channel);

            var result = await dpsex.GetVersionAsync();

            Assert.True(result.Success);
            Assert.Equal("DPS-EX-2.1.0", result.Value);
        }

        [Fact]
        public async Task GetSerialNumberAsync_ReturnsSN()
        {
            //var scenario = new ChannelScenario();
            //scenario.When(req => Encoding.ASCII.GetString(req).Contains("R:OTYPE"))
            //        .Respond(Encoding.ASCII.GetBytes("1:F:OTYPE:21803001\0"))
            //        .Named("读取序列号")
            //        .Add();

            //var channel = new ScenarioChannel(scenario) { Name = "DPSEX-Test" };
            var dpsex = new DPSEX("COM3", 4800, 8, StopBits.Two, Parity.None);

            var result = await dpsex.GetSerialNumberAsync();
            await dpsex.SetInstrumentCodeAsync("DPSE0230560080");
            result = await dpsex.GetSerialNumberAsync();
            Assert.True(result.Success);
            Assert.Equal("21803001", result.Value);
        }

        [Fact]
        public async Task GetPressureAsync_DeviceError_ReturnsFailure()
        {
            var scenario = new ChannelScenario();
            scenario.When(req => Encoding.ASCII.GetString(req).Contains("R:MRMD"))
                    .Respond(Encoding.ASCII.GetBytes("1:E:ERR_OVER\0"))
                    .Named("压力读取错误")
                    .Add();

            var channel = new ScenarioChannel(scenario) { Name = "DPSEX-Test" };
            var dpsex = new DPSEX(channel);

            var result = await dpsex.GetPressureAsync();

            Assert.False(result.Success);
            Assert.Contains("设备错误", result.Error);
        }

        [Fact]
        public async Task GetPressureAsync_Timeout_ReturnsFailure()
        {
            var scenario = new ChannelScenario();
            scenario.When(req => Encoding.ASCII.GetString(req).Contains("R:MRMD"))
                    .Timeout()
                    .Named("通讯超时")
                    .Add();

            var channel = new ScenarioChannel(scenario) { Name = "DPSEX-Test" };
            var dpsex = new DPSEX(channel);

            var result = await dpsex.GetPressureAsync();

            Assert.False(result.Success);
        }

        #endregion

        #region 多传输验证：DPSEX 代码完全相同

        [Fact]
        public async Task DPSEX_WithLoopbackTransport_WorksCorrectly()
        {
            // 用 LoopbackTransport 模拟串口收发
            var loopback = new LoopbackTransport { Name = "VirtualCOM3" };
            loopback.OnSend += sent =>
            {
                // 解析发送的命令，返回模拟响应
                var text = Encoding.ASCII.GetString(sent);
                if (text.Contains("R:MRMD"))
                    loopback.EnqueueReceive(Encoding.ASCII.GetBytes("1:F:MRMD:2.50000:kPa\0"));
                else if (text.Contains("R:OTYPE"))
                    loopback.EnqueueReceive(Encoding.ASCII.GetBytes("1:F:OTYPE:TEST001\0"));
            };

            var channel = new DirectChannel(loopback,
                new DelimiterFrameStrategy(new byte[] { 0 }),
                name: "DPSEX-Loopback");
            var dpsex = new DPSEX(channel);

            var pressureResult = await dpsex.GetPressureAsync();
            var snResult = await dpsex.GetSerialNumberAsync();

            Assert.True(pressureResult.Success);
            Assert.Equal(2.5, pressureResult.Value, 3);
            Assert.True(snResult.Success);
            Assert.Equal("TEST001", snResult.Value);
        }

        [Fact]
        public async Task DPSEX_CanSwitchBetweenTransportModes()
        {
            // 验证：同一 DPSEX 类，换不同 Channel，行为完全一致

            // 方式 1：Scenario Channel
            var scenario = new ChannelScenario();
            scenario.When(req => Encoding.ASCII.GetString(req).Contains("R:MRMD"))
                    .Respond(Encoding.ASCII.GetBytes("1:F:MRMD:1.00000:kPa\0"))
                    .Add();
            var scenarioChannel = new ScenarioChannel(scenario);

            // 方式 2：Loopback Transport Channel
            var loopback = new LoopbackTransport();
            loopback.OnSend += sent =>
            {
                if (Encoding.ASCII.GetString(sent).Contains("R:MRMD"))
                    loopback.EnqueueReceive(Encoding.ASCII.GetBytes("1:F:MRMD:1.00000:kPa\0"));
            };
            var directChannel = new DirectChannel(loopback,
                new DelimiterFrameStrategy(new byte[] { 0 }));

            // 两个 DPSEX 实例，代码完全相同
            var dpsex1 = new DPSEX(scenarioChannel);
            var dpsex2 = new DPSEX(directChannel);

            // 行为一致
            var r1 = await dpsex1.GetPressureAsync();
            var r2 = await dpsex2.GetPressureAsync();

            Assert.True(r1.Success);
            Assert.True(r2.Success);
            Assert.Equal(r1.Value, r2.Value, 5);
        }

        #endregion
    }
}
