namespace DeviceLink.Device.ConST685
{
    /// <summary>
    /// 校准结果 —— CALibration:ELECtricity:SCAN? 返回值
    /// 格式：错误码,模式,功能,量程,完成状态,原始值
    /// </summary>
    public class CalibrationResult
    {
        /// <summary>
        /// 错误码（0=无错误）
        /// </summary>
        public string ErrorCode { get; set; } = string.Empty;

        /// <summary>
        /// 校准模式（0=APF, 10=ADC, 11=ADCLiner, 12=ADC400ΩRatio, 13=ADC200ΩRatio, 14=ADC100ΩRatio, 15=Noise）
        /// </summary>
        public int CalibrationMode { get; set; } = -1;

        /// <summary>
        /// 校准功能（0=DCV, 1=DCI, 2=Resistance, 3=PRT, 4=Thermistor）
        /// </summary>
        public int CalibrationFunction { get; set; } = -1;

        /// <summary>
        /// 校准量程索引
        /// </summary>
        public int CalibrationRange { get; set; } = -1;

        /// <summary>
        /// 校准完成状态（true=成功）
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 原始校准值
        /// </summary>
        public double OriginalValue { get; set; } = double.NaN;

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(ErrorCode);

        /// <inheritdoc/>
        public override string ToString() =>
            $"Error={ErrorCode},Mode={CalibrationMode},Func={CalibrationFunction},Range={CalibrationRange},Success={IsSuccess},Value={OriginalValue}";
    }
}
