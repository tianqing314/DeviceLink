using System;

namespace DeviceLink.DataLink
{
    /// <summary>
    /// 数据链路层异常基类
    /// </summary>
    public class DataLinkException : Exception
    {
        /// <summary>
        /// 初始化数据链路层异常
        /// </summary>
        public DataLinkException() : base()
        {
        }

        /// <summary>
        /// 初始化数据链路层异常
        /// </summary>
        /// <param name="message">错误消息</param>
        public DataLinkException(string message) : base(message)
        {
        }

        /// <summary>
        /// 初始化数据链路层异常
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">内部异常</param>
        public DataLinkException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// 帧超时异常
    /// </summary>
    public class FrameTimeoutException : DataLinkException
    {
        /// <summary>
        /// 初始化帧超时异常
        /// </summary>
        public FrameTimeoutException() : base()
        {
        }

        /// <summary>
        /// 初始化帧超时异常
        /// </summary>
        /// <param name="message">错误消息</param>
        public FrameTimeoutException(string message) : base(message)
        {
        }

        /// <summary>
        /// 初始化帧超时异常
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">内部异常</param>
        public FrameTimeoutException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// 帧格式错误异常
    /// </summary>
    public class FrameFormatException : DataLinkException
    {
        /// <summary>
        /// 初始化帧格式错误异常
        /// </summary>
        public FrameFormatException() : base()
        {
        }

        /// <summary>
        /// 初始化帧格式错误异常
        /// </summary>
        /// <param name="message">错误消息</param>
        public FrameFormatException(string message) : base(message)
        {
        }

        /// <summary>
        /// 初始化帧格式错误异常
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">内部异常</param>
        public FrameFormatException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
