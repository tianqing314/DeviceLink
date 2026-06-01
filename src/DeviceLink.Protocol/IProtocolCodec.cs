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

    /// <summary>
    /// 逻辑命令
    /// </summary>
    public class Command
    {
        /// <summary>
        /// 命令类型
        /// </summary>
        public CommandKind Kind { get; set; }

        /// <summary>
        /// 命令ID（如寄存器地址、SCPI命令等）
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 命令参数
        /// </summary>
        public string[] Parameters { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 命令数据（用于写入操作）
        /// </summary>
        public byte[]? Data { get; set; }

        /// <summary>
        /// 创建读取命令
        /// </summary>
        /// <param name="id">命令ID</param>
        /// <param name="parameters">参数</param>
        /// <returns>读取命令</returns>
        public static Command Read(string id, params string[] parameters)
        {
            return new Command
            {
                Kind = CommandKind.Read,
                Id = id,
                Parameters = parameters
            };
        }

        /// <summary>
        /// 创建写入命令
        /// </summary>
        /// <param name="id">命令ID</param>
        /// <param name="parameters">参数</param>
        /// <returns>写入命令</returns>
        public static Command Write(string id, params string[] parameters)
        {
            return new Command
            {
                Kind = CommandKind.Write,
                Id = id,
                Parameters = parameters
            };
        }

        /// <summary>
        /// 创建无返回命令
        /// </summary>
        /// <param name="id">命令ID</param>
        /// <param name="parameters">参数</param>
        /// <returns>无返回命令</returns>
        public static Command NonQuery(string id, params string[] parameters)
        {
            return new Command
            {
                Kind = CommandKind.NonQuery,
                Id = id,
                Parameters = parameters
            };
        }
    }

    /// <summary>
    /// 命令类型
    /// </summary>
    public enum CommandKind
    {
        /// <summary>
        /// 读取命令（需要返回数据）
        /// </summary>
        Read,

        /// <summary>
        /// 写入命令（发送数据，需要确认）
        /// </summary>
        Write,

        /// <summary>
        /// 无返回命令（发送命令，不需要返回）
        /// </summary>
        NonQuery
    }

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
