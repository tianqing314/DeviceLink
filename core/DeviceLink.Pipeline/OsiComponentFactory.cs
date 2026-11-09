using System;
using DeviceLink.DataLink;
using DeviceLink.Osi;
using DeviceLink.Protocol;
using DeviceLink.Session;
using DeviceLink.Transport;

namespace DeviceLink.Pipeline
{
    /// <summary>
    /// 把既有组件实例包装成 <see cref="IOsiComponent"/> 的唯一入口。
    ///
    /// 规则：
    /// · 已登记的组件（<see cref="OsiLayerRegistry"/>）以登记信息为准；
    /// · 未登记的组件按接口语义兜底：<c>IFrameStrategy</c> 是 L2 契约、
    ///   <c>ISession</c> 是 L5 契约、<c>IProtocolCodec</c> 是 L6+L7 契约；
    /// · <c>IPhysicalTransport</c> 层高不一致（串口只有 L1、TCP 覆盖 L1-L4），
    ///   <b>必须显式登记</b>，不做兜底 —— 猜错的代价比报错更大。
    /// </summary>
    internal static class OsiComponentFactory
    {
        /// <summary>按运行时类型分派包装。</summary>
        public static IOsiComponent Describe(object instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            return instance switch
            {
                IFrameStrategy strategy => DescribeFrameStrategy(strategy),
                ISession session => DescribeSession(session),
                IProtocolCodec protocol => DescribeProtocol(protocol),
                IPhysicalTransport transport => OsiComponentDescriptor.FromRegistry(transport),
                _ => throw new ArgumentException(
                    $"无法确定 {instance.GetType().Name} 的 OSI 层归属，请先在 OsiComponentRegistrations 中登记。",
                    nameof(instance))
            };
        }

        private static IOsiComponent DescribeFrameStrategy(IFrameStrategy strategy)
        {
            if (OsiLayerRegistry.TryGet(strategy.GetType(), out var decl) && decl != null)
                return OsiComponentDescriptor.FromRegistry(strategy);

            return OsiComponentDescriptor.Create(
                strategy, strategy.Name, OsiLayerSet.Of(OsiLayer.DataLink),
                reason: "自定义帧策略：IFrameStrategy 即 L2 契约，按 L2 登记。");
        }

        private static IOsiComponent DescribeSession(ISession session)
        {
            if (OsiLayerRegistry.TryGet(session.GetType(), out var decl) && decl != null)
                return OsiComponentDescriptor.FromRegistry(session);

            return OsiComponentDescriptor.Create(
                session, session.Name, OsiLayerSet.Of(OsiLayer.Session),
                reason: "自定义会话：ISession 即 L5 契约，按 L5 登记。");
        }

        private static IOsiComponent DescribeProtocol(IProtocolCodec protocol)
        {
            if (OsiLayerRegistry.TryGet(protocol.GetType(), out var decl) && decl != null)
                return OsiComponentDescriptor.FromRegistry(protocol);

            return OsiComponentDescriptor.Create(
                protocol, protocol.ProtocolName,
                OsiLayerSet.Of(OsiLayer.Presentation, OsiLayer.Application),
                reason: "自定义编解码器：IProtocolCodec 即 L6+L7 契约，按 L6+L7 登记。");
        }
    }
}
