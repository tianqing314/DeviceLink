namespace DeviceLink.Device.ConST685
{
    /// <summary>
    /// 系统版本信息 —— SYSTem:VERSion? 返回值
    /// </summary>
    public class SystemVersionInfo
    {
        /// <summary>
        /// SCPI 版本号
        /// </summary>
        public string ScpiVersion { get; set; } = string.Empty;

        /// <summary>
        /// 主程序应用软件版本
        /// </summary>
        public string ApplicationVersion { get; set; } = string.Empty;

        /// <summary>
        /// 电测板固件版本
        /// </summary>
        public string ElectricityFirmware { get; set; } = string.Empty;

        /// <summary>
        /// 电测板硬件版本
        /// </summary>
        public string ElectricityHardware { get; set; } = string.Empty;

        /// <summary>
        /// 系统固件版本
        /// </summary>
        public string OsFirmware { get; set; } = string.Empty;

        /// <summary>
        /// 系统硬件版本
        /// </summary>
        public string OsHardware { get; set; } = string.Empty;

        /// <summary>
        /// 接线盒硬件版本
        /// </summary>
        public string JunctionHardware { get; set; } = string.Empty;

        /// <summary>
        /// 接线盒固件版本
        /// </summary>
        public string JunctionFirmware { get; set; } = string.Empty;

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(ScpiVersion) || !string.IsNullOrEmpty(ApplicationVersion);

        /// <inheritdoc/>
        public override string ToString() =>
            $"SCPI={ScpiVersion},App={ApplicationVersion},ElecFW={ElectricityFirmware},ElecHW={ElectricityHardware}";
    }
}
