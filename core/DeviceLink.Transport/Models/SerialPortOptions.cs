using System.IO.Ports;

namespace DeviceLink.Transport
{
    /// <summary>
    /// 串口配置选项
    /// </summary>
    public class SerialPortOptions
    {
        /// <summary>
        /// 串口名称，如 "COM1"
        /// </summary>
        public string PortName { get; set; } = "COM1";

        /// <summary>
        /// 波特率
        /// </summary>
        public int BaudRate { get; set; } = 9600;

        /// <summary>
        /// 数据位
        /// </summary>
        public int DataBits { get; set; } = 8;

        /// <summary>
        /// 停止位
        /// </summary>
        public StopBits StopBits { get; set; } = StopBits.One;

        /// <summary>
        /// 校验位
        /// </summary>
        public Parity Parity { get; set; } = Parity.None;

        /// <summary>
        /// 读取缓冲区大小
        /// </summary>
        public int ReadBufferSize { get; set; } = 4096;

        /// <summary>
        /// 写入缓冲区大小
        /// </summary>
        public int WriteBufferSize { get; set; } = 2048;

        /// <summary>
        /// 启用 DTR（数据终端就绪）信号
        /// </summary>
        public bool DtrEnable { get; set; }

        /// <summary>
        /// 启用 RTS（请求发送）信号
        /// </summary>
        public bool RtsEnable { get; set; }
    }
}
