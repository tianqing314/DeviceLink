namespace DeviceLink.Device.ConST860
{
    /// <summary>
    /// 滤波信息
    /// </summary>
    public class FilterInfo
    {
        /// <summary>使能：0=关闭, 1=开启</summary>
        public bool Enabled { get; set; }

        /// <summary>滤波类型：0=一阶滤波, 1=平均滤波</summary>
        public int FilterType { get; set; }

        /// <summary>滤波参数（一阶滤波=系数0-1, 平均滤波=采样时间1-20s）</summary>
        public double Value { get; set; }

        public override string ToString() => $"Enabled={Enabled}, Type={FilterType}, Value={Value}";
    }
}
