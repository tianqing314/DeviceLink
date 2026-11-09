using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DeviceLink.DataLink;
using DeviceLink.Osi;
using DeviceLink.Protocol;
using DeviceLink.Session;
using DeviceLink.Transport;
using Microsoft.Extensions.Logging;

namespace DeviceLink.Pipeline
{
    /// <summary>
    /// 通信管道构建器 —— 支持灵活的层组合，根据通信场景选择需要的层。
    ///
    /// 构建时做三件事（缺一不可）：
    /// 1. <b>补缺</b>：未显式给出的 L2 帧策略按 <see cref="ProtocolBindings"/> 推导，未给出的 L5 自动补 <see cref="DirectSession"/>；
    /// 2. <b>校验</b>：按协议画像做一次 OSI 层次校验，报告挂在 <see cref="CommunicationPipeline.OsiReport"/> 上；
    /// 3. <b>注入</b>：把日志工厂真正传给 L2/L5（过去是个空开关）。
    ///
    /// 使用示例：
    /// <code>
    /// // 串口通信：帧策略由 ConSTCodec 的终止符推导
    /// var pipeline = new CommunicationPipelineBuilder()
    ///     .UseTransport(new SerialPortTransport("COM3", 9600))
    ///     .UseProtocol(new ConSTCodec(255))
    ///     .Build();
    ///
    /// // Modbus TCP：帧策略自动选中 MBAP 长度前缀分帧
    /// var pipeline = new CommunicationPipelineBuilder()
    ///     .UseTransport(new TcpTransport("192.168.1.100", 502))
    ///     .UseProtocol(new ModbusTcpCodec())
    ///     .Build();
    /// </code>
    /// </summary>
    public class CommunicationPipelineBuilder
    {
        private IPhysicalTransport? _transport;
        private IDataLink? _dataLink;
        private IFrameStrategy? _frameStrategy;
        private ISession? _session;
        private IProtocolCodec? _protocol;
        private ILoggerFactory? _loggerFactory;
        private DataLinkOptions? _dataLinkOptions;
        private SessionOptions? _sessionOptions;
        private OsiStackProfile? _osiProfile;
        private bool _strictOsi;
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
            _frameStrategy = frameStrategy ?? throw new ArgumentNullException(nameof(frameStrategy));
            if (options != null) _dataLinkOptions = options;
            return this;
        }

        /// <summary>
        /// 只配置数据链路层参数，帧策略留给框架按协议推导。
        /// </summary>
        /// <param name="options">数据链路配置选项</param>
        /// <returns>构建器</returns>
        public CommunicationPipelineBuilder UseDataLinkOptions(DataLinkOptions options)
        {
            _dataLinkOptions = options ?? throw new ArgumentNullException(nameof(options));
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
            if (_transport == null) _transport = dataLink.Transport;
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
        /// 配置会话层参数，会话实现留给框架自动创建（<see cref="DirectSession"/>）。
        /// </summary>
        /// <param name="options">会话配置选项</param>
        /// <returns>构建器</returns>
        public CommunicationPipelineBuilder UseSessionOptions(SessionOptions options)
        {
            _sessionOptions = options ?? throw new ArgumentNullException(nameof(options));
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
        /// 设置日志工厂（会真正传给数据链路层与会话层）
        /// </summary>
        /// <param name="loggerFactory">日志工厂</param>
        /// <returns>构建器</returns>
        public CommunicationPipelineBuilder UseLoggerFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            return this;
        }

        /// <summary>
        /// 显式指定协议栈画像；缺省时按协议类型自动推导。
        /// </summary>
        /// <param name="profile">协议栈画像</param>
        /// <returns>构建器</returns>
        public CommunicationPipelineBuilder UseOsiProfile(OsiStackProfile profile)
        {
            _osiProfile = profile ?? throw new ArgumentNullException(nameof(profile));
            return this;
        }

        /// <summary>
        /// 严格模式：OSI 层次校验存在错误时直接抛 <see cref="OsiStackViolationException"/>，
        /// 而不是只记日志。新增协议时建议开启。
        /// </summary>
        /// <returns>构建器</returns>
        public CommunicationPipelineBuilder StrictOsi()
        {
            _strictOsi = true;
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
            if (_protocol == null)
                throw new InvalidOperationException("必须设置协议层");

            var logger = _loggerFactory?.CreateLogger("DeviceLink.Pipeline");

            // ── L2：帧策略未显式给出时按协议推导，推导不出来就报错，绝不静默用分隔符兜底 ──
            if (_dataLink == null)
            {
                if (_frameStrategy == null)
                {
                    if (_transport == null && _session == null)
                        throw new InvalidOperationException("必须设置物理传输层或会话层");

                    // 只有传输层时才能推导帧策略
                    if (_transport != null)
                        _frameStrategy = ResolveDefaultFrameStrategy(_protocol, _transport);
                }

                if (_frameStrategy != null)
                {
                    if (_transport == null)
                        throw new InvalidOperationException("设置帧策略前必须先设置物理传输层");

                    _dataLink = new DirectDataLink(
                        _transport, _frameStrategy, _dataLinkOptions,
                        _loggerFactory?.CreateLogger<DirectDataLink>());
                }
            }

            // ── L5：未显式给出时补直连会话 ──
            if (_session == null)
            {
                if (_dataLink == null)
                    throw new InvalidOperationException("必须设置物理传输层或会话层");

                _session = new DirectSession(
                    _dataLink, _sessionOptions,
                    _loggerFactory?.CreateLogger<DirectSession>());
            }

            var pipeline = new CommunicationPipeline(
                _transport, _dataLink, _session, _protocol, _loggerFactory);

            // ── 装配即校验 ──
            pipeline.OsiReport = ValidateOsi(logger);

            if (_strictOsi && pipeline.OsiReport != null && !pipeline.OsiReport.IsValid)
                throw new OsiStackViolationException(pipeline.OsiReport);

            foreach (var configuration in _configurations)
            {
                configuration(pipeline);
            }

            return pipeline;
        }

        /// <summary>
        /// 按协议推荐帧策略：先查登记表，再退回协议自带的终止符。
        /// </summary>
        private static IFrameStrategy ResolveDefaultFrameStrategy(
            IProtocolCodec protocol, IPhysicalTransport transport)
        {
            var fromBinding = ProtocolBindings.TryCreateFrameStrategy(protocol, transport);
            if (fromBinding != null) return fromBinding;

            if (protocol is IFramedProtocol framed)
                return new DelimiterFrameStrategy(framed.FrameTerminator);

            throw new InvalidOperationException(
                $"协议 {protocol.ProtocolName}({protocol.GetType().Name}) 未登记帧策略。" +
                "请在 ProtocolBindings 中登记，或显式调用 UseDataLink(...) 指定帧策略。" +
                "此处不做静默兜底 —— 用错帧策略的代价是协议层悄悄收到错帧。");
        }

        /// <summary>
        /// 执行 OSI 层次校验。画像或层归属缺失时返回 null（并在日志里告警），不阻断构建。
        /// </summary>
        private OsiStackReport? ValidateOsi(ILogger? logger)
        {
            OsiComponentRegistrations.EnsureRegistered();

            var profile = _osiProfile
                ?? ProtocolBindings.TryResolveProfile(_protocol!, _transport, _session);

            if (profile == null)
            {
                logger?.LogWarning(
                    "[Pipeline] 协议 {Protocol} 没有对应的 OSI 画像，跳过层次校验。请在 ProtocolBindings 中登记。",
                    _protocol!.ProtocolName);
                return null;
            }

            var assembler = new OsiAssembler();
            try
            {
                if (_transport != null) assembler.Add(OsiComponentFactory.Describe(_transport));
                if (_dataLink != null) assembler.Add(_dataLink);
                else if (_frameStrategy != null) assembler.Add(OsiComponentFactory.Describe(_frameStrategy));
                if (_session != null) assembler.Add(_session);
                assembler.Add(_protocol!);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                logger?.LogWarning(ex, "[Pipeline] 组件层归属未登记，跳过 OSI 层次校验");
                return null;
            }

            var report = assembler.Assemble(profile);

            logger?.LogDebug("[Pipeline] OSI 装配报告:\n{Report}", report.ToTextTable());

            foreach (var violation in report.Violations)
            {
                if (violation.Severity == OsiSeverity.Error)
                    logger?.LogWarning("[Pipeline] OSI 层次错误: {Violation}", violation.ToString());
            }

            return report;
        }
    }

    /// <summary>
    /// 通信管道 —— 封装完整的通信栈
    /// </summary>
    public class CommunicationPipeline : IDisposable, IAsyncDisposable
    {
        private bool _disposed;

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
        /// 装配时生成的 OSI 层次校验报告（无画像时为 null）
        /// </summary>
        public OsiStackReport? OsiReport { get; internal set; }

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
            await Session.OpenAsync(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// 关闭管道
        /// </summary>
        public async Task CloseAsync()
        {
            await Session.CloseAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// 发送请求并接收响应
        /// </summary>
        /// <param name="request">请求数据</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>响应数据</returns>
        public async Task<byte[]> SendAndReceiveAsync(byte[] request, CancellationToken ct = default)
        {
            return await Session.SendAndReceiveAsync(request, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // 所有权：Session 拥有 DataLink，DataLink 拥有 Transport。
            // 只有链路层不存在时，Pipeline 才直接持有并释放 Transport —— 否则会重复 Dispose。
            Session.Dispose();

            if (DataLink == null)
                Transport?.Dispose();
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            await Session.DisposeAsync().ConfigureAwait(false);

            if (DataLink == null)
                Transport?.Dispose();
        }
    }

    /// <summary>
    /// 预定义的管道配置
    /// </summary>
    public static class PipelinePresets
    {
        /// <summary>
        /// 创建串口通信管道（帧策略按协议自动推导）
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
                .UseProtocol(new ConSTCodec(address))
                .Build();
        }

        /// <summary>
        /// 创建 TCP 通信管道（帧策略按协议自动推导）
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
                .UseProtocol(new ConSTCodec(address))
                .Build();
        }

        /// <summary>
        /// 创建 Modbus RTU 通信管道
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
                .UseProtocol(new ModbusRtuCodec(slaveAddress))
                .Build();
        }

        /// <summary>
        /// 创建 Modbus TCP 通信管道（MBAP 长度前缀分帧，无会话层）
        /// </summary>
        /// <param name="host">主机地址</param>
        /// <param name="port">端口号，默认 502</param>
        /// <param name="unitId">单元标识</param>
        /// <returns>通信管道</returns>
        public static CommunicationPipeline CreateModbusTcpPipeline(
            string host, int port = 502, byte unitId = 1)
        {
            return new CommunicationPipelineBuilder()
                .UseTransport(new TcpTransport(host, port))
                .UseProtocol(new ModbusTcpCodec(unitId))
                .Build();
        }

        /// <summary>
        /// 创建 SCPI 通信管道
        /// </summary>
        /// <param name="host">主机地址</param>
        /// <param name="port">端口号</param>
        /// <returns>通信管道</returns>
        public static CommunicationPipeline CreateScpiPipeline(string host, int port)
        {
            return new CommunicationPipelineBuilder()
                .UseTransport(new TcpTransport(host, port))
                .UseProtocol(new ScpiCodec("\n"))
                .Build();
        }

        /// <summary>
        /// 创建 MQTT 通信管道
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
