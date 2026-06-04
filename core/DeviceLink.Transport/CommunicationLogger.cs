using System;
using System.IO;
using System.Text;
using System.Threading;

namespace DeviceLink.Transport
{
    /// <summary>
    /// 通信日志记录器 —— 记录完整的通信链路日志到文件
    /// </summary>
    public static class CommunicationLogger
    {
        private static readonly object _lock = new object();
        private static string _logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        private static string _logFileName = "communication.log";
        private static bool _enabled = true;

        /// <summary>
        /// 日志目录
        /// </summary>
        public static string LogDirectory
        {
            get => _logDirectory;
            set
            {
                _logDirectory = value;
                EnsureDirectoryExists();
            }
        }

        /// <summary>
        /// 日志文件名
        /// </summary>
        public static string LogFileName
        {
            get => _logFileName;
            set => _logFileName = value;
        }

        /// <summary>
        /// 日志文件完整路径
        /// </summary>
        public static string LogFilePath => Path.Combine(_logDirectory, _logFileName);

        /// <summary>
        /// 是否启用日志
        /// </summary>
        public static bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        static CommunicationLogger()
        {
            EnsureDirectoryExists();
        }

        private static void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        /// <summary>
        /// 记录发送命令
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        /// <param name="commandId">命令 ID</param>
        /// <param name="commandKind">命令类型</param>
        /// <param name="commandString">最终指令字符串（含结束符）</param>
        /// <param name="bytes">字节数组</param>
        public static void LogSend(string deviceName, string commandId, string commandKind,
            string commandString, byte[] bytes)
        {
            if (!_enabled) return;

            var separator = new string('=', 60);
            var subSeparator = new string('-', 60);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine(separator);
            sb.AppendLine($"[{timestamp}] [{deviceName}] >>> 发送命令 [{commandId}]");
            sb.AppendLine(subSeparator);
            sb.AppendLine($"指令类型: {commandKind}");
            sb.AppendLine($"指令字符串: {EscapeString(commandString)}");
            sb.AppendLine($"字节数组: [{BitConverter.ToString(bytes)}]");
            sb.AppendLine($"字节长度: {bytes.Length}");

            WriteToFile(sb.ToString());
        }

        /// <summary>
        /// 记录接收响应
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        /// <param name="elapsedMs">耗时（毫秒）</param>
        /// <param name="responseBytes">响应字节数组</param>
        /// <param name="responseText">响应文本</param>
        public static void LogReceive(string deviceName, long elapsedMs,
            byte[] responseBytes, string responseText)
        {
            if (!_enabled) return;

            var subSeparator = new string('-', 60);
            var separator = new string('=', 60);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            var sb = new StringBuilder();
            sb.AppendLine(subSeparator);
            sb.AppendLine($"[{timestamp}] [{deviceName}] <<< 接收响应 ({elapsedMs}ms)");
            sb.AppendLine(subSeparator);
            sb.AppendLine($"响应字节: [{BitConverter.ToString(responseBytes)}]");
            sb.AppendLine($"响应文本: \"{EscapeString(responseText)}\"");
            sb.AppendLine(separator);

            WriteToFile(sb.ToString());
        }

        /// <summary>
        /// 记录错误
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        /// <param name="message">错误消息</param>
        /// <param name="exception">异常对象（可选）</param>
        public static void LogError(string deviceName, string message, Exception? exception = null)
        {
            if (!_enabled) return;

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var sb = new StringBuilder();
            sb.AppendLine($"[{timestamp}] [{deviceName}] !!! 错误: {message}");
            if (exception != null)
            {
                sb.AppendLine($"异常类型: {exception.GetType().Name}");
                sb.AppendLine($"异常消息: {exception.Message}");
                if (exception.StackTrace != null)
                {
                    sb.AppendLine($"堆栈跟踪: {exception.StackTrace}");
                }
            }

            WriteToFile(sb.ToString());
        }

        /// <summary>
        /// 记录信息
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        /// <param name="message">信息消息</param>
        public static void LogInfo(string deviceName, string message)
        {
            if (!_enabled) return;

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] [{deviceName}] {message}";
            WriteToFile(logEntry);
        }

        /// <summary>
        /// 记录原始数据（用于调试）
        /// </summary>
        /// <param name="deviceName">设备名称</param>
        /// <param name="direction">方向标识（如 &gt;&gt;&gt; 或 &lt;&lt;&lt;）</param>
        /// <param name="data">原始数据</param>
        public static void LogRaw(string deviceName, string direction, byte[] data)
        {
            if (!_enabled) return;

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var hexDump = FormatHexDump(data);
            var sb = new StringBuilder();
            sb.AppendLine($"[{timestamp}] [{deviceName}] {direction} 原始数据 ({data.Length} 字节)");
            sb.AppendLine(hexDump);

            WriteToFile(sb.ToString());
        }

        /// <summary>
        /// 格式化十六进制转储
        /// </summary>
        private static string FormatHexDump(byte[] data)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < data.Length; i += 16)
            {
                // 偏移量
                sb.Append($"{i:X4}  ");

                // 十六进制
                for (int j = 0; j < 16; j++)
                {
                    if (i + j < data.Length)
                        sb.Append($"{data[i + j]:X2} ");
                    else
                        sb.Append("   ");

                    if (j == 7) sb.Append(" ");
                }

                sb.Append(" | ");

                // ASCII
                for (int j = 0; j < 16 && i + j < data.Length; j++)
                {
                    var b = data[i + j];
                    sb.Append(b >= 32 && b < 127 ? (char)b : '.');
                }

                sb.AppendLine();
            }
            return sb.ToString();
        }

        /// <summary>
        /// 转义特殊字符以便显示
        /// </summary>
        private static string EscapeString(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            return input
                .Replace("\\", "\\\\")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t")
                .Replace("\0", "\\0");
        }

        /// <summary>
        /// 写入日志文件
        /// </summary>
        private static void WriteToFile(string content)
        {
            try
            {
                lock (_lock)
                {
                    File.AppendAllText(LogFilePath, content + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch
            {
                // 日志写入失败不应影响正常业务
            }
        }

        /// <summary>
        /// 清空日志文件
        /// </summary>
        public static void ClearLog()
        {
            try
            {
                lock (_lock)
                {
                    if (File.Exists(LogFilePath))
                    {
                        File.WriteAllText(LogFilePath, string.Empty, Encoding.UTF8);
                    }
                }
            }
            catch
            {
                // 忽略清理错误
            }
        }

        /// <summary>
        /// 获取日志文件大小（字节）
        /// </summary>
        public static long GetLogFileSize()
        {
            try
            {
                if (File.Exists(LogFilePath))
                {
                    return new FileInfo(LogFilePath).Length;
                }
            }
            catch
            {
                // 忽略查询错误
            }
            return 0;
        }
    }
}
