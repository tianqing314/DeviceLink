using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceLink.Core.Framing;
using DeviceLink.Core.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DeviceLink.Core.Channel
{
    /// <summary>
    /// 直连设备通道 —— 适用于串口、TCP、USB 等点对点连接。
    /// 
    /// 内建功能：
    /// · 接收循环 (轮询 → 读取 → 累积 → 分帧匹配)
    /// · 超时重试 (RequestTimeout / ReceiveIdleTimeout / MaxRetryCount)
    /// · 自动连接 (首次发送时自动 Connect)
    /// · 日志记录 (HEX + 文本格式收发日志)
    /// · 线程安全 (SemaphoreSlim 保证了串行化)
    /// </summary>
    public class DirectChannel : IChannel
    {
        private readonly IByteTransport _transport;
        private readonly IFrameStrategy _frameStrategy;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private bool _disposed;

        public DirectChannel(
            IByteTransport transport,
            IFrameStrategy frameStrategy,
            ILogger<DirectChannel>? logger = null)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _frameStrategy = frameStrategy ?? throw new ArgumentNullException(nameof(frameStrategy));
            _logger = logger ?? NullLogger<DirectChannel>.Instance;
        }

        public DirectChannel(
            IByteTransport transport,
            IFrameStrategy frameStrategy,
            string name,
            ILogger<DirectChannel>? logger = null)
            : this(transport, frameStrategy, logger)
        {
            Name = name;
        }

        public string Name { get; set; } = "DirectChannel";
        public bool IsOpen => _transport.IsOpen;

        public async Task OpenAsync(CancellationToken ct = default)
        {
            if (!_transport.IsOpen)
                await _transport.ConnectAsync(ct);
        }

        public async Task CloseAsync()
        {
            await _transport.CloseAsync();
        }

        public async Task<ChannelResult> SendAndReceiveAsync(
            byte[] request,
            ChannelPolicy? policy = null,
            CancellationToken ct = default)
        {
            if (request == null || request.Length == 0)
                return ChannelResult.Fail("请求数据为空");

            policy ??= ChannelPolicy.Default;

            await _semaphore.WaitAsync(ct);
            try
            {
                // 自动连接
                if (!_transport.IsOpen)
                {
                    await _transport.ConnectAsync(ct);
                    if (!_transport.IsOpen)
                        return ChannelResult.Fail("无法建立连接");
                }

                for (int retry = 0; retry <= policy.MaxRetryCount; retry++)
                {
                    var sw = Stopwatch.StartNew();

                    // 清空残留缓冲区
                    await _transport.ClearReceiveBufferAsync(ct);

                    // 发送请求
                    await _transport.WriteAsync(request, 0, request.Length, ct);
                    LogSend(request);

                    // 接收循环
                    var result = await ReceiveLoopAsync(policy, ct);
                    sw.Stop();

                    if (result.Success)
                    {
                        LogReceive(result.Data);
                        return ChannelResult.Succeed(result.Data, retry, sw.Elapsed);
                    }

                    if (retry < policy.MaxRetryCount)
                    {
                        _logger.LogDebug(
                            "[{Channel}] 通讯失败 (第{Retry}/{Max}次重试): {Error}",
                            Name, retry + 1, policy.MaxRetryCount, result.Error);

                        await Task.Delay(policy.RetryDelay, ct);
                    }
                }

                return ChannelResult.Timeout(policy.MaxRetryCount);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task SendOnlyAsync(byte[] request, CancellationToken ct = default)
        {
            if (request == null || request.Length == 0)
                return;

            await _semaphore.WaitAsync(ct);
            try
            {
                if (!_transport.IsOpen)
                    await _transport.ConnectAsync(ct);

                await _transport.ClearReceiveBufferAsync(ct);
                await _transport.WriteAsync(request, 0, request.Length, ct);
                LogSend(request);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<ChannelResult> ReceiveOnlyAsync(
            ChannelPolicy? policy = null,
            CancellationToken ct = default)
        {
            policy ??= ChannelPolicy.Default;

            await _semaphore.WaitAsync(ct);
            try
            {
                if (!_transport.IsOpen)
                    await _transport.ConnectAsync(ct);

                var result = await ReceiveLoopAsync(policy, ct);
                if (result.Success)
                    LogReceive(result.Data);

                return result;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 核心接收循环 —— Xmas11 中复制了 6 次的代码只在这里实现一次。
        /// </summary>
        private async Task<ChannelResult> ReceiveLoopAsync(ChannelPolicy policy, CancellationToken ct)
        {
            var accumulated = new List<byte>();
            var requestTimer = Stopwatch.StartNew();
            var idleTimer = Stopwatch.StartNew();
            bool hasReceivedData = false;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                await Task.Delay(policy.ReceivePollIntervalMs, ct);

                // 尝试读取
                var buffer = new byte[4096];
                int read = await _transport.ReadAsync(buffer, 0, buffer.Length, ct);

                if (read > 0)
                {
                    accumulated.AddRange(buffer.AsSpan(0, read).ToArray());
                    hasReceivedData = true;
                    idleTimer.Restart();

                    // 帧匹配检查
                    var accArray = accumulated.ToArray();
                    if (_frameStrategy.TryMatchFrame(accArray, out int frameLen))
                    {
                        return ChannelResult.Succeed(accArray.AsSpan(0, frameLen).ToArray());
                    }
                }

                // 首次响应超时检查
                if (!hasReceivedData &&
                    requestTimer.Elapsed > policy.RequestTimeout)
                {
                    break;
                }

                // 接收空闲超时检查
                if (hasReceivedData &&
                    idleTimer.Elapsed > policy.ReceiveIdleTimeout)
                {
                    if (accumulated.Count > 0)
                        return ChannelResult.Succeed(accumulated.ToArray());

                    break;
                }
            }

            return ChannelResult.Fail("接收超时或未收到有效数据");
        }

        private void LogSend(byte[] data)
        {
            _logger.LogDebug(
                "[{Channel}] → Send ({Bytes} bytes): {Hex}  {Text}",
                Name, data.Length, BytesToHex(data), BytesToSafeText(data));
        }

        private void LogReceive(byte[] data)
        {
            _logger.LogDebug(
                "[{Channel}] ← Recv ({Bytes} bytes): {Hex}  {Text}",
                Name, data.Length, BytesToHex(data), BytesToSafeText(data));
        }

        private static string BytesToHex(byte[] data)
        {
            if (data.Length == 0) return "(empty)";
            var sb = new StringBuilder(data.Length * 3);
            for (int i = 0; i < Math.Min(data.Length, 64); i++)
            {
                sb.Append(data[i].ToString("X2"));
                sb.Append(' ');
            }
            if (data.Length > 64) sb.Append("...");
            return sb.ToString().Trim();
        }

        private static string BytesToSafeText(byte[] data)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < Math.Min(data.Length, 200); i++)
            {
                byte b = data[i];
                sb.Append(b >= 32 && b < 127 ? (char)b : '.');
            }
            if (data.Length > 200) sb.Append("...");
            return sb.ToString();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _semaphore.Dispose();
            _transport.Dispose();
        }
    }
}
