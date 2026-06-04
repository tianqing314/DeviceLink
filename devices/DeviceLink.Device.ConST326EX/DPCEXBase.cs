using DeviceLink.Device.ConST326EX.Enums;
using DeviceLink.DeviceBase;
using DeviceLink.Protocol;
using DeviceLink.Session;
using System.IO.Ports;
using System.Net;

namespace DeviceLink.Device.ConST326EX
{
    /// <summary>
    /// ConST326EX 设备基类 —— 定义 ConST326EX 设备的通用属性和方法。
    /// </summary>
    public class DPCEXBase : DeviceLink.DeviceBase.DeviceBase
    {
        private readonly ScpiCodec _codec;
        #region 构造函数

        /// <summary>
        /// 构造函数（串口通讯方式使用）
        /// </summary>
        /// <param name="serialPortName">串口号（如 COM3）</param>
        /// <param name="baudRate">波特率</param>
        /// <param name="dataBits">数据位</param>
        /// <param name="stopBits">停止位</param>
        /// <param name="parity">校验位</param>
        public DPCEXBase(string serialPortName, int baudRate = 9600, int dataBits = 8,
            StopBits stopBits = StopBits.One, Parity parity = Parity.None)
            : base(serialPortName, baudRate, dataBits, stopBits, parity, new ScpiCodec("\r\n"), new byte[] { 0x0D, 0x0A })
        {
            _codec = (ScpiCodec)Codec;
        }

        /// <summary>
        /// 构造函数（串口通讯方式使用，默认配置）
        /// </summary>
        /// <param name="serialPortName">串口号（如 COM3）</param>
        public DPCEXBase(string serialPortName)
            : base(serialPortName, 9600, 8, StopBits.One, Parity.None, new ScpiCodec("\r\n"), new byte[] { 0x0D, 0x0A })
        {
            _codec = (ScpiCodec)Codec;
        }

        /// <summary>
        /// 构造函数（TCP 通讯方式使用）
        /// </summary>
        /// <param name="ipAddress">IP 地址</param>
        /// <param name="port">端口号</param>
        public DPCEXBase(IPAddress ipAddress, int port)
            : base(ipAddress, port, new ScpiCodec("\r\n"))
        {
            _codec = (ScpiCodec)Codec;
        }

        /// <summary>
        /// 构造函数（通信设置实例方式适用）
        /// </summary>
        /// <param name="settings">通信配置</param>
        public DPCEXBase(DeviceCommSettings settings)
            : base(settings, new ScpiCodec("\r\n"))
        {
            _codec = (ScpiCodec)Codec;
        }

        /// <summary>
        /// 构造函数（MQTT 通讯方式使用）
        /// </summary>
        /// <param name="brokerHost">MQTT Broker 地址</param>
        /// <param name="brokerPort">MQTT Broker 端口号</param>
        /// <param name="requestTopic">请求主题（设备接收命令的主题）</param>
        /// <param name="responseTopic">响应主题（设备发送响应的主题）</param>
        /// <param name="requestTimeoutMs">请求超时时间（毫秒，默认 5000）</param>
        public DPCEXBase(string brokerHost, int brokerPort, string requestTopic, string responseTopic,
            byte address = 255, int requestTimeoutMs = 5000)
            : base(new MqttSession(new MqttSessionOptions
            {
                BrokerHost = brokerHost,
                BrokerPort = brokerPort,
                RequestTopic = requestTopic,
                ResponseTopic = responseTopic,
                RequestTimeoutMs = requestTimeoutMs
            }), new ScpiCodec("\r\n"))
        {
            _codec = (ScpiCodec)Codec;
        }

        /// <summary>
        /// 配置构造函数默认信息
        /// </summary>
        protected override void ConstructDefaultInfo()
        {
            base.ConstructDefaultInfo();
            Name = "DPCEXBase";
        }

        #endregion 构造函数

        /// <summary>
        /// 读 TAG 标签（TAG 指令，参数为长度）
        /// </summary>
        /// <param name="length">标签长度（默认48）</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>TAG标签</returns>
        public async Task<string> GetVersion(VersionType versionType, CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("SYSTem:VERSion", versionType.ToString().Replace('_', ':')),
                raw => _codec.ExtractField(raw),
                ct);
        }
    }
}
