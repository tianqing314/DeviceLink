using System;

namespace DeviceLink.Core.Protocol
{
    /// <summary>
    /// 设备操作结果 —— 替代旧 Xmas11 的 iResponse / iResponse&lt;T&gt;。
    /// </summary>
    public class DeviceResult
    {
        /// <summary>操作是否成功</summary>
        public bool Success { get; protected set; }

        /// <summary>失败时的错误信息</summary>
        public string Error { get; protected set; } = string.Empty;

        /// <summary>底层 Channel 结果（含原始字节、重试次数、耗时等）</summary>
        public Channel.ChannelResult? ChannelResult { get; protected set; }

        public static DeviceResult Succeed(Channel.ChannelResult channelResult)
        {
            return new DeviceResult { Success = true, ChannelResult = channelResult };
        }

        public static DeviceResult Failed(string error, Channel.ChannelResult? channelResult = null)
        {
            return new DeviceResult { Success = false, Error = error, ChannelResult = channelResult };
        }
    }

    /// <summary>
    /// 带返回值的设备操作结果
    /// </summary>
    public class DeviceResult<T> : DeviceResult
    {
        /// <summary>解析后的业务数据</summary>
        public T Value { get; private set; } = default!;

        public static DeviceResult<T> Succeed(T value, Channel.ChannelResult channelResult)
        {
            return new DeviceResult<T>
            {
                Success = true,
                Value = value,
                ChannelResult = channelResult
            };
        }

        public new static DeviceResult<T> Failed(string error, Channel.ChannelResult? channelResult = null)
        {
            return new DeviceResult<T>
            {
                Success = false,
                Error = error,
                ChannelResult = channelResult
            };
        }
    }
}
