using DeviceLink.Device.PS02;
using DeviceLink.DeviceBase;
using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace DeviceLink.Tests.PS02
{
    /// <summary>
    /// PS02 设备串口通信测试
    ///
    /// 使用真实串口连接 PS02 设备进行测试。
    /// 需要实际硬件连接才能运行。
    ///
    /// 使用方法：
    /// 1. 连接 PS02 设备到串口（如 COM3）
    /// 2. 设置环境变量 PS02_SERIAL_PORT 指定串口号（可选，默认 COM3）
    /// 3. 运行测试：dotnet test --filter "Category=Serial"
    ///
    /// 注意：这些测试需要实际设备连接，可能因设备状态而失败。
    /// </summary>
    public class PS02SerialTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly string _portName;
        private readonly int _baudRate;
        private readonly byte _slaveAddress;
        private PS02Base? _device;
        private bool _disposed = false;

        public PS02SerialTests(ITestOutputHelper output)
        {
            _output = output;

            // 从环境变量读取串口配置，默认 COM3
            _portName = Environment.GetEnvironmentVariable("PS02_SERIAL_PORT") ?? "COM101";
            _baudRate = int.TryParse(Environment.GetEnvironmentVariable("PS02_BAUD_RATE"), out var baud) ? baud : 9600;
            _slaveAddress = byte.TryParse(Environment.GetEnvironmentVariable("PS02_SLAVE_ADDRESS"), out var addr) ? addr : (byte)1;

            _output.WriteLine($"串口配置: {_portName}, {_baudRate}bps, 从站地址: {_slaveAddress}");
        }

        /// <summary>
        /// 检查串口是否可用
        /// </summary>
        private bool IsSerialPortAvailable()
        {
            try
            {
                // 检查串口是否存在
                string[] ports = SerialPort.GetPortNames();
                if (Array.IndexOf(ports, _portName) < 0)
                {
                    _output.WriteLine($"串口 {_portName} 不存在，可用串口: {string.Join(", ", ports)}");
                    return false;
                }

                // 尝试打开串口（短暂打开测试）
                using var port = new SerialPort(_portName, _baudRate);
                port.Open();
                port.Close();
                return true;
            }
            catch (Exception ex)
            {
                _output.WriteLine($"串口 {_portName} 不可用: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 创建 PS02 设备实例
        /// </summary>
        private PS02Base CreateDevice()
        {
            var settings = new SerialPortSettings
            {
                PortName = _portName,
                BaudRate = _baudRate,
                DataBits = 8,
                StopBits = StopBits.Two,
                Parity = Parity.None,
                DtrEnable = false,
                RtsEnable = false,
                ReceiveTimeoutMs = 15000,      // 15秒超时
                ReceiveIdleTimeoutMs = 100,    // 100ms空闲超时
                MaxRetryCount = 2,             // 重试2次
                RetryDelayMs = 500             // 重试延迟500ms
            };

            return new PS02Base(settings, _slaveAddress);
        }

        /// <summary>
        /// 安全打开设备
        /// </summary>
        private async Task<bool> OpenDeviceAsync()
        {
            try
            {
                _device = CreateDevice();
                await _device.OpenAsync();
                _output.WriteLine("设备已成功打开");
                return true;
            }
            catch (Exception ex)
            {
                _output.WriteLine($"打开设备失败: {ex.Message}");
                _device?.Dispose();
                _device = null;
                return false;
            }
        }

        /// <summary>
        /// 安全关闭设备
        /// </summary>
        private async Task CloseDeviceAsync()
        {
            if (_device != null)
            {
                try
                {
                    await _device.CloseAsync();
                    _output.WriteLine("设备已关闭");
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"关闭设备时出错: {ex.Message}");
                }
                finally
                {
                    _device.Dispose();
                    _device = null;
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // 连接测试
        // ═══════════════════════════════════════════════════════════

        [Fact]
        [Trait("Category", "Serial")]
        public async Task Serial_ConnectAndDisconnect_ShouldSucceed()
        {
            if (!IsSerialPortAvailable())
            {
                _output.WriteLine("跳过测试: 串口不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");
                Assert.True(_device!.IsOpen, "设备应该处于打开状态");

                _output.WriteLine("连接测试通过");
            }
            finally
            {
                await CloseDeviceAsync();
            }
        }

        [Fact]
        [Trait("Category", "Serial")]
        public async Task Serial_TestConnection_ShouldReturnTrue()
        {
            if (!IsSerialPortAvailable())
            {
                _output.WriteLine("跳过测试: 串口不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");

                // 尝试读取模块类型（简单寄存器）
                var moduleType = await _device!.GetModuleTypeAsync();
                _output.WriteLine($"模块类型: 0x{moduleType:X4}");

                Assert.True(moduleType > 0, "模块类型应该大于0");
                _output.WriteLine("连接测试通过");
            }
            finally
            {
                await CloseDeviceAsync();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // 设备信息读取测试
        // ═══════════════════════════════════════════════════════════

        [Fact]
        [Trait("Category", "Serial")]
        public async Task Serial_GetSerialNumber_ShouldReturnNonEmpty()
        {
            if (!IsSerialPortAvailable())
            {
                _output.WriteLine("跳过测试: 串口不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");

                var serialNumber = await _device!.GetSerialNumberAsync();
                _output.WriteLine($"序列号: {serialNumber}");

                Assert.False(string.IsNullOrEmpty(serialNumber), "序列号不应为空");
                Assert.True(serialNumber.Length >= 8, "序列号长度应该至少8个字符");
            }
            finally
            {
                await CloseDeviceAsync();
            }
        }
        [Fact]
        [Trait("Category", "Serial")]
        public async Task Serial_SetSerialNumber_ShouldReturnNonEmpty()
        {
            if (!IsSerialPortAvailable())
            {
                _output.WriteLine("跳过测试: 串口不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");

                await _device!.SetSerialNumberAsync("C1025D010001");

            }
            finally
            {
                await CloseDeviceAsync();
            }
        }

        [Fact]
        [Trait("Category", "Serial")]
        public async Task Serial_GetFirmwareVersion_ShouldReturnNonEmpty()
        {
            if (!IsSerialPortAvailable())
            {
                _output.WriteLine("跳过测试: 串口不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");

                var version = await _device!.GetFirmwareVersionAsync();
                _output.WriteLine($"固件版本: {version}");

                Assert.False(string.IsNullOrEmpty(version), "固件版本不应为空");
            }
            finally
            {
                await CloseDeviceAsync();
            }
        }

        [Fact]
        [Trait("Category", "Serial")]
        public async Task Serial_GetHardwareVersion_ShouldReturnNonEmpty()
        {
            if (!IsSerialPortAvailable())
            {
                _output.WriteLine("跳过测试: 串口不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");

                var version = await _device!.GetHardwareVersionAsync();
                _output.WriteLine($"硬件版本: {version}");

                Assert.False(string.IsNullOrEmpty(version), "硬件版本不应为空");
            }
            finally
            {
                await CloseDeviceAsync();
            }
        }

        [Fact]
        [Trait("Category", "Serial")]
        public async Task Serial_GetIdentification_ShouldReturnAllInfo()
        {
            if (!IsSerialPortAvailable())
            {
                _output.WriteLine("跳过测试: 串口不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");

                var id = await _device!.GetIdentificationAsync();
                _output.WriteLine($"设备标识:");
                _output.WriteLine($"  序列号: {id.SerialNumber}");
                _output.WriteLine($"  固件版本: {id.FirmwareVersion}");
                _output.WriteLine($"  硬件版本: {id.HardwareVersion}");

                Assert.NotNull(id);
                Assert.False(string.IsNullOrEmpty(id.SerialNumber), "序列号不应为空");
                Assert.False(string.IsNullOrEmpty(id.FirmwareVersion), "固件版本不应为空");
                Assert.False(string.IsNullOrEmpty(id.HardwareVersion), "硬件版本不应为空");
            }
            finally
            {
                await CloseDeviceAsync();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // 压力读取测试
        // ═══════════════════════════════════════════════════════════

        [Fact]
        [Trait("Category", "Serial")]
        public async Task Serial_GetPressure_ShouldReturnValue()
        {
            if (!IsSerialPortAvailable())
            {
                _output.WriteLine("跳过测试: 串口不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");

                var pressure = await _device!.GetPressureAsync();
                _output.WriteLine($"压力值: {pressure:F3} kPa");

                // 压力值可能是 NaN（如果传感器未校准），但不应抛出异常
                if (double.IsNaN(pressure))
                {
                    _output.WriteLine("警告: 压力值为 NaN，可能传感器未校准或未连接");
                }
                else
                {
                    Assert.True(pressure >= -1000 && pressure <= 10000,
                        $"压力值应该在合理范围内，实际值: {pressure}");
                }
            }
            finally
            {
                await CloseDeviceAsync();
            }
        }

        [Fact]
        [Trait("Category", "Serial")]
        public async Task Serial_GetPressureF40_ShouldReturnValue()
        {
            if (!IsSerialPortAvailable())
            {
                _output.WriteLine("跳过测试: 串口不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");

                var pressure = await _device!.GetPressureF40Async();
                _output.WriteLine($"压力值 (F40): {pressure:F3} kPa");

                if (double.IsNaN(pressure))
                {
                    _output.WriteLine("警告: 压力值为 NaN，可能传感器未校准或未连接");
                }
                else
                {
                    Assert.True(pressure >= -1000 && pressure <= 10000,
                        $"压力值应该在合理范围内，实际值: {pressure}");
                }
            }
            finally
            {
                await CloseDeviceAsync();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // 设备参数读取测试
        // ═══════════════════════════════════════════════════════════

        [Fact]
        [Trait("Category", "Serial")]
        public async Task Serial_GetPrecision_ShouldReturnValue()
        {
            if (!IsSerialPortAvailable())
            {
                _output.WriteLine("跳过测试: 串口不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");

                var precision = await _device!.GetPrecisionAsync();
                _output.WriteLine($"精度: {precision} (×100%)");

                Assert.True(precision > 0, "精度应该大于0");
            }
            finally
            {
                await CloseDeviceAsync();
            }
        }

        [Fact]
        [Trait("Category", "Serial")]
        public async Task Serial_GetPressureType_ShouldReturnValidType()
        {
            if (!IsSerialPortAvailable())
            {
                _output.WriteLine("跳过测试: 串口不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");

                var type = await _device!.GetPressureTypeAsync();
                _output.WriteLine($"压力类型: {type} ({(ushort)type})");

                // 验证压力类型是已知值
                Assert.True(Enum.IsDefined(typeof(PressureType), type),
                    $"压力类型应该是已知值，实际值: {type}");
            }
            finally
            {
                await CloseDeviceAsync();
            }
        }

        [Fact]
        [Trait("Category", "Serial")]
        public async Task Serial_GetModuleType_ShouldReturnValue()
        {
            if (!IsSerialPortAvailable())
            {
                _output.WriteLine("跳过测试: 串口不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");

                var moduleType = await _device!.GetModuleTypeAsync();
                _output.WriteLine($"模块类型: 0x{moduleType:X4} ('{(char)moduleType}')");

                Assert.True(moduleType > 0, "模块类型应该大于0");
            }
            finally
            {
                await CloseDeviceAsync();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // 量程读取测试
        // ═══════════════════════════════════════════════════════════

        [Fact]
        [Trait("Category", "Serial")]
        public async Task Serial_GetMigrationRange_ShouldReturnRange()
        {
            if (!IsSerialPortAvailable())
            {
                _output.WriteLine("跳过测试: 串口不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");

                var range = await _device!.GetMigrationRangeAsync();
                _output.WriteLine($"迁移量程: {range}");

                Assert.NotNull(range);
                // 量程值可能是 NaN（如果未配置），但对象不应为 null
            }
            finally
            {
                await CloseDeviceAsync();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // 寄存器读写测试
        // ═══════════════════════════════════════════════════════════

        [Fact]
        [Trait("Category", "Serial")]
        public async Task Serial_ReadRegister_ShouldReturnValue()
        {
            if (!IsSerialPortAvailable())
            {
                _output.WriteLine("跳过测试: 串口不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");

                // 读取模块类型寄存器 (0x8020)
                var value = await _device!.ReadRegisterAsync(PS02Registers.ModuleType);
                _output.WriteLine($"寄存器 0x{PS02Registers.ModuleType:X4} = 0x{value:X4}");

                Assert.True(value > 0, "寄存器值应该大于0");
            }
            finally
            {
                await CloseDeviceAsync();
            }
        }

        [Fact]
        [Trait("Category", "Serial")]
        public async Task Serial_ReadRegisters_ShouldReturnMultipleValues()
        {
            if (!IsSerialPortAvailable())
            {
                _output.WriteLine("跳过测试: 串口不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");

                // 读取多个寄存器（版本信息）
                var values = await _device!.ReadRegistersAsync(PS02Registers.FirmwareVersion, 9);
                _output.WriteLine($"读取到 {values.Length} 个寄存器值:");

                for (int i = 0; i < values.Length; i++)
                {
                    _output.WriteLine($"  [{i}] 0x{values[i]:X4}");
                }

                Assert.True(values.Length == 9, "应该读取到9个寄存器值");
            }
            finally
            {
                await CloseDeviceAsync();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // 超时和错误处理测试
        // ═══════════════════════════════════════════════════════════

        [Fact]
        [Trait("Category", "Serial")]
        public async Task Serial_WithCancellation_ShouldRespectToken()
        {
            if (!IsSerialPortAvailable())
            {
                _output.WriteLine("跳过测试: 串口不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");

                // 创建快速取消的令牌
                using var cts = new CancellationTokenSource(100); // 100ms后取消

                try
                {
                    await _device!.GetSerialNumberAsync(cts.Token);
                    _output.WriteLine("警告: 操作在取消前完成");
                }
                catch (OperationCanceledException)
                {
                    _output.WriteLine("操作被正确取消");
                    Assert.True(true, "操作应该被取消");
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"其他异常: {ex.GetType().Name}: {ex.Message}");
                    // 其他异常也可能发生（如超时）
                }
            }
            finally
            {
                await CloseDeviceAsync();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // 性能测试
        // ═══════════════════════════════════════════════════════════

        [Fact]
        [Trait("Category", "Serial")]
        public async Task Serial_MultipleReads_ShouldBeConsistent()
        {
            if (!IsSerialPortAvailable())
            {
                _output.WriteLine("跳过测试: 串口不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");

                // 多次读取模块类型，结果应该一致
                ushort firstValue = 0;
                for (int i = 0; i < 3; i++)
                {
                    var value = await _device!.GetModuleTypeAsync();
                    _output.WriteLine($"第 {i + 1} 次读取: 0x{value:X4}");

                    if (i == 0)
                    {
                        firstValue = value;
                    }
                    else
                    {
                        Assert.Equal(firstValue, value);
                    }
                }

                _output.WriteLine("多次读取结果一致");
            }
            finally
            {
                await CloseDeviceAsync();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // IDisposable 实现
        // ═══════════════════════════════════════════════════════════

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 释放托管资源
                    _device?.Dispose();
                    _device = null;
                }

                _disposed = true;
            }
        }
    }
}