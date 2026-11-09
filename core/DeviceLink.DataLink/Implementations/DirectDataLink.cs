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
    /// · 超时控制 (ReceiveTimeout / ReceiveIdleTimeout)，<b>不含重试</b>
    /// · 自动连接 (首次发送时自动 Connect)
    /// · 日志记录 (HEX + 文本格式收发日志)
    ///
    /// 重试<b>不在本层</b>：本层在连接失败时抛 <see cref="RequestNotSentException"/>
    /// （确定未送达），在收到字节但定界失败时抛 <see cref="FrameFormatException"/>，
    /// 在完全无响应时抛 <see cref="FrameTimeoutException"/>。会话层据此决定能否安全重发。
    /// </summary>
    public class DirectDataLink : IDataLink, IRawFrameChannel
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
        /// <remarks>
        /// 本方法<b>不做任何重试</b>。重试属于会话层：只有会话层知道请求是否已送达，
        /// 而在"已送达但无响应"的情况下重发会让写类命令（Modbus 0x10 / SCPI SET）被执行两次。
        /// </remarks>
        public async Task<byte[]> SendAndReceiveFrameAsync(byte[] frameData, CancellationToken ct = default)
        {
            if (frameData == null || frameData.Length == 0)
                throw new ArgumentException("帧数据为空", nameof(frameData));

            await _semaphore.WaitAsync(ct);
            try
            {
                await EnsureConnectedAsync(ct).ConfigureAwait(false);

                // 清空残留缓冲区：避免上一次请求的迟到响应被当成本次响应
                await _transport.ClearReceiveBufferAsync(ct).ConfigureAwait(false);

                var frame = _frameStrategy.BuildFrame(frameData);
                await WriteFrameAsync(frame, ct).ConfigureAwait(false);
                LogSend(frame);

                var result = await ReceiveLoopAsync(ct).ConfigureAwait(false);
                LogReceive(result);
                return result;
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
                await EnsureConnectedAsync(ct).ConfigureAwait(false);
                await _transport.ClearReceiveBufferAsync(ct).ConfigureAwait(false);

                var frame = _frameStrategy.BuildFrame(frameData);
                await WriteFrameAsync(frame, ct).ConfigureAwait(false);
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
                await EnsureConnectedAsync(ct).ConfigureAwait(false);

                var result = await ReceiveLoopAsync(ct).ConfigureAwait(false);
                LogReceive(result);
                return result;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        // ─────────────────────── IRawFrameChannel ───────────────────────

        /// <inheritdoc/>
        public async Task<byte[]> ExchangeRawAsync(byte[] frame, RawFrameOptions options, CancellationToken ct = default)
        {
            if (frame == null || frame.Length == 0)
                throw new ArgumentException("帧数据为空", nameof(frame));

            options ??= new RawFrameOptions();

            await _semaphore.WaitAsync(ct);
            try
            {
                await EnsureConnectedAsync(ct).ConfigureAwait(false);
                await _transport.ClearReceiveBufferAsync(ct).ConfigureAwait(false);

                await WriteFrameAsync(frame, ct).ConfigureAwait(false);
                LogSend(frame);

                var response = await ReceiveRawLoopAsync(options, ct).ConfigureAwait(false);
                LogReceive(response);
                return response;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task SendRawAsync(byte[] frame, CancellationToken ct = default)
        {
            if (frame == null || frame.Length == 0)
                return;

            await _semaphore.WaitAsync(ct);
            try
            {
                await EnsureConnectedAsync(ct).ConfigureAwait(false);
                await _transport.ClearReceiveBufferAsync(ct).ConfigureAwait(false);

                await WriteFrameAsync(frame, ct).ConfigureAwait(false);
                LogSend(frame);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 原始帧接收循环：定界规则由调用方提供，链路层只负责读字节与超时。
        /// </summary>
        private async Task<byte[]> ReceiveRawLoopAsync(RawFrameOptions options, CancellationToken ct)
        {
            var accumulated = new List<byte>();
            var buffer = new byte[4096];
            var firstTimer = Stopwatch.StartNew();
            var idleTimer = Stopwatch.StartNew();
            bool hasReceivedData = false;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                int read = await _transport.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);

                if (read > 0)
                {
                    for (int i = 0; i < read; i++)
                        accumulated.Add(buffer[i]);

                    if (accumulated.Count > _options.MaxAccumulatedBytes)
                    {
                        _logger.LogWarning(
                            "[{DataLink}] 原始帧累积 {Bytes} 字节仍未定界，超过上限 {Max}，判定为坏帧并清空",
                            Name, accumulated.Count, _options.MaxAccumulatedBytes);

                        accumulated.Clear();
                        hasReceivedData = false;
                        firstTimer.Restart();
                        idleTimer.Restart();
                        continue;
                    }

                    hasReceivedData = true;
                    idleTimer.Restart();

                    if (options.TryParse != null)
                    {
                        int frameLength = options.TryParse(accumulated.ToArray());
                        if (frameLength > 0 && frameLength <= accumulated.Count)
                            return accumulated.GetRange(0, frameLength).ToArray();
                    }
                }
                else if (options.PollIntervalMs > 0)
                {
                    await Task.Delay(options.PollIntervalMs, ct).ConfigureAwait(false);
                }

                if (!hasReceivedData && firstTimer.ElapsedMilliseconds > options.FirstByteTimeoutMs)
                {
                    throw new FrameTimeoutException(
                        $"原始帧接收超时（{options.FirstByteTimeoutMs}ms 内未收到任何字节）");
                }

                if (hasReceivedData && idleTimer.ElapsedMilliseconds > options.IdleTimeoutMs)
                {
                    if (accumulated.Count > 0)
                    {
                        // 原始通道的契约：定界规则由调用方给出，链路层不擅自判定半帧非法，
                        // 但必须留下痕迹，便于排查"设备只回了半帧"这类问题。
                        _logger.LogDebug(
                            "[{DataLink}] 空闲超时，返回 {Bytes} 字节原始帧（未完全定界），交由调用方校验",
                            Name, accumulated.Count);

                        return accumulated.ToArray();
                    }

                    throw new FrameTimeoutException("原始帧接收超时或未收到有效数据");
                }
            }
        }

        /// <summary>
        /// 确保连接可用。连接失败一律包装为 <see cref="RequestNotSentException"/> ——
        /// 这是"确定未送达"的唯一可靠信号，供会话层决定是否可以安全重发。
        /// </summary>
        private async Task EnsureConnectedAsync(CancellationToken ct)
        {
            if (_transport.IsOpen) return;

            try
            {
                await _transport.ConnectAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RequestNotSentException(
                    $"连接未建立，请求未发送: {ex.Message}", ex);
            }

            if (!_transport.IsOpen)
                throw new RequestNotSentException("连接未建立，请求未发送");
        }

        /// <summary>
        /// 写入一帧。若写入失败且连接已断开，说明字节没有出去，按"未送达"上报；
        /// 否则可能是部分送达，按"结果未知"上报 —— 这两种情况的处置完全不同。
        /// </summary>
        private async Task WriteFrameAsync(byte[] frame, CancellationToken ct)
        {
            try
            {
                await _transport.WriteAsync(frame, 0, frame.Length, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (!_transport.IsOpen)
                {
                    throw new RequestNotSentException(
                        $"写入前连接已断开，请求未发送: {ex.Message}", ex);
                }

                // 连接仍在：无法确定对端是否收到，不包装为"未送达"
                throw;
            }
        }

        /// <summary>
        /// 核心接收循环。
        ///
        /// 契约：要么返回一个<b>经帧策略校验通过的完整帧</b>，要么抛异常。
        /// 绝不再把"未定界的残留字节"当作成功响应返回 —— 那会绕过 CRC/长度校验，
        /// 把半帧或串帧静默交给编解码器。
        /// </summary>
        private async Task<byte[]> ReceiveLoopAsync(CancellationToken ct)
        {
            var accumulated = new List<byte>();
            // 复用同一个读缓冲区：数据链路层由信号量串行化，局部缓冲区不会并发访问
            var buffer = new byte[4096];
            var requestTimer = Stopwatch.StartNew();
            var idleTimer = Stopwatch.StartNew();
            bool hasReceivedData = false;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                // 先读后等：有数据时立即处理，避免固定的轮询间隔叠加到响应延迟上
                int read = await _transport.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);
                if (read == 0)
                {
                    // 仅在无数据时等待，降低 CPU 空转
                    await Task.Delay(_options.ReceivePollIntervalMs, ct).ConfigureAwait(false);
                }

                if (read > 0)
                {
                    for (int i = 0; i < read; i++)
                        accumulated.Add(buffer[i]);

                    // 防御：对端持续发送无法定界的字节流时，累积缓冲区会无限增长。
                    // 链路层必须设硬上限，否则这就是一个内存耗尽的入口。
                    if (accumulated.Count > _options.MaxAccumulatedBytes)
                    {
                        _logger.LogWarning(
                            "[{DataLink}] 累积 {Bytes} 字节仍未定界，超过上限 {Max}，判定为坏帧并清空缓冲区",
                            Name, accumulated.Count, _options.MaxAccumulatedBytes);

                        accumulated.Clear();
                        hasReceivedData = false;
                        requestTimer.Restart();
                        idleTimer.Restart();
                        continue;
                    }

                    hasReceivedData = true;
                    idleTimer.Restart();

                    // 帧解析检查
                    var accArray = accumulated.ToArray();
                    if (_frameStrategy.TryParseFrame(accArray, out _, out byte[] frameData))
                    {
                        return frameData;
                    }
                }

                // 首次响应超时检查：一个字节都没收到
                if (!hasReceivedData &&
                    requestTimer.Elapsed > TimeSpan.FromMilliseconds(_options.ReceiveTimeoutMs))
                {
                    throw new FrameTimeoutException(
                        $"接收超时（{_options.ReceiveTimeoutMs}ms 内未收到任何字节）");
                }

                // 接收空闲超时检查：收到了字节，但始终没能定界成完整帧
                if (hasReceivedData &&
                    idleTimer.Elapsed > TimeSpan.FromMilliseconds(_options.ReceiveIdleTimeoutMs))
                {
                    var residue = accumulated.Count;
                    accumulated.Clear();

                    if (residue == 0)
                    {
                        throw new FrameTimeoutException("接收超时或未收到有效数据");
                    }

                    _logger.LogWarning(
                        "[{DataLink}] 收到 {Bytes} 字节但未能定界为完整帧，判定为坏帧并丢弃。帧策略: {Strategy}",
                        Name, residue, _frameStrategy.Name);

                    throw new FrameFormatException(
                        $"收到 {residue} 字节但未能定界为完整帧（帧策略 {_frameStrategy.Name}），已丢弃");
                }
            }
        }

        private void LogSend(byte[] data)
        {
            _logger.LogDebug(
                "[{DataLink}] → 帧发送 ({Bytes} bytes)\n" +
                "  HEX: {Hex}\n" +
                "  TXT: {Text}",
                Name, data.Length, BytesToHex(data), BytesToSafeText(data));
        }

        private void LogReceive(byte[] data)
        {
            _logger.LogDebug(
                "[{DataLink}] ← 帧接收 ({Bytes} bytes)\n" +
                "  HEX: {Hex}\n" +
                "  TXT: {Text}",
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
