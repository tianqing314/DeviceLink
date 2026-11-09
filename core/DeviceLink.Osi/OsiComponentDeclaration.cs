using System;

namespace DeviceLink.Osi
{
    /// <summary>
    /// 组件层归属声明 —— 把一个组件类型绑定到具体的 OSI 层，
    /// 并记录"为什么它在这一层"的依据，便于评审与后续维护。
    /// </summary>
    public sealed class OsiComponentDeclaration
    {
        /// <summary>创建声明。</summary>
        /// <param name="componentType">组件类型</param>
        /// <param name="name">显示名</param>
        /// <param name="provides">提供的层</param>
        /// <param name="requires">依赖的层</param>
        /// <param name="reason">层归属依据</param>
        public OsiComponentDeclaration(
            Type componentType, string name, OsiLayerSet provides, OsiLayerSet requires, string reason = "")
        {
            ComponentType = componentType ?? throw new ArgumentNullException(nameof(componentType));
            Name = name ?? componentType.Name;
            Provides = provides;
            Requires = requires;
            Reason = reason ?? string.Empty;
        }

        /// <summary>组件类型。</summary>
        public Type ComponentType { get; }

        /// <summary>显示名。</summary>
        public string Name { get; }

        /// <summary>提供的层。</summary>
        public OsiLayerSet Provides { get; }

        /// <summary>依赖的层。</summary>
        public OsiLayerSet Requires { get; }

        /// <summary>层归属依据（写入文档与报告）。</summary>
        public string Reason { get; }

        /// <inheritdoc/>
        public override string ToString() => Name + " → " + Provides;
    }
}
