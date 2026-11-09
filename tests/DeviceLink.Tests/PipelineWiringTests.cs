using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceLink.DataLink;
using DeviceLink.Pipeline;
using DeviceLink.Protocol;
using DeviceLink.Session;
using DeviceLink.Transport;
using Xunit;

namespace DeviceLink.Tests
{
    /// <summary>
    /// 锁定"构建器把设计和实现接上线"这件事 —— 这些点过去全部是断的：
    /// · 帧策略靠设备基类 switch，漏映射就静默退化成分隔符帧；
    /// · OSI 校验只存在于无人调用的 OsiStackBuilder 里；
    /// · UseLoggerFactory 是个空开关；
    /// · 半帧会被当成成功响应返回。
    /// </summary>
    public class PipelineWiringTests
    {
        // ─────────────────── 帧策略推导 ───────────────────

        [Fact]
        public void ModbusTcpCodec_ResolvesToLengthPrefixedFraming()
        {
            var strategy = ProtocolBindings.TryCreateFrameStrategy(
                new ModbusTcpCodec(unitId: 1), transport: null);

            Assert.IsType<LengthPrefixedFrameStrategy>(strategy);
        }

        [Fact]
        public void ModbusRtuCodec_ResolvesToCrcFraming()
        {
            var strategy = ProtocolBindings.TryCreateFrameStrategy(
                new ModbusRtuCodec(1), new SerialPortTransport("COM1", 9600));

            Assert.IsType<ModbusRtuFrameStrategy>(strategy);
        }

        [Fact]
        public void ScpiCodec_UsesItsOwnTerminator_NotNul()
        {
            var strategy = ProtocolBindings.TryCreateFrameStrategy(
                new ScpiCodec("\r\n"), new TcpTransport("127.0.0.1", 5025));

            var delimiter = Assert.IsType<DelimiterFrameStrategy>(strategy);

            // 用 \0 定帧的旧行为会让 SCPI 响应永远定不了界
            var response = Encoding.ASCII.GetBytes("1.234\r\n");
            Assert.True(delimiter.TryParseFrame(response, out _, out var frame));
            Assert.Equal("1.234", Encoding.ASCII.GetString(frame));
        }

        [Fact]
        public void UnregisteredProtocol_IsNotSilentlyGuessed()
        {
            Assert.Null(ProtocolBindings.TryCreateFrameStrategy(
                new UnknownCodec(), new SerialPortTransport("COM1", 9600)));
        }

        [Fact]
        public void Builder_ThrowsInsteadOfFallingBackToDelimiter_ForUnregisteredProtocol()
        {
            var builder = new CommunicationPipelineBuilder()
                .UseTransport(new LoopbackTransport())
                .UseProtocol(new UnknownCodec());

            var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
            Assert.Contains("未登记帧策略", ex.Message);
        }

        // ─────────────────── OSI 校验接入真实路径 ───────────────────

        [Fact]
        public void Build_ProducesOsiReport_ForKnownProtocol()
        {
            using var pipeline = new CommunicationPipelineBuilder()
                .UseTransport(new LoopbackTransport())
                .UseProtocol(new ModbusRtuCodec(1))
                .Build();

            Assert.NotNull(pipeline.OsiReport);
            Assert.Equal("Modbus RTU over RS-485", pipeline.OsiReport!.Profile.Name);
            Assert.True(pipeline.OsiReport.IsValid);

            // 会话层是框架增强（extendable），不应被报成越界
            Assert.DoesNotContain(pipeline.OsiReport.Violations,
                v => v.Kind == Osi.OsiViolationKind.OutOfProfile);
        }

        [Fact]
        public void ModbusTcp_KeepsSessionLayerAsGap()
        {
            using var pipeline = new CommunicationPipelineBuilder()
                .UseTransport(new TcpTransport("127.0.0.1", 502))
                .UseProtocol(new ModbusTcpCodec())
                .Build();

            var report = pipeline.OsiReport;
            Assert.NotNull(report);
            Assert.Equal("Modbus TCP", report!.Profile.Name);

            // 结论必须保持：Modbus TCP 没有会话层
            Assert.True(report.Profile.Gaps.Contains(Osi.OsiLayer.Session));
            Assert.False(report.Profile.Implemented.Contains(Osi.OsiLayer.Session));
        }

        [Fact]
        public void StrictOsi_ThrowsWhenProfileCannotBeSatisfied()
        {
            // 没有传输层也没有会话层 → 缺 L1，严格模式下必须炸
            var ex = Assert.ThrowsAny<Exception>(() =>
                new CommunicationPipelineBuilder()
                    .UseProtocol(new ModbusRtuCodec(1))
                    .StrictOsi()
                    .Build());

            Assert.True(ex is InvalidOperationException || ex is Osi.OsiStackViolationException);
        }

        // ─────────────────── 半帧不再被当成功响应 ───────────────────

        [Fact]
        public async Task IncompleteFrame_IsRejected_NotReturnedAsSuccess()
        {
            // 设备只回了 "OK"（没有 \0 终止符），且发送完毕后不再有数据
            var transport = new ScriptedTransport(Encoding.ASCII.GetBytes("OK"));
            var dataLink = new DirectDataLink(
                transport,
                new DelimiterFrameStrategy(new byte[] { 0 }),
                new DataLinkOptions
                {
                    ReceiveTimeoutMs = 3000,
                    ReceiveIdleTimeoutMs = 30,
                    ReceivePollIntervalMs = 5
                });

            await Assert.ThrowsAsync<FrameFormatException>(() =>
                dataLink.SendAndReceiveFrameAsync(Encoding.ASCII.GetBytes("PING")));
        }

        [Fact]
        public async Task IncompleteFrame_IsNotRetried_AtDataLinkLevel()
        {
            var transport = new ScriptedTransport(Encoding.ASCII.GetBytes("OK"));
            var dataLink = new DirectDataLink(
                transport,
                new DelimiterFrameStrategy(new byte[] { 0 }),
                new DataLinkOptions
                {
                    ReceiveTimeoutMs = 3000,
                    ReceiveIdleTimeoutMs = 30,
                    ReceivePollIntervalMs = 5
                });

            await Assert.ThrowsAnyAsync<DataLinkException>(() =>
                dataLink.SendAndReceiveFrameAsync(Encoding.ASCII.GetBytes("WRITE")));

            // 链路层不再有重试：写命令只应发出一次
            Assert.Equal(1, transport.WriteCount);
        }

        // ─────────────────── 未送达语义 ───────────────────

        [Fact]
        public async Task ConnectFailure_SurfacesAsRequestNotSent()
        {
            var transport = new ScriptedTransport(response: null) { FailConnect = true };
            var dataLink = new DirectDataLink(
                transport, new DelimiterFrameStrategy(new byte[] { 0 }));

            await Assert.ThrowsAsync<RequestNotSentException>(() =>
                dataLink.SendAndReceiveFrameAsync(Encoding.ASCII.GetBytes("PING")));
        }

        [Fact]
        public async Task RequestNotSent_IsRetriedBySession_AndEventuallySucceeds()
        {
            var transport = new ScriptedTransport(Encoding.ASCII.GetBytes("OK\0"))
            {
                FailConnectTimes = 1
            };

            var dataLink = new DirectDataLink(
                transport, new DelimiterFrameStrategy(new byte[] { 0 }));

            using var session = new DirectSession(dataLink, new SessionOptions
            {
                MaxConnectRetries = 2,
                ReconnectDelayMs = 1
            });

            var response = await session.SendAndReceiveAsync(Encoding.ASCII.GetBytes("PING"));

            Assert.Equal("OK", Encoding.ASCII.GetString(response));
            Assert.Equal(2, transport.ConnectAttempts);
        }

        // ─────────────────── 测试替身 ───────────────────

        private sealed class UnknownCodec : IProtocolCodec
        {
            public string ProtocolName => "Unknown";
            public bool PayloadIncludesTerminator => false;
            public byte[] Encode(Command command) => Array.Empty<byte>();
            public string DecodeText(byte[] raw) => string.Empty;
            public bool IsErrorResponse(byte[] raw, out string errorMessage)
            {
                errorMessage = string.Empty;
                return false;
            }
        }

        /// <summary>按脚本返回一次响应字节的传输层。</summary>
        private sealed class ScriptedTransport : IPhysicalTransport
        {
            private readonly byte[]? _response;
            private bool _isOpen;
            private bool _delivered;

            public ScriptedTransport(byte[]? response) => _response = response;

            public bool FailConnect { get; set; }
            public int FailConnectTimes { get; set; }
            public int WriteCount { get; private set; }
            public int ConnectAttempts { get; private set; }

            public string Name => "Scripted";
            public bool IsOpen => _isOpen;

            public Task ConnectAsync(CancellationToken ct = default)
            {
                ConnectAttempts++;

                if (FailConnect)
                    return Task.FromException(new ConnectionException("模拟连接失败"));

                if (FailConnectTimes > 0)
                {
                    FailConnectTimes--;
                    return Task.FromException(new ConnectionException("模拟连接失败"));
                }

                _isOpen = true;
                return Task.CompletedTask;
            }

            public Task CloseAsync()
            {
                _isOpen = false;
                return Task.CompletedTask;
            }

            public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct = default)
            {
                if (_response == null || !_isOpen) return Task.FromResult(0);

                // 只交付一次：真实设备不会把同一段响应当成无限流反复吐出来
                if (_delivered) return Task.FromResult(0);
                _delivered = true;

                int length = Math.Min(_response.Length, count);
                Array.Copy(_response, 0, buffer, offset, length);
                return Task.FromResult(length);
            }

            public Task WriteAsync(byte[] data, int offset, int count, CancellationToken ct = default)
            {
                WriteCount++;
                return Task.CompletedTask;
            }

            public Task ClearReceiveBufferAsync(CancellationToken ct = default) => Task.CompletedTask;

            public void Dispose() => _isOpen = false;
        }
    }
}
