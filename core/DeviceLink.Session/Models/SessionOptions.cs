namespace DeviceLink.Session
{
    /// <summary>
    /// 会话配置选项
    /// </summary>
    public class SessionOptions
    {
        /// <summary>
        /// 会话级请求超时时间（毫秒）。0 表示禁用（默认，保持历史行为）。
        /// 说明：DirectSession 仅在大于 0 时启用会话级超时；默认超时由数据链路层的
        /// ReceiveTimeoutMs 控制。MQTT 会话使用独立的 MqttSessionOptions。
        /// 重试不由会话层负责：帧级重试统一由数据链路层（DataLinkOptions.MaxRetryCount）管理。
        /// </summary>
        public int RequestTimeoutMs { get; set; } = 0;
    }
}
