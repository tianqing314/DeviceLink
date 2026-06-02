using System;

namespace DeviceLink.Protocol
{
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
}