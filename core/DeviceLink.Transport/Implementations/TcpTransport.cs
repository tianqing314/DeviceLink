using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DeviceLink.Transport
{
    /// <summary>
    /// TCP 传输实现 —— 封装 System.Net.Sockets.TcpClient
    /// </summary>
    public class TcpTransport : IPhysicalTransport
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private readonly TcpOptions _options;
        private readonly ILogger<TcpTransport>? _logger;

        /// <summary>
        /// 初始化TCP传输
        /// </summary>
        /// <param name="options">TCP配置选项</param>
        /// <param name="logger">日志记录器</param>
        public TcpTransport(TcpOptions options, ILogger<TcpTransport>? logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        /// <summary>
        /// 初始化TCP传输
        /// </summary>
        /// <param name="host">主机地址</param>
        /// <param name="port">端口号</param>
        /// <param name="connectTimeoutMs">连接超时时间（毫秒）</param>
        /// <param name="logger">日志记录器</param>
        public TcpTransport(string host, int port, int connectTimeoutMs = 5000,
            ILogger<TcpTransport>? logger = null)
            : this(new TcpOptions
            {
                Host = host,
                Port = port,
                ConnectTimeoutMs = connectTimeoutMs
            }, logger)
        { }

        /// <inheritdoc/>
        public string Name => $"{_options.Host}:{_options.Port}";

        /// <inheritdoc/>
        public bool IsOpen => _client?.Connected ?? false;

        /// <inheritdoc/>
        public async Task ConnectAsync(CancellationToken ct = default)
        {
            if (IsOpen) return;

            try
            {
                _client = new TcpClient
                {
                    ReceiveBufferSize = _options.ReadBufferSize > 0 ? _options.ReadBufferSize : 8192,
                    SendBufferSize = _options.WriteBufferSize > 0 ? _options.WriteBufferSize : 4096,
                    ReceiveTimeout = 0,
                    SendTimeout = 0
                };

                using var timeoutCts = new CancellationTokenSource(
                    TimeSpan.FromMilliseconds(_options.ConnectTimeoutMs));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

                try
                {
                    await _client.ConnectAsync(_options.Host, _options.Port);
                }
                catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                {
                    throw new TransportTimeoutException(
                        $"TCP 连接超时 ({_options.ConnectTimeoutMs}ms): {Name}");
                }

                _stream = _client.GetStream();
                _logger?.LogInformation("TCP {Name} 已连接", Name);
            }
            catch (Exception ex) when (ex is not TransportTimeoutException)
            {
                _logger?.LogError(ex, "连接TCP {Name} 失败", Name);
                throw new ConnectionException($"连接TCP {Name} 失败: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public Task CloseAsync()
        {
            if (_stream != null)
            {
                try { _stream.Close(); } catch { }
                try { _stream.Dispose(); } catch { }
                _stream = null;
            }
            if (_client != null)
            {
                try { _client.Close(); } catch { }
                try { _client.Dispose(); } catch { }
                _client = null;
            }
            _logger?.LogInformation("TCP {Name} 已关闭", Name);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct = default)
        {
            if (_client == null || !_client.Connected || _stream == null)
                return 0;

            try
            {
                int available = Math.Min(_client.Available, count);
                if (available == 0)
                    return 0;

                int read = await _stream.ReadAsync(buffer, offset, available, ct);
                _logger?.LogDebug("从TCP {Name} 读取了 {Count} 字节", Name, read);
                return read;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "从TCP {Name} 读取数据失败", Name);
                throw new TransportException($"从TCP {Name} 读取数据失败: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task WriteAsync(byte[] data, int offset, int count, CancellationToken ct = default)
        {
            if (_stream == null || _client?.Connected != true)
                return;

            try
            {
                await _stream.WriteAsync(data, offset, count, ct);
                await _stream.FlushAsync(ct);
                _logger?.LogDebug("向TCP {Name} 写入了 {Count} 字节", Name, count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "向TCP {Name} 写入数据失败", Name);
                throw new TransportException($"向TCP {Name} 写入数据失败: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public Task ClearReceiveBufferAsync(CancellationToken ct = default)
        {
            if (_client != null && _client.Available > 0)
            {
                try
                {
                    var dummy = new byte[Math.Min(_client.Available, 4096)];
                    _stream?.Read(dummy, 0, dummy.Length);
                    _logger?.LogDebug("已清空TCP {Name} 接收缓冲区", Name);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "清空TCP {Name} 接收缓冲区时发生异常", Name);
                }
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            CloseAsync().Wait();
        }
    }

}
