namespace DeviceLink.Protocol
{
    /// <summary>
    /// 自带明确帧终止符的协议。
    ///
    /// 存在意义：协议层知道自己的帧以什么字节结束（ConST 用 \0、SCPI 用 \r\n 或 \n），
    /// 数据链路层需要这个字节来做定界。过去由设备基类硬编码分隔符，
    /// 结果 SCPI 设备被套上用 \0 定帧的策略，只能靠"接收空闲超时就返回半帧"侥幸工作。
    ///
    /// 本接口只暴露字节，不依赖 <c>DeviceLink.DataLink</c>，不破坏层次方向：
    /// 协议声明"我的帧长什么样"，链路层据此选用定界算法。
    /// </summary>
    public interface IFramedProtocol
    {
        /// <summary>帧终止符字节（如 <c>{0x00}</c> 或 <c>{0x0D,0x0A}</c>）。</summary>
        byte[] FrameTerminator { get; }
    }
}
