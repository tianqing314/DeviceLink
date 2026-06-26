namespace DeviceLink.Device.ConST171A
{
    /// <summary>
    /// 设备版本信息 —— SYSTem:VERSion? 返回值
    /// 格式：BOOT=2.1.0，EPU_DM_V1.0.1.11，EPU-LP V1.0，EPU_LP_V1.0.0.15
    /// </summary>
    public class DeviceVersionInfo
    {
        /// <summary>Bootloader 版本</summary>
        public string Bootloader { get; set; } = string.Empty;

        /// <summary>显示模块版本</summary>
        public string DisplayModule { get; set; } = string.Empty;

        /// <summary>硬件版本</summary>
        public string Hardware { get; set; } = string.Empty;

        /// <summary>固件版本</summary>
        public string Firmware { get; set; } = string.Empty;

        /// <summary>是否有效 —— Firmware 或 Hardware 任一非空即为有效</summary>
        public bool IsValid => !string.IsNullOrEmpty(Firmware) || !string.IsNullOrEmpty(Hardware);

        /// <inheritdoc/>
        public override string ToString() =>
            $"BOOT={Bootloader}, DM={DisplayModule}, HARD={Hardware}, FIRM={Firmware}";
    }
}
