using System;

namespace DeviceLink.Core.Channel
{
    /// <summary>
    /// 通道执行策略 —— 替代旧 Xmas11 的 iPolicy。
    /// 控制请求超时、接收空闲超时、重试行为。
    /// </summary>
    public class ChannelPolicy
    {
        /// <summary>
        /// 默认策略：1秒请求超时 / 50ms空闲超时 / 不重试
        /// </summary>
        public static readonly ChannelPolicy Default = new();

        /// <summary>
        /// 发送指令后等待首个字节响应的最大时间（默认 1000ms）
        /// </summary>
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromMilliseconds(1000);

        /// <summary>
        /// 接收开始后连续无新数据的最大空闲时间（默认 50ms）
        /// </summary>
        public TimeSpan ReceiveIdleTimeout { get; set; } = TimeSpan.FromMilliseconds(50);

        /// <summary>
        /// 通讯失败时最大重试次数（默认 0，即不重试）
        /// </summary>
        public int MaxRetryCount { get; set; } = 0;

        /// <summary>
        /// 两次重试之间的等待时间（默认 300ms）
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(300);

        /// <summary>
        /// 接收循环的轮询间隔（默认 10ms）
        /// </summary>
        public int ReceivePollIntervalMs { get; set; } = 10;

        /// <summary>
        /// 快速构造：只需指定重试次数
        /// </summary>
        public static ChannelPolicy WithRetry(int retryCount, int requestTimeoutMs = 1000)
        {
            return new ChannelPolicy
            {
                MaxRetryCount = retryCount,
                RequestTimeout = TimeSpan.FromMilliseconds(requestTimeoutMs)
            };
        }
    }
}
