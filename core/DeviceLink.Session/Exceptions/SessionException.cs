using System;

namespace DeviceLink.Session
{
    /// <summary>
    /// 会话层异常基类
    /// </summary>
    public class SessionException : Exception
    {
        /// <summary>
        /// 初始化会话层异常
        /// </summary>
        public SessionException() : base()
        {
        }

        /// <summary>
        /// 初始化会话层异常
        /// </summary>
        /// <param name="message">错误消息</param>
        public SessionException(string message) : base(message)
        {
        }

        /// <summary>
        /// 初始化会话层异常
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">内部异常</param>
        public SessionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// 会话超时异常
    /// </summary>
    public class SessionTimeoutException : SessionException
    {
        /// <summary>
        /// 初始化会话超时异常
        /// </summary>
        public SessionTimeoutException() : base()
        {
        }

        /// <summary>
        /// 初始化会话超时异常
        /// </summary>
        /// <param name="message">错误消息</param>
        public SessionTimeoutException(string message) : base(message)
        {
        }

        /// <summary>
        /// 初始化会话超时异常
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">内部异常</param>
        public SessionTimeoutException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// 会话连接异常
    /// </summary>
    public class SessionConnectionException : SessionException
    {
        /// <summary>
        /// 初始化会话连接异常
        /// </summary>
        public SessionConnectionException() : base()
        {
        }

        /// <summary>
        /// 初始化会话连接异常
        /// </summary>
        /// <param name="message">错误消息</param>
        public SessionConnectionException(string message) : base(message)
        {
        }

        /// <summary>
        /// 初始化会话连接异常
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">内部异常</param>
        public SessionConnectionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
