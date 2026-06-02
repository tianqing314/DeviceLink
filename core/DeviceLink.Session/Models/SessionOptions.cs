namespace DeviceLink.Session
{
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