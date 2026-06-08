using System;

namespace DeviceLink.Transport
{
    /// <summary>
    /// 蓝牙通讯配置选项
    /// </summary>
    public class BluetoothOptions
    {
        /// <summary>
        /// 蓝牙设备地址（MAC地址或蓝牙名称）
        /// </summary>
        public string DeviceAddress { get; set; } = string.Empty;

        /// <summary>
        /// 蓝牙服务UUID（SPP默认：00001101-0000-1000-8000-00805F9B34FB）
        /// </summary>
        public Guid ServiceUuid { get; set; } = new Guid("00001101-0000-1000-8000-00805F9B34FB");

        /// <summary>
        /// 连接超时时间（毫秒）
        /// </summary>
        public int ConnectTimeoutMs { get; set; } = 10000;

        /// <summary>
        /// 读取缓冲区大小
        /// </summary>
        public int ReadBufferSize { get; set; } = 4096;

        /// <summary>
        /// 写入缓冲区大小
        /// </summary>
        public int WriteBufferSize { get; set; } = 2048;

        /// <summary>
        /// 是否在连接前自动配对
        /// </summary>
        public bool AutoPair { get; set; } = true;

        /// <summary>
        /// 配对PIN码（部分设备需要）
        /// </summary>
        public string? PinCode { get; set; }

        /// <summary>
        /// 是否使用蓝牙经典模式（RFCOMM/SPP）
        /// </summary>
        public bool UseClassicBluetooth { get; set; } = true;

        /// <summary>
        /// 蓝牙设备发现超时时间（毫秒）
        /// </summary>
        public int DiscoveryTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// 是否在连接前自动发现设备
        /// </summary>
        public bool AutoDiscover { get; set; } = false;

        /// <summary>
        /// 蓝牙设备类过滤（可选，用于过滤特定类型的设备）
        /// </summary>
        public string? DeviceClassFilter { get; set; }

        /// <summary>
        /// 是否启用蓝牙安全认证
        /// </summary>
        public bool EnableAuthentication { get; set; } = true;

        /// <summary>
        /// 是否启用蓝牙加密
        /// </summary>
        public bool EnableEncryption { get; set; } = false;
    }
}