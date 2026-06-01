using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceLink.Session
{
    /// <summary>
    /// 会话层接口 —— 管理请求-响应会话。
    /// 会话层可以基于数据链路层（直连）或网络层（MQTT）实现。
    /// </summary>
    public interface ISession : IDisposable
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

    /// <summary>
    /// 会话配置选项
    /// </summary>
    public class SessionOptions
    {
        /// <summary>
        /// 请求超时时间（毫秒）
        /// </summary>
        public int RequestTimeoutMs { get; set; } = 1000;

        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetryCount { get; set; } = 0;

        /// <summary>
        /// 重试延迟时间（毫秒）
        /// </summary>
        public int RetryDelayMs { get; set; } = 300;
    }
}
