using System;

namespace DeviceLink.Core.Protocol
{
    /// <summary>
    /// 逻辑命令 —— 协议无关的命令表示。
    /// 替代旧 Xmas11 的 iCommand 抽象类。
    /// </summary>
    public class Command
    {
        /// <summary>读写标识：Read=查询 / Write=设置 / NonQuery=单向无返回</summary>
        public CommandKind Kind { get; }

        /// <summary>命令标识符，如 "PRES"、"IDN"、"SN"</summary>
        public string Id { get; }

        /// <summary>命令参数（可选）</summary>
        public string[] Parameters { get; }

        public Command(CommandKind kind, string id, params string[] parameters)
        {
            Kind = kind;
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Parameters = parameters ?? Array.Empty<string>();
        }

        /// <summary>快捷：创建读取命令</summary>
        public static Command Read(string id) => new(CommandKind.Read, id);

        /// <summary>快捷：创建写入命令</summary>
        public static Command Write(string id, params string[] parameters) =>
            new(CommandKind.Write, id, parameters);

        /// <summary>快捷：创建单向命令</summary>
        public static Command NonQuery(string id, params string[] parameters) =>
            new(CommandKind.NonQuery, id, parameters);
    }

    /// <summary>
    /// 命令类型
    /// </summary>
    public enum CommandKind
    {
        /// <summary>查询命令 —— 期待设备返回数据</summary>
        Read,

        /// <summary>写入命令 —— 设置参数并以返回值确认</summary>
        Write,

        /// <summary>单向命令 —— 不期待返回</summary>
        NonQuery
    }
}
