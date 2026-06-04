using System;
using System.IO.Ports;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DeviceLink.DataLink;
using DeviceLink.Pipeline;
using DeviceLink.Protocol;
using DeviceLink.Session;
using DeviceLink.Transport;

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
    /// </summary>
    public abstract class DeviceBase : IDisposable
    {
        /// <summary>通信管道（封装完整的 OSI 链路）</summary>
        protected CommunicationPipeline Pipeline { get; }

        /// <summary>会话层（从 Pipeline 中提取）</summary>
        protected ISession Session => Pipeline.Session;

        /// <summary>协议编解码器</summary>
        protected IProtocolCodec Codec { get; }

        /// <summary>设备名称，如 "DPSEX"、"DPG"</summary>
        public string Name { get; set; }

        /// <summary>
        /// 初始化设备基类（基础构造函数，直接注入会话层）
        /// 适用于测试、MQTT 等场景，不需要完整 OSI 链路
        /// </summary>
        /// <param name="session">会话层</param>
        /// <param name="codec">协议编解码器</param>
        protected DeviceBase(
            ISession session,
            IProtocolCodec codec)
        {
            var sessionObj = session ?? throw new ArgumentNullException(nameof(session));
            Codec = codec ?? throw new ArgumentNullException(nameof(codec));
            Name = GetType().Name;

            // 创建一个只包含 Session 的 Pipeline（无 Transport/DataLink）
            Pipeline = new CommunicationPipelineBuilder()
                .UseSession(sessionObj)
                .UseProtocol(Codec)
                .Build();

            ConstructDefaultInfo();
        }

        /// <summary>
        /// 初始化设备基类（串口通信，完整参数）
        /// 通过 CommunicationPipelineBuilder 组装完整 OSI 链路
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
        /// 初始化设备基类（串口通信，带自定义帧分隔符）
        /// 通过 CommunicationPipelineBuilder 组装完整 OSI 链路
        /// </summary>
        /// <param name="portName">串口名称</param>
        /// <param name="baudRate">波特率</param>
        /// <param name="dataBits">数据位</param>
        /// <param name="stopBits">停止位</param>
        /// <param name="parity">校验位</param>
        /// <param name="codec">协议编解码器</param>
        /// <param name="delimiter">帧分隔符（如 "\r\n" 对应 new byte[]{0x0D,0x0A}），null 则使用默认值 \0</param>
        protected DeviceBase(
            string portName,
            int baudRate,
            int dataBits,
            StopBits stopBits,
            Parity parity,
            IProtocolCodec codec,
            byte[]? delimiter)
        {
            Codec = codec ?? throw new ArgumentNullException(nameof(codec));
            Name = GetType().Name;

            var frameDelimiter = delimiter ?? new byte[] { 0 };

            // 通过 PipelineBuilder 组装完整 OSI 链路
            Pipeline = new CommunicationPipelineBuilder()
                .UseTransport(new SerialPortTransport(portName, baudRate, dataBits, stopBits, parity))
                .UseDataLink(new DelimiterFrameStrategy(frameDelimiter))
                .UseProtocol(Codec)
                .Build();

            ConstructDefaultInfo();
        }

        /// <summary>
        /// 初始化设备基类（串口通信，使用默认配置：9600,8,1,None）
        /// </summary>
        /// <param name="portName">串口名称</param>
        /// <param name="codec">协议编解码器</param>
        protected DeviceBase(
            string portName,
            IProtocolCodec codec)
            : this(portName, 9600, 8, StopBits.One, Parity.None, codec)
        {
        }

        /// <summary>
        /// 初始化设备基类（TCP通信）
        /// 通过 CommunicationPipelineBuilder 组装完整 OSI 链路
        /// </summary>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="port">端口号</param>
        /// <param name="codec">协议编解码器</param>
        protected DeviceBase(
            IPAddress ipAddress,
            int port,
            IProtocolCodec codec)
        {
            Codec = codec ?? throw new ArgumentNullException(nameof(codec));
            Name = GetType().Name;

            // 通过 PipelineBuilder 组装完整 OSI 链路
            Pipeline = new CommunicationPipelineBuilder()
                .UseTransport(new TcpTransport(ipAddress.ToString(), port))
                .UseDataLink(new DelimiterFrameStrategy(new byte[] { 0 }))
                .UseProtocol(Codec)
                .Build();

            ConstructDefaultInfo();
        }

        /// <summary>
        /// 初始化设备基类（通信设置实例）
        /// 通过 DeviceCommSettings 创建完整 OSI 链路
        /// </summary>
        /// <param name="settings">通信配置</param>
        /// <param name="codec">协议编解码器</param>
        protected DeviceBase(
            DeviceCommSettings settings,
            IProtocolCodec codec)
        {
            Codec = codec ?? throw new ArgumentNullException(nameof(codec));
            Name = GetType().Name;

            // 通过 Settings 创建完整 OSI 链路
            Pipeline = settings.CreatePipeline(Codec);

            ConstructDefaultInfo();
        }

        /// <summary>设备是否已连接</summary>
        public bool IsOpen => Session.IsOpen;

        /// <summary>打开设备连接</summary>
        public virtual async Task OpenAsync(CancellationToken ct = default)
        {
            await Session.OpenAsync(ct);
            CommunicationLogger.LogInfo(Name, "设备已打开");
        }

        /// <summary>关闭设备连接</summary>
        public virtual async Task CloseAsync()
        {
            await Session.CloseAsync();
            CommunicationLogger.LogInfo(Name, "设备已关闭");
        }

        /// <summary>
        /// 发送命令并接收响应（不做业务解析，返回原始字节）。
        /// 适合需要在外部做自定义解析的场景。
        /// </summary>
        /// <param name="command">逻辑命令</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>原始响应数据</returns>
        protected async Task<byte[]> SendAsync(
            Command command,
            CancellationToken ct = default)
        {
            // 编码命令
            var request = Codec.Encode(command);
            var commandString = System.Text.Encoding.ASCII.GetString(request);

            // 记录发送日志
            CommunicationLogger.LogSend(Name, command.Id, command.Kind.ToString(),
                commandString, request);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            byte[] response;
            try
            {
                response = await Session.SendAndReceiveAsync(request, ct);
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

            // 记录接收日志
            var responseText = Codec.DecodeText(response);
            CommunicationLogger.LogReceive(Name, sw.ElapsedMilliseconds, response, responseText);

            // 检查设备错误
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
            var response = await SendAsync(command, ct);

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
        protected async Task SendNonQueryAsync(
            Command command,
            CancellationToken ct = default)
        {
            var request = Codec.Encode(command);
            var commandString = System.Text.Encoding.ASCII.GetString(request);

            // 记录发送日志
            CommunicationLogger.LogSend(Name, command.Id, command.Kind.ToString(),
                commandString, request);

            try
            {
                await Session.SendOnlyAsync(request, ct);
            }
            catch (Exception ex)
            {
                CommunicationLogger.LogError(Name, $"单向发送命令 [{command.Id}] 失败", ex);
                throw;
            }
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
            Pipeline.Dispose();
        }
    }

}