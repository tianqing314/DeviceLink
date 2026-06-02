using System;

namespace DeviceLink.Transport
{
    /// <summary>
    /// 传输层异常基类
    /// </summary>
    public class TransportException : Exception
    {
        /// <summary>
        /// 初始化传输层异常
        /// </summary>
        public TransportException() : base()
        {
        }

        /// <summary>
        /// 初始化传输层异常
        /// </summary>
        /// <param name="message">错误消息</param>
        public TransportException(string message) : base(message)
        {
        }

        /// <summary>
        /// 初始化传输层异常
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">内部异常</param>
        public TransportException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// 连接异常
    /// </summary>
    public class ConnectionException : TransportException
    {
        /// <summary>
        /// 初始化连接异常
        /// </summary>
        public ConnectionException() : base()
        {
        }

        /// <summary>
        /// 初始化连接异常
        /// </summary>
        /// <param name="message">错误消息</param>
        public ConnectionException(string message) : base(message)
        {
        }

        /// <summary>
        /// 初始化连接异常
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">内部异常</param>
        public ConnectionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// 传输超时异常
    /// </summary>
    public class TransportTimeoutException : TransportException
    {
        /// <summary>
        /// 初始化传输超时异常
        /// </summary>
        public TransportTimeoutException() : base()
        {
        }

        /// <summary>
        /// 初始化传输超时异常
        /// </summary>
        /// <param name="message">错误消息</param>
        public TransportTimeoutException(string message) : base(message)
        {
        }

        /// <summary>
        /// 初始化传输超时异常
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">内部异常</param>
        public TransportTimeoutException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
