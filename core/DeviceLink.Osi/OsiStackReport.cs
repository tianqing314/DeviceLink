using System;
using System.Collections.Generic;
using System.Text;

namespace DeviceLink.Osi
{
    /// <summary>
    /// 协议栈层次校验报告 —— 输出"哪一层由谁提供、哪一层空缺、有没有越界"。
    /// 这是保证 OSI 层次清晰的可见化手段：装配即出报告，层次不需要靠文档口头约定。
    /// </summary>
    public sealed class OsiStackReport
    {
        private readonly List<OsiComponentDescriptor> _components;
        private readonly List<OsiViolation> _violations;

        internal OsiStackReport(
            OsiStackProfile profile,
            List<OsiComponentDescriptor> components,
            List<OsiViolation> violations)
        {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _components = components ?? new List<OsiComponentDescriptor>();
            _violations = violations ?? new List<OsiViolation>();
        }

        /// <summary>本次装配使用的协议栈画像。</summary>
        public OsiStackProfile Profile { get; }

        /// <summary>参与装配的组件。</summary>
        public IReadOnlyList<OsiComponentDescriptor> Components => _components;

        /// <summary>全部违规记录。</summary>
        public IReadOnlyList<OsiViolation> Violations => _violations;

        /// <summary>是否存在阻断性错误。</summary>
        public bool IsValid
        {
            get
            {
                foreach (var v in _violations)
                {
                    if (v.Severity == OsiSeverity.Error) return false;
                }
                return true;
            }
        }

        /// <summary>组件实际提供的层并集。</summary>
        public OsiLayerSet Provided
        {
            get
            {
                var acc = OsiLayerSet.Empty;
                foreach (var c in _components) acc = acc.Union(c.Provides);
                return acc;
            }
        }

        /// <summary>取得某层的全部提供者。</summary>
        public IReadOnlyList<OsiComponentDescriptor> ProvidersOf(OsiLayer layer)
        {
            var list = new List<OsiComponentDescriptor>();
            foreach (var c in _components)
            {
                if (c.Provides.Contains(layer)) list.Add(c);
            }
            return list;
        }

        /// <summary>生成多行文本报告，可直接写入日志。</summary>
        public string ToTextTable()
        {
            const int nameWidth = 16;
            var sb = new StringBuilder();

            sb.AppendLine("OSI 协议栈校验: " + Profile.Name);
            sb.AppendLine("  实现层 : " + Profile.Implemented);
            sb.AppendLine("  委托层 : " + (Profile.Delegated.IsEmpty ? "无" : Profile.Delegated.ToString()));
            sb.AppendLine("  内联层 : " + (Profile.Inlined.IsEmpty ? "无" : Profile.Inlined.ToString()));
            sb.AppendLine("  可扩展 : " + (Profile.Extendable.IsEmpty ? "无" : Profile.Extendable.ToString()));
            sb.AppendLine("  空缺层 : " + (Profile.Gaps.IsEmpty ? "无" : Profile.Gaps.ToString()));
            if (!string.IsNullOrEmpty(Profile.Notes))
                sb.AppendLine("  说明   : " + Profile.Notes);

            sb.AppendLine("  结果   : " + (IsValid ? "通过" : "失败") +
                          " (" + _components.Count + " 个组件, " +
                          Count(OsiSeverity.Error) + " 个错误, " +
                          Count(OsiSeverity.Warning) + " 个警告)");
            sb.AppendLine(new string('-', 62));

            for (int i = 6; i >= 0; i--)
            {
                var layer = OsiLayerInfo.At(i);
                var caption = OsiLayerInfo.ShortName(layer) + " " + OsiLayerInfo.DisplayName(layer);

                var owners = ProvidersOf(layer);
                string ownerText;
                if (owners.Count == 0)
                {
                    if (Profile.Delegated.Contains(layer))
                        ownerText = "(委托外部: OS/硬件/对端)";
                    else if (Profile.Inlined.Contains(layer))
                        ownerText = "(内联于协议编解码器，无独立组件)";
                    else
                        ownerText = "(空缺 - 协议本身不提供该层)";
                }
                else
                {
                    var names = new List<string>();
                    foreach (var o in owners) names.Add(o.OsiName);
                    ownerText = string.Join(" + ", names);
                }

                sb.Append("  ").Append(PadDisplay(caption, nameWidth)).Append(ownerText).AppendLine();
            }

            if (_violations.Count > 0)
            {
                sb.AppendLine(new string('-', 62));
                foreach (var v in _violations)
                    sb.AppendLine("  " + v);
            }

            return sb.ToString();
        }

        private int Count(OsiSeverity severity)
        {
            int n = 0;
            foreach (var v in _violations)
            {
                if (v.Severity == severity) n++;
            }
            return n;
        }

        private static int DisplayWidth(string text)
        {
            int w = 0;
            foreach (var ch in text) w += ch > 0x2E80 ? 2 : 1;
            return w;
        }

        private static string PadDisplay(string text, int width)
        {
            int pad = width - DisplayWidth(text);
            return pad > 0 ? text + new string(' ', pad) : text;
        }

        /// <inheritdoc/>
        public override string ToString() => ToTextTable();
    }
}
