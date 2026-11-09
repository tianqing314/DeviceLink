using System;
using System.Collections.Generic;

namespace DeviceLink.Osi
{
    /// <summary>
    /// 组件层归属登记表 —— 为零改造接入既有组件而设计。
    ///
    /// 既有组件（Transport / DataLink / Session / Protocol）不需要修改类定义，
    /// 只需在启动时登记一次层归属，装配器即可据此校验。
    ///
    /// 设计要点：查询未登记的组件类型会抛异常，而不是静默返回"未知层"。
    /// 层次清晰的前提是"必须声明"，不能有默认值蒙混过关。
    /// </summary>
    public static class OsiLayerRegistry
    {
        private static readonly object Gate = new object();
        private static readonly Dictionary<Type, OsiComponentDeclaration> Map =
            new Dictionary<Type, OsiComponentDeclaration>();

        /// <summary>登记一个组件的层归属。</summary>
        public static void Register(
            Type componentType, string name, OsiLayerSet provides, OsiLayerSet requires = default, string reason = "")
        {
            if (componentType == null) throw new ArgumentNullException(nameof(componentType));
            if (provides.IsEmpty)
                throw new ArgumentException($"组件 {componentType.Name} 必须至少提供一个 OSI 层", nameof(provides));

            var decl = new OsiComponentDeclaration(componentType, name, provides, requires, reason);
            lock (Gate)
            {
                Map[componentType] = decl;
            }
        }

        /// <summary>以层表达式字符串登记一个组件的层归属。</summary>
        public static void Register<T>(string name, string provides, string? requires = null, string reason = "")
        {
            if (provides == null || provides.Trim().Length == 0)
                throw new ArgumentException("提供的层表达式不能为空", nameof(provides));

            var req = (requires == null || requires.Trim().Length == 0)
                ? OsiLayerSet.Empty
                : OsiLayerSet.Parse(requires);

            Register(typeof(T), name, OsiLayerSet.Parse(provides), req, reason);
        }

        /// <summary>尝试取得组件的层归属声明。</summary>
        public static bool TryGet(Type componentType, out OsiComponentDeclaration? declaration)
        {
            if (componentType == null)
            {
                declaration = null;
                return false;
            }
            lock (Gate)
            {
                return Map.TryGetValue(componentType, out declaration);
            }
        }

        /// <summary>取得组件的层归属声明，未登记时抛出异常。</summary>
        public static OsiComponentDeclaration Get(Type componentType)
        {
            if (TryGet(componentType, out var decl) && decl != null) return decl;

            throw new InvalidOperationException(
                $"组件 {componentType?.FullName} 未登记 OSI 层归属。" +
                "请在框架初始化时调用 OsiLayerRegistry.Register 声明它提供哪些层，" +
                "否则无法保证通信栈的层次一致性。");
        }

        /// <summary>当前已登记的全部声明。</summary>
        public static IReadOnlyDictionary<Type, OsiComponentDeclaration> Snapshot()
        {
            lock (Gate)
            {
                return new Dictionary<Type, OsiComponentDeclaration>(Map);
            }
        }

        /// <summary>清空登记表（仅用于单元测试）。</summary>
        public static void Clear()
        {
            lock (Gate)
            {
                Map.Clear();
            }
        }
    }
}
