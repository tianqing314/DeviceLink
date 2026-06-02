using System;
using System.Text;

namespace DeviceLink.Protocol
{
    /// <summary>
    /// ConST 私有协议编解码器。
    /// 
    /// 格式：address:mark:command:param1:param2:...\0
    /// 
    /// 请求：R=读 / W=写
    /// 响应：F=成功 / E=错误
    /// 
    /// 示例：
    ///   发送: "1:R:PRES:\0"           → 读取压力
    ///   接收: "1:F:PRES:1.23456\0"   → 压力值 1.23456
    ///   发送: "1:W:PUNIT:bar\0"      → 设置压力单位
    ///   接收: "1:F:PUNIT:\0"          → 设置成功
    ///   接收: "1:E:ERR_OVER\0"       → 设备错误
    /// </summary>
    public class ConSTCodec : IProtocolCodec
    {
        private readonly byte _address;
        private readonly char _separator;
        private readonly byte[] _terminator;
        private readonly Encoding _encoding;

        /// <summary>
        /// 创建 ConST 协议编解码器
        /// </summary>
        /// <param name="address">设备地址（0~255，默认 255）</param>
        /// <param name="separator">字段分隔符（默认 ':'）</param>
        /// <param name="terminator">帧结束符（默认 \0）</param>
        public ConSTCodec(byte address = 255, char separator = ':', byte[]? terminator = null)
        {
            _address = address;
            _separator = separator;
            _terminator = terminator ?? new byte[] { 0 };
            _encoding = Encoding.ASCII;
        }

        /// <inheritdoc/>
        public string ProtocolName => "ConST";

        /// <inheritdoc/>
        public byte[] Encode(Command command)
        {
            var mark = command.Kind switch
            {
                CommandKind.Read => 'R',
                CommandKind.Write => 'W',
                CommandKind.NonQuery => 'W',
                _ => 'R'
            };

            var sb = new StringBuilder();
            sb.Append(_address);
            sb.Append(_separator);
            sb.Append(mark);
            sb.Append(_separator);
            sb.Append(command.Id);

            if (command.Parameters.Length > 0)
            {
                foreach (var p in command.Parameters)
                {
                    sb.Append(_separator);
                    sb.Append(p);
                }
            }

            // 最后一个字段后跟分隔符（ConST 协议规定）
            sb.Append(_separator);

            // 拼接到字节数组：文本 + 帧结束符
            var text = _encoding.GetBytes(sb.ToString());
            var result = new byte[text.Length + _terminator.Length];
            Buffer.BlockCopy(text, 0, result, 0, text.Length);
            Buffer.BlockCopy(_terminator, 0, result, text.Length, _terminator.Length);
            return result;
        }

        /// <inheritdoc/>
        public string DecodeText(byte[] raw)
        {
            if (raw == null || raw.Length == 0)
                return string.Empty;

            // 去掉帧结束符
            int len = raw.Length;
            while (len > 0 && IsTerminator(raw[len - 1]))
                len--;

            return _encoding.GetString(raw, 0, len);
        }

        /// <inheritdoc/>
        public bool IsErrorResponse(byte[] raw, out string errorMessage)
        {
            errorMessage = string.Empty;
            var text = DecodeText(raw);

            // ConST 错误格式：address:E:ERRORCODE 或 address:E:ERRORCODE:message
            var parts = text.Split(_separator);
            if (parts.Length >= 2 && parts[1] == "E")
            {
                errorMessage = parts.Length >= 4 ? parts[3] 
                             : parts.Length >= 3 ? parts[2] 
                             : "未知错误";
                return true;
            }

            return false;
        }

        /// <summary>
        /// 从 ConST 响应中提取字段值。
        /// 格式：address:F:command:value1:value2:...
        /// 返回从 index 开始的字段数组。
        /// </summary>
        /// <param name="raw">原始响应数据</param>
        /// <param name="startIndex">起始索引（默认3）</param>
        /// <returns>字段数组</returns>
        public string[] ExtractFields(byte[] raw, int startIndex = 3)
        {
            var text = DecodeText(raw);
            var parts = text.Split(_separator);
            if (parts.Length > startIndex)
            {
                var result = new string[parts.Length - startIndex];
                Array.Copy(parts, startIndex, result, 0, result.Length);
                return result;
            }
            return Array.Empty<string>();
        }

        /// <summary>
        /// 从 ConST 响应中提取单个字段值
        /// </summary>
        /// <param name="raw">原始响应数据</param>
        /// <param name="index">字段索引（默认3）</param>
        /// <returns>字段值</returns>
        public string ExtractField(byte[] raw, int index = 3)
        {
            var fields = ExtractFields(raw, index);
            return fields.Length > 0 ? fields[0] : string.Empty;
        }

        private bool IsTerminator(byte b)
        {
            foreach (var t in _terminator)
                if (b == t) return true;
            return false;
        }
    }
}
