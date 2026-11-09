using System;
using System.Collections.Generic;
using System.Text;

namespace DeviceLink.Osi
{
    /// <summary>
    /// 协议栈层次校验失败异常 —— 装配阶段发现越界、缺层或依赖未满足时抛出。
    /// 抛出时机是"装配"，不是"运行"，确保层次问题在启动时就暴露。
    /// </summary>
    public sealed class OsiStackViolationException : Exception
    {
        /// <summary>创建异常。</summary>
        public OsiStackViolationException(OsiStackReport report)
            : base(BuildMessage(report))
        {
            Report = report ?? throw new ArgumentNullException(nameof(report));
        }

        /// <summary>触发异常的层次报告。</summary>
        public OsiStackReport Report { get; }

        private static string BuildMessage(OsiStackReport report)
        {
            if (report == null) return "OSI 协议栈校验失败";

            var sb = new StringBuilder();
            sb.AppendLine("OSI 协议栈校验失败: " + report.Profile.Name);

            var errors = new List<OsiViolation>();
            foreach (var v in report.Violations)
            {
                if (v.Severity == OsiSeverity.Error) errors.Add(v);
            }

            foreach (var v in errors) sb.AppendLine("  · " + v);
            sb.AppendLine();
            sb.Append(report.ToTextTable());
            return sb.ToString();
        }
    }
}
