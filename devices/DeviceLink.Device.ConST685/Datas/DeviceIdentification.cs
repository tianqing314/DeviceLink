namespace DeviceLink.Device.ConST685
{
    /// <summary>
    /// 设备标识 —— *IDN? 返回值
    /// 格式：厂家,型号,序列号,软件版本号
    /// </summary>
    public class DeviceIdentification
    {
        /// <summary>
        /// 厂家
        /// </summary>
        public string Manufacturer { get; set; } = string.Empty;

        /// <summary>
        /// 产品型号
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// 产品序列号
        /// </summary>
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 软件版本号
        /// </summary>
        public string FirmwareVersion { get; set; } = string.Empty;

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid =>
            !string.IsNullOrEmpty(Manufacturer) ||
            !string.IsNullOrEmpty(Model);

        /// <inheritdoc/>
        public override string ToString() => $"{Manufacturer},{Model},{SerialNumber},{FirmwareVersion}";
    }
}
