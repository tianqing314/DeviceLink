using System;

namespace DeviceLink.Osi
{
    /// <summary>
    /// 协议栈画像 —— 描述"某一种协议栈按 OSI 归属应该由哪些层构成"。
    ///
    /// 它是层次约束的唯一来源：装配通信栈时，组件的层归属必须落在画像的
    /// <see cref="Implemented"/> 集合内，且 <see cref="Implemented"/> 的每一层都必须被提供。
    ///
    /// 两个集合的区分很重要：
    /// · <see cref="Implemented"/>：必须由框架内组件实现的层（要校验）；
    /// · <see cref="Delegated"/>：由操作系统、硬件或对端设备天然提供的层（不要求框架实现）。
    ///
    /// 例如 MQTT over TCP：
    ///   Implemented = L1-L4（TcpTransport 提供）+ L5（MQTT 会话）+ L7（业务协议）
    ///   Delegated   = 空（TCP/IP 由框架的 TcpTransport 承担）
    ///   Gaps        = L6（MQTT 载荷为裸字节，不做表示层转换）
    /// </summary>
    public sealed class OsiStackProfile
    {
        /// <summary>创建协议栈画像。</summary>
        /// <param name="name">画像名称，如 "Modbus RTU over RS-485"</param>
        /// <param name="implemented">必须由框架组件实现的层表达式，如 "L1,L2,L7"</param>
        /// <param name="delegated">由外部（OS/硬件/对端）提供的层表达式，可为 null</param>
        /// <param name="notes">层次说明，用于文档与报告</param>
        /// <param name="inlined">
        /// 被折叠进相邻层的层表达式。这些层没有独立组件，但职责确实存在，
        /// 例如 Modbus 的字节序解释（L6）被折叠进协议编解码器（L7）。
        /// 内联层既不算空缺，也不要求单独提供组件。
        /// </param>
        /// <param name="allowShared">
        /// 允许由多个组件叠加实现的层。默认只允许每层一个提供者，用于发现"两个组件抢同一层职责"。
        /// 例如 L2 通常由"帧策略（定帧）"和"收发循环（搬帧）"两个组件共同承担，可显式放开。
        /// </param>
        /// <param name="extendable">
        /// 框架允许<b>额外补位</b>的层。这些层不属于协议本身（因此仍计入 <see cref="Gaps"/>），
        /// 但框架出于工程需要可以补一个实现，且不视为越界。
        /// 典型例子：RS-232 没有会话层，但框架用 <c>DirectSession</c> 补了请求-响应管理
        /// 与重连能力——这是能力增强，不是"把上层职责写进下层"。
        /// </param>
        public OsiStackProfile(
            string name, string implemented, string? delegated = null, string? notes = null,
            string? inlined = null, string? allowShared = null, string? extendable = null)
        {
            if (name == null || name.Trim().Length == 0)
                throw new ArgumentException("协议栈画像名称不能为空", nameof(name));
            if (implemented == null || implemented.Trim().Length == 0)
                throw new ArgumentException("协议栈画像必须声明至少一个实现层", nameof(implemented));

            Name = name;
            Implemented = OsiLayerSet.Parse(implemented);
            Delegated = (delegated == null || delegated.Trim().Length == 0)
                ? OsiLayerSet.Empty
                : OsiLayerSet.Parse(delegated);
            Inlined = (inlined == null || inlined.Trim().Length == 0)
                ? OsiLayerSet.Empty
                : OsiLayerSet.Parse(inlined);
            AllowShared = (allowShared == null || allowShared.Trim().Length == 0)
                ? OsiLayerSet.Empty
                : OsiLayerSet.Parse(allowShared);
            Extendable = (extendable == null || extendable.Trim().Length == 0)
                ? OsiLayerSet.Empty
                : OsiLayerSet.Parse(extendable);
            Notes = notes ?? string.Empty;

            if (Implemented.Contains(OsiLayer.Application) == false)
            {
                throw new ArgumentException(
                    "协议栈画像必须包含 L7 应用层，否则设备协议无处落地: " + name, nameof(implemented));
            }
        }

        /// <summary>画像名称。</summary>
        public string Name { get; }

        /// <summary>必须由框架组件实现的层。</summary>
        public OsiLayerSet Implemented { get; }

        /// <summary>由操作系统、硬件或对端设备提供的层。</summary>
        public OsiLayerSet Delegated { get; }

        /// <summary>被折叠进相邻层的层（无独立组件，但职责存在）。</summary>
        public OsiLayerSet Inlined { get; }

        /// <summary>允许由多个组件叠加实现的层。</summary>
        public OsiLayerSet AllowShared { get; }

        /// <summary>
        /// 框架允许额外补位的层。不属于协议本身（仍计入 <see cref="Gaps"/>），
        /// 但框架提供实现时不算越界。
        /// </summary>
        public OsiLayerSet Extendable { get; }

        /// <summary>层次说明。</summary>
        public string Notes { get; }

        /// <summary>该协议栈在链路上覆盖的全部层。</summary>
        public OsiLayerSet Covered => Implemented.Union(Delegated).Union(Inlined);

        /// <summary>装配时允许出现的层（覆盖层 + 可扩展层）。越出此集合即为越界。</summary>
        public OsiLayerSet Allowed => Covered.Union(Extendable);

        /// <summary>协议栈天然空缺的层（协议本身不提供的层）。</summary>
        public OsiLayerSet Gaps => OsiLayerSet.All.Except(Covered);

        /// <summary>生成一句话描述，用于日志与文档。</summary>
        public string Describe()
        {
            return string.Format(
                "{0} | 实现: {1} | 委托: {2} | 内联: {3} | 可扩展: {4} | 空缺: {5}",
                Name,
                Implemented.ToString(),
                Delegated.IsEmpty ? "无" : Delegated.ToString(),
                Inlined.IsEmpty ? "无" : Inlined.ToString(),
                Extendable.IsEmpty ? "无" : Extendable.ToString(),
                Gaps.IsEmpty ? "无" : Gaps.ToString());
        }

        /// <inheritdoc/>
        public override string ToString() => Describe();
    }
}
