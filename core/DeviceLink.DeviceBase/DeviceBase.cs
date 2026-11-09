using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceLink.DataLink;
using DeviceLink.Pipeline;
using DeviceLink.Protocol;
using DeviceLink.Session;
using DeviceLink.Transport;
using Microsoft.Extensions.Logging;
using System.IO.Ports;

namespace DeviceLink.DeviceBase
{
    /// <summary>
    /// 设备基类 —— 框架唯一提供给设备层的基类。
    ///
    /// 提供：
    /// · SendAsync / SendForResultAsync — 发命令、收响应
    /// · 自动错误检查（通过 IProtocolCodec.IsErrorResponse）
    /// · 日志记录（设备名 + 操作名）
    /// · 通过 CommunicationPipelineBuilder 组装完整 OSI 通信栈
    ///
    /// 不提供（也不该提供）：
    /// · 任何业务接口（压力、温度、校准等——这些都是设备类自己的事）
    /// · 任何重试逻辑（重试在会话层已处理）
    ///
    /// <b>帧策略不再由本类决定</b>：过去这里用 <c>switch (codec)</c> 硬编码映射，
    /// 漏映射就静默退化成"分隔符 \0"，SCPI 与 Modbus TCP 都因此长期工作在错误的帧策略上。
    /// 现在统一交给 <see cref="ProtocolBindings"/>，并允许子类用 <see cref="CreatePipeline"/> 完全接管。
    /// </summary>
    public abstract class DeviceBase : IDisposable, IAsyncDisposable
    {
        private bool _disposed;

        /// <summary>通信管道（封装完整的 OSI 链路）</summary>
        protected CommunicationPipeline Pipeline { get; }

        /// <summary>会话层（从 Pipeline 中提取）</summary>
        protected ISession Session => Pipeline.Session;

        /// <summary>协议编解码器</summary>
        protected IProtocolCodec Codec { get; }

        /// <summary>
        /// 链路层的原始帧通道（当前链路不支持时为 null）。
        ///
        /// 用于"帧由设备侧完全掌控"的协议：设备只决定"什么算一帧"，
        /// 读循环、超时、累积、串行化仍归 L2。设备层因此<b>不需要</b>触碰
        /// <c>IPhysicalTransport</c>，也不会再越级访问 <see cref="CommunicationPipeline"/> 的内部件。
        /// </summary>
        protected IRawFrameChannel? RawFrameChannel => Pipeline.DataLink as IRawFrameChannel;

        /// <summary>设备名称，如 "DPSEX"、"DPG"</summary>
        public string Name { get; set; }

        /// <summary>
        /// 用现成的通信管道初始化设备（高级用法：自定义栈、测试替身）。
        /// </summary>
        /// <param name="pipeline">已装配的通信管道</param>
        /// <param name="codec">协议编解码器（应与管道中的一致）</param>
        protected DeviceBase(CommunicationPipeline pipeline, IProtocolCodec codec)
        {
            Pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            Codec = codec ?? throw new ArgumentNullException(nameof(codec));
            Name = GetType().Name;
            ConstructDefaultInfo();
        }

        /// <summary>
        /// 初始化设备基类（基础构造函数，直接注入会话层）
        /// 适用于测试、MQTT 等场景，不需要完整 OSI 链路
        /// </summary>
        /// <param name="session">会话层</param>
        /// <param name="codec">协议编解码器</param>
        protected DeviceBase(ISession session, IProtocolCodec codec)
            : this(CreatePipeline(null, null, null, null, session, codec), codec)
        {
        }

        /// <summary>
        /// 初始化设备基类（串口通信，完整参数）
        /// </summary>
        /// <param name="portName">串口名称</param>
        /// <param name="baudRate">波特率</param>
        /// <param name="dataBits">数据位</param>
        /// <param name="stopBits">停止位</param>
        /// <param name="parity">校验位</param>
        /// <param name="codec">协议编解码器</param>
        protected DeviceBase(
            string portName,
            int baudRate,
            int dataBits,
            StopBits stopBits,
            Parity parity,
            IProtocolCodec codec)
            : this(portName, baudRate, dataBits, stopBits, parity, codec, (byte[]?)null)
        {
        }

        /// <summary>
        /// 初始化设备基类（串口通信，自定义帧策略）
        /// 适用于 Modbus RTU 等需要特定帧格式的协议
        /// </summary>
        /// <param name="portName">串口名称</param>
        /// <param name="baudRate">波特率</param>
        /// <param name="dataBits">数据位</param>
        /// <param name="stopBits">停止位</param>
        /// <param name="parity">校验位</param>
        /// <param name="codec">协议编解码器</param>
        /// <param name="frameStrategy">帧策略（如 ModbusRtuFrameStrategy）</param>
        /// <param name="dataLinkOptions">数据链路配置选项（控制超时等），null 使用默认值</param>
        protected DeviceBase(
            string portName,
            int baudRate,
            int dataBits,
            StopBits stopBits,
            Parity parity,
            IProtocolCodec codec,
            IFrameStrategy frameStrategy,
            DataLinkOptions? dataLinkOptions = null)
            : this(CreatePipeline(
                    new SerialPortTransport(portName, baudRate, dataBits, stopBits, parity,
                        dtrEnable: true, rtsEnable: true),
                    frameStrategy, dataLinkOptions, null, null, codec),
                codec)
        {
        }

        /// <summary>
        /// 初始化设备基类（串口通信，带自定义帧分隔符）
        /// </summary>
        /// <param name="portName">串口名称</param>
        /// <param name="baudRate">波特率</param>
        /// <param name="dataBits">数据位</param>
        /// <param name="stopBits">停止位</param>
        /// <param name="parity">校验位</param>
        /// <param name="codec">协议编解码器</param>
        /// <param name="delimiter">帧分隔符，null 或不适用时按协议自动推导</param>
        /// <param name="dataLinkOptions">数据链路配置选项（控制超时等），null 使用默认值</param>
        protected DeviceBase(
            string portName,
            int baudRate,
            int dataBits,
            StopBits stopBits,
            Parity parity,
            IProtocolCodec codec,
            byte[]? delimiter,
            DataLinkOptions? dataLinkOptions = null)
            : this(CreatePipeline(
                    new SerialPortTransport(portName, baudRate, dataBits, stopBits, parity,
                        dtrEnable: true, rtsEnable: true),
                    ResolveFallbackFrameStrategy(codec, delimiter), dataLinkOptions, null, null, codec),
                codec)
        {
        }

        /// <summary>
        /// 初始化设备基类（串口通信，使用默认配置：9600,8,1,None）
        /// </summary>
        /// <param name="portName">串口名称</param>
        /// <param name="codec">协议编解码器</param>
        protected DeviceBase(string portName, IProtocolCodec codec)
            : this(portName, 9600, 8, StopBits.One, Parity.None, codec)
        {
        }

        /// <summary>
        /// 初始化设备基类（串口通信，默认串口参数 + 自定义帧策略）
        /// </summary>
        /// <param name="portName">串口名称</param>
        /// <param name="codec">协议编解码器</param>
        /// <param name="frameStrategy">帧策略</param>
        protected DeviceBase(string portName, IProtocolCodec codec, IFrameStrategy frameStrategy)
            : this(portName, 9600, 8, StopBits.One, Parity.None, codec, frameStrategy)
        {
        }

        /// <summary>
        /// 初始化设备基类（TCP通信，帧策略按协议自动推导）
        /// </summary>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="port">端口号</param>
        /// <param name="codec">协议编解码器</param>
        protected DeviceBase(IPAddress ipAddress, int port, IProtocolCodec codec)
            : this(CreatePipeline(
                    new TcpTransport(ipAddress.ToString(), port),
                    null, null, null, null, codec),
                codec)
        {
        }

        /// <summary>
        /// 初始化设备基类（TCP通信，自定义帧策略）
        /// </summary>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="port">端口号</param>
        /// <param name="codec">协议编解码器</param>
        /// <param name="frameStrategy">帧策略（如 ModbusRtuFrameStrategy）</param>
        protected DeviceBase(IPAddress ipAddress, int port, IProtocolCodec codec, IFrameStrategy frameStrategy)
            : this(CreatePipeline(
                    new TcpTransport(ipAddress.ToString(), port),
                    frameStrategy, null, null, null, codec),
                codec)
        {
        }

        /// <summary>
        /// 初始化设备基类（蓝牙通信）
        /// </summary>
        /// <param name="bluetoothOptions">蓝牙配置选项</param>
        /// <param name="codec">协议编解码器</param>
        protected DeviceBase(BluetoothOptions bluetoothOptions, IProtocolCodec codec)
            : this(new BluetoothSettings
            {
                BluetoothOptions = bluetoothOptions ?? throw new ArgumentNullException(nameof(bluetoothOptions))
            }.CreatePipeline(codec), codec)
        {
        }

        /// <summary>
        /// 初始化设备基类（通信设置实例）
        /// </summary>
        /// <param name="settings">通信配置</param>
        /// <param name="codec">协议编解码器</param>
        protected DeviceBase(DeviceCommSettings settings, IProtocolCodec codec)
            : this((settings ?? throw new ArgumentNullException(nameof(settings))).CreatePipeline(codec), codec)
        {
        }

        /// <summary>
        /// 初始化设备基类（通信设置实例，自定义帧策略）
        /// </summary>
        /// <param name="settings">通信配置</param>
        /// <param name="codec">协议编解码器</param>
        /// <param name="frameStrategy">帧策略（如 ModbusRtuFrameStrategy）</param>
        protected DeviceBase(DeviceCommSettings settings, IProtocolCodec codec, IFrameStrategy frameStrategy)
            : this(ApplyFrameStrategy(settings, frameStrategy).CreatePipeline(codec), codec)
        {
        }

        // ───────────────────────── 装配辅助 ─────────────────────────

        /// <summary>
        /// 统一的管道装配入口，消除 11 个构造函数里各写一遍的样板。
        /// </summary>
        private static CommunicationPipeline CreatePipeline(
            IPhysicalTransport? transport,
            IFrameStrategy? frameStrategy,
            DataLinkOptions? dataLinkOptions,
            SessionOptions? sessionOptions,
            ISession? session,
            IProtocolCodec codec,
            ILoggerFactory? loggerFactory = null)
        {
            var builder = new CommunicationPipelineBuilder();

            if (transport != null) builder.UseTransport(transport);
            if (frameStrategy != null) builder.UseDataLink(frameStrategy, dataLinkOptions);
            else if (dataLinkOptions != null) builder.UseDataLinkOptions(dataLinkOptions);

            if (session != null) builder.UseSession(session);
            if (sessionOptions != null) builder.UseSessionOptions(sessionOptions);
            if (loggerFactory != null) builder.UseLoggerFactory(loggerFactory);

            return builder.UseProtocol(codec).Build();
        }

        /// <summary>
        /// 显式分隔符只在"协议未登记且不自带终止符"时使用，否则交给协议推导。
        /// </summary>
        private static IFrameStrategy? ResolveFallbackFrameStrategy(IProtocolCodec codec, byte[]? delimiter)
        {
            if (delimiter == null) return null;
            if (ProtocolBindings.IsRegistered(codec)) return null;
            if (codec is IFramedProtocol) return null;

            return new DelimiterFrameStrategy(delimiter);
        }

        private static DeviceCommSettings ApplyFrameStrategy(
            DeviceCommSettings settings, IFrameStrategy frameStrategy)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (frameStrategy == null) throw new ArgumentNullException(nameof(frameStrategy));

            switch (settings)
            {
                case SerialPortSettings serial:
                    serial.FrameStrategy = frameStrategy;
                    break;
                case TcpSettings tcp:
                    tcp.FrameStrategy = frameStrategy;
                    break;
                case BluetoothSettings bluetooth:
                    bluetooth.FrameStrategy = frameStrategy;
                    break;
                default:
                    throw new ArgumentException(
                        $"{settings.GetType().Name} 不支持指定帧策略，请改用其它构造函数。",
                        nameof(settings));
            }

            return settings;
        }

        // ───────────────────────── 公共能力 ─────────────────────────

        /// <summary>设备是否已连接</summary>
        public bool IsOpen => Session.IsOpen;

        /// <summary>打开设备连接</summary>
        public virtual async Task OpenAsync(CancellationToken ct = default)
        {
            await Session.OpenAsync(ct).ConfigureAwait(false);
            CommunicationLogger.LogInfo(Name, "设备已打开");
        }

        /// <summary>关闭设备连接</summary>
        public virtual async Task CloseAsync()
        {
            await Session.CloseAsync().ConfigureAwait(false);
            CommunicationLogger.LogInfo(Name, "设备已关闭");
        }

        /// <summary>
        /// 发送命令并接收响应（不做业务解析，返回原始字节）。
        /// 适合需要在外部做自定义解析的场景。
        /// </summary>
        /// <param name="command">逻辑命令</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>原始响应数据</returns>
        protected virtual async Task<byte[]> SendAsync(
            Command command,
            CancellationToken ct = default)
        {
            var request = Codec.Encode(command);

            CommunicationLogger.LogSend(Name, command.Id, command.Kind.ToString(),
                ToLogText(Codec, request), request);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            byte[] response;
            try
            {
                response = await Session.SendAndReceiveAsync(request, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                CommunicationLogger.LogError(Name, $"发送命令 [{command.Id}] 失败", ex);
                throw;
            }
            finally
            {
                sw.Stop();
            }

            CommunicationLogger.LogReceive(Name, sw.ElapsedMilliseconds, response,
                ToLogText(Codec, response));

            if (Codec.IsErrorResponse(response, out var errMsg))
            {
                CommunicationLogger.LogError(Name, $"设备返回错误: {errMsg}");
                throw new DeviceException($"设备错误: {errMsg}");
            }

            return response;
        }

        /// <summary>
        /// 发送命令并返回带业务数据的操作结果。
        /// </summary>
        /// <typeparam name="T">业务数据类型</typeparam>
        /// <param name="command">逻辑命令</param>
        /// <param name="decoder">响应字节 → 业务数据的解析函数</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>业务数据</returns>
        protected async Task<T> SendForResultAsync<T>(
            Command command,
            Func<byte[], T> decoder,
            CancellationToken ct = default)
        {
            var response = await SendAsync(command, ct).ConfigureAwait(false);

            try
            {
                return decoder(response);
            }
            catch (Exception ex)
            {
                CommunicationLogger.LogError(Name, $"解析响应失败: {command.Id}", ex);
                throw new DeviceException($"解析响应失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 发送单向命令（不等待响应）
        /// </summary>
        /// <param name="command">逻辑命令</param>
        /// <param name="ct">取消令牌</param>
        protected virtual async Task SendNonQueryAsync(
            Command command,
            CancellationToken ct = default)
        {
            var request = Codec.Encode(command);

            CommunicationLogger.LogSend(Name, command.Id, command.Kind.ToString(),
                ToLogText(Codec, request), request);

            try
            {
                await Session.SendOnlyAsync(request, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                CommunicationLogger.LogError(Name, $"单向发送命令 [{command.Id}] 失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 日志用的文本视图。
        ///
        /// 二进制协议（Modbus 等）不能被当成 ASCII 直接打印 —— 过去
        /// <c>Encoding.ASCII.GetString(request)</c> 会让日志里充满乱码与控制字符。
        /// 这里改为：文本协议用 <see cref="IProtocolCodec.DecodeText"/>，
        /// 含不可打印字符时退回"可打印视图"。
        /// </summary>
        private static string ToLogText(IProtocolCodec codec, byte[] data)
        {
            if (data == null || data.Length == 0) return string.Empty;

            string decoded;
            try
            {
                decoded = codec.DecodeText(data) ?? string.Empty;
            }
            catch
            {
                return ToPrintable(data);
            }

            if (decoded.Length == 0) return ToPrintable(data);

            foreach (var c in decoded)
            {
                if (c < 0x20 && c != '\r' && c != '\n' && c != '\t')
                    return ToPrintable(data);
            }

            return decoded;
        }

        private static string ToPrintable(byte[] data)
        {
            var sb = new StringBuilder(data.Length);
            foreach (var b in data)
            {
                sb.Append(b == 0x0D || b == 0x0A || b == 0x09
                    ? (char)b
                    : b >= 0x20 && b < 0x7F ? (char)b : '.');
            }
            return sb.ToString();
        }

        /// <summary>
        /// 配置构造函数默认信息（子类可重写）
        /// </summary>
        protected virtual void ConstructDefaultInfo()
        {
            // 基类默认实现为空，子类可重写以设置设备特定默认值
        }

        /// <inheritdoc/>
        public virtual void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Pipeline.Dispose();
        }

        /// <inheritdoc/>
        public virtual async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            await Pipeline.DisposeAsync().ConfigureAwait(false);
        }
    }
}
