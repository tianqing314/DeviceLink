using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DeviceLink.DataLink;
using DeviceLink.Protocol;
using DeviceLink.Session;
using DeviceLink.Transport;
using Microsoft.Extensions.Logging;

namespace DeviceLink.Pipeline
{
    /// <summary>
    /// 通信管道构建器 —— 支持灵活的层组合，根据通信场景选择需要的层。
    /// 
    /// 使用示例：
    ///   // 串口通信（跳过会话层）
    ///   var pipeline = new CommunicationPipelineBuilder()
    ///       .UseTransport(new SerialPortTransport("COM3", 9600))
    ///       .UseDataLink(new DelimiterFrameStrategy(new byte[]{0}))
    ///       .UseProtocol(new ConSTCodec(255))
    ///       .Build();
    /// 
    ///   // TCP通信（跳过数据链路层）
    ///   var pipeline = new CommunicationPipelineBuilder()
    ///       .UseTransport(new TcpTransport("192.168.1.100", 502))
    ///       .UseSession(new DirectSession(...))
    ///       .UseProtocol(new ModbusTcpCodec())
    ///       .Build();
    /// </summary>
    public class CommunicationPipelineBuilder
    {
        private IPhysicalTransport? _transport;
        private IDataLink? _dataLink;
        private ISession? _session;
        private IProtocolCodec? _protocol;
        private ILoggerFactory? _loggerFactory;
        private readonly List<Action<CommunicationPipeline>> _configurations = new();

        /// <summary>
        /// 设置物理传输层
        /// </summary>
        /// <param name="transport">物理传输层</param>
        /// <returns>构建器</returns>
        public CommunicationPipelineBuilder UseTransport(IPhysicalTransport transport)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            return this;
        }

        /// <summary>
        /// 设置数据链路层（使用帧策略）
        /// </summary>
        /// <param name="frameStrategy">帧策略</param>
        /// <param name="options">数据链路配置选项</param>
        /// <returns>构建器</returns>
        public CommunicationPipelineBuilder UseDataLink(IFrameStrategy frameStrategy, DataLinkOptions? options = null)
        {
            if (_transport == null)
                throw new InvalidOperationException("必须先设置物理传输层");

            _dataLink = new DirectDataLink(_transport, frameStrategy, options);
            return this;
        }

        /// <summary>
        /// 设置数据链路层（直接注入）
        /// </summary>
        /// <param name="dataLink">数据链路层</param>
        /// <returns>构建器</returns>
        public CommunicationPipelineBuilder UseDataLink(IDataLink dataLink)
        {
            _dataLink = dataLink ?? throw new ArgumentNullException(nameof(dataLink));
            return this;
        }

        /// <summary>
        /// 设置会话层（直接注入）
        /// </summary>
        /// <param name="session">会话层</param>
        /// <returns>构建器</returns>
        public CommunicationPipelineBuilder UseSession(ISession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            return this;
        }

        /// <summary>
        /// 设置协议层
        /// </summary>
        /// <param name="protocol">协议编解码器</param>
        /// <returns>构建器</returns>
        public CommunicationPipelineBuilder UseProtocol(IProtocolCodec protocol)
        {
            _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            return this;
        }

        /// <summary>
        /// 设置日志工厂
        /// </summary>
        /// <param name="loggerFactory">日志工厂</param>
        /// <returns>构建器</returns>
        public CommunicationPipelineBuilder UseLoggerFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            return this;
        }

        /// <summary>
        /// 添加配置动作
        /// </summary>
        /// <param name="configuration">配置动作</param>
        /// <returns>构建器</returns>
        public CommunicationPipelineBuilder Configure(Action<CommunicationPipeline> configuration)
        {
            _configurations.Add(configuration ?? throw new ArgumentNullException(nameof(configuration)));
            return this;
        }

        /// <summary>
        /// 构建通信管道
        /// </summary>
        /// <returns>通信管道</returns>
        public CommunicationPipeline Build()
        {
            // 自动创建会话层（如果未设置）
            if (_session == null)
            {
                if (_dataLink != null)
                {
                    _session = new DirectSession(_dataLink);
                }
                else if (_transport != null)
                {
                    // 如果没有数据链路层，创建一个默认的
                    var defaultFrameStrategy = new DelimiterFrameStrategy(new byte[] { 0 });
                    _dataLink = new DirectDataLink(_transport, defaultFrameStrategy);
                    _session = new DirectSession(_dataLink);
                }
                else
                {
                    throw new InvalidOperationException("必须设置物理传输层或会话层");
                }
            }

            if (_protocol == null)
                throw new InvalidOperationException("必须设置协议层");

            var pipeline = new CommunicationPipeline(_transport, _dataLink, _session, _protocol, _loggerFactory);

            // 应用配置
            foreach (var configuration in _configurations)
            {
                configuration(pipeline);
            }

            return pipeline;
        }
    }

    /// <summary>
    /// 通信管道 —— 封装完整的通信栈
    /// </summary>
    public class CommunicationPipeline : IDisposable
    {
        /// <summary>
        /// 物理传输层（可能为 null）
        /// </summary>
        public IPhysicalTransport? Transport { get; }

        /// <summary>
        /// 数据链路层（可能为 null）
        /// </summary>
        public IDataLink? DataLink { get; }

        /// <summary>
        /// 会话层
        /// </summary>
        public ISession Session { get; }

        /// <summary>
        /// 协议层
        /// </summary>
        public IProtocolCodec Protocol { get; }

        /// <summary>
        /// 日志工厂
        /// </summary>
        public ILoggerFactory? LoggerFactory { get; }

        /// <summary>
        /// 初始化通信管道
        /// </summary>
        /// <param name="transport">物理传输层</param>
        /// <param name="dataLink">数据链路层</param>
        /// <param name="session">会话层</param>
        /// <param name="protocol">协议层</param>
        /// <param name="loggerFactory">日志工厂</param>
        internal CommunicationPipeline(
            IPhysicalTransport? transport,
            IDataLink? dataLink,
            ISession session,
            IProtocolCodec protocol,
            ILoggerFactory? loggerFactory)
        {
            Transport = transport;
            DataLink = dataLink;
            Session = session ?? throw new ArgumentNullException(nameof(session));
            Protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            LoggerFactory = loggerFactory;
        }

        /// <summary>
        /// 打开管道
        /// </summary>
        /// <param name="ct">取消令牌</param>
        public async Task OpenAsync(CancellationToken ct = default)
        {
            await Session.OpenAsync(ct);
        }

        /// <summary>
        /// 关闭管道
        /// </summary>
        public async Task CloseAsync()
        {
            await Session.CloseAsync();
        }

        /// <summary>
        /// 发送请求并接收响应
        /// </summary>
        /// <param name="request">请求数据</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>响应数据</returns>
        public async Task<byte[]> SendAndReceiveAsync(byte[] request, CancellationToken ct = default)
        {
            return await Session.SendAndReceiveAsync(request, ct);
        }

        /// <summary>
        /// 发送命令并接收响应
        /// </summary>
        /// <param name="command">逻辑命令</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>响应数据</returns>
        public async Task<byte[]> SendCommandAsync(Command command, CancellationToken ct = default)
        {
            var request = Protocol.Encode(command);
            var response = await Session.SendAndReceiveAsync(request, ct);

            if (Protocol.IsErrorResponse(response, out var errorMessage))
            {
                throw new ProtocolException($"设备错误: {errorMessage}");
            }

            return response;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Session.Dispose();
            Transport?.Dispose();
        }
    }

    /// <summary>
    /// 预定义的管道配置
    /// </summary>
    public static class PipelinePresets
    {
        /// <summary>
        /// 创建串口通信管道
        /// </summary>
        /// <param name="portName">串口名称</param>
        /// <param name="baudRate">波特率</param>
        /// <param name="address">设备地址</param>
        /// <returns>通信管道</returns>
        public static CommunicationPipeline CreateSerialPortPipeline(
            string portName, int baudRate = 9600, byte address = 255)
        {
            return new CommunicationPipelineBuilder()
                .UseTransport(new SerialPortTransport(portName, baudRate))
                .UseDataLink(new DelimiterFrameStrategy(new byte[] { 0 }))
                .UseProtocol(new ConSTCodec(address))
                .Build();
        }

        /// <summary>
        /// 创建TCP通信管道
        /// </summary>
        /// <param name="host">主机地址</param>
        /// <param name="port">端口号</param>
        /// <param name="address">设备地址</param>
        /// <returns>通信管道</returns>
        public static CommunicationPipeline CreateTcpPipeline(
            string host, int port, byte address = 255)
        {
            return new CommunicationPipelineBuilder()
                .UseTransport(new TcpTransport(host, port))
                .UseDataLink(new DelimiterFrameStrategy(new byte[] { 0 }))
                .UseProtocol(new ConSTCodec(address))
                .Build();
        }

        /// <summary>
        /// 创建Modbus RTU通信管道
        /// </summary>
        /// <param name="portName">串口名称</param>
        /// <param name="baudRate">波特率</param>
        /// <param name="slaveAddress">从站地址</param>
        /// <returns>通信管道</returns>
        public static CommunicationPipeline CreateModbusRtuPipeline(
            string portName, int baudRate = 9600, byte slaveAddress = 1)
        {
            return new CommunicationPipelineBuilder()
                .UseTransport(new SerialPortTransport(portName, baudRate))
                .UseDataLink(new ModbusRtuFrameStrategy())
                .UseProtocol(new ModbusRtuCodec(slaveAddress))
                .Build();
        }

        /// <summary>
        /// 创建SCPI通信管道
        /// </summary>
        /// <param name="host">主机地址</param>
        /// <param name="port">端口号</param>
        /// <returns>通信管道</returns>
        public static CommunicationPipeline CreateScpiPipeline(string host, int port)
        {
            return new CommunicationPipelineBuilder()
                .UseTransport(new TcpTransport(host, port))
                .UseDataLink(new DelimiterFrameStrategy("\n"))
                .UseProtocol(new ScpiCodec())
                .Build();
        }

        /// <summary>
        /// 创建MQTT通信管道
        /// </summary>
        /// <param name="brokerHost">MQTT Broker 地址</param>
        /// <param name="brokerPort">MQTT Broker 端口号</param>
        /// <param name="requestTopic">请求主题</param>
        /// <param name="responseTopic">响应主题</param>
        /// <param name="address">ConST 设备地址（默认 255）</param>
        /// <param name="requestTimeoutMs">请求超时时间（毫秒，默认 5000）</param>
        /// <returns>通信管道</returns>
        public static CommunicationPipeline CreateMqttPipeline(
            string brokerHost,
            int brokerPort,
            string requestTopic,
            string responseTopic,
            byte address = 255,
            int requestTimeoutMs = 5000)
        {
            var session = new MqttSession(new MqttSessionOptions
            {
                BrokerHost = brokerHost,
                BrokerPort = brokerPort,
                RequestTopic = requestTopic,
                ResponseTopic = responseTopic,
                RequestTimeoutMs = requestTimeoutMs
            });

            return new CommunicationPipelineBuilder()
                .UseSession(session)
                .UseProtocol(new ConSTCodec(address))
                .Build();
        }
    }
}
