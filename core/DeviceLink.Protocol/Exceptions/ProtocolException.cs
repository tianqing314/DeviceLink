using System;

namespace DeviceLink.Protocol
{
    /// <summary>
    /// 协议异常
    /// </summary>
    public class ProtocolException : Exception
    {
        /// <summary>
        /// 初始化协议异常
        /// </summary>
        public ProtocolException() : base()
        {
        }

        /// <summary>
        /// 初始化协议异常
        /// </summary>
        /// <param name="message">错误消息</param>
        public ProtocolException(string message) : base(message)
        {
        }

        /// <summary>
        /// 初始化协议异常
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">内部异常</param>
        public ProtocolException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}