using System;

namespace DeviceLink.DeviceBase
{
    /// <summary>
    /// 设备异常
    /// </summary>
    public class DeviceException : Exception
    {
        /// <summary>
        /// 初始化设备异常
        /// </summary>
        public DeviceException() : base()
        {
        }

        /// <summary>
        /// 初始化设备异常
        /// </summary>
        /// <param name="message">错误消息</param>
        public DeviceException(string message) : base(message)
        {
        }

        /// <summary>
        /// 初始化设备异常
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">内部异常</param>
        public DeviceException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
