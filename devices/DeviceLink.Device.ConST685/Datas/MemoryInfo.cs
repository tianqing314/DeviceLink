namespace DeviceLink.Device.ConST685
{
    /// <summary>
    /// 存储器信息 —— MMEMory:FREE[:ALL]? 返回值
    /// </summary>
    public class MemoryInfo
    {
        /// <summary>
        /// 可用字节数
        /// </summary>
        public long FreeBytes { get; set; }

        /// <summary>
        /// 已用字节数
        /// </summary>
        public long UsedBytes { get; set; }

        /// <summary>
        /// 总字节数
        /// </summary>
        public long TotalBytes => FreeBytes + UsedBytes;

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid => FreeBytes >= 0 && UsedBytes >= 0;

        /// <inheritdoc/>
        public override string ToString() => $"Free={FreeBytes},Used={UsedBytes},Total={TotalBytes}";
    }
}
