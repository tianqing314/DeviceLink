using System;

namespace DeviceLink.Core.Channel
{
    /// <summary>
    /// 通道操作结果
    /// </summary>
    public class ChannelResult
    {
        /// <summary>通讯是否成功（收到了有效数据）</summary>
        public bool Success { get; private set; }

        /// <summary>接收到的完整帧数据</summary>
        public byte[] Data { get; private set; } = Array.Empty<byte>();

        /// <summary>实际重试次数</summary>
        public int RetryCount { get; private set; }

        /// <summary>总耗时</summary>
        public TimeSpan Elapsed { get; private set; }

        /// <summary>失败原因（Success=false 时）</summary>
        public string Error { get; private set; } = string.Empty;

        /// <summary>快速构造成功结果</summary>
        public static ChannelResult Succeed(byte[] data, int retryCount = 0, TimeSpan elapsed = default)
        {
            return new ChannelResult
            {
                Success = true,
                Data = data,
                RetryCount = retryCount,
                Elapsed = elapsed
            };
        }

        /// <summary>快速构造失败结果</summary>
        public static ChannelResult Fail(string error, int retryCount = 0)
        {
            return new ChannelResult
            {
                Success = false,
                Error = error,
                RetryCount = retryCount
            };
        }

        /// <summary>快速构造超时结果</summary>
        public static ChannelResult Timeout(int retryCount = 0)
        {
            return new ChannelResult
            {
                Success = false,
                Error = "请求超时：未收到响应",
                RetryCount = retryCount
            };
        }
    }
}
