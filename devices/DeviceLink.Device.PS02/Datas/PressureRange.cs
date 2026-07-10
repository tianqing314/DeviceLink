using System;

namespace DeviceLink.Device.PS02;

/// <summary>
/// PS02 量程信息（含迁移量程）
/// </summary>
public class PressureRange
{
    /// <summary>
    /// 量程下限（单位：kPa）
    /// </summary>
    public double Lower { get; set; } = double.NaN;

    /// <summary>
    /// 量程上限（单位：kPa）
    /// </summary>
    public double Upper { get; set; } = double.NaN;

    /// <summary>
    /// 数据是否有效
    /// </summary>
    public bool IsValid { get { return !double.IsNaN(Lower) && !double.IsNaN(Upper); } }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Lower} ~ {Upper} kPa";
    }
}
