using DeviceLink.DataLink;
using DeviceLink.DeviceBase;
using DeviceLink.Pipeline;
using DeviceLink.Protocol;
using DeviceLink.Transport;

namespace DeviceLink.Tests.Helpers
{
    /// <summary>
    /// 回环通信配置 —— 用于单元测试，不依赖任何物理硬件。
    /// 
    /// 使用方式：
    ///   var settings = new LoopbackSettings();
    ///   var dpsex = new DPSEX(settings);
    ///   
    ///   // 设置回环响应
    ///   settings.Transport.OnSend += data => { ... };
    /// </summary>
    public class LoopbackSettings : DeviceCommSettings
    {
        /// <summary>
        /// 回环传输实例，可用于设置 OnSend 回调和 EnqueueReceive
        /// </summary>
        public LoopbackTransport Transport { get; }

        /// <summary>
        /// 帧分隔符（仅在协议未登记且未实现 IFramedProtocol 时作为兜底）
        /// </summary>
        public byte[] Delimiter { get; set; } = new byte[] { 0 };

        /// <summary>
        /// 初始化回环通信配置
        /// </summary>
        public LoopbackSettings()
        {
            Transport = new LoopbackTransport();
        }

        /// <summary>
        /// 创建回环通信管道（完整 OSI 链路）
        /// </summary>
        /// <param name="codec">协议编解码器</param>
        /// <returns>通信管道</returns>
        protected override CommunicationPipeline CreatePipeline(IProtocolCodec codec)
        {
            // 帧策略交给框架按协议推导 —— 过去这里硬编码 DelimiterFrameStrategy({0})，
            // 会让 SCPI 用例靠"空闲超时返回半帧"侥幸通过，等于把分帧错误掩盖掉了。
            var builder = new CommunicationPipelineBuilder()
                .UseTransport(Transport)
                .UseProtocol(codec);

            if (!ProtocolBindings.IsRegistered(codec) && codec is not IFramedProtocol)
                builder.UseDataLink(new DelimiterFrameStrategy(Delimiter));

            return builder.Build();
        }
    }
}
