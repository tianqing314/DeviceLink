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
        /// 帧分隔符
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
            return new CommunicationPipelineBuilder()
                .UseTransport(Transport)
                .UseDataLink(new DelimiterFrameStrategy(Delimiter))
                .UseProtocol(codec)
                .Build();
        }
    }
}
