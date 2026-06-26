using System;

namespace DeviceLink.Device.ConST171A
{
    /// <summary>
    /// 气源 / 泵 模块类型枚举
    /// 
    /// 压力控制、校准指令使用：Pressure（正压）、Vacuum（真空）
    /// 诊断指令（风扇、泵）额外支持：Pre（前级泵）
    /// </summary>
    public enum SourceModule
    {
        /// <summary>正压气源 / 增压泵</summary>
        Pressure,

        /// <summary>真空气源 / 真空气泵</summary>
        Vacuum,

        /// <summary>前级泵（仅诊断指令使用）</summary>
        Pre
    }

    /// <summary>
    /// SourceModule 扩展方法
    /// </summary>
    public static class SourceModuleExtensions
    {
        /// <summary>
        /// 将枚举值转换为 SCPI 指令中使用的字符串
        /// </summary>
        public static string ToScpiString(this SourceModule module) => module switch
        {
            SourceModule.Pressure => "Pressure",
            SourceModule.Vacuum => "Vacuum",
            SourceModule.Pre => "Pre",
            _ => throw new ArgumentOutOfRangeException(nameof(module), module, null)
        };
    }
}
