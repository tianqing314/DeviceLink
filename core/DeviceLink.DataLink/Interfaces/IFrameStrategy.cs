namespace DeviceLink.DataLink
{
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
}