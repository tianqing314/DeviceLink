using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace DeviceLink.Transport
{
    /// <summary>
    /// 蓝牙传输实现 —— 封装 InTheHand.Net.Bluetooth 库
    /// 支持经典蓝牙 RFCOMM/SPP 协议
    /// </summary>
    public class BluetoothTransport : IPhysicalTransport
    {
        private readonly BluetoothOptions _options;
        private readonly ILogger<BluetoothTransport>? _logger;
        private BluetoothClient? _bluetoothClient;
        private NetworkStream? _networkStream;
        private bool _isConnected;
        private readonly object _lock = new object();

        /// <summary>
        /// 初始化蓝牙传输
        /// </summary>
        /// <param name="options">蓝牙配置选项</param>
        /// <param name="logger">日志记录器</param>
        public BluetoothTransport(BluetoothOptions options, ILogger<BluetoothTransport>? logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        /// <inheritdoc/>
        public string Name => $"Bluetooth({_options.DeviceAddress})";

        /// <inheritdoc/>
        public bool IsOpen
        {
            get
            {
                lock (_lock)
                {
                    return _isConnected && _bluetoothClient?.Connected == true;
                }
            }
        }

        /// <inheritdoc/>
        public async Task ConnectAsync(CancellationToken ct = default)
        {
            if (IsOpen) return;

            try
            {
                _logger?.LogInformation("正在连接蓝牙设备 {DeviceAddress}", _options.DeviceAddress);

                // 检查蓝牙是否可用
                if (!IsBluetoothAvailable())
                {
                    throw new ConnectionException("蓝牙适配器不可用，请检查系统是否安装了蓝牙适配器并已启用");
                }

                // 创建蓝牙客户端
                _bluetoothClient = new BluetoothClient();

                // 设置连接超时
                var connectTask = Task.Run(() =>
                {
                    // 解析设备地址
                    if (BluetoothAddress.TryParse(_options.DeviceAddress, out var address))
                    {
                        // 使用MAC地址连接
                        _bluetoothClient.Connect(address, _options.ServiceUuid);
                    }
                    else
                    {
                        // 使用设备名称搜索并连接
                        var devices = _bluetoothClient.DiscoverDevices().ToArray();
                        var device = devices.FirstOrDefault(d => d.DeviceName == _options.DeviceAddress);
                        if (device == null)
                        {
                            throw new ConnectionException($"未找到蓝牙设备: {_options.DeviceAddress}");
                        }
                        _bluetoothClient.Connect(device.DeviceAddress, _options.ServiceUuid);
                    }
                });

                // 等待连接完成或超时
                if (await Task.WhenAny(connectTask, Task.Delay(_options.ConnectTimeoutMs, ct)) != connectTask)
                {
                    throw new ConnectionException($"连接蓝牙设备超时: {_options.DeviceAddress}");
                }

                // 获取网络流
                _networkStream = _bluetoothClient.GetStream();
                _isConnected = true;

                _logger?.LogInformation("蓝牙设备 {DeviceAddress} 已连接", _options.DeviceAddress);
            }
            catch (Exception ex) when (ex is not ConnectionException)
            {
                _logger?.LogError(ex, "连接蓝牙设备 {DeviceAddress} 失败", _options.DeviceAddress);
                throw new ConnectionException($"连接蓝牙设备失败: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public Task CloseAsync()
        {
            lock (_lock)
            {
                if (_bluetoothClient != null)
                {
                    try
                    {
                        _networkStream?.Close();
                        _bluetoothClient.Close();
                        _logger?.LogInformation("蓝牙设备 {DeviceAddress} 已断开", _options.DeviceAddress);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "断开蓝牙设备 {DeviceAddress} 时发生异常", _options.DeviceAddress);
                    }
                    finally
                    {
                        _networkStream?.Dispose();
                        _bluetoothClient.Dispose();
                        _networkStream = null;
                        _bluetoothClient = null;
                        _isConnected = false;
                    }
                }
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct = default)
        {
            if (!IsOpen || _networkStream == null)
                return 0;

            try
            {
                int read = await _networkStream.ReadAsync(buffer, offset, count, ct);
                _logger?.LogDebug("从蓝牙设备 {DeviceAddress} 读取了 {Count} 字节", _options.DeviceAddress, read);
                return read;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "从蓝牙设备 {DeviceAddress} 读取数据失败", _options.DeviceAddress);
                throw new TransportException($"从蓝牙设备读取数据失败: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task WriteAsync(byte[] data, int offset, int count, CancellationToken ct = default)
        {
            if (!IsOpen || _networkStream == null)
                return;

            try
            {
                await _networkStream.WriteAsync(data, offset, count, ct);
                await _networkStream.FlushAsync(ct);
                _logger?.LogDebug("向蓝牙设备 {DeviceAddress} 写入了 {Count} 字节", _options.DeviceAddress, count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "向蓝牙设备 {DeviceAddress} 写入数据失败", _options.DeviceAddress);
                throw new TransportException($"向蓝牙设备写入数据失败: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public Task ClearReceiveBufferAsync(CancellationToken ct = default)
        {
            if (!IsOpen || _networkStream == null)
                return Task.CompletedTask;

            try
            {
                // 清空接收缓冲区
                while (_networkStream.DataAvailable)
                {
                    _networkStream.ReadByte();
                }
                _logger?.LogDebug("已清空蓝牙设备 {DeviceAddress} 接收缓冲区", _options.DeviceAddress);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "清空蓝牙设备 {DeviceAddress} 接收缓冲区时发生异常", _options.DeviceAddress);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// 检查蓝牙是否可用
        /// </summary>
        /// <returns>如果蓝牙可用返回true，否则返回false</returns>
        private static bool IsBluetoothAvailable()
        {
            try
            {
                // 尝试创建 BluetoothClient 来检查蓝牙是否可用
                using var client = new BluetoothClient();
                return true;
            }
            catch (PlatformNotSupportedException)
            {
                return false;
            }
            catch
            {
                // 其他异常也认为蓝牙不可用
                return false;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            CloseAsync().Wait();
        }
    }
}