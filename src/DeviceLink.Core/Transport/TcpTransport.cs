using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceLink.Core.Transport
{
    /// <summary>
    /// TCP 传输实现 —— 封装 System.Net.Sockets.TcpClient
    /// </summary>
    public class TcpTransport : IByteTransport
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private readonly TcpOptions _options;

        public TcpTransport(TcpOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public TcpTransport(string host, int port, int connectTimeoutMs = 5000)
            : this(new TcpOptions
            {
                Host = host,
                Port = port,
                ConnectTimeoutMs = connectTimeoutMs
            })
        { }

        public string Name => $"{_options.Host}:{_options.Port}";

        public bool IsOpen => _client?.Connected ?? false;

        public async Task ConnectAsync(CancellationToken ct = default)
        {
            if (IsOpen) return;

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
                throw new TimeoutException(
                    $"TCP 连接超时 ({_options.ConnectTimeoutMs}ms): {Name}");
            }

            _stream = _client.GetStream();
        }

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
            return Task.CompletedTask;
        }

        public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct = default)
        {
            if (_client == null || !_client.Connected || _stream == null)
                return 0;

            int available = Math.Min(_client.Available, count);
            if (available == 0)
                return 0;

            return await _stream.ReadAsync(buffer, offset, available, ct);
        }

        public async Task WriteAsync(byte[] data, int offset, int count, CancellationToken ct = default)
        {
            if (_stream == null || !_client?.Connected == true)
                return;

            await _stream.WriteAsync(data, offset, count, ct);
            await _stream.FlushAsync(ct);
        }

        public Task ClearReceiveBufferAsync(CancellationToken ct = default)
        {
            if (_client != null && _client.Available > 0)
            {
                var dummy = new byte[Math.Min(_client.Available, 4096)];
                _stream?.Read(dummy, 0, dummy.Length);
            }
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            CloseAsync();
        }
    }

    /// <summary>
    /// TCP 配置选项
    /// </summary>
    public class TcpOptions
    {
        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 10001;
        public int ConnectTimeoutMs { get; set; } = 5000;
        public int ReadBufferSize { get; set; } = 8192;
        public int WriteBufferSize { get; set; } = 4096;
    }
}
