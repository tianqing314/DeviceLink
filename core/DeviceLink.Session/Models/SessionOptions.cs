namespace DeviceLink.Session
{
    /// <summary>
    /// 会话配置选项。
    ///
    /// 职责边界（按 OSI L5 定义）：
    /// · 会话层负责<b>连接生命周期</b>与<b>请求-响应配对</b>；
    /// · <b>重连</b>在这里决策并执行；
    /// · <b>重试</b>在这里受到严格限制 —— 只有"请求确定未送达"时才允许重发。
    ///
    /// 为什么不无条件重试：
    /// 写类命令（Modbus 0x06/0x10、SCPI 的 SET 操作）如果已经送达设备但响应丢失，
    /// 重发会导致设备执行两次。因此超时后的正确动作是"断开并交给应用层决策"，
    /// 而不是悄悄重发。
    ///
    /// "是否送达"不再靠 <c>IsOpen</c> 事后推断，而是由数据链路层用异常类型表达：
    /// <see cref="DeviceLink.DataLink.RequestNotSentException"/> 表示确定未送达，
    /// 其余异常一律视为"结果未知"。
    /// </summary>
    public class SessionOptions
    {
        /// <summary>
        /// 会话级请求超时时间（毫秒）。0 表示禁用（默认）。
        /// 说明：DirectSession 仅在大于 0 时启用会话级超时；默认超时由数据链路层的
        /// ReceiveTimeoutMs 控制。MQTT 会话使用独立的 MqttSessionOptions。
        /// </summary>
        public int RequestTimeoutMs { get; set; } = 0;

        /// <summary>
        /// 连接建立失败或连接已断开时的最大重连次数。默认 0，失败即抛出。
        ///
        /// 只有"请求确定未送达"时才会重连重发：
        /// · 连接建立失败 → 请求尚未发出 → 重连后重发是安全的；
        /// · 已发出但读超时 → 设备可能已执行 → <b>不重发</b>，只断开连接。
        /// </summary>
        public int MaxConnectRetries { get; set; } = 0;

        /// <summary>重连间隔（毫秒），仅在 <see cref="MaxConnectRetries"/> 大于 0 时生效。</summary>
        public int ReconnectDelayMs { get; set; } = 300;

        /// <summary>
        /// 请求失败后是否关闭连接，使下一次调用重新建立连接。
        /// 默认 false。长连接设备（Modbus TCP、SCPI over TCP）在超时后建议开启，
        /// 避免在一条已被对端半关闭的 socket 上继续收发。
        /// </summary>
        public bool ReconnectOnFailure { get; set; } = false;
    }
}
