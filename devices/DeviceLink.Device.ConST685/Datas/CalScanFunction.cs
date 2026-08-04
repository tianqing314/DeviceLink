namespace DeviceLink.Device.ConST685
{
    /// <summary>
    /// 校准扫描功能
    /// </summary>
    public enum CalScanFunction : int
    {
        /// <summary>
        /// 电压
        /// </summary>
        V = 0,
        /// <summary>
        /// 电流
        /// </summary>
        I = 1,
        /// <summary>
        /// 电阻
        /// </summary>
        R = 2,
        /// <summary>
        /// PRT
        /// </summary>
        PRT = 3,
        /// <summary>
        /// 热敏电阻
        /// </summary>
        RTC = 4,
        /// <summary>
        /// 冷端
        /// </summary>
        Cjc = 5,
        /// <summary>
        /// ad自校准
        /// </summary>
        ADA = 6,
        /// <summary>
        /// ad线性校准
        /// </summary>
        ADC = 7,
        /// <summary>
        /// 全部
        /// </summary>
        All = 99
    }
}
