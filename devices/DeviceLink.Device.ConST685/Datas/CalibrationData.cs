using System;

namespace DeviceLink.Device.ConST685
{
    /// <summary>
    /// 校准数据信息 —— CALibration:ELECtricity:DATA? 返回值
    /// </summary>
    public class CalibrationData
    {
        /// <summary>
        /// 单位 ID
        /// </summary>
        public int UnitId { get; set; }

        /// <summary>
        /// 标准值列表（逗号分隔）
        /// </summary>
        public string StandardValues { get; set; } = string.Empty;

        /// <summary>
        /// 校准点列表（逗号分隔）
        /// </summary>
        public string CalibrationPoints { get; set; } = string.Empty;

        /// <summary>
        /// 年
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// 月
        /// </summary>
        public int Month { get; set; }

        /// <summary>
        /// 日
        /// </summary>
        public int Day { get; set; }

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(StandardValues);

        /// <inheritdoc/>
        public override string ToString() =>
            $"UnitId={UnitId},Points={CalibrationPoints},Values={StandardValues},Date={Year}-{Month:D2}-{Day:D2}";
    }
}
