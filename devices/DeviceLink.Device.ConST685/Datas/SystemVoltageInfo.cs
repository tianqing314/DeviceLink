using System;

namespace DeviceLink.Device.ConST685
{
    /// <summary>
    /// 系统电压信息 —— DIAGnostic:SYSTem:INFOs:VOLTages? 返回值
    /// </summary>
    public class SystemVoltageInfo
    {
        /// <summary>
        /// AD 通道 12V 值
        /// </summary>
        public double Voltage12V { get; set; } = double.NaN;

        /// <summary>
        /// 5V 电源是否正常（true=正常）
        /// </summary>
        public bool Is5VNormal { get; set; }

        /// <summary>
        /// 3.3V 电源是否正常（true=正常）
        /// </summary>
        public bool Is33VNormal { get; set; }

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid => !double.IsNaN(Voltage12V);

        /// <inheritdoc/>
        public override string ToString() =>
            $"12V={Voltage12V:F3},5V={(Is5VNormal ? "OK" : "FAIL")},3.3V={(Is33VNormal ? "OK" : "FAIL")}";
    }
}
