using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceLink.Core.Transport
{
    /// <summary>
    /// 纯字节传输抽象 —— 只负责收发字节，无协议/分帧/命令概念。
    /// 每种物理介质（串口/TCP/USB/蓝牙）实现一次此接口。
    /// </summary>
    public interface IByteTransport : IDisposable
    {
        /// <summary>
        /// 传输名称（用于日志和调试），如 "COM3@9600" / "192.168.1.100:10001"
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 是否已建立物理连接
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// 建立连接
        /// </summary>
        Task ConnectAsync(CancellationToken ct = default);

        /// <summary>
        /// 关闭连接
        /// </summary>
        Task CloseAsync();

        /// <summary>
        /// 从传输介质读取字节到 buffer，返回实际读取字节数。
        /// 若无数据可用，应返回 0 而非阻塞。
        /// </summary>
        Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct = default);

        /// <summary>
        /// 向传输介质写入字节
        /// </summary>
        Task WriteAsync(byte[] data, int offset, int count, CancellationToken ct = default);

        /// <summary>
        /// 清空接收缓冲区中的残留数据
        /// </summary>
        Task ClearReceiveBufferAsync(CancellationToken ct = default);
    }
}
