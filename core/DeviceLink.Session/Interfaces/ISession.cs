using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceLink.Session
{
    /// <summary>
    /// 会话层接口 —— 管理请求-响应会话。
    /// 会话层可以基于数据链路层（直连）或网络层（MQTT）实现。
    ///
    /// <b>线程安全契约（实现方必须满足）</b>：
    /// 同一个会话实例被多个线程并发调用时，实现必须自行串行化请求，
    /// 保证"一个请求对应一个响应"不会错配。调用方不需要自己加锁。
    ///
    /// <b>异常契约</b>：
    /// · 超时抛 <see cref="SessionTimeoutException"/>；
    /// · 连接无法建立抛 <see cref="SessionConnectionException"/>；
    /// · 其余失败抛 <see cref="SessionException"/>。
    /// 所有异常都继承 <see cref="SessionException"/>，调用方可只捕获基类。
    /// </summary>
    public interface ISession : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// 会话名称（用于日志）
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 会话是否已打开
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// 打开会话
        /// </summary>
        /// <param name="ct">取消令牌</param>
        Task OpenAsync(CancellationToken ct = default);

        /// <summary>
        /// 关闭会话
        /// </summary>
        Task CloseAsync();

        /// <summary>
        /// 发送请求并等待响应。
        /// 内建超时、重试、线程安全等逻辑。
        /// </summary>
        /// <param name="request">请求数据</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>响应数据</returns>
        Task<byte[]> SendAndReceiveAsync(byte[] request, CancellationToken ct = default);

        /// <summary>
        /// 单向发送（不等待响应）
        /// </summary>
        /// <param name="request">请求数据</param>
        /// <param name="ct">取消令牌</param>
        Task SendOnlyAsync(byte[] request, CancellationToken ct = default);

        /// <summary>
        /// 仅接收（不发送命令，等待数据到达）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>接收到的数据</returns>
        Task<byte[]> ReceiveOnlyAsync(CancellationToken ct = default);
    }
}
