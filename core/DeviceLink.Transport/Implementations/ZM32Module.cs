using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DeviceLink.Transport
{
    /// <summary>
    /// 周立功 ZM32模块实现
    /// 支持ZM32系列Zigbee模块的AT指令配置和透明传输
    /// </summary>
    public class ZM32Module : IZigbeeModule
    {
        private readonly ILogger<ZM32Module>? _logger;
        private readonly ZigbeeOptions _options;

        /// <summary>
        /// 初始化ZM32模块
        /// </summary>
        /// <param name="options">Zigbee配置选项</param>
        /// <param name="logger">日志记录器</param>
        public ZM32Module(ZigbeeOptions options, ILogger<ZM32Module>? logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        /// <inheritdoc/>
        public string Name => "ZM32";

        /// <inheritdoc/>
        public async Task EnterCommandModeAsync(IPhysicalTransport transport, CancellationToken ct = default)
        {
            if (transport == null) throw new ArgumentNullException(nameof(transport));

            try
            {
                // ZM32进入命令模式：发送"+++"
                var guardTime = Encoding.ASCII.GetBytes("+++");
                await transport.WriteAsync(guardTime, 0, guardTime.Length, ct);
                
                // 等待保护时间
                await Task.Delay(_options.GuardTimeMs, ct);

                // 读取响应
                var response = await ReadResponseAsync(transport, ct);
                if (!response.Contains("OK"))
                {
                    throw new TransportException("ZM32进入命令模式失败：未收到OK响应");
                }

                _logger?.LogInformation("ZM32已进入命令模式");
            }
            catch (Exception ex) when (ex is not TransportException)
            {
                _logger?.LogError(ex, "ZM32进入命令模式失败");
                throw new TransportException($"ZM32进入命令模式失败: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task ExitCommandModeAsync(IPhysicalTransport transport, CancellationToken ct = default)
        {
            if (transport == null) throw new ArgumentNullException(nameof(transport));

            try
            {
                // 发送AT+EXIT退出命令模式
                var command = Encoding.ASCII.GetBytes("AT+EXIT\r\n");
                await transport.WriteAsync(command, 0, command.Length, ct);
                
                var response = await ReadResponseAsync(transport, ct);
                if (!response.Contains("OK"))
                {
                    throw new TransportException("ZM32退出命令模式失败");
                }

                _logger?.LogInformation("ZM32已退出命令模式");
            }
            catch (Exception ex) when (ex is not TransportException)
            {
                _logger?.LogError(ex, "ZM32退出命令模式失败");
                throw new TransportException($"ZM32退出命令模式失败: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task ConfigurePanIdAsync(IPhysicalTransport transport, ushort panId, CancellationToken ct = default)
        {
            if (transport == null) throw new ArgumentNullException(nameof(transport));

            try
            {
                // ZM32 AT指令格式: AT+PANID=xxxx
                var command = Encoding.ASCII.GetBytes($"AT+PANID={panId:X4}\r\n");
                await transport.WriteAsync(command, 0, command.Length, ct);
                
                var response = await ReadResponseAsync(transport, ct);
                if (!response.Contains("OK"))
                {
                    throw new TransportException($"ZM32配置PAN ID失败: {response}");
                }

                _logger?.LogInformation("ZM32 PAN ID已设置为: {PanId}", panId.ToString("X4"));
            }
            catch (Exception ex) when (ex is not TransportException)
            {
                _logger?.LogError(ex, "ZM32配置PAN ID失败");
                throw new TransportException($"ZM32配置PAN ID失败: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task ConfigureChannelAsync(IPhysicalTransport transport, byte channel, CancellationToken ct = default)
        {
            if (transport == null) throw new ArgumentNullException(nameof(transport));

            try
            {
                // ZM32 AT指令格式: AT+CHANNEL=xx
                var command = Encoding.ASCII.GetBytes($"AT+CHANNEL={channel:X2}\r\n");
                await transport.WriteAsync(command, 0, command.Length, ct);
                
                var response = await ReadResponseAsync(transport, ct);
                if (!response.Contains("OK"))
                {
                    throw new TransportException($"ZM32配置信道失败: {response}");
                }

                _logger?.LogInformation("ZM32信道已设置为: {Channel}", channel);
            }
            catch (Exception ex) when (ex is not TransportException)
            {
                _logger?.LogError(ex, "ZM32配置信道失败");
                throw new TransportException($"ZM32配置信道失败: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task ConfigureDestinationAsync(IPhysicalTransport transport, ulong destAddress, CancellationToken ct = default)
        {
            if (transport == null) throw new ArgumentNullException(nameof(transport));

            try
            {
                // ZM32 AT指令格式: AT+DESTADDR=xxxxxxxxxxxxxxxx
                var command = Encoding.ASCII.GetBytes($"AT+DESTADDR={destAddress:X16}\r\n");
                await transport.WriteAsync(command, 0, command.Length, ct);
                
                var response = await ReadResponseAsync(transport, ct);
                if (!response.Contains("OK"))
                {
                    throw new TransportException($"ZM32配置目标地址失败: {response}");
                }

                _logger?.LogInformation("ZM32目标地址已设置为: {Address}", destAddress.ToString("X16"));
            }
            catch (Exception ex) when (ex is not TransportException)
            {
                _logger?.LogError(ex, "ZM32配置目标地址失败");
                throw new TransportException($"ZM32配置目标地址失败: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public byte[] BuildDataFrame(byte[] data, string? destination = null)
        {
            // ZM32使用完全透明传输，直接返回原始数据
            return data;
        }

        /// <inheritdoc/>
        public bool TryParseDataFrame(byte[] frame, out byte[] data, out string? source)
        {
            // ZM32使用完全透明传输，直接返回原始数据
            data = frame;
            source = null;
            return true;
        }

        /// <summary>
        /// 读取串口响应
        /// </summary>
        private async Task<string> ReadResponseAsync(IPhysicalTransport transport, CancellationToken ct)
        {
            var buffer = new byte[1024];
            var response = new StringBuilder();
            var startTime = DateTime.UtcNow;

            while ((DateTime.UtcNow - startTime).TotalMilliseconds < _options.CommandTimeoutMs)
            {
                var bytesRead = await transport.ReadAsync(buffer, 0, buffer.Length, ct);
                if (bytesRead > 0)
                {
                    response.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
                    
                    // 检查是否收到完整响应
                    if (response.ToString().Contains("\r\n") || response.ToString().Contains("OK"))
                    {
                        break;
                    }
                }
                await Task.Delay(10, ct);
            }

            return response.ToString().Trim();
        }
    }
}
