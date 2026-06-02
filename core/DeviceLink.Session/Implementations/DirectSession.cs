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

            for (int retry = 0; retry <= _options.MaxRetryCount; retry++)
            {
                try
                {
                    var response = await _dataLink.SendAndReceiveFrameAsync(request, ct);
                    return response;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    if (retry < _options.MaxRetryCount)
                    {
                        _logger.LogWarning(ex,
                            "[{Session}] 请求失败 (第{Retry}/{Max}次重试): {Error}",
                            Name, retry + 1, _options.MaxRetryCount, ex.Message);

                        await Task.Delay(_options.RetryDelayMs, ct);
                    }
                    else
                    {
                        throw new SessionException($"请求失败: {ex.Message}", ex);
                    }
                }
            }

            throw new SessionTimeoutException("请求超时");
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
