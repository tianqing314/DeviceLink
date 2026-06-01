using System;
using System.Threading;
using System.Threading.Tasks;
using DeviceLink.Transport;

namespace DeviceLink.DataLink
{
    /// <summary>
    /// 数据链路层接口 —— 负责帧的组装、解析和边界检测。
    /// 每种帧格式（分隔符帧、定长帧、Modbus RTU帧）实现一次此接口。
    /// </summary>
    public interface IDataLink : IDisposable
    {
        /// <summary>
        /// 数据链路名称（用于日志）
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 底层物理传输
        /// </summary>
        IPhysicalTransport Transport { get; }

        /// <summary>
        /// 是否已连接
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// 打开数据链路（建立底层连接）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        Task OpenAsync(CancellationToken ct = default);

        /// <summary>
        /// 关闭数据链路
        /// </summary>
        Task CloseAsync();

        /// <summary>
        /// 发送一帧数据并等待完整帧响应。
        /// 内建超时、接收循环、帧边界检测、重试逻辑。
        /// </summary>
        /// <param name="frameData">帧数据（不含帧分隔符/帧头帧尾）</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>接收到的响应帧数据</returns>
        Task<byte[]> SendAndReceiveFrameAsync(byte[] frameData, CancellationToken ct = default);

        /// <summary>
        /// 单向发送一帧数据（不等待响应）
        /// </summary>
        /// <param name="frameData">帧数据</param>
        /// <param name="ct">取消令牌</param>
        Task SendFrameAsync(byte[] frameData, CancellationToken ct = default);

        /// <summary>
        /// 仅接收一帧数据（不发送命令，等待数据到达）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>接收到的帧数据</returns>
        Task<byte[]> ReceiveFrameAsync(CancellationToken ct = default);
    }

    /// <summary>
    /// 帧策略接口 —— 定义帧的组装和解析规则
    /// </summary>
    public interface IFrameStrategy
    {
        /// <summary>
        /// 帧策略名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 将数据组装成帧（添加帧头、帧尾、校验等）
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <returns>完整的帧数据</returns>
        byte[] BuildFrame(byte[] data);

        /// <summary>
        /// 从累积缓冲区中尝试解析一个完整帧
        /// </summary>
        /// <param name="accumulated">累积的字节缓冲区</param>
        /// <param name="frameLength">输出：完整帧的字节长度</param>
        /// <param name="frameData">输出：解析出的帧数据（不含帧头帧尾）</param>
        /// <returns>true 表示已解析出完整帧</returns>
        bool TryParseFrame(byte[] accumulated, out int frameLength, out byte[] frameData);
    }

    /// <summary>
    /// 数据链路配置选项
    /// </summary>
    public class DataLinkOptions
    {
        /// <summary>
        /// 接收超时时间（毫秒）
        /// </summary>
        public int ReceiveTimeoutMs { get; set; } = 1000;

        /// <summary>
        /// 接收空闲超时时间（毫秒）—— 连续无数据到达视为帧结束
        /// </summary>
        public int ReceiveIdleTimeoutMs { get; set; } = 50;

        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetryCount { get; set; } = 0;

        /// <summary>
        /// 重试延迟时间（毫秒）
        /// </summary>
        public int RetryDelayMs { get; set; } = 300;

        /// <summary>
        /// 接收轮询间隔（毫秒）
        /// </summary>
        public int ReceivePollIntervalMs { get; set; } = 10;
    }
}
