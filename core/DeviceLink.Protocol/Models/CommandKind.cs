namespace DeviceLink.Protocol
{
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
}