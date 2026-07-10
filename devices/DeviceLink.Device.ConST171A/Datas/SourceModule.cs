namespace DeviceLink.Device.ConST171A;

/// <summary>
/// 气源模块标识
/// </summary>
public enum SourceModule
{
    /// <summary>
    /// 增压气源
    /// </summary>
    Pressure,

    /// <summary>
    /// 真空气源
    /// </summary>
    Vacuum
}

/// <summary>
/// SourceModule 扩展方法
/// </summary>
public static class SourceModuleExtensions
{
    /// <summary>
    /// 转换为 SCPI 指令参数字符串
    /// </summary>
    public static string ToScpiString(this SourceModule module)
    {
        return module switch
        {
            SourceModule.Pressure => "Pressure",
            SourceModule.Vacuum => "VACUUM",
            _ => throw new System.ArgumentOutOfRangeException(nameof(module), module, "未知的气源模块")
        };
    }
}
