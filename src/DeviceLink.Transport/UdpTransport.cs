using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DeviceLink.Transport
{
    /// <summary>
    /// UDP 传输实现 —— 封装 System.Net.Sockets.UdpClient
    /// </summary>
    public class UdpTransport : IPhysicalTransport
    {
        private UdpClient? _client;
        private IPEndPoint? _remoteEndPoint;
        private readonly UdpOptions _options;
        private readonly ILogger<UdpTransport>? _logger;

        /// <summary>
        /// 初始化UDP传输
        /// </summary>
        /// <param name="options">UDP配置选项</param>
        /// <param name="logger">日志记录器</param>
        public UdpTransport(UdpOptions options, ILogger<UdpTransport>? logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        /// <summary>
        /// 初始化UDP传输
        /// </summary>
        /// <param name="host">远程主机地址</param>
        /// <param name="port">远程端口号</param>
        /// <param name="localPort">本地端口号（0表示自动分配）</param>
        /// <param name="logger">日志记录器</param>
        public UdpTransport(string host, int port, int localPort = 0, ILogger<UdpTransport>? logger = null)
            : this(new UdpOptions
            {
                RemoteHost = host,
                RemotePort = port,
                LocalPort = localPort
            }, logger)
        { }

        /// <inheritdoc/>
        public string Name => $"{_options.RemoteHost}:{_options.RemotePort}";

        /// <inheritdoc/>
        public bool IsOpen => _client != null;

        /// <inheritdoc/>
        public Task ConnectAsync(CancellationToken ct = default)
        {
            if (IsOpen) return Task.CompletedTask;

            try
            {
                _remoteEndPoint = new IPEndPoint(
                    IPAddress.Parse(_options.RemoteHost), _options.RemotePort);

                if (_options.LocalPort > 0)
                {
                    _client = new UdpClient(_options.LocalPort);
                }
                else
                {
                    _client = new UdpClient();
                }

                _client.Client.ReceiveBufferSize = _options.ReadBufferSize > 0 ? _options.ReadBufferSize : 8192;
                _client.Client.SendBufferSize = _options.WriteBufferSize > 0 ? _options.WriteBufferSize : 4096;

                _logger?.LogInformation("UDP {Name} 已连接", Name);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "连接UDP {Name} 失败", Name);
                throw new ConnectionException($"连接UDP {Name} 失败: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public Task CloseAsync()
        {
            if (_client != null)
            {
                try { _client.Close(); } catch { }
                try { _client.Dispose(); } catch { }
                _client = null;
                _logger?.LogInformation("UDP {Name} 已关闭", Name);
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct = default)
        {
            if (_client == null)
                return 0;

            try
            {
                var result = await _client.ReceiveAsync();
                int toRead = Math.Min(result.Buffer.Length, count);
                Array.Copy(result.Buffer, 0, buffer, offset, toRead);
                _logger?.LogDebug("从UDP {Name} 读取了 {Count} 字节", Name, toRead);
                return toRead;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "从UDP {Name} 读取数据失败", Name);
                throw new TransportException($"从UDP {Name} 读取数据失败: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task WriteAsync(byte[] data, int offset, int count, CancellationToken ct = default)
        {
            if (_client == null || _remoteEndPoint == null)
                return;

            try
            {
                var sendData = new byte[count];
                Array.Copy(data, offset, sendData, 0, count);
                await _client.SendAsync(sendData, sendData.Length, _remoteEndPoint);
                _logger?.LogDebug("向UDP {Name} 写入了 {Count} 字节", Name, count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "向UDP {Name} 写入数据失败", Name);
                throw new TransportException($"向UDP {Name} 写入数据失败: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public Task ClearReceiveBufferAsync(CancellationToken ct = default)
        {
            if (_client != null && _client.Available > 0)
            {
                try
                {
                    while (_client.Available > 0)
                    {
                        IPEndPoint? remoteEP = null;
                        _client.Receive(ref remoteEP);
                    }
                    _logger?.LogDebug("已清空UDP {Name} 接收缓冲区", Name);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "清空UDP {Name} 接收缓冲区时发生异常", Name);
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

    /// <summary>
    /// UDP 配置选项
    /// </summary>
    public class UdpOptions
    {
        /// <summary>
        /// 远程主机地址
        /// </summary>
        public string RemoteHost { get; set; } = "127.0.0.1";

        /// <summary>
        /// 远程端口号
        /// </summary>
        public int RemotePort { get; set; } = 10001;

        /// <summary>
        /// 本地端口号（0表示自动分配）
        /// </summary>
        public int LocalPort { get; set; } = 0;

        /// <summary>
        /// 读取缓冲区大小
        /// </summary>
        public int ReadBufferSize { get; set; } = 8192;

        /// <summary>
        /// 写入缓冲区大小
        /// </summary>
        public int WriteBufferSize { get; set; } = 4096;
    }
}
