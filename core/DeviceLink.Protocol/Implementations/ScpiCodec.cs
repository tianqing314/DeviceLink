using System;
using System.Text;

namespace DeviceLink.Protocol
{
    /// <summary>
    /// SCPI 协议编解码器。
    /// 
    /// SCPI (Standard Commands for Programmable Instruments) 是一种标准的仪器控制协议。
    /// 格式：命令以换行符(\n)结尾，响应也以换行符结尾。
    /// 
    /// 示例：
    ///   发送: "*IDN?\n"              → 查询设备标识
    ///   接收: "Manufacturer,Model,SN,FW\n" → 设备标识响应
    ///   发送: "MEAS:VOLT?\n"         → 测量电压
    ///   接收: "1.2345\n"             → 电压值
    /// </summary>
    public class ScpiCodec : IProtocolCodec
    {
        private readonly char _terminator;
        private readonly Encoding _encoding;

        /// <summary>
        /// 创建 SCPI 协议编解码器
        /// </summary>
        /// <param name="terminator">命令结束符（默认 '\n'）</param>
        public ScpiCodec(char terminator = '\n')
        {
            _terminator = terminator;
            _encoding = Encoding.ASCII;
        }

        /// <inheritdoc/>
        public string ProtocolName => "SCPI";

        /// <inheritdoc/>
        public byte[] Encode(Command command)
        {
            var sb = new StringBuilder();

            // SCPI 命令格式
            switch (command.Kind)
            {
                case CommandKind.Read:
                    // 查询命令：命令 + ?
                    sb.Append(command.Id);
                    sb.Append('?');
                    if (command.Parameters.Length > 0)
                    {
                        sb.Append(' ');
                        sb.Append(string.Join(",", command.Parameters));
                    }
                    break;

                case CommandKind.Write:
                    // 设置命令：命令 + 空格 + 参数
                    sb.Append(command.Id);
                    if (command.Parameters.Length > 0)
                    {
                        sb.Append(' ');
                        sb.Append(string.Join(",", command.Parameters));
                    }
                    break;

                case CommandKind.NonQuery:
                    // 无返回命令：直接发送命令
                    sb.Append(command.Id);
                    if (command.Parameters.Length > 0)
                    {
                        sb.Append(' ');
                        sb.Append(string.Join(",", command.Parameters));
                    }
                    break;
            }

            // 添加结束符
            sb.Append(_terminator);

            return _encoding.GetBytes(sb.ToString());
        }

        /// <inheritdoc/>
        public string DecodeText(byte[] raw)
        {
            if (raw == null || raw.Length == 0)
                return string.Empty;

            // 去掉结束符
            int len = raw.Length;
            while (len > 0 && raw[len - 1] == (byte)_terminator)
                len--;

            return _encoding.GetString(raw, 0, len);
        }

        /// <inheritdoc/>
        public bool IsErrorResponse(byte[] raw, out string errorMessage)
        {
            errorMessage = string.Empty;
            var text = DecodeText(raw);

            // SCPI 错误格式：+/-数字,"错误消息"
            // 例如：-100,"Command error"
            // 例如：-200,"Execution error"
            if (text.StartsWith("-"))
            {
                var parts = text.Split(new[] {','}, 2);
                if (parts.Length >= 2)
                {
                    errorMessage = parts[1].Trim('"');
                }
                else
                {
                    errorMessage = text;
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// 从 SCPI 响应中提取数值
        /// </summary>
        /// <param name="raw">原始响应数据</param>
        /// <returns>数值，如果解析失败返回 double.NaN</returns>
        public double ExtractNumeric(byte[] raw)
        {
            var text = DecodeText(raw);
            if (double.TryParse(text, out double value))
            {
                return value;
            }
            return double.NaN;
        }

        /// <summary>
        /// 从 SCPI 响应中提取字符串
        /// </summary>
        /// <param name="raw">原始响应数据</param>
        /// <returns>字符串值</returns>
        public string ExtractString(byte[] raw)
        {
            return DecodeText(raw);
        }

        /// <summary>
        /// 从 SCPI 响应中提取布尔值
        /// </summary>
        /// <param name="raw">原始响应数据</param>
        /// <returns>布尔值，如果解析失败返回 false</returns>
        public bool ExtractBoolean(byte[] raw)
        {
            var text = DecodeText(raw).ToUpper();
            return text == "1" || text == "ON" || text == "TRUE";
        }
    }
}
