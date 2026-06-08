using System;
using System.IO.Ports;
using DeviceLink.DataLink;
using DeviceLink.Pipeline;
using DeviceLink.Protocol;
using DeviceLink.Transport;

namespace DeviceLink.DeviceBase
{
    /// <summary>
    /// 蓝牙通讯配置
    /// </summary>
    public class BluetoothSettings : DeviceCommSettings
    {
        /// <summary>
        /// 蓝牙配置选项
        /// </summary>
        public BluetoothOptions BluetoothOptions { get; set; } = new BluetoothOptions();

        /// <summary>
        /// 帧分隔符（当 FrameStrategy 为 null 时使用 DelimiterFrameStrategy）
        /// </summary>
        public byte[] Delimiter { get; set; } = new byte[] { 0 };

        /// <summary>
        /// 自定义帧策略（如 ModbusRtuFrameStrategy），为 null 时使用 DelimiterFrameStrategy
        /// </summary>
        public IFrameStrategy? FrameStrategy { get; set; }

        /// <summary>
        /// 初始化蓝牙通讯配置
        /// </summary>
        public BluetoothSettings()
        {
        }

        /// <summary>
        /// 初始化蓝牙通讯配置
        /// </summary>
        /// <param name="deviceAddress">蓝牙设备地址</param>
        public BluetoothSettings(string deviceAddress)
        {
            BluetoothOptions = new BluetoothOptions { DeviceAddress = deviceAddress };
        }

        /// <summary>
        /// 初始化蓝牙通讯配置
        /// </summary>
        /// <param name="options">蓝牙配置选项</param>
        public BluetoothSettings(BluetoothOptions options)
        {
            BluetoothOptions = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// 创建蓝牙通讯管道（完整 OSI 链路）
        /// </summary>
        /// <param name="codec">协议编解码器</param>
        /// <returns>通讯管道</returns>
        protected internal override CommunicationPipeline CreatePipeline(IProtocolCodec codec)
        {
            var dataLink = FrameStrategy ?? new DelimiterFrameStrategy(Delimiter);
            
            return new CommunicationPipelineBuilder()
                .UseTransport(new BluetoothTransport(BluetoothOptions))
                .UseDataLink(dataLink)
                .UseProtocol(codec)
                .Build();
        }
    }
}