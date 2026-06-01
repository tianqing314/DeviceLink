using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceLink.Core.Channel
{
    /// <summary>
    /// 统一请求-响应通道 —— 所有通讯模式的抽象。
    /// DirectChannel（直连设备）和 MqttChannel（Broker 中转）实现此接口。
    /// 上层设备类只依赖此接口，不感知传输方式。
    /// </summary>
    public interface IChannel : IDisposable
    {
        /// <summary>
        /// 通道名称（用于日志），如 "DPSEX@COM3"
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 通道是否已连接
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// 打开通道（建立底层连接）
        /// </summary>
        Task OpenAsync(CancellationToken ct = default);

        /// <summary>
        /// 关闭通道
        /// </summary>
        Task CloseAsync();

        /// <summary>
        /// 发送请求并等待完整帧响应。
        /// 内建超时、接收循环、EOF 分帧、重试逻辑。
        /// </summary>
        /// <param name="request">请求数据（不含帧分隔符，由 IProtocolCodec 编码后传入）</param>
        /// <param name="policy">通道策略（超时/重试），为 null 时使用默认策略</param>
        /// <param name="ct">取消令牌</param>
        Task<ChannelResult> SendAndReceiveAsync(
            byte[] request,
            ChannelPolicy? policy = null,
            CancellationToken ct = default);

        /// <summary>
        /// 单向发送（不等待响应）
        /// </summary>
        Task SendOnlyAsync(byte[] request, CancellationToken ct = default);

        /// <summary>
        /// 仅接收（不发送命令，等待数据到达）
        /// </summary>
        Task<ChannelResult> ReceiveOnlyAsync(
            ChannelPolicy? policy = null,
            CancellationToken ct = default);
    }
}
