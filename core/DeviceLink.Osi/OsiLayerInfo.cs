using System;
using System.Collections.Generic;

namespace DeviceLink.Osi
{
    /// <summary>
    /// 层的元信息：层号、中英文名、职责摘要。
    /// 用于生成人类可读的层次报告，避免各处重复硬编码中文层名。
    /// </summary>
    public static class OsiLayerInfo
    {
        /// <summary>全部七层的并集。</summary>
        public const OsiLayer AllLayers =
            OsiLayer.Physical | OsiLayer.DataLink | OsiLayer.Network | OsiLayer.Transport |
            OsiLayer.Session | OsiLayer.Presentation | OsiLayer.Application;

        /// <summary>按 L1 → L7 顺序排列的所有层。</summary>
        public static readonly IReadOnlyList<OsiLayer> Ordered = new[]
        {
            OsiLayer.Physical,
            OsiLayer.DataLink,
            OsiLayer.Network,
            OsiLayer.Transport,
            OsiLayer.Session,
            OsiLayer.Presentation,
            OsiLayer.Application
        };

        /// <summary>取得单层的层号（1~7）。</summary>
        public static int NumberOf(OsiLayer layer)
        {
            switch (layer)
            {
                case OsiLayer.Physical: return 1;
                case OsiLayer.DataLink: return 2;
                case OsiLayer.Network: return 3;
                case OsiLayer.Transport: return 4;
                case OsiLayer.Session: return 5;
                case OsiLayer.Presentation: return 6;
                case OsiLayer.Application: return 7;
                default: return 0;
            }
        }

        /// <summary>按序号（0 基）取得单层。</summary>
        public static OsiLayer At(int index)
        {
            if (index < 0 || index > 6)
                throw new ArgumentOutOfRangeException(nameof(index), index, "OSI 层序号必须在 0~6 之间");
            return Ordered[index];
        }

        /// <summary>层的短名，如 "L3"。</summary>
        public static string ShortName(OsiLayer layer)
        {
            int n = NumberOf(layer);
            return n == 0 ? "-" : "L" + n;
        }

        /// <summary>层的中文名，如 "网络层"。</summary>
        public static string DisplayName(OsiLayer layer)
        {
            switch (layer)
            {
                case OsiLayer.Physical: return "物理层";
                case OsiLayer.DataLink: return "数据链路层";
                case OsiLayer.Network: return "网络层";
                case OsiLayer.Transport: return "传输层";
                case OsiLayer.Session: return "会话层";
                case OsiLayer.Presentation: return "表示层";
                case OsiLayer.Application: return "应用层";
                default: return "(无层)";
            }
        }

        /// <summary>层的职责摘要，用于文档与报告。</summary>
        public static string Responsibility(OsiLayer layer)
        {
            switch (layer)
            {
                case OsiLayer.Physical: return "比特流、电气特性、连接建立";
                case OsiLayer.DataLink: return "帧定界、寻址、CRC 校验、差错检测";
                case OsiLayer.Network: return "逻辑寻址、路由";
                case OsiLayer.Transport: return "端到端可靠传输、重传、流控、分片";
                case OsiLayer.Session: return "连接生命周期、请求-响应配对、重试策略";
                case OsiLayer.Presentation: return "字节序、编码、类型解释、序列化";
                case OsiLayer.Application: return "业务命令、设备语义、错误码含义";
                default: return string.Empty;
            }
        }
    }
}
