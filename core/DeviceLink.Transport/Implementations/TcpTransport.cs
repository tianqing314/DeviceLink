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
        private volatile bool _isConnected;
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
        public bool IsOpen => _isConnected;

        /// <inheritdoc/>
        public async Task ConnectAsync(CancellationToken ct = default)
        {
            if (IsOpen) return;

            try
            {
                _client = new TcpClient
                {
                    NoDelay = true,  // 禁用 Nagle 算法，确保小数据包立即发送
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
                _isConnected = true;
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
            _isConnected = false;
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
            if (_client == null || !_isConnected || _stream == null)
                return 0;

            try
            {
                // 先检查是否有数据可用
                int available = Math.Min(_client.Available, count);
                if (available > 0)
                {
                    int read = await _stream.ReadAsync(buffer, offset, available, ct);
                    _logger?.LogDebug("从TCP {Name} 读取了 {Count} 字节", Name, read);
                    return read;
                }

                // 没有数据时：短暂等待（最多50ms）等待数据到达，避免轮询竞态
                try
                {
                    using var readCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    readCts.CancelAfter(50);
                    int read = await _stream.ReadAsync(buffer, offset, 1, readCts.Token);
                    if (read > 0)
                    {
                        // 读完第1字节后，立即读取剩余可用数据
                        int remaining = Math.Min(_client.Available, count - 1);
                        if (remaining > 0)
                        {
                            int more = await _stream.ReadAsync(buffer, offset + 1, remaining, ct);
                            return read + more;
                        }
                        return read;
                    }
                }
                catch (OperationCanceledException)
                {
                    // 内部50ms超时或外部取消——让 ReceiveRawFrameAsync 的循环自行处理
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                // CancellationToken 取消，直接向上传播（不包装）
                throw;
            }
            catch (IOException)
            {
                _isConnected = false;
                throw new TransportException($"TCP {Name} 连接已断开");
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _logger?.LogError(ex, "从TCP {Name} 读取数据失败", Name);
                throw new TransportException($"从TCP {Name} 读取数据失败: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task WriteAsync(byte[] data, int offset, int count, CancellationToken ct = default)
        {
            if (_stream == null)
                throw new TransportException($"TCP {Name} 写入失败: 网络流未初始化");
            if (!_isConnected)
                throw new TransportException($"TCP {Name} 写入失败: 连接已断开");

            try
            {
                await _stream.WriteAsync(data, offset, count, ct);
                await _stream.FlushAsync(ct);
                _logger?.LogDebug("向TCP {Name} 写入了 {Count} 字节", Name, count);
            }
            catch (IOException)
            {
                _isConnected = false;
                throw new TransportException($"TCP {Name} 连接已断开");
            }
            catch (Exception ex)
            {
                _isConnected = false;
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
