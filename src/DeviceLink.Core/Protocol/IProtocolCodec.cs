using System.Threading;
using System.Threading.Tasks;

namespace DeviceLink.Core.Protocol
{
    /// <summary>
    /// 协议编解码器 —— 将逻辑命令编码为字节，将响应字节解码为业务结果。
    /// 每个协议（SCPI / ConST / Modbus / CPPI）实现一次此接口。
    /// </summary>
    public interface IProtocolCodec
    {
        /// <summary>协议名称（用于日志），如 "ConST"、"SCPI"</summary>
        string ProtocolName { get; }

        /// <summary>将逻辑命令编码为传输字节（已包含帧分隔符）</summary>
        byte[] Encode(Command command);

        /// <summary>将接收到的原始字节解码为文本（用于日志和简单查询）</summary>
        string DecodeText(byte[] raw);

        /// <summary>检查响应是否表示设备错误</summary>
        bool IsErrorResponse(byte[] raw, out string errorMessage);
    }
}
