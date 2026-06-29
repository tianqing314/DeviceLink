namespace DeviceLink.Device.PS02
{
    /// <summary>
    /// PS02 压力类型枚举
    /// </summary>
    public enum PressureType : ushort
    {
        /// <summary>表压（Gauge Pressure）</summary>
        Gauge = 0,

        /// <summary>绝压（Absolute Pressure）</summary>
        Absolute = 2,

        /// <summary>差压（Differential Pressure）</summary>
        Differential = 3
    }
}
