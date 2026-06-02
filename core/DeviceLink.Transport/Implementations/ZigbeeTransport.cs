using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DeviceLink.Transport
{
    /// <summary>
    /// Zigbee传输层实现 —— 封装串口传输和Zigbee模块配置
    /// 支持XBee、CC2530、ZM32等不同厂商的Zigbee模块
    /// </summary>
    public class ZigbeeTransport : IPhysicalTransport
    {
        private readonly SerialPortTransport _serialPort;
        private readonly IZigbeeModule _module;
        private readonly ZigbeeOptions _options;
        private readonly ILogger<ZigbeeTransport>? _logger;
        private bool _isConfigured;

        /// <summary>
        /// 初始化Zigbee传输层
        /// </summary>
        /// <param name="options">Zigbee配置选项</param>
        /// <param name="logger">日志记录器</param>
        public ZigbeeTransport(ZigbeeOptions options, ILogger<ZigbeeTransport>? logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;

            // 创建串口传输层
            _serialPort = new SerialPortTransport(options);

            // 根据模块类型创建对应的模块实例
            _module = CreateModule(options.ModuleType);
        }

        /// <summary>
        /// 初始化Zigbee传输层（使用自定义模块）
        /// </summary>
        /// <param name="options">Zigbee配置选项</param>
        /// <param name="module">自定义Zigbee模块实例</param>
        /// <param name="logger">日志记录器</param>
        public ZigbeeTransport(ZigbeeOptions options, IZigbeeModule module, ILogger<ZigbeeTransport>? logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _module = module ?? throw new ArgumentNullException(nameof(module));
            _logger = logger;

            // 创建串口传输层
            _serialPort = new SerialPortTransport(options);
        }

        /// <inheritdoc/>
        public string Name => $"Zigbee({_module.Name}):{_options.PortName}";

        /// <inheritdoc/>
        public bool IsOpen => _serialPort.IsOpen;

        /// <inheritdoc/>
        public async Task ConnectAsync(CancellationToken ct = default)
        {
            try
            {
                // 1. 打开串口连接
                await _serialPort.ConnectAsync(ct);
                _logger?.LogInformation("Zigbee串口 {PortName} 已连接", _options.PortName);

                // 2. 配置Zigbee模块
                if (!_isConfigured)
                {
                    await ConfigureModuleAsync(ct);
                    _isConfigured = true;
                }

                _logger?.LogInformation("Zigbee模块 {ModuleName} 已连接并配置完成", _module.Name);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Zigbee连接失败");
                throw new TransportException($"Zigbee连接失败: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task CloseAsync()
        {
            try
            {
                await _serialPort.CloseAsync();
                _logger?.LogInformation("Zigbee连接已关闭");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "关闭Zigbee连接时发生异常");
            }
        }

        /// <inheritdoc/>
        public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct = default)
        {
            return _serialPort.ReadAsync(buffer, offset, count, ct);
        }

        /// <inheritdoc/>
        public Task WriteAsync(byte[] data, int offset, int count, CancellationToken ct = default)
        {
            // 使用模块构建数据帧
            var frameData = new byte[count];
            Array.Copy(data, offset, frameData, 0, count);
            var frame = _module.BuildDataFrame(frameData);
            
            return _serialPort.WriteAsync(frame, 0, frame.Length, ct);
        }

        /// <inheritdoc/>
        public Task ClearReceiveBufferAsync(CancellationToken ct = default)
        {
            return _serialPort.ClearReceiveBufferAsync(ct);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            CloseAsync().Wait();
            _serialPort.Dispose();
        }

        /// <summary>
        /// 配置Zigbee模块（自动配置，用户无感）
        /// </summary>
        private async Task ConfigureModuleAsync(CancellationToken ct)
        {
            try
            {
                // 进入命令模式
                await _module.EnterCommandModeAsync(_serialPort, ct);

                // 配置PAN ID
                if (_options.PanId != 0)
                {
                    await _module.ConfigurePanIdAsync(_serialPort, _options.PanId, ct);
                }

                // 配置信道
                if (_options.Channel != 0)
                {
                    await _module.ConfigureChannelAsync(_serialPort, _options.Channel, ct);
                }

                // 配置目标地址
                if (_options.DestinationAddress != 0)
                {
                    await _module.ConfigureDestinationAsync(_serialPort, _options.DestinationAddress, ct);
                }

                // ZM32特有配置
                if (_options.ModuleType == ZigbeeModuleType.ZM32)
                {
                    await ConfigureZM32Async(ct);
                }

                // 退出命令模式
                await _module.ExitCommandModeAsync(_serialPort, ct);

                _logger?.LogInformation("Zigbee模块配置完成: PanId={PanId}, Channel={Channel}", 
                    _options.PanId.ToString("X4"), _options.Channel);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Zigbee模块配置失败");
                throw new TransportException($"Zigbee模块配置失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 配置ZM32特有参数
        /// </summary>
        private async Task ConfigureZM32Async(CancellationToken ct)
        {
            // 配置目标网络地址
            if (_options.ZM32_TargetNetworkAddress != 0x0000)
            {
                await ConfigureZM32TargetAddressAsync(_options.ZM32_TargetNetworkAddress, ct);
            }

            // 配置发送模式
            if (_options.ZM32_SendMode != 0x01) // 非默认单播模式
            {
                await ConfigureZM32SendModeAsync(_options.ZM32_SendMode, ct);
            }

            // 配置自组网
            if (_options.ZM32_EnableAutoNetwork)
            {
                await ConfigureZM32AutoNetworkAsync(ct);
            }

            _logger?.LogInformation("ZM32特有配置完成: TargetAddr={TargetAddr}, SendMode={SendMode}", 
                _options.ZM32_TargetNetworkAddress.ToString("X4"), _options.ZM32_SendMode.ToString("X2"));
        }

        /// <summary>
        /// 配置ZM32目标网络地址
        /// 协议: DE DF EF D2 + 网络地址(2字节)
        /// </summary>
        private async Task ConfigureZM32TargetAddressAsync(ushort networkAddress, CancellationToken ct)
        {
            var command = new byte[]
            {
                0xDE, 0xDF, 0xEF, // 协议标志
                0xD2,             // 命令标识符：修改目标网络地址
                (byte)(networkAddress >> 8),   // 网络地址高字节
                (byte)(networkAddress & 0xFF)  // 网络地址低字节
            };

            await _serialPort.WriteAsync(command, 0, command.Length, ct);
            await Task.Delay(100, ct); // 等待响应

            // 读取响应
            var response = new byte[6];
            var bytesRead = await _serialPort.ReadAsync(response, 0, response.Length, ct);
            
            if (bytesRead >= 5 && response[3] == 0xD2 && response[4] == 0x00)
            {
                _logger?.LogDebug("ZM32目标网络地址配置成功: {Address}", networkAddress.ToString("X4"));
            }
            else
            {
                _logger?.LogWarning("ZM32目标网络地址配置可能失败: {Response}", BitConverter.ToString(response, 0, bytesRead));
            }
        }

        /// <summary>
        /// 配置ZM32发送模式
        /// 协议: DE DF EF D9 + 发送模式(1字节)
        /// </summary>
        private async Task ConfigureZM32SendModeAsync(byte sendMode, CancellationToken ct)
        {
            var command = new byte[]
            {
                0xDE, 0xDF, 0xEF, // 协议标志
                0xD9,             // 命令标识符：设置发送模式
                sendMode          // 发送模式
            };

            await _serialPort.WriteAsync(command, 0, command.Length, ct);
            await Task.Delay(100, ct); // 等待响应

            // 读取响应
            var response = new byte[5];
            var bytesRead = await _serialPort.ReadAsync(response, 0, response.Length, ct);
            
            if (bytesRead >= 5 && response[3] == 0xD9 && response[4] == 0x00)
            {
                _logger?.LogDebug("ZM32发送模式配置成功: {Mode}", sendMode.ToString("X2"));
            }
            else
            {
                _logger?.LogWarning("ZM32发送模式配置可能失败: {Response}", BitConverter.ToString(response, 0, bytesRead));
            }
        }

        /// <summary>
        /// 配置ZM32自组网功能
        /// 协议: AB BC CD 27 + R/W + 自组网使能 + 设备类型 + AA
        /// </summary>
        private async Task ConfigureZM32AutoNetworkAsync(CancellationToken ct)
        {
            var command = new byte[]
            {
                0xAB, 0xBC, 0xCD, // 协议标志（永久配置）
                0x27,             // 命令标识符：配置自组网
                0x01,             // R/W: 1=写参数
                0x01,             // 自组网使能: 1=普通自组网
                _options.ZM32_DeviceType, // 设备类型
                0xAA              // 帧尾
            };

            await _serialPort.WriteAsync(command, 0, command.Length, ct);
            await Task.Delay(200, ct); // 等待响应

            // 读取响应
            var response = new byte[7];
            var bytesRead = await _serialPort.ReadAsync(response, 0, response.Length, ct);
            
            if (bytesRead >= 7 && response[3] == 0x27 && response[6] == 0x00)
            {
                _logger?.LogDebug("ZM32自组网配置成功: DeviceType={DeviceType}", _options.ZM32_DeviceType);
            }
            else
            {
                _logger?.LogWarning("ZM32自组网配置可能失败: {Response}", BitConverter.ToString(response, 0, bytesRead));
            }
        }

        /// <summary>
        /// 根据模块类型创建模块实例
        /// </summary>
        private IZigbeeModule CreateModule(ZigbeeModuleType moduleType)
        {
            return moduleType switch
            {
                ZigbeeModuleType.ZM32 => new ZM32Module(_options),
                _ => throw new NotSupportedException($"不支持的Zigbee模块类型: {moduleType}")
            };
        }
    }
}
