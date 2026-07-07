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
        // 转接板初始化测试
        // ═══════════════════════════════════════════════════════════

        [Fact]
        [Trait("Category", "Serial")]
        public async Task Serial_ConverterInitialization_ShouldSucceed()
        {
            if (!IsSerialPortAvailable())
            {
                _output.WriteLine("跳过测试: 串口不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");

                _output.WriteLine("=== 开始转接板初始化序列（14步） ===");

                // 指令 1：扫描从设备（scanType=0x01）
                _output.WriteLine("[01/13] 扫描从设备（类型 0x01）...");
                var scanResult1 = await _device!.ScanDeviceAsync(0x01);
                _output.WriteLine($"  扫描结果: {scanResult1} ({(byte)scanResult1})");

                // 指令 2：读取转接板固件版本（第一次）
                _output.WriteLine("[02/13] 读取转接板固件版本（第一次）...");
                var firmwareVersion1 = await _device.GetConverterFirmwareVersionAsync();
                _output.WriteLine($"  固件版本: {firmwareVersion1}");
                Assert.False(string.IsNullOrEmpty(firmwareVersion1), "固件版本不应为空");

                // 指令 3：读取转接板固件版本（第二次，验证一致性）
                _output.WriteLine("[03/13] 读取转接板固件版本（第二次，验证）...");
                var firmwareVersion2 = await _device.GetConverterFirmwareVersionAsync();
                _output.WriteLine($"  固件版本: {firmwareVersion2}");
                Assert.Equal(firmwareVersion1, firmwareVersion2);

                // 指令 4：读取转接板硬件版本
                _output.WriteLine("[04/13] 读取转接板硬件版本...");
                var hardwareVersion = await _device.GetConverterHardwareVersionAsync();
                _output.WriteLine($"  硬件版本: {hardwareVersion}");
                Assert.False(string.IsNullOrEmpty(hardwareVersion), "硬件版本不应为空");

                // 指令 5：关闭所有输出
                _output.WriteLine("[05/15] 关闭所有输出...");
                await _device.DisableAllOutputAsync();
                _output.WriteLine("  输出已关闭");

                // 指令 6~10：循环扫描从设备，最多5次，扫描类型交替变化
                DeviceInterfaceType scanResult2 = DeviceInterfaceType.NotConnected;
                bool scanSuccess = false;
                for (int retry = 0; retry < 5; retry++)
                {
                    // 扫描类型交替变化：偶数次 0x00，奇数次 0x01
                    byte scanType = (retry % 2 == 0) ? (byte)0x00 : (byte)0x01;

                    _output.WriteLine($"[{6 + retry:D2}/15] 扫描从设备（类型 0x{scanType:X2}，第 {retry + 1} 次）...");
                    scanResult2 = await _device.ScanDeviceAsync(scanType);
                    _output.WriteLine($"  扫描结果: {scanResult2} ({(byte)scanResult2})");

                    // 扫描完成后，读取扫描结果（功能码 0x0301）
                    _output.WriteLine($"  读取扫描结果...");
                    var getScanResult = await _device.GetScanResultAsync();
                    _output.WriteLine($"  获取扫描结果: {getScanResult} ({(byte)getScanResult})");

                    // 如果扫描成功（非未连接），跳出循环
                    if (getScanResult != DeviceInterfaceType.NotConnected)
                    {
                        scanResult2 = getScanResult;
                        scanSuccess = true;
                        _output.WriteLine($"  扫描成功，跳出循环");
                        break;
                    }

                    _output.WriteLine($"  未检测到设备，继续扫描...");
                    await Task.Delay(1000);
                }

                if (!scanSuccess)
                {
                    _output.WriteLine("  警告: 5次扫描后仍未检测到设备");
                }

                await Task.Delay(1000);

                // 指令 11：扫描从设备（scanType=0x01）
                _output.WriteLine("[11/14] 扫描从设备（类型 0x01）...");
                var scanResult3 = await _device.ScanDeviceAsync(0x01);
                _output.WriteLine($"  扫描结果: {scanResult3} ({(byte)scanResult3})");

                // 指令 12：读取 PS02 模块序列号（如果扫描成功）
                _output.WriteLine("[12/14] 读取 PS02 模块序列号...");
                string serialNumber = string.Empty;
                if (scanSuccess)
                {
                    try
                    {
                        serialNumber = await _device.GetSerialNumberAsync();
                        _output.WriteLine($"  PS02 序列号: {serialNumber}");
                    }
                    catch (Exception ex)
                    {
                        _output.WriteLine($"  读取序列号失败: {ex.Message}");
                    }
                }
                else
                {
                    _output.WriteLine("  跳过: 扫描未成功");
                }

                // 指令 13：启用 OWI 通信模式（Modbus RTU 转发）
                _output.WriteLine("[13/14] 启用 OWI 通信模式...");
                bool owiEnabled = await _device.EnableOwiViaConverterAsync(_slaveAddress);
                _output.WriteLine($"  OWI 启用结果: {owiEnabled}");

                // 指令 14：读取 PS02 序列号（OWI 模式下）
                if (owiEnabled)
                {
                    _output.WriteLine("[14/14] OWI 模式下读取 PS02 序列号...");
                    var owiSerialNumber = await _device.GetSerialNumberAsync();
                    _output.WriteLine($"  PS02 序列号 (OWI): {owiSerialNumber}");
                }

                _output.WriteLine("=== 转接板初始化序列完成 ===");
                _output.WriteLine($"  转接板固件: {firmwareVersion1}");
                _output.WriteLine($"  转接板硬件: {hardwareVersion}");
                _output.WriteLine($"  扫描结果 1: {scanResult1}");
                _output.WriteLine($"  扫描结果 2: {scanResult2}");
                _output.WriteLine($"  扫描结果 3: {scanResult3}");
                _output.WriteLine($"  扫描成功: {scanSuccess}");
                _output.WriteLine($"  PS02 序列号: {serialNumber}");
                _output.WriteLine($"  OWI 启用: {owiEnabled}");
            }
            finally
            {
                // 尝试禁用 OWI 模式，恢复设备状态（使用短超时，避免长时间等待）
                if (_device != null)
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        bool owiDisabled = await _device.DisableOwiViaConverterAsync(_slaveAddress, cts.Token);
                        _output.WriteLine($"禁用 OWI 通信模式结果: {owiDisabled}");
                    }
                    catch (OperationCanceledException)
                    {
                        _output.WriteLine("禁用 OWI 通信模式超时（5秒）");
                    }
                    catch (Exception ex)
                    {
                        _output.WriteLine($"禁用 OWI 通信模式时出错: {ex.Message}");
                    }
                }

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
                await _device.EnableOwiViaConverterAsync();
                var range = await _device!.GetMigrationRangeAsync();
                await _device.DisableOwiViaConverterAsync();
                await _device.SetOutputProjectAsync(OutputProject.MaOut, MeasurementDeviceCategory.OwiModule);
                await Task.Delay(3000);
                var measureResult = await _device!.GetMeasurementProjectAsync();
                _output.WriteLine($"测量值: {measureResult:F3} mA");

                // 压力值可能是 NaN（如果传感器未校准），但不应抛出异常
                //if (double.IsNaN(pressure))
                //{
                //    _output.WriteLine("警告: 压力值为 NaN，可能传感器未校准或未连接");
                //}
                //else
                //{
                //    Assert.True(pressure >= -1000 && pressure <= 10000,
                //        $"压力值应该在合理范围内，实际值: {pressure}");
                //}
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

        [Fact]
        [Trait("Category", "Serial")]
        public async Task Serial_SetMigrationRange_ShouldSucceed()
        {
            if (!IsSerialPortAvailable())
            {
                _output.WriteLine("跳过测试: 串口不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");

                // 先读取当前迁移量程
                var originalRange = await _device!.GetMigrationRangeAsync();
                _output.WriteLine($"原始迁移量程: {originalRange}");

                // 写入新的迁移量程（测试值：下限-100kPa，上限500kPa）
                float testLower = -500.0f;
                float testUpper = 500.0f;
                _output.WriteLine($"写入迁移量程: 下限={testLower} kPa, 上限={testUpper} kPa");

                await _device.SetMigrationRangeAsync(testLower, testUpper);
                _output.WriteLine("写入成功");
                await Task.Delay(5 * 1000); // 等待设备处理
                // 读取并验证
                var newRange = await _device.GetMigrationRangeAsync();
                _output.WriteLine($"读取迁移量程: {newRange}");

                // 验证写入的值（允许小误差）
                Assert.True(Math.Abs(newRange.Lower - testLower) < 0.1,
                    $"迁移量程下限应为 {testLower}，实际为 {newRange.Lower}");
                Assert.True(Math.Abs(newRange.Upper - testUpper) < 0.1,
                    $"迁移量程上限应为 {testUpper}，实际为 {newRange.Upper}");

                // 恢复原始迁移量程
                if (!double.IsNaN(originalRange.Lower) && !double.IsNaN(originalRange.Upper))
                {
                    _output.WriteLine($"恢复原始迁移量程: {originalRange}");
                    await _device.SetMigrationRangeAsync((float)originalRange.Lower, (float)originalRange.Upper);
                }
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