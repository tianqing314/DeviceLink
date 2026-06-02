using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DeviceLink.Transport
{
    /// <summary>
    /// 串口传输实现 —— 封装 System.IO.Ports.SerialPort
    /// </summary>
    public class SerialPortTransport : IPhysicalTransport
    {
        private SerialPort? _serialPort;
        private readonly SerialPortOptions _options;
        private readonly ILogger<SerialPortTransport>? _logger;

        /// <summary>
        /// 初始化串口传输
        /// </summary>
        /// <param name="options">串口配置选项</param>
        /// <param name="logger">日志记录器</param>
        public SerialPortTransport(SerialPortOptions options, ILogger<SerialPortTransport>? logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        /// <summary>
        /// 初始化串口传输
        /// </summary>
        /// <param name="portName">串口名称</param>
        /// <param name="baudRate">波特率</param>
        /// <param name="dataBits">数据位</param>
        /// <param name="stopBits">停止位</param>
        /// <param name="parity">校验位</param>
        /// <param name="logger">日志记录器</param>
        public SerialPortTransport(string portName, int baudRate = 9600,
            int dataBits = 8, StopBits stopBits = StopBits.One, Parity parity = Parity.None,
            ILogger<SerialPortTransport>? logger = null)
            : this(new SerialPortOptions
            {
                PortName = portName,
                BaudRate = baudRate,
                DataBits = dataBits,
                StopBits = stopBits,
                Parity = parity
            }, logger)
        { }

        /// <inheritdoc/>
        public string Name => $"{_options.PortName}@{_options.BaudRate}";

        /// <inheritdoc/>
        public bool IsOpen => _serialPort?.IsOpen ?? false;

        /// <inheritdoc/>
        public Task ConnectAsync(CancellationToken ct = default)
        {
            if (IsOpen) return Task.CompletedTask;

            try
            {
                _serialPort = new SerialPort(
                    _options.PortName,
                    _options.BaudRate,
                    _options.Parity,
                    _options.DataBits,
                    _options.StopBits)
                {
                    ReadTimeout = SerialPort.InfiniteTimeout,
                    WriteTimeout = SerialPort.InfiniteTimeout,
                    ReadBufferSize = _options.ReadBufferSize > 0 ? _options.ReadBufferSize : 4096,
                    WriteBufferSize = _options.WriteBufferSize > 0 ? _options.WriteBufferSize : 2048
                };

                _serialPort.Open();
                _logger?.LogInformation("串口 {PortName} 已连接", Name);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "连接串口 {PortName} 失败", Name);
                throw new ConnectionException($"连接串口 {Name} 失败: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public Task CloseAsync()
        {
            if (_serialPort != null)
            {
                try
                {
                    _serialPort.Close();
                    _logger?.LogInformation("串口 {PortName} 已关闭", Name);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "关闭串口 {PortName} 时发生异常", Name);
                }
                finally
                {
                    try { _serialPort.Dispose(); } catch { }
                    _serialPort = null;
                }
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct = default)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
                return Task.FromResult(0);

            try
            {
                int available = Math.Min(_serialPort.BytesToRead, count);
                if (available == 0)
                    return Task.FromResult(0);

                int read = _serialPort.Read(buffer, offset, available);
                _logger?.LogDebug("从串口 {PortName} 读取了 {Count} 字节", Name, read);
                return Task.FromResult(read);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "从串口 {PortName} 读取数据失败", Name);
                throw new TransportException($"从串口 {Name} 读取数据失败: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public Task WriteAsync(byte[] data, int offset, int count, CancellationToken ct = default)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
                return Task.CompletedTask;

            try
            {
                _serialPort.Write(data, offset, count);
                _logger?.LogDebug("向串口 {PortName} 写入了 {Count} 字节", Name, count);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "向串口 {PortName} 写入数据失败", Name);
                throw new TransportException($"向串口 {Name} 写入数据失败: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public Task ClearReceiveBufferAsync(CancellationToken ct = default)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                try
                {
                    _serialPort.DiscardInBuffer();
                    _logger?.LogDebug("已清空串口 {PortName} 接收缓冲区", Name);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "清空串口 {PortName} 接收缓冲区时发生异常", Name);
                }
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            CloseAsync().Wait();
        }
    }

}
