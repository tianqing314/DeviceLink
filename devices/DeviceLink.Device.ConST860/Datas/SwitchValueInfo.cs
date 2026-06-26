namespace DeviceLink.Device.ConST860
{
    /// <summary>
    /// 压力开关动作值
    /// </summary>
    public class SwitchValueInfo
    {
        /// <summary>关闭值</summary>
        public double CloseValue { get; set; }

        /// <summary>关闭值单位</summary>
        public string CloseUnit { get; set; } = string.Empty;

        /// <summary>打开值</summary>
        public double OpenValue { get; set; }

        /// <summary>打开值单位</summary>
        public string OpenUnit { get; set; } = string.Empty;

        public override string ToString() => $"Close={CloseValue} {CloseUnit}, Open={OpenValue} {OpenUnit}";
    }
}
