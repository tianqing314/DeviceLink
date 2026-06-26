namespace DeviceLink.Device.ConST860
{
    /// <summary>
    /// 高度差修正信息
    /// </summary>
    public class HeightCorrectionInfo
    {
        /// <summary>使能：0=关闭, 1=开启</summary>
        public bool Enabled { get; set; }

        /// <summary>单位制：0=英制, 1=公制</summary>
        public int UnitType { get; set; }

        /// <summary>高度差（公制cm, 英制in）</summary>
        public double Height { get; set; }

        /// <summary>介质密度（公制kg/m³, 英制lb/ft³）</summary>
        public double Density { get; set; }

        /// <summary>重力加速度（公制m/s², 英制ft/s²）</summary>
        public double Gravity { get; set; }

        /// <summary>温度（℃）</summary>
        public double Temperature { get; set; }

        public override string ToString() => $"Enabled={Enabled}, UnitType={UnitType}, Height={Height}, Density={Density}, Gravity={Gravity}, Temp={Temperature}";
    }
}
