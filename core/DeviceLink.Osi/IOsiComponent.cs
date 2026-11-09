namespace DeviceLink.Osi
{
    /// <summary>
    /// OSI 层契约 —— 任何参与通信栈装配的组件都必须显式声明自己提供哪些层、
    /// 依赖哪些层。声明即承诺：提供了 L1 就不允许做帧定界，声明了 L2 就不允许做重试。
    ///
    /// 装配器（<see cref="OsiAssembler"/>）会依据此声明校验协议栈的一致性，
    /// 越界声明的组件会在装配阶段被拒绝，而不是在产线上偶发崩溃。
    /// </summary>
    public interface IOsiComponent
    {
        /// <summary>组件在层次报告中的显示名。</summary>
        string OsiName { get; }

        /// <summary>本组件提供的层（它实际完成的职责所在层）。</summary>
        OsiLayerSet Provides { get; }

        /// <summary>本组件依赖下层提供的层。为空表示直接对接物理介质。</summary>
        OsiLayerSet Requires { get; }
    }
}
