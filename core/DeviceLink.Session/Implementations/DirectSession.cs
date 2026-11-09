using DeviceLink.DataLink;
using DeviceLink.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceLink.Session
{
    /// <summary>
    /// 直连会话 —— 基于数据链路层的直连会话实现。
    /// 适用于串口、TCP、USB 等点对点连接场景。
    ///
    /// 承担三件事：
    /// · <b>串行化</b>：同一会话上的请求排队执行，保证一个请求只配一个响应；
    /// · <b>连接生命周期</b>：断线重连的决策权在 L5，不在链路层；
    /// · <b>安全重试</b>：仅当请求"确定未送达"（<see cref="RequestNotSentException"/>）时才重连重发。
    /// </summary>
    public class DirectSession : ISession
    {
        private readonly IDataLink _dataLink;
        private readonly SessionOptions _options;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private bool _disposed;

        /// <summary>
        /// 初始化直连会话
        /// </summary>
        /// <param name="dataLink">数据链路层</param>
        /// <param name="options">配置选项</param>
        /// <param name="logger">日志记录器</param>
        public DirectSession(
            IDataLink dataLink,
            SessionOptions? options = null,
            ILogger<DirectSession>? logger = null)
        {
            _dataLink = dataLink ?? throw new ArgumentNullException(nameof(dataLink));
            _options = options ?? new SessionOptions();
            _logger = logger ?? NullLogger<DirectSession>.Instance;
        }

        /// <summary>
        /// 初始化直连会话
        /// </summary>
        /// <param name="dataLink">数据链路层</param>
        /// <param name="name">会话名称</param>
        /// <param name="options">配置选项</param>
        /// <param name="logger">日志记录器</param>
        public DirectSession(
            IDataLink dataLink,
            string name,
            SessionOptions? options = null,
            ILogger<DirectSession>? logger = null)
            : this(dataLink, options, logger)
        {
            Name = name;
        }

        /// <inheritdoc/>
        public string Name { get; set; } = "DirectSession";

        /// <inheritdoc/>
        public bool IsOpen => _dataLink.IsOpen;

        /// <inheritdoc/>
        public async Task OpenAsync(CancellationToken ct = default)
        {
            if (!_dataLink.IsOpen)
                await _dataLink.OpenAsync(ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task CloseAsync()
        {
            await _dataLink.CloseAsync().ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<byte[]> SendAndReceiveAsync(byte[] request, CancellationToken ct = default)
        {
            if (request == null || request.Length == 0)
                throw new ArgumentException("请求数据为空", nameof(request));

            // 会话层自己保证串行化，不依赖下层实现细节（ISession 的线程安全契约）
            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                int maxAttempts = _options.MaxConnectRetries < 0 ? 0 : _options.MaxConnectRetries;

                for (int attempt = 0; ; attempt++)
                {
                    try
                    {
                        if (!_dataLink.IsOpen)
                            await _dataLink.OpenAsync(ct).ConfigureAwait(false);

                        return await SendOnceAsync(request, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // 调用方取消，直接向上传播
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex,
                            "[{Session}] 请求失败（第 {Attempt} 次）: {Error}", Name, attempt + 1, ex.Message);

                        // 安全重试的唯一判据：下层用异常类型明确告知"请求确定未送达"。
                        // 不再靠 !IsOpen 事后推断 —— 那个信号在写失败、半关闭等场景会漏判。
                        if (IsRequestNotDelivered(ex) && attempt < maxAttempts)
                        {
                            await SafeCloseAsync().ConfigureAwait(false);
                            await DelayAsync(_options.ReconnectDelayMs, ct).ConfigureAwait(false);
                            continue;
                        }

                        if (_options.ReconnectOnFailure)
                            await SafeCloseAsync().ConfigureAwait(false);

                        throw MapException(ex);
                    }
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>
        /// 请求是否确定未被送达 —— 决定能否在重连后安全重发。
        /// </summary>
        private static bool IsRequestNotDelivered(Exception ex)
        {
            return ex is RequestNotSentException || ex is ConnectionException;
        }

        /// <summary>
        /// 单次请求 —— 仅在显式配置 RequestTimeoutMs (&gt;0) 时启用会话级超时；
        /// 默认 0 表示禁用，由数据链路层的 ReceiveTimeoutMs 负责超时控制。
        /// </summary>
        private async Task<byte[]> SendOnceAsync(byte[] request, CancellationToken ct)
        {
            if (_options.RequestTimeoutMs <= 0)
                return await _dataLink.SendAndReceiveFrameAsync(request, ct).ConfigureAwait(false);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(_options.RequestTimeoutMs);
            try
            {
                return await _dataLink.SendAndReceiveFrameAsync(request, timeoutCts.Token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                throw new SessionTimeoutException(
                    $"请求超时（会话超时 {_options.RequestTimeoutMs}ms）");
            }
        }

        /// <summary>
        /// 将下层异常映射为会话层异常，保留超时/连接语义。
        /// 所有映射结果都继承 <see cref="SessionException"/>，原有 catch 调用方不受影响。
        /// </summary>
        private static SessionException MapException(Exception ex)
        {
            if (ex is SessionException sessionEx)
                return sessionEx;

            if (ex is RequestNotSentException || ex is ConnectionException)
                return new SessionConnectionException($"连接不可用: {ex.Message}", ex);

            if (ex is FrameTimeoutException)
                return new SessionTimeoutException($"请求超时: {ex.Message}", ex);

            return new SessionException($"请求失败: {ex.Message}", ex);
        }

        private static async Task DelayAsync(int milliseconds, CancellationToken ct)
        {
            if (milliseconds <= 0) return;
            await Task.Delay(milliseconds, ct).ConfigureAwait(false);
        }

        /// <summary>关闭连接并吞掉关闭过程中的异常 —— 关闭失败不应掩盖原始错误。</summary>
        private async Task SafeCloseAsync()
        {
            try
            {
                await _dataLink.CloseAsync().ConfigureAwait(false);
            }
            catch (Exception closeEx)
            {
                _logger.LogDebug(closeEx, "[{Session}] 关闭连接时发生异常（已忽略）", Name);
            }
        }

        /// <inheritdoc/>
        public async Task SendOnlyAsync(byte[] request, CancellationToken ct = default)
        {
            if (request == null || request.Length == 0)
                return;

            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await _dataLink.SendFrameAsync(request, ct).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<byte[]> ReceiveOnlyAsync(CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                return await _dataLink.ReceiveFrameAsync(ct).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _gate.Dispose();
            _dataLink.Dispose();
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                await _dataLink.CloseAsync().ConfigureAwait(false);
            }
            catch
            {
                // 关闭失败不影响释放
            }

            _gate.Dispose();
            _dataLink.Dispose();
        }
    }
}
