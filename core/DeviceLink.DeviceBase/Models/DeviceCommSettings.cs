using System;
using System.IO.Ports;
using System.Net;
using DeviceLink.DataLink;
using DeviceLink.Pipeline;
using DeviceLink.Protocol;
using DeviceLink.Transport;

namespace DeviceLink.DeviceBase
{
    /// <summary>
    /// 设备通信配置基类
    /// 通过 CommunicationPipelineBuilder 组装完整的 OSI 通信栈
    /// </summary>
    public abstract class DeviceCommSettings
    {
        /// <summary>
        /// 创建通信管道（完整 OSI 链路）
        /// </summary>
        /// <param name="codec">协议编解码器</param>
        /// <returns>通信管道</returns>
        internal abstract CommunicationPipeline CreatePipeline(IProtocolCodec codec);
    }

    /// <summary>
    /// 串口通信配置
    /// </summary>
    public class SerialPortSettings : DeviceCommSettings
    {
        /// <summary>
        /// 串口名称
        /// </summary>
        public string PortName { get; set; } = "COM1";

        /// <summary>
        /// 波特率
        /// </summary>
        public int BaudRate { get; set; } = 4800;

        /// <summary>
        /// 数据位
        /// </summary>
        public int DataBits { get; set; } = 8;

        /// <summary>
        /// 停止位
        /// </summary>
        public StopBits StopBits { get; set; } = StopBits.Two;

        /// <summary>
        /// 校验位
        /// </summary>
        public Parity Parity { get; set; } = Parity.None;

        /// <summary>
        /// 帧分隔符
        /// </summary>
        public byte[] Delimiter { get; set; } = new byte[] { 0 };

        /// <summary>
        /// 初始化串口通信配置
        /// </summary>
        public SerialPortSettings()
        {
        }

        /// <summary>
        /// 初始化串口通信配置
        /// </summary>
        /// <param name="portName">串口名称</param>
        /// <param name="baudRate">波特率</param>
        /// <param name="dataBits">数据位</param>
        /// <param name="stopBits">停止位</param>
        /// <param name="parity">校验位</param>
        public SerialPortSettings(string portName, int baudRate, int dataBits, StopBits stopBits, Parity parity)
        {
            PortName = portName ?? throw new ArgumentNullException(nameof(portName));
            BaudRate = baudRate;
            DataBits = dataBits;
            StopBits = stopBits;
            Parity = parity;
        }

        /// <summary>
        /// 创建默认串口配置（4800,8,2,None）
        /// </summary>
        /// <param name="portName">串口名称</param>
        /// <returns>默认串口配置</returns>
        public static SerialPortSettings CreateDefault(string portName)
        {
            return new SerialPortSettings
            {
                PortName = portName,
                BaudRate = 4800,
                DataBits = 8,
                StopBits = StopBits.Two,
                Parity = Parity.None
            };
        }

        /// <summary>
        /// 创建串口通信管道（完整 OSI 链路）
        /// </summary>
        /// <param name="codec">协议编解码器</param>
        /// <returns>通信管道</returns>
        internal override CommunicationPipeline CreatePipeline(IProtocolCodec codec)
        {
            return new CommunicationPipelineBuilder()
                .UseTransport(new SerialPortTransport(PortName, BaudRate, DataBits, StopBits, Parity))
                .UseDataLink(new DelimiterFrameStrategy(Delimiter))
                .UseProtocol(codec)
                .Build();
        }
    }

    /// <summary>
    /// TCP通信配置
    /// </summary>
    public class TcpSettings : DeviceCommSettings
    {
        /// <summary>
        /// IP地址
        /// </summary>
        public IPAddress IpAddress { get; set; } = IPAddress.Loopback;

        /// <summary>
        /// 端口号
        /// </summary>
        public int Port { get; set; } = 10001;

        /// <summary>
        /// 连接超时时间（毫秒）
        /// </summary>
        public int ConnectTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// 帧分隔符
        /// </summary>
        public byte[] Delimiter { get; set; } = new byte[] { 0 };

        /// <summary>
        /// 初始化TCP通信配置
        /// </summary>
        public TcpSettings()
        {
        }

        /// <summary>
        /// 初始化TCP通信配置
        /// </summary>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="port">端口号</param>
        public TcpSettings(IPAddress ipAddress, int port)
        {
            IpAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
            Port = port;
        }

        /// <summary>
        /// 初始化TCP通信配置
        /// </summary>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="port">端口号</param>
        /// <param name="connectTimeoutMs">连接超时时间（毫秒）</param>
        public TcpSettings(IPAddress ipAddress, int port, int connectTimeoutMs)
            : this(ipAddress, port)
        {
            ConnectTimeoutMs = connectTimeoutMs;
        }

        /// <summary>
        /// 创建TCP通信管道（完整 OSI 链路）
        /// </summary>
        /// <param name="codec">协议编解码器</param>
        /// <returns>通信管道</returns>
        internal override CommunicationPipeline CreatePipeline(IProtocolCodec codec)
        {
            return new CommunicationPipelineBuilder()
                .UseTransport(new TcpTransport(IpAddress.ToString(), Port, ConnectTimeoutMs))
                .UseDataLink(new DelimiterFrameStrategy(Delimiter))
                .UseProtocol(codec)
                .Build();
        }
    }
}