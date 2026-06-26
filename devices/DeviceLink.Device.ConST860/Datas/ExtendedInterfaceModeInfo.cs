using System;

namespace DeviceLink.Device.ConST860
{
    /// <summary>
    /// 扩展接口输出模式信息
    /// </summary>
    public class ExtendedInterfaceModeInfo
    {
        /// <summary>当前输出模式</summary>
        public int CurrentMode { get; set; }

        /// <summary>可用输出模式列表</summary>
        public int[] AvailableModes { get; set; } = Array.Empty<int>();

        public override string ToString() => $"Current={CurrentMode}, Available=[{string.Join(",", AvailableModes)}]";
    }
}
