namespace DeviceLink.Device.ConST685
{
    /// <summary>
    /// 校准扫描量程（对应 Xmas11 CalScanRange 枚举）
    /// </summary>
    public enum CalScanRange : int
    {
        /// <summary>
        /// 100mV
        /// </summary>
        mV_100 = 0,
        /// <summary>
        /// 1V
        /// </summary>
        V_1 = 1,
        /// <summary>
        /// 10V
        /// </summary>
        V_10 = 2,
        /// <summary>
        /// 50V
        /// </summary>
        V_50 = 3,

        /// <summary>
        /// 100uA
        /// </summary>
        uA_100 = 0,
        /// <summary>
        /// 1mA
        /// </summary>
        mA_1 = 1,
        /// <summary>
        /// 10mA
        /// </summary>
        mA_10 = 2,
        /// <summary>
        /// 100mA
        /// </summary>
        mA_100 = 3,

        /// <summary>
        /// 100Ω
        /// </summary>
        R_100 = 0,
        /// <summary>
        /// 1kΩ
        /// </summary>
        kR_1 = 1,
        /// <summary>
        /// 10kΩ
        /// </summary>
        kR_10 = 2,
        /// <summary>
        /// 100kΩ
        /// </summary>
        kR_100 = 3,
        /// <summary>
        /// 1MΩ
        /// </summary>
        MR_1 = 4,
        /// <summary>
        /// 10MΩ
        /// </summary>
        MR_10 = 5,
        /// <summary>
        /// 100MΩ
        /// </summary>
        MR_100 = 6,

        /// <summary>
        /// PRT100Ω
        /// </summary>
        PRT_100 = 0,
        /// <summary>
        /// PRT400Ω
        /// </summary>
        PRT_400 = 1,
        /// <summary>
        /// PRT4kΩ
        /// </summary>
        PRT_4k = 2,

        /// <summary>
        /// RTC-10kΩ
        /// </summary>
        Thermistor_10k = 0,
        /// <summary>
        /// RTC-100kΩ
        /// </summary>
        Thermistor_100k = 1,
        /// <summary>
        /// RTC-1MΩ
        /// </summary>
        Thermistor_1M = 2
    }
}
