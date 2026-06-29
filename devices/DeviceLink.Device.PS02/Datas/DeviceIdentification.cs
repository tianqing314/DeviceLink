namespace DeviceLink.Device.PS02
{
    /// <summary>
    /// PS02 设备标识信息
    /// </summary>
    public class DeviceIdentification
    {
        /// <summary>序列号</summary>
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>固件版本</summary>
        public string FirmwareVersion { get; set; } = string.Empty;

        /// <summary>硬件版本</summary>
        public string HardwareVersion { get; set; } = string.Empty;

        /// <summary>数据是否有效</summary>
        public bool IsValid => !string.IsNullOrEmpty(SerialNumber);

        /// <inheritdoc/>
        public override string ToString() =>
            $"SN={SerialNumber}, FW={FirmwareVersion}, HW={HardwareVersion}";
    }
}
