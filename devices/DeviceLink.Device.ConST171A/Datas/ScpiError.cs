namespace DeviceLink.Device.ConST171A
{
    /// <summary>
    /// SCPI 错误信息 —— SYSTem:ERRor? 返回值
    /// 格式：0,"No error" 或 120,"Command parameter error"
    /// </summary>
    public class ScpiError
    {
        /// <summary>错误码（0 表示无错误）</summary>
        public int Code { get; set; }

        /// <summary>错误描述</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>是否有错误</summary>
        public bool IsError => Code != 0;

        /// <inheritdoc/>
        public override string ToString() => $@"{Code},""{Message}""";
    }
}
