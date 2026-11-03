namespace DeviceLink.DataLink
{
    /// <summary>
    /// 数据链路配置选项
    /// </summary>
    public class DataLinkOptions
    {
        /// <summary>
        /// 接收超时时间（毫秒）
        /// </summary>
        public int ReceiveTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// 接收空闲超时时间（毫秒）—— 连续无数据到达视为帧结束
        /// </summary>
        public int ReceiveIdleTimeoutMs { get; set; } = 50;

        /// <summary>
        /// 最大重试次数（帧级，默认 0 表示不重试）。
        /// 这里是框架内唯一的重试入口：会话层只负责超时、不再重试，
        /// 避免两层重试叠加放大实际尝试次数。
        /// </summary>
        public int MaxRetryCount { get; set; } = 0;

        /// <summary>
        /// 重试延迟时间（毫秒），仅在 MaxRetryCount &gt; 0 时生效
        /// </summary>
        public int RetryDelayMs { get; set; } = 300;

        /// <summary>
        /// 接收轮询间隔（毫秒）
        /// </summary>
        public int ReceivePollIntervalMs { get; set; } = 10;
    }
}