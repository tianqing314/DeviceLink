using System;
using System.Text;

namespace DeviceLink.Core.Protocol
{
    /// <summary>
    /// SCPI 标准协议编解码器。
    /// 
    /// SCPI 命令是纯文本，以换行符分隔：
    ///   查询: "*IDN?\n"、"MEAS:PRES?\n"
    ///   命令: "*RST\n"、"SYST:REM\n"
    /// 
    /// 帧结束符由 IFrameStrategy (DelimiterFrameStrategy with \n) 处理，
    /// 编解码器只负责命令文本的构建和响应的文本解析。
    /// </summary>
    public class ScpiCodec : IProtocolCodec
    {
        private readonly Encoding _encoding;

        public ScpiCodec()
        {
            _encoding = Encoding.ASCII;
        }

        public string ProtocolName => "SCPI";

        public byte[] Encode(Command command)
        {
            var text = command.Id;

            // 查询命令自动带 ?
            if (command.Kind == CommandKind.Read && !text.EndsWith("?"))
                text += "?";

            // 拼接参数
            if (command.Parameters.Length > 0)
            {
                text += " " + string.Join(",", command.Parameters);
            }

            return _encoding.GetBytes(text);
        }

        public string DecodeText(byte[] raw)
        {
            if (raw == null || raw.Length == 0)
                return string.Empty;

            // 去掉尾部空白
            return _encoding.GetString(raw).TrimEnd('\0', '\r', '\n', ' ');
        }

        public bool IsErrorResponse(byte[] raw, out string errorMessage)
        {
            errorMessage = string.Empty;
            var text = DecodeText(raw);

            // SCPI 错误响应通常以 "Err" 或数字错误码开头
            // SysT:ERR? 返回格式: "0,\"No error\""
            // 非零 = 有错误
            if (text.Length > 0 && text[0] != '0' && char.IsDigit(text[0]))
            {
                errorMessage = text;
                return true;
            }

            return false;
        }
    }
}
