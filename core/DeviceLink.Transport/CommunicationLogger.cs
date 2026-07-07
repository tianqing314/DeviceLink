using System;
using System.IO;
using System.Text;
using System.Threading;

namespace DeviceLink.Transport
{
    /// <summary>
    /// 通信日志记录器 —— 记录完整的通信链路日志到 HTML 文件
    /// </summary>
    public static class CommunicationLogger
    {
        private static readonly object _lock = new object();
        private static string _logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        private static string _logFileName = "communication.html";
        private static bool _enabled = true;
        private static bool _headerWritten = false;

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
        /// 写入 HTML 头部
        /// </summary>
        private static void EnsureHtmlHeader()
        {
            if (_headerWritten) return;

            var header = @"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
<title>通信日志</title>
<style>
body { font-family: 'Consolas', 'Monaco', monospace; background: #1e1e1e; color: #d4d4d4; padding: 20px; }
.separator { color: #888; margin: 10px 0; }
.log-entry { margin: 5px 0; padding: 8px; border-radius: 4px; }
.send { background: #1a3a1a; border-left: 4px solid #4CAF50; }
.receive { background: #3a1a1a; border-left: 4px solid #f44336; }
.info { background: #1a2a3a; border-left: 4px solid #2196F3; }
.error { background: #3a1a1a; border-left: 4px solid #ff9800; }
.label { font-weight: bold; }
.send .label { color: #4CAF50; }
.receive .label { color: #f44336; }
.info .label { color: #2196F3; }
.error .label { color: #ff9800; }
.timestamp { color: #888; font-size: 0.9em; }
.device { color: #9C27B0; }
.data { color: #fff; background: #2d2d2d; padding: 4px 8px; display: inline-block; margin: 4px 0; border-radius: 2px; }
.divider { border-top: 2px dashed #444; margin: 15px 0; }
.command-separator { 
  background: #333; 
  color: #4CAF50; 
  padding: 8px 15px; 
  margin: 15px 0 5px 0; 
  font-weight: bold; 
  font-size: 1.1em;
  border-radius: 4px;
  letter-spacing: 2px;
}
.command-separator.end {
  margin: 5px 0 15px 0;
  color: #f44336;
}
</style>
</head>
<body>
";
            File.WriteAllText(LogFilePath, header, Encoding.UTF8);
            _headerWritten = true;
        }

        /// <summary>
        /// 记录发送命令
        /// </summary>
        public static void LogSend(string deviceName, string commandId, string commandKind,
            string commandString, byte[] bytes)
        {
            if (!_enabled) return;

            var separatorText = $"============发送指令 {commandId}===========";
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var sb = new StringBuilder();
            sb.AppendLine($"<div class='command-separator'>{EscapeHtml(separatorText)}</div>");
            sb.AppendLine("<div class='log-entry send'>");
            sb.AppendLine($"  <div><span class='label'>功能：</span>发送指令</div>");
            sb.AppendLine($"  <div><span class='label'>设备：</span><span class='device'>{EscapeHtml(deviceName)}</span></div>");
            sb.AppendLine($"  <div><span class='label'>时间：</span><span class='timestamp'>{timestamp}</span></div>");
            sb.AppendLine($"  <div><span class='label'>命令ID：</span>{EscapeHtml(commandId)}</div>");
            sb.AppendLine($"  <div><span class='label'>指令类型：</span>{EscapeHtml(commandKind)}</div>");
            sb.AppendLine($"  <div><span class='label'>指令字符串：</span><span class='data'>{EscapeHtml(commandString)}</span></div>");
            sb.AppendLine($"  <div><span class='label'>字节数组：</span><span class='data'>{BitConverter.ToString(bytes).Replace("-", " ")}</span></div>");
            sb.AppendLine($"  <div><span class='label'>字节长度：</span>{bytes.Length}</div>");
            sb.AppendLine("</div>");

            WriteToFile(sb.ToString());
        }

        /// <summary>
        /// 记录接收响应
        /// </summary>
        public static void LogReceive(string deviceName, long elapsedMs,
            byte[] responseBytes, string responseText)
        {
            if (!_enabled) return;

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var sb = new StringBuilder();
            sb.AppendLine("<div class='log-entry receive'>");
            sb.AppendLine($"  <div><span class='label'>功能：</span>接收指令</div>");
            sb.AppendLine($"  <div><span class='label'>设备：</span><span class='device'>{EscapeHtml(deviceName)}</span></div>");
            sb.AppendLine($"  <div><span class='label'>时间：</span><span class='timestamp'>{timestamp}</span></div>");
            sb.AppendLine($"  <div><span class='label'>耗时：</span>{elapsedMs} ms</div>");
            sb.AppendLine($"  <div><span class='label'>响应字节：</span><span class='data'>{BitConverter.ToString(responseBytes).Replace("-", " ")}</span></div>");
            sb.AppendLine($"  <div><span class='label'>响应文本：</span><span class='data'>{EscapeHtml(responseText)}</span></div>");
            sb.AppendLine("</div>");
            sb.AppendLine("<div class='command-separator end'>============接收指令完成===========</div>");

            WriteToFile(sb.ToString());
        }

        /// <summary>
        /// 记录错误
        /// </summary>
        public static void LogError(string deviceName, string message, Exception? exception = null)
        {
            if (!_enabled) return;

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var sb = new StringBuilder();
            sb.AppendLine("<div class='log-entry error'>");
            sb.AppendLine($"  <div><span class='label'>功能：</span>错误</div>");
            sb.AppendLine($"  <div><span class='label'>设备：</span><span class='device'>{EscapeHtml(deviceName)}</span></div>");
            sb.AppendLine($"  <div><span class='label'>时间：</span><span class='timestamp'>{timestamp}</span></div>");
            sb.AppendLine($"  <div><span class='label'>错误信息：</span>{EscapeHtml(message)}</div>");
            if (exception != null)
            {
                sb.AppendLine($"  <div><span class='label'>异常类型：</span>{EscapeHtml(exception.GetType().Name)}</div>");
                sb.AppendLine($"  <div><span class='label'>异常消息：</span>{EscapeHtml(exception.Message)}</div>");
                if (exception.StackTrace != null)
                {
                    sb.AppendLine($"  <div><span class='label'>堆栈跟踪：</span><pre>{EscapeHtml(exception.StackTrace)}</pre></div>");
                }
            }
            sb.AppendLine("</div>");

            WriteToFile(sb.ToString());
        }

        /// <summary>
        /// 记录信息
        /// </summary>
        public static void LogInfo(string deviceName, string message)
        {
            if (!_enabled) return;

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var sb = new StringBuilder();
            sb.AppendLine("<div class='log-entry info'>");
            sb.AppendLine($"  <div><span class='label'>信息：</span>{EscapeHtml(message)}</div>");
            sb.AppendLine($"  <div><span class='label'>设备：</span><span class='device'>{EscapeHtml(deviceName)}</span></div>");
            sb.AppendLine($"  <div><span class='label'>时间：</span><span class='timestamp'>{timestamp}</span></div>");
            sb.AppendLine("</div>");

            WriteToFile(sb.ToString());
        }

        /// <summary>
        /// 记录原始数据（用于调试）
        /// </summary>
        public static void LogRaw(string deviceName, string direction, byte[] data)
        {
            if (!_enabled) return;

            // 去掉 >>> <<< 前缀，保留有意义的功能描述
            var cleanDirection = direction
                .Replace(">>> ", "")
                .Replace("<<< ", "")
                .Trim();

            // 判断是发送还是接收
            var isSend = direction.Contains(">>>");
            var cssClass = isSend ? "send" : "receive";
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            
            // 生成分隔线：============{功能}===========
            var separatorText = $"============{cleanDirection}===========";
            
            var sb = new StringBuilder();
            
            // 发送指令前添加开始分隔线
            if (isSend)
            {
                sb.AppendLine($"<div class='command-separator'>{EscapeHtml(separatorText)}</div>");
            }
            
            sb.AppendLine($"<div class='log-entry {cssClass}'>");
            sb.AppendLine($"  <div><span class='label'>功能：</span>{EscapeHtml(cleanDirection)}</div>");
            sb.AppendLine($"  <div><span class='label'>设备：</span><span class='device'>{EscapeHtml(deviceName)}</span></div>");
            sb.AppendLine($"  <div><span class='label'>时间：</span><span class='timestamp'>{timestamp}</span></div>");
            sb.AppendLine($"  <div><span class='label'>字节数：</span>{data.Length}</div>");
            sb.AppendLine($"  <div><span class='label'>数据：</span><span class='data'>{BitConverter.ToString(data).Replace("-", " ")}</span></div>");
            sb.AppendLine("</div>");
            
            // 接收指令后添加结束分隔线
            if (!isSend)
            {
                sb.AppendLine($"<div class='command-separator end'>{EscapeHtml(separatorText)}</div>");
            }

            WriteToFile(sb.ToString());
        }

        /// <summary>
        /// 转义 HTML 特殊字符
        /// </summary>
        private static string EscapeHtml(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            return input
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
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
                    EnsureHtmlHeader();
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
                    _headerWritten = false;
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
