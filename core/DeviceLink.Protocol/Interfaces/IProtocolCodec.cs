using System;

namespace DeviceLink.Protocol
{
    /// <summary>
    /// 协议编解码器接口 —— 将逻辑命令编码为字节，将响应字节解码为业务结果。
    /// 每个协议（SCPI / ConST / Modbus / 自定义协议）实现一次此接口。
    /// </summary>
    public interface IProtocolCodec
    {
        /// <summary>
        /// 协议名称（用于日志），如 "ConST"、"SCPI"、"ModbusRTU"
        /// </summary>
        string ProtocolName { get; }

        /// <summary>
        /// 将逻辑命令编码为传输字节（已包含帧分隔符）
        /// </summary>
        /// <param name="command">逻辑命令</param>
        /// <returns>编码后的字节数据</returns>
        byte[] Encode(Command command);

        /// <summary>
        /// 将接收到的原始字节解码为文本（用于日志和简单查询）
        /// </summary>
        /// <param name="raw">原始字节数据</param>
        /// <returns>解码后的文本</returns>
        string DecodeText(byte[] raw);

        /// <summary>
        /// 检查响应是否表示设备错误
        /// </summary>
        /// <param name="raw">原始响应数据</param>
        /// <param name="errorMessage">输出：错误消息</param>
        /// <returns>true 表示是错误响应</returns>
        bool IsErrorResponse(byte[] raw, out string errorMessage);
    }
}