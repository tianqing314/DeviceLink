using DeviceLink.DataLink;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceLink.Session
{
    /// <summary>
    /// 直连会话 —— 基于数据链路层的直连会话实现。
    /// 适用于串口、TCP、USB等点对点连接场景。
    /// </summary>
    public class DirectSession : ISession
    {
        private readonly IDataLink _dataLink;
        private readonly SessionOptions _options;
        private readonly ILogger _logger;
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
                await _dataLink.OpenAsync(ct);
        }

        /// <inheritdoc/>
        public async Task CloseAsync()
        {
            await _dataLink.CloseAsync();
        }

        /// <inheritdoc/>
        public async Task<byte[]> SendAndReceiveAsync(byte[] request, CancellationToken ct = default)
        {
            if (request == null || request.Length == 0)
                throw new ArgumentException("请求数据为空", nameof(request));

            try
            {
                return await SendOnceAsync(request, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // 调用方取消，直接向上传播
                throw;
            }
            catch (Exception ex)
            {
                // 重试由数据链路层统一负责，会话层只做单次请求与异常映射
                _logger.LogDebug(ex, "[{Session}] 请求失败: {Error}", Name, ex.Message);
                throw MapException(ex);
            }
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
        /// 将下层异常映射为会话层异常，保留超时语义。
        /// SessionTimeoutException 继承自 SessionException，原有 catch (SessionException) 调用方不受影响。
        /// </summary>
        private static SessionException MapException(Exception ex)
        {
            if (ex is SessionException sessionEx)
                return sessionEx;

            if (ex is FrameTimeoutException)
                return new SessionTimeoutException($"请求超时: {ex.Message}", ex);

            return new SessionException($"请求失败: {ex.Message}", ex);
        }

        /// <inheritdoc/>
        public async Task SendOnlyAsync(byte[] request, CancellationToken ct = default)
        {
            if (request == null || request.Length == 0)
                return;

            await _dataLink.SendFrameAsync(request, ct);
        }

        /// <inheritdoc/>
        public async Task<byte[]> ReceiveOnlyAsync(CancellationToken ct = default)
        {
            return await _dataLink.ReceiveFrameAsync(ct);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _dataLink.Dispose();
        }
    }
}
