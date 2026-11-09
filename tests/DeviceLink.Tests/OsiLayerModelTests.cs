using System;
using DeviceLink.DataLink;
using DeviceLink.Osi;
using DeviceLink.Pipeline;
using DeviceLink.Protocol;
using DeviceLink.Transport;
using Xunit;

namespace DeviceLink.Tests
{
    /// <summary>
    /// OSI 层次模型测试 —— 锁定层集合表达式、协议画像与装配校验的行为。
    /// 这些用例是"层次清晰"的回归防线：任何把职责写错层的改动都会在这里失败。
    /// </summary>
    public class OsiLayerModelTests
    {
        // ───────────────── 层集合表达式 ─────────────────

        [Fact]
        public void LayerSet_ToString_CompressesLongRuns()
        {
            var set = OsiLayerSet.Of(OsiLayer.Physical, OsiLayer.DataLink, OsiLayer.Application);
            Assert.Equal("L1,L2,L7", set.ToString());   // 只有 2 层连续，不压缩
        }

        [Fact]
        public void LayerSet_Range_ThreeOrMoreLayersBecomesRange()
        {
            var set = OsiLayerSet.Range(OsiLayer.Physical, OsiLayer.Transport);
            Assert.Equal("L1-L4", set.ToString());
        }

        [Fact]
        public void LayerSet_Parse_MqttShape()
        {
            // "L1-L4,L5,L7" 与 "L1-L5,L7" 是同一个层集合，规范化后统一显示
            var parsed = OsiLayerSet.Parse("L1-L4,L5,L7");
            Assert.Equal("L1-L5,L7", parsed.ToString());
            Assert.Equal(6, parsed.Count);
            Assert.False(parsed.Contains(OsiLayer.Presentation));
        }

        [Fact]
        public void LayerSet_Parse_RejectsInvalidToken()
        {
            Assert.Throws<FormatException>(() => OsiLayerSet.Parse("L9"));
        }

        [Fact]
        public void LayerSet_SetOperations()
        {
            var a = OsiLayerSet.Parse("L1,L2,L7");
            var b = OsiLayerSet.Parse("L1,L5");

            Assert.Equal(OsiLayerSet.Parse("L1,L2,L5,L7"), a.Union(b));
            Assert.Equal(OsiLayerSet.Parse("L1"), a.Intersect(b));
            Assert.Equal(OsiLayerSet.Parse("L2,L7"), a.Except(b));
            Assert.True(a.Contains(OsiLayer.Application));
            Assert.False(a.Contains(OsiLayer.Session));
        }

        // ───────────────── 协议画像 ─────────────────

        [Theory]
        [InlineData("L1,L2,L7")]        // RS-232
        [InlineData("L1-L4,L7")]        // SCPI over TCP
        [InlineData("L1-L5,L7")]        // Modbus TCP / MQTT
        public void Profile_AlwaysTerminatesAtApplicationLayer(string shape)
        {
            var profile = new OsiStackProfile("test", shape);
            Assert.True(profile.Implemented.Contains(OsiLayer.Application));
        }

        [Fact]
        public void Profile_WithoutApplicationLayer_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => new OsiStackProfile("bad", "L1,L2"));
        }

        [Fact]
        public void Profile_Rs232_LeavesMiddleLayersEmpty()
        {
            var p = OsiProfiles.Rs232;

            Assert.True(p.Implemented.Contains(OsiLayer.Physical));
            Assert.True(p.Implemented.Contains(OsiLayer.DataLink));
            Assert.True(p.Implemented.Contains(OsiLayer.Application));

            // RS-232 没有网络/传输/会话层
            Assert.True(p.Gaps.Contains(OsiLayer.Network));
            Assert.True(p.Gaps.Contains(OsiLayer.Transport));
            Assert.True(p.Gaps.Contains(OsiLayer.Session));
        }

        [Fact]
        public void Profile_Mqtt_HasSessionLayerButNoPresentationLayer()
        {
            var p = OsiProfiles.Mqtt;

            Assert.True(p.Implemented.Contains(OsiLayer.Session));       // MQTT 自带会话
            Assert.False(p.Implemented.Contains(OsiLayer.Presentation)); // 载荷为裸字节
            Assert.False(p.Gaps.Contains(OsiLayer.Session));
        }

        [Fact]
        public void Profile_Zigbee_DelegatesNetworkToModule()
        {
            var p = OsiProfiles.ZigbeeTransparent;

            Assert.True(p.Delegated.Contains(OsiLayer.Network));   // 模块内部完成 Zigbee NWK
            Assert.False(p.Implemented.Contains(OsiLayer.DataLink));
        }

        // ───────────────── 装配校验 ─────────────────

        [Fact]
        public void Assembler_CompleteRs232Stack_IsValid()
        {
            var report = new OsiStackBuilder(OsiProfiles.ModbusRtu)
                .UseTransport(new LoopbackTransport())          // L1
                .UseDataLink(new ModbusRtuFrameStrategy())      // L2
                .UseProtocol(new ModbusRtuCodec(1))             // L6+L7
                .Inspect();

            Assert.True(report.IsValid, report.ToTextTable());
            Assert.True(report.Provided.Contains(OsiLayer.Application));
        }

        [Fact]
        public void Profile_ModbusTcp_HasNoSessionLayer()
        {
            // MBAP 事务标识只做单次请求-响应配对（correlation），不建立也不保持会话状态，
            // 因此不构成 L5 —— Modbus TCP 没有会话层。
            var p = OsiProfiles.ModbusTcp;

            Assert.False(p.Implemented.Contains(OsiLayer.Session));
            Assert.False(p.Covered.Contains(OsiLayer.Session));
            Assert.True(p.Gaps.Contains(OsiLayer.Session));
        }

        [Fact]
        public void Assembler_ModbusTcpWithoutSessionLayer_IsValid()
        {
            // 没有会话层是 Modbus TCP 的正常形态，不能被判为缺层。
            // 这里用显式描述符模拟 TCP 传输，避免在单元测试里依赖真实网络。
            OsiComponentRegistrations.EnsureRegistered();

            var report = new OsiAssembler()
                .Add(OsiComponentDescriptor.Create(
                    new object(), "SimulatedTcpTransport", OsiLayerSet.Parse("L1-L4"),
                    reason: "测试用：模拟系统 TCP/IP 栈"))
                .Add(OsiComponentDescriptor.Create(
                    new LengthPrefixedFrameStrategy(lengthFieldOffset: ModbusTcpCodec.LengthFieldOffset),
                    "LengthPrefixedFrameStrategy", OsiLayerSet.Parse("L2"),
                    reason: "MBAP 长度前缀定界"))
                .Add(OsiComponentDescriptor.FromRegistry(new ModbusTcpCodec(1)))
                .Assemble(OsiProfiles.ModbusTcp);

            Assert.True(report.IsValid, report.ToTextTable());

            // 唯一的空缺就是 L5 —— Modbus TCP 没有会话层
            Assert.Equal(OsiLayerSet.Parse("L5"), report.Profile.Gaps);
        }

        [Fact]
        public void Assembler_MissingApplicationLayer_IsReported()
        {
            var report = new OsiStackBuilder(OsiProfiles.ModbusRtu)
                .UseTransport(new LoopbackTransport())
                .UseDataLink(new ModbusRtuFrameStrategy())
                .Inspect();   // 故意不设置协议层

            Assert.False(report.IsValid);
            Assert.Contains(report.Violations,
                v => v.Kind == OsiViolationKind.MissingLayer && v.Layer == OsiLayer.Application);
        }

        [Fact]
        public void Assembler_UnregisteredTransport_Throws()
        {
            // 未登记层归属的组件不允许进入通信栈，"默认按物理层"是不被接受的
            using var transport = new UnregisteredTransport();

            Assert.Throws<InvalidOperationException>(() =>
                new OsiStackBuilder(OsiProfiles.Rs232).UseTransport(transport));
        }

        [Fact]
        public void Report_TextTable_ListsEveryLayer()
        {
            var report = new OsiStackBuilder(OsiProfiles.ModbusRtu)
                .UseTransport(new LoopbackTransport())
                .UseDataLink(new ModbusRtuFrameStrategy())
                .UseProtocol(new ModbusRtuCodec(1))
                .Inspect();

            var text = report.ToTextTable();

            for (int i = 1; i <= 7; i++)
                Assert.Contains("L" + i + " ", text);

            Assert.Contains("ModbusRtuFrameStrategy", text);
            Assert.Contains("ModbusRtuCodec", text);
        }

        /// <summary>故意不登记层归属的传输实现，用于验证装配器拒绝未声明组件。</summary>
        private sealed class UnregisteredTransport : IPhysicalTransport
        {
            public string Name => "Unregistered";
            public bool IsOpen => false;

            public System.Threading.Tasks.Task ConnectAsync(System.Threading.CancellationToken ct = default)
                => System.Threading.Tasks.Task.CompletedTask;

            public System.Threading.Tasks.Task CloseAsync()
                => System.Threading.Tasks.Task.CompletedTask;

            public System.Threading.Tasks.Task<int> ReadAsync(
                byte[] buffer, int offset, int count, System.Threading.CancellationToken ct = default)
                => System.Threading.Tasks.Task.FromResult(0);

            public System.Threading.Tasks.Task WriteAsync(
                byte[] data, int offset, int count, System.Threading.CancellationToken ct = default)
                => System.Threading.Tasks.Task.CompletedTask;

            public System.Threading.Tasks.Task ClearReceiveBufferAsync(
                System.Threading.CancellationToken ct = default)
                => System.Threading.Tasks.Task.CompletedTask;

            public void Dispose() { }
        }
    }
}
