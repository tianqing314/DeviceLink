namespace DeviceLink.Transport
{
    /// <summary>
    /// UDP 配置选项
    /// </summary>
    public class UdpOptions
    {
        /// <summary>
        /// 远程主机地址
        /// </summary>
        public string RemoteHost { get; set; } = "127.0.0.1";

        /// <summary>
        /// 远程端口号
        /// </summary>
        public int RemotePort { get; set; } = 10001;

        /// <summary>
        /// 本地端口号（0表示自动分配）
        /// </summary>
        public int LocalPort { get; set; } = 0;

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
