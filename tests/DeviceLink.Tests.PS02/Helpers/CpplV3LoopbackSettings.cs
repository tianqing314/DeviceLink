using DeviceLink.DataLink;
using DeviceLink.DeviceBase;
using DeviceLink.Pipeline;
using DeviceLink.Protocol;
using DeviceLink.Transport;

namespace DeviceLink.Tests.PS02.Helpers
{
    /// <summary>
    /// CPPI V3 回环通信配置 —— 用于 PS02 单元测试。
    ///
    /// 使用 CpplV3FrameStrategy 替代 DelimiterFrameStrategy，
    /// 模拟 PC → CPPI V3 → 转换板 → Modbus RTU → PS02 的完整通信链路。
    ///
    /// 使用方式：
    ///   var settings = new CpplV3LoopbackSettings();
    ///   var ps02 = new PS02(settings);
    ///   settings.Transport.OnSend += data => { ... };
    /// </summary>
    public class CpplV3LoopbackSettings : DeviceCommSettings
    {
        /// <summary>
        /// 回环传输实例
        /// </summary>
        public LoopbackTransport Transport { get; }

        /// <summary>
        /// CPPI V3 帧策略实例（可在外部访问以验证帧数据）
        /// </summary>
        public CpplV3FrameStrategy FrameStrategy { get; }

        /// <summary>
        /// 初始化 CPPI V3 回环通信配置
        /// </summary>
        public CpplV3LoopbackSettings()
        {
            Transport = new LoopbackTransport();
            FrameStrategy = new CpplV3FrameStrategy();
        }

        /// <summary>
        /// 创建 CPPI V3 回环通信管道
        /// </summary>
        /// <param name="codec">协议编解码器</param>
        /// <returns>通信管道</returns>
        protected override CommunicationPipeline CreatePipeline(IProtocolCodec codec)
        {
            return new CommunicationPipelineBuilder()
                .UseTransport(Transport)
                .UseDataLink(FrameStrategy)
                .UseProtocol(codec)
                .Build();
        }
    }
}
