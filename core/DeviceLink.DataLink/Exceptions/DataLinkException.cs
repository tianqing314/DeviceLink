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
    /// 请求未送达异常 —— 表示请求字节<b>确定没有发到线上</b>。
    ///
    /// 这是会话层判断"能否安全重发"的唯一判据：连接根本没建立、或写之前连接已断开，
    /// 设备不可能执行该命令，因此重连后重发是安全的。
    ///
    /// 反例（不允许包装成本异常）：写调用中途失败、写成功但读超时 ——
    /// 这些情况下设备可能已经执行，重发会造成重复写。
    /// </summary>
    public class RequestNotSentException : DataLinkException
    {
        /// <summary>
        /// 初始化请求未送达异常
        /// </summary>
        public RequestNotSentException() : base()
        {
        }

        /// <summary>
        /// 初始化请求未送达异常
        /// </summary>
        /// <param name="message">错误消息</param>
        public RequestNotSentException(string message) : base(message)
        {
        }

        /// <summary>
        /// 初始化请求未送达异常
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">内部异常</param>
        public RequestNotSentException(string message, Exception innerException) : base(message, innerException)
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
