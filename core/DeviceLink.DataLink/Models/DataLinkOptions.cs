namespace DeviceLink.DataLink
{
    /// <summary>
    /// 数据链路配置选项。
    ///
    /// 职责边界：本层只负责<b>定界、校验、超时</b>，不负责重试。
    /// 重试（含重连重发）属于会话层 <c>SessionOptions</c> —— 因为只有会话层
    /// 才知道"请求是否已送达"，而这个信息决定了重发是否安全。
    /// 历史上这里曾提供 MaxRetryCount 做无条件重发，会让写类命令被执行两次，
    /// 已移除（见 docs/OSI层次归属.md 迁移项 M1）。
    /// </summary>
    public class DataLinkOptions
    {
        /// <summary>
        /// 接收超时时间（毫秒）
        /// </summary>
        public int ReceiveTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// 接收空闲超时时间（毫秒）—— 连续无数据到达视为帧结束。
        /// 若此时仍有未定界的残留字节，判定为坏帧并抛出 <see cref="FrameFormatException"/>，
        /// 绝不把半帧当作成功响应返回。
        /// </summary>
        public int ReceiveIdleTimeoutMs { get; set; } = 50;

        /// <summary>
        /// 接收轮询间隔（毫秒）
        /// </summary>
        public int ReceivePollIntervalMs { get; set; } = 10;

        /// <summary>
        /// 接收累积缓冲区上限（字节）。对端持续发送无法定界的字节流时，
        /// 累积缓冲区会无限增长直到 OOM；链路层必须设硬上限，超过即判为坏帧并清空。
        /// 默认 64 KiB，远大于任何常见工业协议的单帧长度。
        /// </summary>
        public int MaxAccumulatedBytes { get; set; } = 64 * 1024;
    }
}
