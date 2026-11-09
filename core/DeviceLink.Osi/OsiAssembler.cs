using System;
using System.Collections.Generic;

namespace DeviceLink.Osi
{
    /// <summary>
    /// 协议栈装配器 —— 依据 <see cref="OsiStackProfile"/> 校验一组组件的层归属。
    ///
    /// 校验规则（全部为强制项，没有隐性默认值）：
    /// 1. 组件提供的层必须落在画像覆盖范围内，否则视为越界（OutOfProfile）；
    /// 2. 画像要求实现的每一层必须至少有一个提供者，否则视为缺层（MissingLayer）；
    /// 3. 同一层出现多个提供者时给出冲突警告，提示是否真的需要分层叠加；
    /// 4. 组件声明的依赖层必须被其他组件满足，否则视为依赖未落实（UnmetDependency）；
    /// 5. 整个栈必须落到 L7 应用层，否则设备协议无处落地。
    /// </summary>
    public sealed class OsiAssembler
    {
        private readonly List<IOsiComponent> _components = new List<IOsiComponent>();

        /// <summary>已加入的组件。</summary>
        public IReadOnlyList<IOsiComponent> Components => _components;

        /// <summary>加入一个已实现层契约的组件。</summary>
        public OsiAssembler Add(IOsiComponent component)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            if (component.Provides.IsEmpty)
                throw new ArgumentException($"组件 {component.OsiName} 未声明提供的层", nameof(component));

            _components.Add(component);
            return this;
        }

        /// <summary>加入一个既有组件实例，层归属从登记表读取。</summary>
        public OsiAssembler Add(object instance)
        {
            return Add(OsiComponentDescriptor.FromRegistry(instance));
        }

        /// <summary>批量加入组件。</summary>
        public OsiAssembler AddRange(IEnumerable<IOsiComponent> components)
        {
            if (components == null) return this;
            foreach (var c in components) Add(c);
            return this;
        }

        /// <summary>清空已加入的组件。</summary>
        public OsiAssembler Clear()
        {
            _components.Clear();
            return this;
        }

        /// <summary>执行校验并返回报告。</summary>
        public OsiStackReport Assemble(OsiStackProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            var descriptors = new List<OsiComponentDescriptor>();
            foreach (var c in _components)
            {
                descriptors.Add(c as OsiComponentDescriptor
                    ?? OsiComponentDescriptor.Create(
                        c, c.OsiName, c.Provides, c.Requires, string.Empty));
            }

            var violations = new List<OsiViolation>();
            var provided = OsiLayerSet.Empty;
            foreach (var d in descriptors) provided = provided.Union(d.Provides);

            // 规则 1：越界声明（可扩展层不计入越界）
            foreach (var d in descriptors)
            {
                var extra = d.Provides.Except(profile.Allowed);
                if (!extra.IsEmpty)
                {
                    violations.Add(new OsiViolation(
                        OsiViolationKind.OutOfProfile,
                        OsiSeverity.Warning,
                        FirstLayer(extra),
                        $"提供了画像 {profile.Name} 之外的层 {extra}。" +
                        "若属于框架的能力增强（例如为无会话协议补充请求-响应管理），" +
                        "应把该层声明到画像的 extendable 中；" +
                        "若是把上层职责写进了下层组件，则必须拆分。",
                        d.OsiName));
                }
            }

            // 规则 2：缺层
            var missing = profile.Implemented.Except(provided);
            if (!missing.IsEmpty)
            {
                for (int i = 0; i < 7; i++)
                {
                    var layer = OsiLayerInfo.At(i);
                    if (!missing.Contains(layer)) continue;

                    violations.Add(new OsiViolation(
                        OsiViolationKind.MissingLayer,
                        OsiSeverity.Error,
                        layer,
                        $"画像要求实现 {OsiLayerInfo.DisplayName(layer)}，" +
                        "但没有任何组件提供该层。请补实现，或确认画像声明是否错误。"));
                }
            }

            // 规则 3：同层多提供者
            for (int i = 0; i < 7; i++)
            {
                var layer = OsiLayerInfo.At(i);
                if (!profile.Implemented.Contains(layer)) continue;
                if (profile.AllowShared.Contains(layer)) continue;

                var owners = new List<string>();
                foreach (var d in descriptors)
                {
                    if (d.Provides.Contains(layer)) owners.Add(d.OsiName);
                }

                if (owners.Count > 1)
                {
                    violations.Add(new OsiViolation(
                        OsiViolationKind.LayerConflict,
                        OsiSeverity.Warning,
                        layer,
                        $"{OsiLayerInfo.DisplayName(layer)} 由 {owners.Count} 个组件同时提供（" +
                        string.Join(", ", owners) + "），请确认是否为有意的分层叠加。"));
                }
            }

            // 规则 4：依赖未满足
            foreach (var d in descriptors)
            {
                if (d.Requires.IsEmpty) continue;

                var unmet = d.Requires.Except(provided);
                if (!unmet.IsEmpty)
                {
                    violations.Add(new OsiViolation(
                        OsiViolationKind.UnmetDependency,
                        OsiSeverity.Error,
                        FirstLayer(unmet),
                        $"依赖的层 {unmet} 没有组件提供。",
                        d.OsiName));
                }
            }

            // 规则 5：必须落到应用层
            if (!provided.Contains(OsiLayer.Application))
            {
                violations.Add(new OsiViolation(
                    OsiViolationKind.NoApplicationLayer,
                    OsiSeverity.Error,
                    OsiLayer.Application,
                    "协议栈未提供 L7 应用层，设备协议无法落地。"));
            }

            return new OsiStackReport(profile, descriptors, violations);
        }

        /// <summary>执行校验，失败时抛出 <see cref="OsiStackViolationException"/>。</summary>
        public OsiStackReport AssembleOrThrow(OsiStackProfile profile)
        {
            var report = Assemble(profile);
            if (!report.IsValid) throw new OsiStackViolationException(report);
            return report;
        }

        private static OsiLayer FirstLayer(OsiLayerSet set)
        {
            for (int i = 0; i < 7; i++)
            {
                var layer = OsiLayerInfo.At(i);
                if (set.Contains(layer)) return layer;
            }
            return OsiLayer.None;
        }
    }
}
