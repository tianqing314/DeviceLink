using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace DeviceLink.Session
{
    /// <summary>
    /// MQTT 会话 —— 基于 MQTT 协议的会话实现。
    /// 适用于通过 MQTT Broker 进行设备通信的场景。
    /// 
    /// 参考 Xmas11 项目的 iMqttClient 实现模式，适配 DeviceLink 分层架构：
    /// - 使用 MQTTnet 4.3.x
    /// - 使用 TaskCompletionSource 替代轮询，提高响应效率
    /// - 支持请求-响应模式（发布到 RequestTopic，等待 ResponseTopic 响应）
    /// </summary>
    public class MqttSession : ISession
    {
        private readonly MqttSessionOptions _options;
        private readonly ILogger _logger;
        private bool _disposed;
        private bool _isOpen;

        private IMqttClient? _mqttClient;
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// 等待响应的 TaskCompletionSource（由 ApplicationMessageReceived 事件唤醒）
        /// </summary>
        private TaskCompletionSource<byte[]>? _responseTcs;

        /// <summary>
        /// 初始化MQTT会话
        /// </summary>
        /// <param name="options">MQTT配置选项</param>
        /// <param name="logger">日志记录器</param>
        public MqttSession(
            MqttSessionOptions options,
            ILogger<MqttSession>? logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? NullLogger<MqttSession>.Instance;
        }

        /// <summary>
        /// 初始化MQTT会话
        /// </summary>
        /// <param name="brokerHost">MQTT Broker地址</param>
        /// <param name="brokerPort">MQTT Broker端口</param>
        /// <param name="requestTopic">请求主题</param>
        /// <param name="responseTopic">响应主题</param>
        /// <param name="logger">日志记录器</param>
        public MqttSession(
            string brokerHost,
            int brokerPort,
            string requestTopic,
            string responseTopic,
            ILogger<MqttSession>? logger = null)
            : this(new MqttSessionOptions
            {
                BrokerHost = brokerHost,
                BrokerPort = brokerPort,
                RequestTopic = requestTopic,
                ResponseTopic = responseTopic
            }, logger)
        { }

        /// <inheritdoc/>
        public string Name => $"MQTT({_options.BrokerHost}:{_options.BrokerPort})";

        /// <inheritdoc/>
        public bool IsOpen => _isOpen && _mqttClient?.IsConnected == true;

        /// <inheritdoc/>
        public async Task OpenAsync(CancellationToken ct = default)
        {
            if (_isOpen) return;

            try
            {
                var factory = new MqttFactory();

                // 配置客户端选项
                var optionsBuilder = new MqttClientOptionsBuilder()
                    .WithClientId(_options.ClientId)
                    .WithTcpServer(_options.BrokerHost, _options.BrokerPort)
                    .WithCleanSession(_options.CleanSession)
                    .WithKeepAlivePeriod(TimeSpan.FromSeconds(_options.KeepAliveSeconds));

                // 可选认证
                if (!string.IsNullOrEmpty(_options.Username))
                {
                    optionsBuilder.WithCredentials(_options.Username, _options.Password);
                }

                // 可选 TLS
                if (_options.UseTls)
                {
                    optionsBuilder.WithTlsOptions(o => o.UseTls());
                }

                var clientOptions = optionsBuilder.Build();

                // 创建客户端并注册事件
                _mqttClient = factory.CreateMqttClient();

                // 消息接收事件 —— 匹配 ResponseTopic 后唤醒等待的 SendAndReceiveAsync
                _mqttClient.ApplicationMessageReceivedAsync += OnApplicationMessageReceived;

                // 连接成功事件
                _mqttClient.ConnectedAsync += OnConnected;

                // 断开连接事件
                _mqttClient.DisconnectedAsync += OnDisconnected;

                // 连接到 Broker
                await _mqttClient.ConnectAsync(clientOptions, ct);

                // 订阅响应主题
                await _mqttClient.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
                    .WithTopicFilter(_options.ResponseTopic)
                    .Build(), ct);

                _isOpen = true;
                _logger?.LogInformation("MQTT会话 {Name} 已连接，订阅主题: {Topic}", Name, _options.ResponseTopic);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "连接MQTT会话 {Name} 失败", Name);
                throw new SessionConnectionException($"连接MQTT会话 {Name} 失败: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task CloseAsync()
        {
            if (_isOpen && _mqttClient != null)
            {
                try
                {
                    await _mqttClient.DisconnectAsync();
                    _mqttClient.Dispose();
                    _mqttClient = null;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "关闭MQTT会话 {Name} 时发生异常", Name);
                }

                _isOpen = false;
                _logger?.LogInformation("MQTT会话 {Name} 已关闭", Name);
            }
        }

        /// <inheritdoc/>
        public async Task<byte[]> SendAndReceiveAsync(byte[] request, CancellationToken ct = default)
        {
            if (!_isOpen || _mqttClient == null)
                throw new SessionException("MQTT会话未打开");

            await _sendLock.WaitAsync(ct);
            try
            {
                // 创建 TaskCompletionSource 用于等待响应
                var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
                _responseTcs = tcs;

                try
                {
                    // 发布请求到 RequestTopic
                    var message = new MqttApplicationMessageBuilder()
                        .WithTopic(_options.RequestTopic)
                        .WithPayload(request)
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                        .Build();

                    await _mqttClient.PublishAsync(message, ct);
                    _logger?.LogDebug("向MQTT {Name} 发送了 {Count} 字节到主题 {Topic}",
                        Name, request.Length, _options.RequestTopic);

                    // 等待响应或超时
                    using var timeoutCts = new CancellationTokenSource(_options.RequestTimeoutMs);
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

                    // 注册超时取消回调
                    linkedCts.Token.Register(() =>
                    {
                        tcs.TrySetCanceled();
                    });

                    return await tcs.Task;
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    throw new SessionTimeoutException(
                        $"MQTT会话 {Name} 请求超时 ({_options.RequestTimeoutMs}ms)");
                }
            }
            catch (Exception ex) when (ex is not SessionException and not SessionTimeoutException)
            {
                _logger?.LogError(ex, "MQTT会话 {Name} 请求失败", Name);
                throw new SessionException($"MQTT会话 {Name} 请求失败: {ex.Message}", ex);
            }
            finally
            {
                _responseTcs = null;
                _sendLock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task SendOnlyAsync(byte[] request, CancellationToken ct = default)
        {
            if (!_isOpen || _mqttClient == null)
                throw new SessionException("MQTT会话未打开");

            try
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(_options.RequestTopic)
                    .WithPayload(request)
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                    .Build();

                await _mqttClient.PublishAsync(message, ct);
                _logger?.LogDebug("向MQTT {Name} 发送了 {Count} 字节到主题 {Topic}",
                    Name, request.Length, _options.RequestTopic);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "MQTT会话 {Name} 发送失败", Name);
                throw new SessionException($"MQTT会话 {Name} 发送失败: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<byte[]> ReceiveOnlyAsync(CancellationToken ct = default)
        {
            if (!_isOpen || _mqttClient == null)
                throw new SessionException("MQTT会话未打开");

            try
            {
                var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
                _responseTcs = tcs;

                _logger?.LogDebug("从MQTT {Name} 等待接收数据", Name);

                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                linkedCts.Token.Register(() => tcs.TrySetCanceled());

                return await tcs.Task;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "MQTT会话 {Name} 接收失败", Name);
                throw new SessionException($"MQTT会话 {Name} 接收失败: {ex.Message}", ex);
            }
            finally
            {
                _responseTcs = null;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _sendLock?.Dispose();
            CloseAsync().Wait();
        }

        #region 事件处理

        /// <summary>
        /// MQTT 消息接收事件处理
        /// 匹配 ResponseTopic 后，将 payload 传递给等待中的 TaskCompletionSource
        /// </summary>
        private Task OnApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            var topic = e.ApplicationMessage.Topic;

            if (string.Equals(topic, _options.ResponseTopic, StringComparison.Ordinal))
            {
                var segment = e.ApplicationMessage.PayloadSegment;
                var payload = segment.Count == 0
                    ? Array.Empty<byte>()
                    : segment.Offset == 0 && segment.Count == segment.Array!.Length
                        ? segment.Array
                        : segment.Array!.AsSpan(segment.Offset, segment.Count).ToArray();
                _logger?.LogDebug("从MQTT {Name} 收到 {Count} 字节响应", Name, payload.Length);

                // 唤醒等待的 SendAndReceiveAsync
                _responseTcs?.TrySetResult(payload);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// MQTT 连接成功事件
        /// </summary>
        private Task OnConnected(MqttClientConnectedEventArgs e)
        {
            _logger?.LogInformation("MQTT会话 {Name} 连接成功", Name);
            return Task.CompletedTask;
        }

        /// <summary>
        /// MQTT 断开连接事件
        /// </summary>
        private Task OnDisconnected(MqttClientDisconnectedEventArgs e)
        {
            _isOpen = false;
            _logger?.LogWarning("MQTT会话 {Name} 连接断开: {Reason}", Name, e.Reason);

            // 唤醒等待中的请求，避免死锁
            _responseTcs?.TrySetException(
                new SessionException($"MQTT会话 {Name} 连接已断开"));

            return Task.CompletedTask;
        }

        #endregion
    }

    /// <summary>
    /// MQTT会话配置选项
    /// </summary>
    public class MqttSessionOptions
    {
        /// <summary>
        /// MQTT Broker地址
        /// </summary>
        public string BrokerHost { get; set; } = "127.0.0.1";

        /// <summary>
        /// MQTT Broker端口
        /// </summary>
        public int BrokerPort { get; set; } = 1883;

        /// <summary>
        /// 请求主题（设备接收命令的主题）
        /// </summary>
        public string RequestTopic { get; set; } = "devicelink/request";

        /// <summary>
        /// 响应主题（设备发送响应的主题）
        /// </summary>
        public string ResponseTopic { get; set; } = "devicelink/response";

        /// <summary>
        /// 客户端ID
        /// </summary>
        public string ClientId { get; set; } = $"DeviceLink_{Guid.NewGuid():N}";

        /// <summary>
        /// 请求超时时间（毫秒）
        /// </summary>
        public int RequestTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// MQTT 用户名（可选）
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// MQTT 密码（可选）
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// 是否使用 TLS 加密
        /// </summary>
        public bool UseTls { get; set; }

        /// <summary>
        /// 是否清理会话
        /// </summary>
        public bool CleanSession { get; set; } = true;

        /// <summary>
        /// 心跳间隔（秒）
        /// </summary>
        public ushort KeepAliveSeconds { get; set; } = 60;
    }
}
