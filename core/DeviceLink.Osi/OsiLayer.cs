using System;

namespace DeviceLink.Osi
{
    /// <summary>
    /// OSI 七层模型中的层。此枚举是框架唯一的层次权威定义，
    /// 任何组件的层归属都必须以此为准，不允许在组件内部自行解释"层"。
    /// </summary>
    [Flags]
    public enum OsiLayer
    {
        /// <summary>无层。</summary>
        None = 0,

        /// <summary>L1 物理层 —— 电气信号、比特流、连接建立。</summary>
        Physical = 1 << 0,

        /// <summary>L2 数据链路层 —— 帧定界、寻址、校验（CRC）、差错检测。</summary>
        DataLink = 1 << 1,

        /// <summary>L3 网络层 —— 路由、逻辑寻址。</summary>
        Network = 1 << 2,

        /// <summary>L4 传输层 —— 端到端可靠性、重传、流控、分片重组。</summary>
        Transport = 1 << 3,

        /// <summary>L5 会话层 —— 连接生命周期、请求-响应配对、会话状态、重试策略。</summary>
        Session = 1 << 4,

        /// <summary>L6 表示层 —— 字节序、编码、类型解释、序列化格式。</summary>
        Presentation = 1 << 5,

        /// <summary>L7 应用层 —— 业务命令、设备语义、错误码含义。</summary>
        Application = 1 << 6
    }
}
