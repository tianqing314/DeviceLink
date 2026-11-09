using System;

namespace DeviceLink.Osi
{
    /// <summary>
    /// 组件描述符 —— 把任意既有组件实例包装成 <see cref="IOsiComponent"/>。
    /// 有两种来源：显式声明，或查 <see cref="OsiLayerRegistry"/> 的登记表。
    /// </summary>
    public sealed class OsiComponentDescriptor : IOsiComponent
    {
        private OsiComponentDescriptor(
            object instance, string name, OsiLayerSet provides, OsiLayerSet requires, string reason)
        {
            Instance = instance;
            OsiName = name;
            Provides = provides;
            Requires = requires;
            Reason = reason;
        }

        /// <summary>被包装的组件实例。</summary>
        public object Instance { get; }

        /// <inheritdoc/>
        public string OsiName { get; }

        /// <inheritdoc/>
        public OsiLayerSet Provides { get; }

        /// <inheritdoc/>
        public OsiLayerSet Requires { get; }

        /// <summary>层归属依据。</summary>
        public string Reason { get; }

        /// <summary>组件运行时类型。</summary>
        public Type? ComponentType => Instance?.GetType();

        /// <summary>按登记表创建描述符，未登记则抛出异常。</summary>
        public static OsiComponentDescriptor FromRegistry(object instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            var decl = OsiLayerRegistry.Get(instance.GetType());
            return new OsiComponentDescriptor(
                instance, decl.Name, decl.Provides, decl.Requires, decl.Reason);
        }

        /// <summary>按显式声明创建描述符。</summary>
        public static OsiComponentDescriptor Create(
            object instance, string name, OsiLayerSet provides, OsiLayerSet requires = default, string reason = "")
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (provides.IsEmpty)
                throw new ArgumentException("组件必须至少提供一个 OSI 层", nameof(provides));

            return new OsiComponentDescriptor(
                instance, name ?? instance.GetType().Name, provides, requires, reason ?? string.Empty);
        }

        /// <inheritdoc/>
        public override string ToString() => OsiName + " → " + Provides;
    }
}
