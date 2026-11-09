using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceLink.DataLink
{
    /// <summary>
    /// 原始帧通道 —— 供"帧格式由设备侧完全掌控"的协议使用（例如 CPPI V3 转接板报文）。
    ///
    /// 它存在的意义是消除设备层对 <c>IPhysicalTransport</c> 的越级调用：
    /// 过去设备为了"发送自己已经拼好的完整帧、不被链路层再加 CRC"，会直接拿
    /// <c>Pipeline.Transport</c> 读写字节，顺带把"读循环 + 超时 + 累积"整段抄进驱动里。
    ///
    /// 现在这两件事各归各位：
    /// · <b>L2（本通道）</b>负责：串行化、连接保证、字节读取、首次超时、空闲超时、累积上限；
    /// · <b>设备（L7）</b>负责：决定"什么算一帧"（通过 <see cref="RawFrameOptions.TryParse"/>）。
    /// </summary>
    public interface IRawFrameChannel
    {
        /// <summary>连接是否可用。</summary>
        bool IsOpen { get; }

        /// <summary>
        /// 发送原始帧并接收响应。整个过程在一次链路事务内完成，
        /// 不会与同一链路上的普通请求交错。
        /// </summary>
        /// <param name="frame">已经拼装完整的帧（链路层不再添加任何字节）</param>
        /// <param name="options">定界与超时选项</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>响应字节；若空闲超时时仍有残留字节，原样返回由调用方校验</returns>
        Task<byte[]> ExchangeRawAsync(byte[] frame, RawFrameOptions options, CancellationToken ct = default);

        /// <summary>
        /// 单向发送原始帧，不等待响应。
        /// </summary>
        /// <param name="frame">已经拼装完整的帧</param>
        /// <param name="ct">取消令牌</param>
        Task SendRawAsync(byte[] frame, CancellationToken ct = default);
    }

    /// <summary>
    /// 原始帧接收选项。
    /// </summary>
    public sealed class RawFrameOptions
    {
        /// <summary>首个字节的等待超时（毫秒）。超时即判定设备无响应。</summary>
        public int FirstByteTimeoutMs { get; set; } = 15000;

        /// <summary>接收空闲超时（毫秒）。连续无新数据视为"这一帧收完了"。</summary>
        public int IdleTimeoutMs { get; set; } = 100;

        /// <summary>无数据时的轮询间隔（毫秒）。</summary>
        public int PollIntervalMs { get; set; } = 5;

        /// <summary>
        /// 自定义定界函数：输入累积字节，返回完整帧长度；返回 0 或负数表示"还没收完"。
        /// 为 null 时只按空闲超时定界。
        /// </summary>
        public Func<byte[], int>? TryParse { get; set; }
    }
}
