using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceLink.DataLink;
using DeviceLink.Session;
using DeviceLink.Transport;
using Xunit;

namespace DeviceLink.Tests
{
    /// <summary>
    /// 会话层韧性测试 —— 锁定"重连可以、盲重试不行"这条边界。
    ///
    /// 会话层（L5）确实拥有重连与重试的概念，但两者风险完全不同：
    /// · 重连：连接重建后重发，请求确定未送达，安全；
    /// · 重试：请求已发出但响应丢失，设备可能已执行，重发会造成重复写。
    /// </summary>
    public class SessionResilienceTests
    {
        [Fact]
        public async Task ConnectFailure_IsRetried_AndEventuallySucceeds()
        {
            var transport = new FlakyTransport(failConnectTimes: 1);
            var dataLink = new DirectDataLink(transport, new DelimiterFrameStrategy(new byte[] { 0 }));

            var options = new SessionOptions
            {
                MaxConnectRetries = 2,
                ReconnectDelayMs = 1
            };

            using var session = new DirectSession(dataLink, options);

            transport.OnWrite = () => transport.Enqueue(Encoding.ASCII.GetBytes("OK\0"));

            var response = await session.SendAndReceiveAsync(Encoding.ASCII.GetBytes("PING\0"));

            Assert.Equal("OK", Encoding.ASCII.GetString(response));
            Assert.Equal(2, transport.ConnectAttempts);   // 失败一次 + 成功一次
        }

        [Fact]
        public async Task ResponseTimeout_IsNotResent_BecauseRequestMayHaveBeenExecuted()
        {
            // 不注入任何响应，制造读超时
            var transport = new FlakyTransport(failConnectTimes: 0);
            var dataLink = new DirectDataLink(
                transport,
                new DelimiterFrameStrategy(new byte[] { 0 }),
                new DataLinkOptions
                {
                    ReceiveTimeoutMs = 150,
                    ReceiveIdleTimeoutMs = 30,
                    ReceivePollIntervalMs = 5
                });

            var options = new SessionOptions
            {
                MaxConnectRetries = 3,       // 即使允许重连
                ReconnectDelayMs = 1,
                ReconnectOnFailure = true
            };

            using var session = new DirectSession(dataLink, options);

            await Assert.ThrowsAnyAsync<SessionException>(
                () => session.SendAndReceiveAsync(Encoding.ASCII.GetBytes("WRITE\0")));

            // 关键断言：请求只发出一次。超时不重发，避免写命令被执行两次。
            Assert.Equal(1, transport.WriteCount);
        }

        [Fact]
        public async Task ConnectFailure_ExhaustingRetries_ThrowsSessionException()
        {
            var transport = new FlakyTransport(failConnectTimes: 10);
            var dataLink = new DirectDataLink(transport, new DelimiterFrameStrategy(new byte[] { 0 }));

            var options = new SessionOptions
            {
                MaxConnectRetries = 2,
                ReconnectDelayMs = 1
            };

            using var session = new DirectSession(dataLink, options);

            await Assert.ThrowsAnyAsync<SessionException>(
                () => session.SendAndReceiveAsync(Encoding.ASCII.GetBytes("PING\0")));

            // 1 次首发 + 2 次重连
            Assert.Equal(3, transport.ConnectAttempts);
            Assert.Equal(0, transport.WriteCount);
        }

        /// <summary>可注入连接失败与响应数据的模拟传输层。</summary>
        private sealed class FlakyTransport : IPhysicalTransport
        {
            private readonly Queue<byte[]> _receiveQueue = new();
            private int _remainingConnectFailures;
            private bool _isOpen;

            public FlakyTransport(int failConnectTimes)
            {
                _remainingConnectFailures = failConnectTimes;
            }

            public string Name => "Flaky";
            public bool IsOpen => _isOpen;
            public int ConnectAttempts { get; private set; }
            public int WriteCount { get; private set; }
            public Action? OnWrite { get; set; }

            public Task ConnectAsync(CancellationToken ct = default)
            {
                ConnectAttempts++;

                if (_remainingConnectFailures > 0)
                {
                    _remainingConnectFailures--;
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
                lock (_receiveQueue)
                {
                    if (_receiveQueue.Count == 0) return Task.FromResult(0);

                    var chunk = _receiveQueue.Dequeue();
                    int length = Math.Min(chunk.Length, count);
                    Array.Copy(chunk, 0, buffer, offset, length);
                    return Task.FromResult(length);
                }
            }

            public Task WriteAsync(byte[] data, int offset, int count, CancellationToken ct = default)
            {
                WriteCount++;
                OnWrite?.Invoke();
                return Task.CompletedTask;
            }

            public Task ClearReceiveBufferAsync(CancellationToken ct = default) => Task.CompletedTask;

            public void Enqueue(byte[] data)
            {
                lock (_receiveQueue)
                {
                    _receiveQueue.Enqueue(data);
                }
            }

            public void Dispose() { }
        }
    }
}
