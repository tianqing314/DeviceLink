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
    /// PS02 转接板 CPPI V3 通信测试
    ///
    /// 使用真实串口连接 PS02 转接板进行测试。
    /// 需要实际硬件连接才能运行。
    ///
    /// 使用方法：
    /// 1. 连接 PS02 转接板到串口（如 COM3）
    /// 2. 设置环境变量 PS02_SERIAL_PORT 指定串口号（可选，默认 COM101）
    /// 3. 运行测试：dotnet test --filter "Category=Converter"
    ///
    /// 注意：这些测试需要实际设备连接，可能因设备状态而失败。
    /// </summary>
    public class ConverterSerialTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly string _portName;
        private readonly int _baudRate;
        private readonly byte _slaveAddress;
        private PS02Base? _device;
        private bool _disposed = false;

        public ConverterSerialTests(ITestOutputHelper output)
        {
            _output = output;

            // 从环境变量读取串口配置，默认 COM101
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
        [Trait("Category", "Converter")]
        public async Task Converter_ConnectAndDisconnect_ShouldSucceed()
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

        // ═══════════════════════════════════════════════════════════
        // 转接板信息读取测试
        // ═══════════════════════════════════════════════════════════

        [Fact]
        [Trait("Category", "Converter")]
        public async Task Converter_GetFirmwareVersion_ShouldReturnNonEmpty()
        {
            if (!IsSerialPortAvailable())
            {
                _output.WriteLine("跳过测试: 串口不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");

                var version = await _device!.GetConverterFirmwareVersionAsync();
                _output.WriteLine($"转接板固件版本: {version}");

                Assert.False(string.IsNullOrEmpty(version), "固件版本不应为空");
                Assert.Contains("V", version); // 版本号通常包含 "V"
            }
            finally
            {
                await CloseDeviceAsync();
            }
        }

        [Fact]
        [Trait("Category", "Converter")]
        public async Task Converter_GetHardwareVersion_ShouldReturnNonEmpty()
        {
            if (!IsSerialPortAvailable())
            {
                _output.WriteLine("跳过测试: 串口不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");

                var version = await _device!.GetConverterHardwareVersionAsync();
                _output.WriteLine($"转接板硬件版本: {version}");

                Assert.False(string.IsNullOrEmpty(version), "硬件版本不应为空");
            }
            finally
            {
                await CloseDeviceAsync();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // 扫描设备测试
        // ═══════════════════════════════════════════════════════════

        [Fact]
        [Trait("Category", "Converter")]
        public async Task Converter_ScanDevice_ShouldReturnInterfaceType()
        {
            if (!IsSerialPortAvailable())
            {
                _output.WriteLine("跳过测试: 串口不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");

                // 0x0300：扫描从设备（参数0=开始扫描）
                await _device!.ScanDeviceAsync(0x00);
                _output.WriteLine("已发送扫描指令");
                
                // 等待转接板完成扫描
                await Task.Delay(200);
                
                // 0x0301：获取扫描结果
                var interfaceType = await _device.GetScanResultAsync();
                _output.WriteLine($"扫描结果 - 接口类型: {interfaceType} ({(byte)interfaceType})");

                // 扫描应该返回一个有效的接口类型
                Assert.True(Enum.IsDefined(typeof(DeviceInterfaceType), interfaceType),
                    $"接口类型 {interfaceType} 应该是有效的枚举值");

                // 输出接口类型说明
                var description = interfaceType switch
                {
                    DeviceInterfaceType.NotConnected => "未连接设备",
                    DeviceInterfaceType.OwiCurrent => "OWI 电流接口",
                    DeviceInterfaceType.OwiVoltage => "OWI 电压接口",
                    DeviceInterfaceType.Rs485 => "485 接口",
                    _ => "未知类型"
                };
                _output.WriteLine($"接口描述: {description}");
            }
            finally
            {
                await CloseDeviceAsync();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // 输出控制测试
        // ═══════════════════════════════════════════════════════════

        [Fact]
        [Trait("Category", "Converter")]
        public async Task Converter_DisableAllOutput_ShouldSucceed()
        {
            if (!IsSerialPortAvailable())
            {
                _output.WriteLine("跳过测试: 串口不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");

                // 关闭所有输出
                await _device!.DisableAllOutputAsync();
                _output.WriteLine("已发送关闭所有输出指令");

                _output.WriteLine("输出控制测试通过");
            }
            finally
            {
                await CloseDeviceAsync();
            }
        }

        [Fact]
        [Trait("Category", "Converter")]
        public async Task Converter_SetOutputProject_MaOut_ShouldSucceed()
        {
            if (!IsSerialPortAvailable())
            {
                _output.WriteLine("跳过测试: 串口不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");

                // 设置电流输出 - 测量OWI模块输出
                await _device!.SetOutputProjectAsync(OutputProject.MaOut, MeasurementDeviceCategory.OwiModule);
                _output.WriteLine("已设置电流输出 - 测量OWI模块输出");

                // 等待一下
                await Task.Delay(500);

                // 设置电流输出 - 测量标准板输出
                await _device.SetOutputProjectAsync(OutputProject.MaOut, MeasurementDeviceCategory.StandardBoard);
                _output.WriteLine("已设置电流输出 - 测量标准板输出");

                // 等待一下
                await Task.Delay(500);

                // 关闭输出
                await _device.DisableAllOutputAsync();
                _output.WriteLine("已关闭输出");

                _output.WriteLine("电流输出测试通过");
            }
            finally
            {
                await CloseDeviceAsync();
            }
        }

        [Fact]
        [Trait("Category", "Converter")]
        public async Task Converter_SetOutputProject_VOut_ShouldSucceed()
        {
            if (!IsSerialPortAvailable())
            {
                _output.WriteLine("跳过测试: 串口不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");

                // 设置电压输出 - 测量OWI模块输出
                await _device!.SetOutputProjectAsync(OutputProject.VOut, MeasurementDeviceCategory.OwiModule);
                _output.WriteLine("已设置电压输出 - 测量OWI模块输出");

                // 等待一下
                await Task.Delay(500);

                // 设置电压输出 - 测量标准板输出
                await _device.SetOutputProjectAsync(OutputProject.VOut, MeasurementDeviceCategory.StandardBoard);
                _output.WriteLine("已设置电压输出 - 测量标准板输出");

                // 等待一下
                await Task.Delay(500);

                // 关闭输出
                await _device.DisableAllOutputAsync();
                _output.WriteLine("已关闭输出");

                _output.WriteLine("电压输出测试通过");
            }
            finally
            {
                await CloseDeviceAsync();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // 参数读取测试
        // ═══════════════════════════════════════════════════════════

        [Fact]
        [Trait("Category", "Converter")]
        public async Task Converter_ReadParameter_ShouldReturnValidValue()
        {
            if (!IsSerialPortAvailable())
            {
                _output.WriteLine("跳过测试: 串口不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");

                // 读取参数 #0
                var param0 = await _device!.ReadConverterParameterAsync(0);
                _output.WriteLine($"参数 #0 = 0x{param0:X2} ({param0})");

                // 读取参数 #1
                var param1 = await _device.ReadConverterParameterAsync(1);
                _output.WriteLine($"参数 #1 = 0x{param1:X2} ({param1})");

                _output.WriteLine("参数读取测试通过");
            }
            finally
            {
                await CloseDeviceAsync();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // 综合测试
        // ═══════════════════════════════════════════════════════════

        [Fact]
        [Trait("Category", "Converter")]
        public async Task Converter_FullSequence_ShouldSucceed()
        {
            if (!IsSerialPortAvailable())
            {
                _output.WriteLine("跳过测试: 串口不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");

                _output.WriteLine("=== 开始转接板综合测试 ===");

                // 1. 读取固件版本
                var firmwareVersion = await _device!.GetConverterFirmwareVersionAsync();
                _output.WriteLine($"[1/5] 固件版本: {firmwareVersion}");

                // 2. 读取硬件版本
                var hardwareVersion = await _device.GetConverterHardwareVersionAsync();
                _output.WriteLine($"[2/5] 硬件版本: {hardwareVersion}");

                // 3. 扫描从设备
                await _device.ScanDeviceAsync(0x00);
                _output.WriteLine("[3/5] 已发送扫描指令");
                
                // 等待转接板完成扫描
                await Task.Delay(200);
                
                // 0x0301：获取扫描结果
                var interfaceType = await _device.GetScanResultAsync();
                _output.WriteLine($"[3/5] 扫描结果: {interfaceType}");

                // 4. 读取参数
                var param0 = await _device.ReadConverterParameterAsync(0);
                _output.WriteLine($"[4/5] 参数 #0 = 0x{param0:X2}");

                // 5. 关闭所有输出
                await _device.DisableAllOutputAsync();
                _output.WriteLine("[5/5] 已关闭所有输出");

                _output.WriteLine("=== 转接板综合测试完成 ===");
            }
            finally
            {
                await CloseDeviceAsync();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // Modbus 转发指令测试（端口38）
        // ═══════════════════════════════════════════════════════════

        [Fact]
        [Trait("Category", "Converter")]
        public async Task Converter_EnableOwiViaConverter_ShouldSucceed()
        {
            if (!IsSerialPortAvailable())
            {
                _output.WriteLine("跳过测试: 串口不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");

                // 启用 OWI 通信模式
                bool enableResult = await _device!.EnableOwiViaConverterAsync(_slaveAddress);
                _output.WriteLine($"启用 OWI 通信模式结果: {enableResult}");
                Assert.True(enableResult, "启用 OWI 通信模式应该成功");

                // 等待一下，确保指令生效
                await Task.Delay(500);

                _output.WriteLine("OWI 通信模式启用测试通过");
            }
            finally
            {
                // 尝试禁用 OWI 模式，恢复设备状态
                if (_device != null)
                {
                    bool disableResult = await _device.DisableOwiViaConverterAsync(_slaveAddress);
                    _output.WriteLine($"禁用 OWI 通信模式结果: {disableResult}");
                }

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
                    _device?.Dispose();
                    _device = null;
                }
                _disposed = true;
            }
        }
    }
}
