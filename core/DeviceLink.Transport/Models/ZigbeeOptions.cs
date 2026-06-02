using System.IO.Ports;

namespace DeviceLink.Transport
{
    /// <summary>
    /// Zigbee模块配置选项
    /// </summary>
    public class ZigbeeOptions : SerialPortOptions
    {
        /// <summary>
        /// Zigbee模块类型
        /// </summary>
        public ZigbeeModuleType ModuleType { get; set; } = ZigbeeModuleType.ZM32;

        /// <summary>
        /// PAN ID (0x0000-0xFFFF)
        /// </summary>
        public ushort PanId { get; set; } = 0x1234;

        /// <summary>
        /// 通讯信道 (11-26)
        /// </summary>
        public byte Channel { get; set; } = 0x0B; // Channel 11

        /// <summary>
        /// 目标地址（64位长地址）
        /// </summary>
        public ulong DestinationAddress { get; set; } = 0;

        /// <summary>
        /// 是否使用API模式（仅XBee模块支持）
        /// </summary>
        public bool UseApiMode { get; set; } = false;

        /// <summary>
        /// 进入命令模式的保护时间（毫秒）
        /// </summary>
        public int GuardTimeMs { get; set; } = 1000;

        /// <summary>
        /// 命令模式超时时间（毫秒）
        /// </summary>
        public int CommandTimeoutMs { get; set; } = 2000;

        // ==================== ZM32 特有配置 ====================

        /// <summary>
        /// ZM32目标网络地址 (0x0000-0xFFFF)
        /// 0xFFFF代表广播给所有设备
        /// 0xFFFD代表广播给所有非睡眠设备
        /// 0xFFFC代表广播给协调器和路由设备
        /// </summary>
        public ushort ZM32_TargetNetworkAddress { get; set; } = 0x0000;

        /// <summary>
        /// ZM32发送模式
        /// Bit 0~2: 数据传输方式 (0=单播, 1=广播所有, 2=广播非睡眠, 3=广播协调器和路由, 4=组播)
        /// Bit 3: 数据目标地址选择 (0=网络地址, 1=MAC地址)
        /// Bit 4: 数据包格式 (0=数据, 1=指定网络地址+数据, 2=指定MAC地址+数据, 3=发送帧格式)
        /// Bit 7: 是否添加源MAC地址 (0=不添加, 1=添加)
        /// </summary>
        public byte ZM32_SendMode { get; set; } = 0x01; // 默认单播模式

        /// <summary>
        /// ZM32设备类型 (0=协调器, 1=路由器, 2=终端设备)
        /// </summary>
        public byte ZM32_DeviceType { get; set; } = 0x00; // 默认协调器

        /// <summary>
        /// ZM32是否启用自组网功能
        /// </summary>
        public bool ZM32_EnableAutoNetwork { get; set; } = false;

        /// <summary>
        /// ZM32目标分组号 (0x0000-0xFFFF，用于组播模式)
        /// </summary>
        public ushort ZM32_TargetGroupNumber { get; set; } = 0x0001;

        /// <summary>
        /// 从SerialPortOptions创建ZigbeeOptions
        /// </summary>
        /// <param name="serialOptions">串口配置选项</param>
        /// <param name="moduleType">Zigbee模块类型</param>
        /// <returns>ZigbeeOptions实例</returns>
        public static ZigbeeOptions FromSerialPortOptions(SerialPortOptions serialOptions, ZigbeeModuleType moduleType = ZigbeeModuleType.ZM32)
        {
            return new ZigbeeOptions
            {
                PortName = serialOptions.PortName,
                BaudRate = serialOptions.BaudRate,
                DataBits = serialOptions.DataBits,
                StopBits = serialOptions.StopBits,
                Parity = serialOptions.Parity,
                ReadBufferSize = serialOptions.ReadBufferSize,
                WriteBufferSize = serialOptions.WriteBufferSize,
                ModuleType = moduleType
            };
        }
    }
}
