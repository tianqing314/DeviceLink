using System;
using System.Threading;
using System.Threading.Tasks;
using DeviceLink.Core.Channel;
using DeviceLink.Core.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DeviceLink.Core
{
    /// <summary>
    /// 设备基类 —— 框架唯一提供给设备层的基类。
    /// 
    /// 提供：
    /// · SendAsync / SendForResultAsync — 发命令、收响应
    /// · 自动错误检查（通过 IProtocolCodec.IsErrorResponse）
    /// · 日志记录（设备名 + 操作名）
    /// 
    /// 不提供（也不该提供）：
    /// · 任何业务接口（压力、温度、校准等——这些都是设备类自己的事）
    /// · 任何重试逻辑（重试在 Channel 层已处理）
    /// </summary>
    public abstract class DeviceBase : IDisposable
    {
        protected IChannel Channel { get; }
        protected IProtocolCodec Codec { get; }
        protected ILogger Logger { get; }

        /// <summary>设备名称，如 "DPSEX"、"DPG"</summary>
        public string Name { get; set; }

        protected DeviceBase(
            IChannel channel,
            IProtocolCodec codec,
            ILogger? logger = null)
        {
            Channel = channel ?? throw new ArgumentNullException(nameof(channel));
            Codec = codec ?? throw new ArgumentNullException(nameof(codec));
            Logger = logger ?? NullLogger.Instance;
            Name = GetType().Name;
        }

        /// <summary>设备是否已连接</summary>
        public bool IsOpen => Channel.IsOpen;

        /// <summary>打开设备连接</summary>
        public virtual async Task OpenAsync(CancellationToken ct = default)
        {
            await Channel.OpenAsync(ct);
            Logger.LogInformation("[{Device}] 设备已打开", Name);
        }

        /// <summary>关闭设备连接</summary>
        public virtual async Task CloseAsync()
        {
            await Channel.CloseAsync();
            Logger.LogInformation("[{Device}] 设备已关闭", Name);
        }

        /// <summary>
        /// 发送命令并接收响应（不做业务解析，返回原始字节和 ChannelResult）。
        /// 适合需要在外部做自定义解析的场景。
        /// </summary>
        /// <param name="command">逻辑命令</param>
        /// <param name="policy">通道策略（为 null 时使用默认）</param>
        /// <param name="ct">取消令牌</param>
        protected async Task<ChannelResult> SendAsync(
            Command command,
            ChannelPolicy? policy = null,
            CancellationToken ct = default)
        {
            var request = Codec.Encode(command);
            Logger.LogDebug("[{Device}] 发送命令: {Command}", Name, command.Id);

            var result = await Channel.SendAndReceiveAsync(request, policy, ct);

            if (result.Success)
            {
                // 检查设备错误
                if (Codec.IsErrorResponse(result.Data, out var errMsg))
                {
                    Logger.LogWarning("[{Device}] 设备返回错误: {Error}", Name, errMsg);
                    return ChannelResult.Fail($"设备错误: {errMsg}");
                }

                Logger.LogDebug("[{Device}] 接收响应: {Text}", Name,
                    Codec.DecodeText(result.Data));
            }
            else
            {
                Logger.LogWarning("[{Device}] 通讯失败: {Error}", Name, result.Error);
            }

            return result;
        }

        /// <summary>
        /// 发送命令并返回带业务数据的操作结果。
        /// </summary>
        /// <typeparam name="T">业务数据类型</typeparam>
        /// <param name="command">逻辑命令</param>
        /// <param name="decoder">响应字节 → 业务数据的解析函数</param>
        /// <param name="policy">通道策略</param>
        /// <param name="ct">取消令牌</param>
        protected async Task<DeviceResult<T>> SendForResultAsync<T>(
            Command command,
            Func<byte[], T> decoder,
            ChannelPolicy? policy = null,
            CancellationToken ct = default)
        {
            var channelResult = await SendAsync(command, policy, ct);

            if (!channelResult.Success)
                return DeviceResult<T>.Failed(channelResult.Error, channelResult);

            try
            {
                var value = decoder(channelResult.Data);
                return DeviceResult<T>.Succeed(value, channelResult);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[{Device}] 解析响应失败: {Command}", Name, command.Id);
                return DeviceResult<T>.Failed($"解析失败: {ex.Message}", channelResult);
            }
        }

        /// <summary>
        /// 发送单向命令（不等待响应）
        /// </summary>
        protected async Task SendNonQueryAsync(
            Command command,
            CancellationToken ct = default)
        {
            var request = Codec.Encode(command);
            Logger.LogDebug("[{Device}] 单向发送: {Command}", Name, command.Id);
            await Channel.SendOnlyAsync(request, ct);
        }

        public virtual void Dispose()
        {
            Channel.Dispose();
        }
    }
}
