using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceLink.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DeviceLink.DataLink
{
    /// <summary>
    /// 直连数据链路 —— 适用于串口、TCP、USB 等点对点连接。
    /// 
    /// 内建功能：
    /// · 接收循环 (轮询 → 读取 → 累积 → 帧解析)
    /// · 超时重试 (ReceiveTimeout / ReceiveIdleTimeout / MaxRetryCount)
    /// · 自动连接 (首次发送时自动 Connect)
    /// · 日志记录 (HEX + 文本格式收发日志)
    /// · 线程安全 (SemaphoreSlim 保证了串行化)
    /// </summary>
    public class DirectDataLink : IDataLink
    {
        private readonly IPhysicalTransport _transport;
        private readonly IFrameStrategy _frameStrategy;
        private readonly DataLinkOptions _options;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private bool _disposed;

        /// <summary>
        /// 初始化直连数据链路
        /// </summary>
        /// <param name="transport">物理传输层</param>
        /// <param name="frameStrategy">帧策略</param>
        /// <param name="options">配置选项</param>
        /// <param name="logger">日志记录器</param>
        public DirectDataLink(
            IPhysicalTransport transport,
            IFrameStrategy frameStrategy,
            DataLinkOptions? options = null,
            ILogger<DirectDataLink>? logger = null)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _frameStrategy = frameStrategy ?? throw new ArgumentNullException(nameof(frameStrategy));
            _options = options ?? new DataLinkOptions();
            _logger = logger ?? NullLogger<DirectDataLink>.Instance;
        }

        /// <summary>
        /// 初始化直连数据链路
        /// </summary>
        /// <param name="transport">物理传输层</param>
        /// <param name="frameStrategy">帧策略</param>
        /// <param name="name">数据链路名称</param>
        /// <param name="options">配置选项</param>
        /// <param name="logger">日志记录器</param>
        public DirectDataLink(
            IPhysicalTransport transport,
            IFrameStrategy frameStrategy,
            string name,
            DataLinkOptions? options = null,
            ILogger<DirectDataLink>? logger = null)
            : this(transport, frameStrategy, options, logger)
        {
            Name = name;
        }

        /// <inheritdoc/>
        public string Name { get; set; } = "DirectDataLink";

        /// <inheritdoc/>
        public IPhysicalTransport Transport => _transport;

        /// <inheritdoc/>
        public bool IsOpen => _transport.IsOpen;

        /// <inheritdoc/>
        public async Task OpenAsync(CancellationToken ct = default)
        {
            if (!_transport.IsOpen)
                await _transport.ConnectAsync(ct);
        }

        /// <inheritdoc/>
        public async Task CloseAsync()
        {
            await _transport.CloseAsync();
        }

        /// <inheritdoc/>
        public async Task<byte[]> SendAndReceiveFrameAsync(byte[] frameData, CancellationToken ct = default)
        {
            if (frameData == null || frameData.Length == 0)
                throw new ArgumentException("帧数据为空", nameof(frameData));

            await _semaphore.WaitAsync(ct);
            try
            {
                // 自动连接
                if (!_transport.IsOpen)
                {
                    await _transport.ConnectAsync(ct);
                    if (!_transport.IsOpen)
                        throw new ConnectionException("无法建立连接");
                }

                for (int retry = 0; retry <= _options.MaxRetryCount; retry++)
                {
                    var sw = Stopwatch.StartNew();

                    // 清空残留缓冲区
                    await _transport.ClearReceiveBufferAsync(ct);

                    // 组装帧并发送
                    var frame = _frameStrategy.BuildFrame(frameData);
                    await _transport.WriteAsync(frame, 0, frame.Length, ct);
                    LogSend(frame);

                    // 接收循环
                    var result = await ReceiveLoopAsync(ct);
                    sw.Stop();

                    if (result != null)
                    {
                        LogReceive(result);
                        return result;
                    }

                    if (retry < _options.MaxRetryCount)
                    {
                        _logger.LogDebug(
                            "[{DataLink}] 通讯失败 (第{Retry}/{Max}次重试)",
                            Name, retry + 1, _options.MaxRetryCount);

                        await Task.Delay(_options.RetryDelayMs, ct);
                    }
                }

                throw new FrameTimeoutException("接收超时或未收到有效数据");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task SendFrameAsync(byte[] frameData, CancellationToken ct = default)
        {
            if (frameData == null || frameData.Length == 0)
                return;

            await _semaphore.WaitAsync(ct);
            try
            {
                if (!_transport.IsOpen)
                    await _transport.ConnectAsync(ct);

                await _transport.ClearReceiveBufferAsync(ct);

                // 组装帧并发送
                var frame = _frameStrategy.BuildFrame(frameData);
                await _transport.WriteAsync(frame, 0, frame.Length, ct);
                LogSend(frame);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<byte[]> ReceiveFrameAsync(CancellationToken ct = default)
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                if (!_transport.IsOpen)
                    await _transport.ConnectAsync(ct);

                var result = await ReceiveLoopAsync(ct);
                if (result != null)
                {
                    LogReceive(result);
                    return result;
                }

                throw new FrameTimeoutException("接收超时或未收到有效数据");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 核心接收循环
        /// </summary>
        private async Task<byte[]?> ReceiveLoopAsync(CancellationToken ct)
        {
            var accumulated = new List<byte>();
            var requestTimer = Stopwatch.StartNew();
            var idleTimer = Stopwatch.StartNew();
            bool hasReceivedData = false;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                await Task.Delay(_options.ReceivePollIntervalMs, ct);

                // 尝试读取
                var buffer = new byte[4096];
                int read = await _transport.ReadAsync(buffer, 0, buffer.Length, ct);

                if (read > 0)
                {
                    accumulated.AddRange(buffer.AsSpan(0, read).ToArray());
                    hasReceivedData = true;
                    idleTimer.Restart();

                    // 帧解析检查
                    var accArray = accumulated.ToArray();
                    if (_frameStrategy.TryParseFrame(accArray, out int frameLen, out byte[] frameData))
                    {
                        return frameData;
                    }
                }

                // 首次响应超时检查
                if (!hasReceivedData &&
                    requestTimer.Elapsed > TimeSpan.FromMilliseconds(_options.ReceiveTimeoutMs))
                {
                    break;
                }

                // 接收空闲超时检查
                if (hasReceivedData &&
                    idleTimer.Elapsed > TimeSpan.FromMilliseconds(_options.ReceiveIdleTimeoutMs))
                {
                    if (accumulated.Count > 0)
                    {
                        // 返回累积的数据（可能不是完整帧）
                        return accumulated.ToArray();
                    }
                    break;
                }
            }

            return null;
        }

        private void LogSend(byte[] data)
        {
            _logger.LogDebug(
                "[{DataLink}] → Send ({Bytes} bytes): {Hex}  {Text}",
                Name, data.Length, BytesToHex(data), BytesToSafeText(data));
        }

        private void LogReceive(byte[] data)
        {
            _logger.LogDebug(
                "[{DataLink}] ← Recv ({Bytes} bytes): {Hex}  {Text}",
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

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _semaphore.Dispose();
            _transport.Dispose();
        }
    }
}
