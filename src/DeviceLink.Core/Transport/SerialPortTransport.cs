using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceLink.Core.Transport
{
    /// <summary>
    /// 串口传输实现 —— 封装 System.IO.Ports.SerialPort
    /// </summary>
    public class SerialPortTransport : IByteTransport
    {
        private SerialPort? _serialPort;
        private readonly SerialPortOptions _options;

        public SerialPortTransport(SerialPortOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public SerialPortTransport(string portName, int baudRate = 9600,
            int dataBits = 8, StopBits stopBits = StopBits.One, Parity parity = Parity.None)
            : this(new SerialPortOptions
            {
                PortName = portName,
                BaudRate = baudRate,
                DataBits = dataBits,
                StopBits = stopBits,
                Parity = parity
            })
        { }

        public string Name => $"{_options.PortName}@{_options.BaudRate}";

        public bool IsOpen => _serialPort?.IsOpen ?? false;

        public Task ConnectAsync(CancellationToken ct = default)
        {
            if (IsOpen) return Task.CompletedTask;

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
            return Task.CompletedTask;
        }

        public Task CloseAsync()
        {
            if (_serialPort != null)
            {
                try { _serialPort.Close(); } catch { /* 忽略关闭异常 */ }
                try { _serialPort.Dispose(); } catch { }
                _serialPort = null;
            }
            return Task.CompletedTask;
        }

        public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct = default)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
                return Task.FromResult(0);

            int available = Math.Min(_serialPort.BytesToRead, count);
            if (available == 0)
                return Task.FromResult(0);

            int read = _serialPort.Read(buffer, offset, available);
            return Task.FromResult(read);
        }

        public Task WriteAsync(byte[] data, int offset, int count, CancellationToken ct = default)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
                return Task.CompletedTask;

            _serialPort.Write(data, offset, count);
            return Task.CompletedTask;
        }

        public Task ClearReceiveBufferAsync(CancellationToken ct = default)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.DiscardInBuffer();
            }
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            CloseAsync();
        }
    }

    /// <summary>
    /// 串口配置选项
    /// </summary>
    public class SerialPortOptions
    {
        public string PortName { get; set; } = "COM1";
        public int BaudRate { get; set; } = 9600;
        public int DataBits { get; set; } = 8;
        public StopBits StopBits { get; set; } = StopBits.One;
        public Parity Parity { get; set; } = Parity.None;
        public int ReadBufferSize { get; set; } = 4096;
        public int WriteBufferSize { get; set; } = 2048;
    }
}
