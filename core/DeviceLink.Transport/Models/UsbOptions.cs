namespace DeviceLink.Transport
{
    /// <summary>
    /// USB 配置选项
    /// </summary>
    public class UsbOptions
    {
        /// <summary>
        /// 厂商ID
        /// </summary>
        public int VendorId { get; set; }

        /// <summary>
        /// 产品ID
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// 读取缓冲区大小
        /// </summary>
        public int ReadBufferSize { get; set; } = 4096;

        /// <summary>
        /// 写入缓冲区大小
        /// </summary>
        public int WriteBufferSize { get; set; } = 4096;
    }
}
