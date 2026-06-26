namespace DeviceLink.Device.ConST860
{
    /// <summary>
    /// 控制信息
    /// </summary>
    public class ControlInfo
    {
        /// <summary>实时值</summary>
        public double Value { get; set; }

        /// <summary>目标值</summary>
        public double Target { get; set; }

        /// <summary>单位</summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>量程</summary>
        public string Range { get; set; } = string.Empty;

        /// <summary>压力类型</summary>
        public string PressureType { get; set; } = string.Empty;

        /// <summary>是否稳定</summary>
        public bool IsStable { get; set; }

        /// <summary>控制状态</summary>
        public string State { get; set; } = string.Empty;

        /// <summary>扩展接口信息</summary>
        public string ExtendInfo { get; set; } = string.Empty;

        public override string ToString() => $"{Value}, {Target}, {Unit}, {Range}, {PressureType}, {IsStable}, {State}, {ExtendInfo}";
    }
}
