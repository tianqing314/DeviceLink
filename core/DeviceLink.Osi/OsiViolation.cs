using System;

namespace DeviceLink.Osi
{
    /// <summary>层违规类型。</summary>
    public enum OsiViolationKind
    {
        /// <summary>组件未登记层归属。</summary>
        Unregistered,

        /// <summary>画像要求的层没有组件提供。</summary>
        MissingLayer,

        /// <summary>同一层被多个组件提供。</summary>
        LayerConflict,

        /// <summary>组件提供了画像范围之外的层（越界）。</summary>
        OutOfProfile,

        /// <summary>组件依赖的层没有组件提供。</summary>
        UnmetDependency,

        /// <summary>协议栈没有落到 L7 应用层。</summary>
        NoApplicationLayer
    }

    /// <summary>违规严重程度。</summary>
    public enum OsiSeverity
    {
        /// <summary>警告：可疑但不阻断装配。</summary>
        Warning,

        /// <summary>错误：阻断装配。</summary>
        Error
    }

    /// <summary>一条层次校验违规记录。</summary>
    public sealed class OsiViolation
    {
        /// <summary>创建违规记录。</summary>
        public OsiViolation(OsiViolationKind kind, OsiSeverity severity, OsiLayer layer, string message, string? subject = null)
        {
            Kind = kind;
            Severity = severity;
            Layer = layer;
            Message = message ?? string.Empty;
            Subject = subject ?? string.Empty;
        }

        /// <summary>违规类型。</summary>
        public OsiViolationKind Kind { get; }

        /// <summary>严重程度。</summary>
        public OsiSeverity Severity { get; }

        /// <summary>涉及的层（可能为 None）。</summary>
        public OsiLayer Layer { get; }

        /// <summary>违规说明。</summary>
        public string Message { get; }

        /// <summary>违规主体（组件名）。</summary>
        public string Subject { get; }

        /// <inheritdoc/>
        public override string ToString()
        {
            var layer = Layer == OsiLayer.None ? "" : OsiLayerInfo.ShortName(Layer) + " ";
            var subject = string.IsNullOrEmpty(Subject) ? "" : "[" + Subject + "] ";
            return string.Format("{0} {1}{2}{3}", Severity, layer, subject, Message);
        }
    }
}
