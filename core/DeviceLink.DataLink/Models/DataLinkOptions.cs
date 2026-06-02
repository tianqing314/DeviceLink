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
        public int ReceiveTimeoutMs { get; set; } = 1000;

        /// <summary>
        /// 接收空闲超时时间（毫秒）—— 连续无数据到达视为帧结束
        /// </summary>
        public int ReceiveIdleTimeoutMs { get; set; } = 50;

        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetryCount { get; set; } = 0;

        /// <summary>
        /// 重试延迟时间（毫秒）
        /// </summary>
        public int RetryDelayMs { get; set; } = 300;

        /// <summary>
        /// 接收轮询间隔（毫秒）
        /// </summary>
        public int ReceivePollIntervalMs { get; set; } = 10;
    }
}