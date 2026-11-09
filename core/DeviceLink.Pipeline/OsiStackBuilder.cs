using System;
using DeviceLink.DataLink;
using DeviceLink.Osi;
using DeviceLink.Protocol;
using DeviceLink.Session;
using DeviceLink.Transport;
using Microsoft.Extensions.Logging;

namespace DeviceLink.Pipeline
{
    /// <summary>
    /// OSI 感知的通信栈装配器 —— 与 <see cref="CommunicationPipelineBuilder"/> 的区别是
    /// <b>装配即校验</b>：先按协议画像检查层次是否完整、是否有越界，通过后才真正构建管道。
    ///
    /// 使用方式（Modbus RTU over RS-485）：
    /// <code>
    /// using var pipeline = new OsiStackBuilder(OsiProfiles.ModbusRtu)
    ///     .UseTransport(new SerialPortTransport("COM3", 9600))     // L1
    ///     .UseDataLink(new ModbusRtuFrameStrategy())               // L2（CRC16 定界）
    ///     .UseProtocol(new ModbusRtuCodec(1))                      // L6+L7
    ///     .Build();                                                // 校验失败直接抛异常
    /// </code>
    ///
    /// 与旧构建器的关系：内部仍然复用 <see cref="CommunicationPipelineBuilder"/>，
    /// 因此对既有代码零破坏；新增的只是"必须先声明层归属"和"装配前校验"两道关口。
    /// </summary>
    public sealed class OsiStackBuilder
    {
        private readonly OsiStackProfile _profile;
        private readonly OsiAssembler _assembler = new OsiAssembler();
        private readonly CommunicationPipelineBuilder _inner = new CommunicationPipelineBuilder();
        private IPhysicalTransport? _transport;

        /// <summary>按指定协议栈画像创建装配器。</summary>
        public OsiStackBuilder(OsiStackProfile profile)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            OsiComponentRegistrations.EnsureRegistered();
        }

        /// <summary>目标协议栈画像。</summary>
        public OsiStackProfile Profile => _profile;

        /// <summary>最近一次校验的报告。</summary>
        public OsiStackReport? LastReport { get; private set; }

        /// <summary>设置 L1 物理传输层。组件必须已登记层归属，否则抛出异常。</summary>
        public OsiStackBuilder UseTransport(IPhysicalTransport transport)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _inner.UseTransport(transport);
            _assembler.Add(transport);
            return this;
        }

        /// <summary>设置 L2 帧策略，内部创建 <see cref="DirectDataLink"/> 承担帧的收发。</summary>
        public OsiStackBuilder UseDataLink(IFrameStrategy frameStrategy, DataLinkOptions? options = null)
        {
            if (frameStrategy == null) throw new ArgumentNullException(nameof(frameStrategy));
            if (_transport == null)
                throw new InvalidOperationException("必须先调用 UseTransport 设置物理传输层");

            var dataLink = new DirectDataLink(_transport, frameStrategy, options);
            _inner.UseDataLink(dataLink);

            _assembler.Add(DescribeFrameStrategy(frameStrategy));
            _assembler.Add(dataLink);
            return this;
        }

        /// <summary>直接注入 L2 数据链路实现。</summary>
        public OsiStackBuilder UseDataLink(IDataLink dataLink)
        {
            _inner.UseDataLink(dataLink);
            _assembler.Add(dataLink);
            return this;
        }

        /// <summary>设置 L5 会话层。缺省时由 <see cref="CommunicationPipelineBuilder"/> 自动补直连会话。</summary>
        public OsiStackBuilder UseSession(ISession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            _inner.UseSession(session);
            _assembler.Add(DescribeSession(session));
            return this;
        }

        /// <summary>设置 L6+L7 协议编解码器。</summary>
        public OsiStackBuilder UseProtocol(IProtocolCodec protocol)
        {
            if (protocol == null) throw new ArgumentNullException(nameof(protocol));
            _inner.UseProtocol(protocol);
            _assembler.Add(DescribeProtocol(protocol));
            return this;
        }

        /// <summary>设置日志工厂。</summary>
        public OsiStackBuilder UseLoggerFactory(ILoggerFactory loggerFactory)
        {
            _inner.UseLoggerFactory(loggerFactory);
            return this;
        }

        /// <summary>只做层次校验，不构建管道。</summary>
        public OsiStackReport Inspect()
        {
            LastReport = _assembler.Assemble(_profile);
            return LastReport;
        }

        /// <summary>执行校验并构建管道；校验不通过抛出 <see cref="OsiStackViolationException"/>。</summary>
        public CommunicationPipeline Build()
        {
            var report = Inspect();
            if (!report.IsValid) throw new OsiStackViolationException(report);
            return _inner.Build();
        }

        /// <summary>构建管道，并把校验报告交给调用方（用于写日志），失败仍然抛出。</summary>
        public CommunicationPipeline Build(out OsiStackReport report)
        {
            report = Inspect();
            if (!report.IsValid) throw new OsiStackViolationException(report);
            return _inner.Build();
        }

        // ─────────────────── 未登记组件的兜底声明 ───────────────────
        // 兜底规则集中在 OsiComponentFactory，与 CommunicationPipelineBuilder 共用一份，
        // 避免两条装配路径对"谁在哪一层"给出不一致的答案。

        private static IOsiComponent DescribeFrameStrategy(IFrameStrategy strategy)
            => OsiComponentFactory.Describe(strategy);

        private static IOsiComponent DescribeSession(ISession session)
            => OsiComponentFactory.Describe(session);

        private static IOsiComponent DescribeProtocol(IProtocolCodec protocol)
            => OsiComponentFactory.Describe(protocol);
    }
}
