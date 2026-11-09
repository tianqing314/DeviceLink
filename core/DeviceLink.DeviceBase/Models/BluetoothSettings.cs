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
        /// 接收超时时间（毫秒），默认 5000ms
        /// 蓝牙设备建议设置为 10000ms 或更长
        /// </summary>
        public int ReceiveTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// 接收空闲超时时间（毫秒），默认 50ms
        /// </summary>
        public int ReceiveIdleTimeoutMs { get; set; } = 50;

        /// <summary>
        /// 连接失败/断开时的最大重连重发次数，默认 0（不重连）。
        /// 蓝牙链路容易断，建议设为 2-3 次 —— 但只在"请求确定未送达"时才真正重发。
        /// </summary>
        public int MaxConnectRetries
        {
            get => SessionOptions.MaxConnectRetries;
            set => SessionOptions.MaxConnectRetries = value;
        }

        /// <summary>
        /// 重连间隔（毫秒），默认 300ms
        /// </summary>
        public int ReconnectDelayMs
        {
            get => SessionOptions.ReconnectDelayMs;
            set => SessionOptions.ReconnectDelayMs = value;
        }

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
            var dataLinkOptions = new DataLinkOptions
            {
                ReceiveTimeoutMs = ReceiveTimeoutMs,
                ReceiveIdleTimeoutMs = ReceiveIdleTimeoutMs
            };

            return BuildPipeline(
                new BluetoothTransport(BluetoothOptions),
                codec, FrameStrategy, Delimiter, dataLinkOptions);
        }
    }
}