using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceLink.Transport
{
    /// <summary>
    /// 物理传输层接口 —— 负责底层字节传输，无协议/分帧/命令概念。
    /// 每种物理介质（串口/TCP/UDP/USB/蓝牙）实现一次此接口。
    /// </summary>
    public interface IPhysicalTransport : IDisposable
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
        /// <param name="ct">取消令牌</param>
        Task ConnectAsync(CancellationToken ct = default);

        /// <summary>
        /// 关闭连接
        /// </summary>
        Task CloseAsync();

        /// <summary>
        /// 从传输介质读取字节到 buffer，返回实际读取字节数。
        /// 若无数据可用，应返回 0 而非阻塞。
        /// </summary>
        /// <param name="buffer">接收缓冲区</param>
        /// <param name="offset">缓冲区偏移量</param>
        /// <param count="count">要读取的字节数</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>实际读取的字节数</returns>
        Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct = default);

        /// <summary>
        /// 向传输介质写入字节
        /// </summary>
        /// <param name="data">要写入的数据</param>
        /// <param name="offset">数据偏移量</param>
        /// <param name="count">要写入的字节数</param>
        /// <param name="ct">取消令牌</param>
        Task WriteAsync(byte[] data, int offset, int count, CancellationToken ct = default);

        /// <summary>
        /// 清空接收缓冲区中的残留数据
        /// </summary>
        /// <param name="ct">取消令牌</param>
        Task ClearReceiveBufferAsync(CancellationToken ct = default);
    }
}
