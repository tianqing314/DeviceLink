namespace DeviceLink.Transport
{
    /// <summary>
    /// TCP 配置选项
    /// </summary>
    public class TcpOptions
    {
        /// <summary>
        /// 主机地址
        /// </summary>
        public string Host { get; set; } = "127.0.0.1";

        /// <summary>
        /// 端口号
        /// </summary>
        public int Port { get; set; } = 10001;

        /// <summary>
        /// 连接超时时间（毫秒）
        /// </summary>
        public int ConnectTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// 读取缓冲区大小
        /// </summary>
        public int ReadBufferSize { get; set; } = 8192;

        /// <summary>
        /// 写入缓冲区大小
        /// </summary>
        public int WriteBufferSize { get; set; } = 4096;
    }
}
