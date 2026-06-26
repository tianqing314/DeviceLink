namespace DeviceLink.Device.ConST171A
{
    /// <summary>
    /// 校准记录 —— CALibration:DATA:VALue? 返回值
    /// 单点格式：标准值,原始值,年,月,日
    /// 多点格式：标准值A:标准值B,原始值A:原始值B,年,月,日
    /// </summary>
    public class CalibrationRecord
    {
        /// <summary>校准标准值数组（kPa）</summary>
        public double[] StandardValues { get; set; } = System.Array.Empty<double>();

        /// <summary>设备原始值数组（kPa）</summary>
        public double[] RawValues { get; set; } = System.Array.Empty<double>();

        /// <summary>校准年份</summary>
        public int Year { get; set; }

        /// <summary>校准月份</summary>
        public int Month { get; set; }

        /// <summary>校准日期</summary>
        public int Day { get; set; }

        /// <summary>是否多点校准</summary>
        public bool IsMultiPoint => StandardValues.Length >= 2;

        /// <summary>是否有效</summary>
        public bool IsValid => StandardValues.Length > 0 && RawValues.Length > 0;

        /// <inheritdoc/>
        public override string ToString() =>
            IsMultiPoint
                ? $"多点校准: 标准={string.Join(":", StandardValues)}, 原始={string.Join(":", RawValues)}, 日期={Year}-{Month}-{Day}"
                : $"单点校准: 标准={StandardValues[0]}, 原始={RawValues[0]}, 日期={Year}-{Month}-{Day}";
    }
}
