using DeviceLink.DataLink;
using DeviceLink.Pipeline;
using DeviceLink.Protocol;
using DeviceLink.Session;
using DeviceLink.Transport;
using Microsoft.Extensions.Logging;
using System;
using System.IO.Ports;
using System.Net;

namespace DeviceLink.DeviceBase
{
    /// <summary>
    /// 设备通信配置基类
    /// 通过 CommunicationPipelineBuilder 组装完整的 OSI 通信栈
    /// </summary>
    public abstract class DeviceCommSettings
    {
        /// <summary>
        /// 会话层配置（重连次数、重连间隔、会话超时等）。
        /// 这是框架内唯一的重试入口 —— 数据链路层不再重试。
        /// </summary>
        public SessionOptions SessionOptions { get; set; } = new SessionOptions();

        /// <summary>
        /// 日志工厂（可选）。设置后 L2/L5 的收发日志才会真正输出。
        /// </summary>
        public ILoggerFactory? LoggerFactory { get; set; }

        /// <summary>
        /// 创建通信管道（完整 OSI 链路）
        /// </summary>
        /// <param name="codec">协议编解码器</param>
        /// <returns>通信管道</returns>
        protected internal abstract CommunicationPipeline CreatePipeline(IProtocolCodec codec);

        /// <summary>
        /// 统一的管道组装逻辑：帧策略优先用显式的，其次按协议推导，最后才用配置里的分隔符兜底。
        /// </summary>
        protected CommunicationPipeline BuildPipeline(
            IPhysicalTransport transport,
            IProtocolCodec codec,
            IFrameStrategy? frameStrategy,
            byte[]? fallbackDelimiter,
            DataLinkOptions dataLinkOptions)
        {
            var builder = new CommunicationPipelineBuilder()
                .UseTransport(transport)
                .UseProtocol(codec)
                .UseDataLinkOptions(dataLinkOptions)
                .UseSessionOptions(SessionOptions);

            if (frameStrategy != null)
            {
                builder.UseDataLink(frameStrategy);
            }
            else if (fallbackDelimiter != null &&
                     !ProtocolBindings.IsRegistered(codec) &&
                     codec is not IFramedProtocol)
            {
                // 未登记协议又没自带终止符：尊重配置里的 Delimiter，
                // 否则由构建器抛出"未登记帧策略"，不做静默兜底。
                builder.UseDataLink(new DelimiterFrameStrategy(fallbackDelimiter));
            }

            if (LoggerFactory != null)
                builder.UseLoggerFactory(LoggerFactory);

            return builder.Build();
        }
    }

    /// <summary>
    /// 串口通信配置
    /// </summary>
    public class SerialPortSettings : DeviceCommSettings
    {
        /// <summary>
        /// 串口名称
        /// </summary>
        public string PortName { get; set; } = "COM1";

        /// <summary>
        /// 波特率
        /// </summary>
        public int BaudRate { get; set; } = 9600;

        /// <summary>
        /// 数据位
        /// </summary>
        public int DataBits { get; set; } = 8;

        /// <summary>
        /// 停止位
        /// </summary>
        public StopBits StopBits { get; set; } = StopBits.One;

        /// <summary>
        /// 校验位
        /// </summary>
        public Parity Parity { get; set; } = Parity.None;

        /// <summary>
        /// 帧分隔符（仅在协议未登记且未实现 IFramedProtocol 时作为兜底）
        /// </summary>
        public byte[] Delimiter { get; set; } = new byte[] { 0 };

        /// <summary>
        /// 自定义帧策略（如 ModbusRtuFrameStrategy）；为 null 时按协议自动推导
        /// </summary>
        public IFrameStrategy? FrameStrategy { get; set; }

        /// <summary>
        /// 启用 DTR（数据终端就绪）信号
        /// </summary>
        public bool DtrEnable { get; set; }

        /// <summary>
        /// 启用 RTS（请求发送）信号
        /// </summary>
        public bool RtsEnable { get; set; }

        /// <summary>
        /// 接收超时时间（毫秒），默认 10000ms
        /// </summary>
        public int ReceiveTimeoutMs { get; set; } = 10 * 1000;

        /// <summary>
        /// 接收空闲超时时间（毫秒），默认 100ms
        /// 连续无数据到达视为帧结束
        /// </summary>
        public int ReceiveIdleTimeoutMs { get; set; } = 100;

        /// <summary>
        /// 连接失败/断开时的最大重连重发次数，默认 0（不重连）。
        ///
        /// 注意语义：只有"请求确定未送达"时才会重发。
        /// 超时（请求已发出但没有响应）<b>不会</b>重发，避免写类命令被执行两次。
        /// </summary>
        public int MaxConnectRetries
        {
            get => SessionOptions.MaxConnectRetries;
            set => SessionOptions.MaxConnectRetries = value;
        }

        /// <summary>
        /// 重连间隔（毫秒），默认 300ms
        /// </summary>
        public int ReconnectDelayMs
        {
            get => SessionOptions.ReconnectDelayMs;
            set => SessionOptions.ReconnectDelayMs = value;
        }

        /// <summary>
        /// 已废弃：帧级重试会把写命令重复执行。请改用 <see cref="MaxConnectRetries"/>。
        /// </summary>
        [Obsolete("帧级重试会让写命令被执行两次，已移除。请改用 MaxConnectRetries（仅重连未送达的请求）。")]
        public int MaxRetryCount
        {
            get => MaxConnectRetries;
            set => MaxConnectRetries = value;
        }

        /// <summary>
        /// 已废弃：请改用 <see cref="ReconnectDelayMs"/>。
        /// </summary>
        [Obsolete("帧级重试已移除，请改用 ReconnectDelayMs。")]
        public int RetryDelayMs
        {
            get => ReconnectDelayMs;
            set => ReconnectDelayMs = value;
        }

        /// <summary>
        /// 初始化串口通信配置
        /// </summary>
        public SerialPortSettings()
        {
        }

        /// <summary>
        /// 初始化串口通信配置
        /// </summary>
        /// <param name="portName">串口名称</param>
        /// <param name="baudRate">波特率</param>
        /// <param name="dataBits">数据位</param>
        /// <param name="stopBits">停止位</param>
        /// <param name="parity">校验位</param>
        public SerialPortSettings(string portName, int baudRate, int dataBits, StopBits stopBits, Parity parity)
        {
            PortName = portName ?? throw new ArgumentNullException(nameof(portName));
            BaudRate = baudRate;
            DataBits = dataBits;
            StopBits = stopBits;
            Parity = parity;
        }

        /// <summary>
        /// 创建默认串口配置（9600,8,1,None）
        /// </summary>
        /// <param name="portName">串口名称</param>
        /// <returns>默认串口配置</returns>
        public static SerialPortSettings CreateDefault(string portName)
        {
            return new SerialPortSettings
            {
                PortName = portName,
                BaudRate = 9600,
                DataBits = 8,
                StopBits = StopBits.One,
                Parity = Parity.None
            };
        }

        /// <summary>
        /// 创建串口通信管道（完整 OSI 链路）
        /// </summary>
        /// <param name="codec">协议编解码器</param>
        /// <returns>通信管道</returns>
        protected internal override CommunicationPipeline CreatePipeline(IProtocolCodec codec)
        {
            var dataLinkOptions = new DataLinkOptions
            {
                ReceiveTimeoutMs = ReceiveTimeoutMs,
                ReceiveIdleTimeoutMs = ReceiveIdleTimeoutMs
            };

            return BuildPipeline(
                new SerialPortTransport(PortName, BaudRate, DataBits, StopBits, Parity, DtrEnable, RtsEnable),
                codec, FrameStrategy, Delimiter, dataLinkOptions);
        }
    }

    /// <summary>
    /// TCP通信配置
    /// </summary>
    public class TcpSettings : DeviceCommSettings
    {
        /// <summary>
        /// IP地址
        /// </summary>
        public IPAddress IpAddress { get; set; } = IPAddress.Loopback;

        /// <summary>
        /// 端口号
        /// </summary>
        public int Port { get; set; } = 10001;

        /// <summary>
        /// 连接超时时间（毫秒）
        /// </summary>
        public int ConnectTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// 接收超时时间（毫秒）
        /// </summary>
        public int ReceiveTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// 接收空闲超时时间（毫秒）
        /// </summary>
        public int ReceiveIdleTimeoutMs { get; set; } = 50;

        /// <summary>
        /// 帧分隔符（仅在协议未登记且未实现 IFramedProtocol 时作为兜底）
        /// </summary>
        public byte[] Delimiter { get; set; } = new byte[] { 0 };

        /// <summary>
        /// 自定义帧策略；为 null 时按协议自动推导
        /// </summary>
        public IFrameStrategy? FrameStrategy { get; set; }

        /// <summary>
        /// 初始化TCP通信配置
        /// </summary>
        public TcpSettings()
        {
        }

        /// <summary>
        /// 初始化TCP通信配置
        /// </summary>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="port">端口号</param>
        public TcpSettings(IPAddress ipAddress, int port)
        {
            IpAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
            Port = port;
        }

        /// <summary>
        /// 初始化TCP通信配置
        /// </summary>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="port">端口号</param>
        /// <param name="connectTimeoutMs">连接超时时间（毫秒）</param>
        public TcpSettings(IPAddress ipAddress, int port, int connectTimeoutMs)
            : this(ipAddress, port)
        {
            ConnectTimeoutMs = connectTimeoutMs;
        }

        /// <summary>
        /// 创建TCP通信管道（完整 OSI 链路）
        /// </summary>
        /// <param name="codec">协议编解码器</param>
        /// <returns>通信管道</returns>
        protected internal override CommunicationPipeline CreatePipeline(IProtocolCodec codec)
        {
            var dataLinkOptions = new DataLinkOptions
            {
                ReceiveTimeoutMs = ReceiveTimeoutMs,
                ReceiveIdleTimeoutMs = ReceiveIdleTimeoutMs
            };

            return BuildPipeline(
                new TcpTransport(IpAddress.ToString(), Port, ConnectTimeoutMs),
                codec, FrameStrategy, Delimiter, dataLinkOptions);
        }
    }

    /// <summary>
    /// MQTT通信配置
    /// </summary>
    public class MqttSettings : DeviceCommSettings
    {
        /// <summary>
        /// MQTT Broker 地址
        /// </summary>
        public string BrokerHost { get; set; } = "127.0.0.1";

        /// <summary>
        /// MQTT Broker 端口号
        /// </summary>
        public int BrokerPort { get; set; } = 1883;

        /// <summary>
        /// 客户端 ID
        /// </summary>
        public string ClientId { get; set; } = $"DeviceLink_{Guid.NewGuid():N}";

        /// <summary>
        /// 请求主题（设备接收命令的主题）
        /// </summary>
        public string RequestTopic { get; set; } = "devicelink/request";

        /// <summary>
        /// 响应主题（设备发送响应的主题）
        /// </summary>
        public string ResponseTopic { get; set; } = "devicelink/response";

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

        /// <summary>
        /// 初始化MQTT通信配置
        /// </summary>
        public MqttSettings()
        {
        }

        /// <summary>
        /// 初始化MQTT通信配置
        /// </summary>
        /// <param name="brokerHost">MQTT Broker 地址</param>
        /// <param name="brokerPort">MQTT Broker 端口号</param>
        /// <param name="requestTopic">请求主题</param>
        /// <param name="responseTopic">响应主题</param>
        public MqttSettings(string brokerHost, int brokerPort, string requestTopic, string responseTopic)
        {
            BrokerHost = brokerHost ?? throw new ArgumentNullException(nameof(brokerHost));
            BrokerPort = brokerPort;
            RequestTopic = requestTopic ?? throw new ArgumentNullException(nameof(requestTopic));
            ResponseTopic = responseTopic ?? throw new ArgumentNullException(nameof(responseTopic));
        }

        /// <summary>
        /// 创建MQTT通信管道（完整 OSI 链路）
        /// </summary>
        /// <param name="codec">协议编解码器</param>
        /// <returns>通信管道</returns>
        protected internal override CommunicationPipeline CreatePipeline(IProtocolCodec codec)
        {
            var session = new MqttSession(new MqttSessionOptions
            {
                BrokerHost = BrokerHost,
                BrokerPort = BrokerPort,
                ClientId = ClientId,
                RequestTopic = RequestTopic,
                ResponseTopic = ResponseTopic,
                RequestTimeoutMs = RequestTimeoutMs,
                Username = Username,
                Password = Password,
                UseTls = UseTls,
                CleanSession = CleanSession,
                KeepAliveSeconds = KeepAliveSeconds
            });

            var builder = new CommunicationPipelineBuilder()
                .UseSession(session)
                .UseProtocol(codec);

            if (LoggerFactory != null)
                builder.UseLoggerFactory(LoggerFactory);

            return builder.Build();
        }
    }
}
