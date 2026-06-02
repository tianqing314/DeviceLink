using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceLink.Transport
{
    /// <summary>
    /// Zigbee模块抽象接口 —— 定义不同厂商Zigbee模块的通用操作。
    /// 每种Zigbee模块（XBee/CC2530/ZM32）实现一次此接口。
    /// </summary>
    public interface IZigbeeModule
    {
        /// <summary>
        /// 模块名称（用于日志和调试），如 "XBee" / "CC2530" / "ZM32"
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 进入AT命令模式
        /// </summary>
        /// <param name="transport">串口传输层实例</param>
        /// <param name="ct">取消令牌</param>
        Task EnterCommandModeAsync(IPhysicalTransport transport, CancellationToken ct = default);

        /// <summary>
        /// 退出AT命令模式
        /// </summary>
        /// <param name="transport">串口传输层实例</param>
        /// <param name="ct">取消令牌</param>
        Task ExitCommandModeAsync(IPhysicalTransport transport, CancellationToken ct = default);

        /// <summary>
        /// 配置PAN ID
        /// </summary>
        /// <param name="transport">串口传输层实例</param>
        /// <param name="panId">PAN ID (0x0000-0xFFFF)</param>
        /// <param name="ct">取消令牌</param>
        Task ConfigurePanIdAsync(IPhysicalTransport transport, ushort panId, CancellationToken ct = default);

        /// <summary>
        /// 配置通讯信道
        /// </summary>
        /// <param name="transport">串口传输层实例</param>
        /// <param name="channel">信道号 (11-26)</param>
        /// <param name="ct">取消令牌</param>
        Task ConfigureChannelAsync(IPhysicalTransport transport, byte channel, CancellationToken ct = default);

        /// <summary>
        /// 配置目标地址
        /// </summary>
        /// <param name="transport">串口传输层实例</param>
        /// <param name="destAddress">目标地址（64位长地址或16位短地址）</param>
        /// <param name="ct">取消令牌</param>
        Task ConfigureDestinationAsync(IPhysicalTransport transport, ulong destAddress, CancellationToken ct = default);

        /// <summary>
        /// 构建数据帧
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <param name="destination">目标地址（可选，用于指定发送目标）</param>
        /// <returns>构建后的数据帧</returns>
        byte[] BuildDataFrame(byte[] data, string? destination = null);

        /// <summary>
        /// 尝试解析数据帧
        /// </summary>
        /// <param name="frame">接收到的帧数据</param>
        /// <param name="data">解析出的有效数据</param>
        /// <param name="source">数据来源地址</param>
        /// <returns>是否解析成功</returns>
        bool TryParseDataFrame(byte[] frame, out byte[] data, out string? source);
    }
}
