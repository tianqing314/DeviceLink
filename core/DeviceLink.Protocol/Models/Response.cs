using System;

namespace DeviceLink.Protocol
{
    /// <summary>
    /// 协议响应
    /// </summary>
    public class Response
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 响应数据
        /// </summary>
        public byte[]? Data { get; set; }

        /// <summary>
        /// 响应文本
        /// </summary>
        public string? Text { get; set; }

        /// <summary>
        /// 错误消息（如果失败）
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 创建成功响应
        /// </summary>
        /// <param name="data">响应数据</param>
        /// <returns>成功响应</returns>
        public static Response Succeed(byte[] data)
        {
            return new Response
            {
                Success = true,
                Data = data,
                Text = System.Text.Encoding.UTF8.GetString(data)
            };
        }

        /// <summary>
        /// 创建成功响应
        /// </summary>
        /// <param name="text">响应文本</param>
        /// <returns>成功响应</returns>
        public static Response Succeed(string text)
        {
            return new Response
            {
                Success = true,
                Data = System.Text.Encoding.UTF8.GetBytes(text),
                Text = text
            };
        }

        /// <summary>
        /// 创建失败响应
        /// </summary>
        /// <param name="errorMessage">错误消息</param>
        /// <returns>失败响应</returns>
        public static Response Fail(string errorMessage)
        {
            return new Response
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
}