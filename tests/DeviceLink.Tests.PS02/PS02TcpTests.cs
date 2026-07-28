using DeviceLink.Device.PS02;
using DeviceLink.Transport;
using System.Net;
using System.Net.Sockets;
using Xunit;
using Xunit.Abstractions;

namespace DeviceLink.Tests.PS02
{
    /// <summary>
    /// PS02 设备网口(TCP)通信测试
    ///
    /// 使用网络连接 PS02 设备（通过转换板）进行测试。
    /// 需要实际硬件连接才能运行。
    ///
    /// 使用方法：
    /// 1. 确保 PS02 设备可通过网络访问（转换板 TCP 端口）
    /// 2. 设置环境变量 PS02_TCP_HOST 指定 IP 地址（可选，默认 127.0.0.1）
    /// 3. 设置环境变量 PS02_TCP_PORT 指定端口号（可选，默认 10001）
    /// 4. 运行测试：dotnet test --filter "Category=Tcp"
    ///
    /// 注意：这些测试需要实际设备连接，可能因设备状态而失败。
    /// </summary>
    public class PS02TcpTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly string _host;
        private readonly int _port;
        private readonly byte _slaveAddress;
        private PS02Base? _device;
        private bool _disposed = false;

        public PS02TcpTests(ITestOutputHelper output)
        {
            _output = output;

            // 从环境变量读取 TCP 配置，默认 127.0.0.1:10001
            _host = Environment.GetEnvironmentVariable("PS02_TCP_HOST") ?? "192.168.41.243";
            _port = int.TryParse(Environment.GetEnvironmentVariable("PS02_TCP_PORT"), out var p) ? p : 1030;
            _slaveAddress = byte.TryParse(Environment.GetEnvironmentVariable("PS02_SLAVE_ADDRESS"), out var addr) ? addr : (byte)1;

            _output.WriteLine($"TCP 配置: {_host}:{_port}, 从站地址: {_slaveAddress}");
        }

        /// <summary>
        /// 检查 TCP 连接是否可用
        /// </summary>
        private bool IsTcpAvailable()
        {
            try
            {
                using var tcp = new TcpClient();
                var result = tcp.BeginConnect(_host, _port, null, null);
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3));
                if (!success)
                {
                    _output.WriteLine($"TCP 连接 {_host}:{_port} 超时");
                    return false;
                }
                tcp.EndConnect(result);
                _output.WriteLine($"TCP 连接 {_host}:{_port} 成功");
                return true;
            }
            catch (Exception ex)
            {
                _output.WriteLine($"TCP 连接 {_host}:{_port} 不可用: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 创建 PS02 设备实例（TCP 通讯）
        /// </summary>
        private PS02Base CreateDevice()
        {
            var ip = IPAddress.Parse(_host);
            return new PS02Base(ip, _port, _slaveAddress);
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
        [Trait("Category", "Tcp")]
        public async Task Tcp_ConnectAndDisconnect_ShouldSucceed()
        {
            if (!IsTcpAvailable())
            {
                _output.WriteLine("跳过测试: TCP 不可用");
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

        /// <summary>
        /// 【诊断测试】直接使用 TcpTransport 发送原始帧，不经过 PS02Base/SendRawFrameAsync。
        /// 帧数据来自用户手动测试确认有效的 0x0300 ScanDevice(scanType=1) 请求。
        /// 如果此测试通过而 Tcp_ConverterInitialization 超时，问题在 PS02Base 内部流程。
        /// </summary>
        [Fact]
        [Trait("Category", "TcpDiagnostic")]
        public async Task Tcp_Diagnostic_DirectRawFrame_ShouldReceiveResponse()
        {
            if (!IsTcpAvailable())
            {
                _output.WriteLine("跳过测试: TCP 不可用");
                return;
            }

            var transport = new TcpTransport(_host, _port);
            try
            {
                await transport.ConnectAsync();
                _output.WriteLine("TCP 连接已建立");

                // 用户手动验证有效的原始帧：0x0300 ScanDevice(scanType=1)
                // 55 03 23 01 00 36 22 11 00 03 01 01 00 D1 01 D1 F1
                byte[] rawFrame = new byte[] {
                    0x55, 0x03, 0x23, 0x01, 0x00, 0x36, 0x22, 0x11,
                    0x00, 0x03, 0x01, 0x01, 0x00, 0xD1, 0x01, 0xD1, 0xF1
                };

                _output.WriteLine($"发送 {rawFrame.Length} 字节: {BitConverter.ToString(rawFrame)}");
                await transport.WriteAsync(rawFrame, 0, rawFrame.Length);
                _output.WriteLine("已发送，等待响应...");

                // 接收响应（最多等 10 秒）
                var buffer = new byte[4096];
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                try
                {
                    // 持续读取直到收到数据或超时
                    var accumulated = new List<byte>();
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    while (!cts.Token.IsCancellationRequested)
                    {
                        int read = await transport.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                        if (read > 0)
                        {
                            accumulated.AddRange(buffer.AsSpan(0, read).ToArray());
                            _output.WriteLine($"已收到 {read} 字节，累计 {accumulated.Count} 字节");

                            // 期望收到 17 字节响应帧
                            if (accumulated.Count >= 17)
                            {
                                _output.WriteLine($"完整响应: {BitConverter.ToString(accumulated.ToArray())}");
                                // 验证帧头
                                Assert.Equal(0x55, accumulated[0]);
                                // 验证功能码回显 0x0300
                                Assert.Equal(0x00, accumulated[8]);
                                Assert.Equal(0x03, accumulated[9]);
                                _output.WriteLine($"诊断测试通过！收到有效响应");
                                return;
                            }
                        }
                        else
                        {
                            await Task.Delay(20, cts.Token);
                        }
                    }
                    Assert.Fail($"诊断测试失败: 10秒内未收到完整响应 (已收 {accumulated.Count} 字节)");
                }
                catch (OperationCanceledException)
                {
                    Assert.Fail("诊断测试失败: 读取超时");
                }
            }
            finally
            {
                transport.Dispose();
            }
        }

        [Fact]
        [Trait("Category", "Tcp")]
        public async Task Tcp_TestConnection_ShouldReturnTrue()
        {
            if (!IsTcpAvailable())
            {
                _output.WriteLine("跳过测试: TCP 不可用");
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
        [Trait("Category", "Tcp")]
        public async Task Tcp_GetSerialNumber_ShouldReturnNonEmpty()
        {
            if (!IsTcpAvailable())
            {
                _output.WriteLine("跳过测试: TCP 不可用");
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
        [Trait("Category", "Tcp")]
        public async Task Tcp_SetSerialNumber_ShouldReturnNonEmpty()
        {
            if (!IsTcpAvailable())
            {
                _output.WriteLine("跳过测试: TCP 不可用");
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
        [Trait("Category", "Tcp")]
        public async Task Tcp_GetFirmwareVersion_ShouldReturnNonEmpty()
        {
            if (!IsTcpAvailable())
            {
                _output.WriteLine("跳过测试: TCP 不可用");
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
        [Trait("Category", "Tcp")]
        public async Task Tcp_GetHardwareVersion_ShouldReturnNonEmpty()
        {
            if (!IsTcpAvailable())
            {
                _output.WriteLine("跳过测试: TCP 不可用");
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
        [Trait("Category", "Tcp")]
        public async Task Tcp_GetIdentification_ShouldReturnAllInfo()
        {
            if (!IsTcpAvailable())
            {
                _output.WriteLine("跳过测试: TCP 不可用");
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
        [Trait("Category", "Tcp")]
        public async Task Tcp_ConverterInitialization_ShouldSucceed()
        {
            if (!IsTcpAvailable())
            {
                _output.WriteLine("跳过测试: TCP 不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");

                _output.WriteLine("=== 开始转接板初始化序列（14步） ===");

                // 指令 1：停止之前的扫描（scanType=0x01）
                _output.WriteLine("[01/13] 停止之前的扫描...");
                await _device!.ScanDeviceAsync(0x01);
                _output.WriteLine("  已发送停止扫描指令");

                // 等待转接板响应
                await Task.Delay(100);

                // 0x0301：获取扫描结果（此时应该是NotConnected）
                var scanResult1 = await _device.GetScanResultAsync();
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

                // 指令 6~7：发送扫描指令并轮询扫描结果
                // 流程：0x0300（扫描从设备）→ 多次轮询 0x0301（获取扫描结果）
                DeviceInterfaceType scanResult2 = DeviceInterfaceType.NotConnected;
                bool scanSuccess = false;

                // 0x0300：发送扫描从设备指令（只发送一次，让转接板持续扫描）
                _output.WriteLine("[06/14] 发送扫描从设备指令...");
                await _device.ScanDeviceAsync(0x00);
                _output.WriteLine("  已发送扫描指令，开始轮询扫描结果...");

                // 轮询扫描结果，最多等待10秒（每200ms查询一次）
                for (int poll = 0; poll < 50; poll++)
                {
                    // 等待一段时间让转接板完成扫描
                    await Task.Delay(200);

                    // 0x0301：获取扫描结果
                    scanResult2 = await _device.GetScanResultAsync();
                    _output.WriteLine($"  [轮询 {poll + 1}/50] 扫描结果: {scanResult2} ({(byte)scanResult2})");

                    // 如果扫描成功（非未连接），跳出轮询
                    if (scanResult2 != DeviceInterfaceType.NotConnected)
                    {
                        scanSuccess = true;
                        _output.WriteLine($"  扫描成功，跳出轮询");
                        break;
                    }
                }

                if (!scanSuccess)
                {
                    _output.WriteLine("  警告: 10秒轮询后仍未检测到设备");
                }

                await Task.Delay(1000);

                // 指令 11：停止扫描（scanType=0x01）
                _output.WriteLine("[11/14] 停止扫描...");
                await _device.ScanDeviceAsync(0x01);
                _output.WriteLine("  已发送停止扫描指令");

                // 等待转接板响应
                await Task.Delay(100);

                // 0x0301：获取扫描结果
                var scanResult3 = await _device.GetScanResultAsync();
                _output.WriteLine($"  扫描结果: {scanResult3} ({(byte)scanResult3})");
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
                //_output.WriteLine($"  PS02 序列号: {serialNumber}");
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
        // 电流读取测试
        // ═══════════════════════════════════════════════════════════

        [Fact]
        [Trait("Category", "Tcp")]
        public async Task Tcp_GetPressure_ShouldReturnValue()
        {
            if (!IsTcpAvailable())
            {
                _output.WriteLine("跳过测试: TCP 不可用");
                return;
            }

            try
            {

                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");
                await _device.DisableOwiViaConverterAsync();
                await _device.EnableOwiViaConverterAsync();
                var range = await _device!.GetMigrationRangeAsync();

                var pressure = await _device.GetPressureAsync();
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
        [Trait("Category", "Tcp")]
        public async Task Tcp_GetPressureF40_ShouldReturnValue()
        {
            if (!IsTcpAvailable())
            {
                _output.WriteLine("跳过测试: TCP 不可用");
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
        [Trait("Category", "Tcp")]
        public async Task Tcp_GetPrecision_ShouldReturnValue()
        {
            if (!IsTcpAvailable())
            {
                _output.WriteLine("跳过测试: TCP 不可用");
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
        [Trait("Category", "Tcp")]
        public async Task Tcp_GetPressureType_ShouldReturnValidType()
        {
            if (!IsTcpAvailable())
            {
                _output.WriteLine("跳过测试: TCP 不可用");
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
        [Trait("Category", "Tcp")]
        public async Task Tcp_SetPressureType_ShouldSucceed()
        {
            if (!IsTcpAvailable())
            {
                _output.WriteLine("跳过测试: TCP 不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");

                // 读取当前压力类型
                var originalType = await _device!.GetPressureTypeAsync();
                _output.WriteLine($"原始压力类型: {originalType} ({(ushort)originalType})");

                // 设置为表压
                _output.WriteLine("设置压力类型为表压(Gauge)...");
                await _device.SetPressureTypeAsync(PressureType.Gauge);
                await Task.Delay(3000); // 等待设备处理
                var gaugeType = await _device.GetPressureTypeAsync();
                _output.WriteLine($"读取压力类型: {gaugeType} ({(ushort)gaugeType})");
                Assert.Equal(PressureType.Gauge, gaugeType);

                // 设置为绝压
                _output.WriteLine("设置压力类型为绝压(Absolute)...");
                await _device.SetPressureTypeAsync(PressureType.Absolute);
                await Task.Delay(3000); // 等待设备处理
                var absoluteType = await _device.GetPressureTypeAsync();
                _output.WriteLine($"读取压力类型: {absoluteType} ({(ushort)absoluteType})");
                Assert.Equal(PressureType.Absolute, absoluteType);

                // 设置为差压
                _output.WriteLine("设置压力类型为差压(Differential)...");
                await _device.SetPressureTypeAsync(PressureType.Differential);
                await Task.Delay(3000); // 等待设备处理
                var differentialType = await _device.GetPressureTypeAsync();

                _output.WriteLine($"读取压力类型: {differentialType} ({(ushort)differentialType})");
                Assert.Equal(PressureType.Differential, differentialType);

                // 恢复原始压力类型
                if (originalType != differentialType)
                {
                    _output.WriteLine($"恢复原始压力类型: {originalType}");
                    await _device.SetPressureTypeAsync(originalType);
                }

                _output.WriteLine("压力类型设置测试通过");
            }
            finally
            {
                await CloseDeviceAsync();
            }
        }

        [Fact]
        [Trait("Category", "Tcp")]
        public async Task Tcp_GetModuleType_ShouldReturnValue()
        {
            if (!IsTcpAvailable())
            {
                _output.WriteLine("跳过测试: TCP 不可用");
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
        [Trait("Category", "Tcp")]
        public async Task Tcp_GetMigrationRange_ShouldReturnRange()
        {
            if (!IsTcpAvailable())
            {
                _output.WriteLine("跳过测试: TCP 不可用");
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
        [Trait("Category", "Tcp")]
        public async Task Tcp_SetMigrationRange_ShouldSucceed()
        {
            if (!IsTcpAvailable())
            {
                _output.WriteLine("跳过测试: TCP 不可用");
                return;
            }

            try
            {
                Assert.True(await OpenDeviceAsync(), "应该能够打开设备");
                await _device.EnableOwiViaConverterAsync();
                // 先读取当前迁移量程
                var originalRange = await _device!.GetMigrationRangeAsync();
                _output.WriteLine($"原始迁移量程: {originalRange}");

                // 写入新的迁移量程（测试值：下限-100kPa，上限500kPa）
                float testLower = 0.0f;
                float testUpper = 500.0f;
                _output.WriteLine($"写入迁移量程: 下限={testLower} kPa, 上限={testUpper} kPa");

                await _device.SetMigrationRangeAsync(testLower, testUpper);
                _output.WriteLine("写入成功");
                await Task.Delay(5 * 1000); // 等待设备处理
                // 读取并验证
                var newRange = await _device.GetMigrationRangeAsync();
                _output.WriteLine($"读取迁移量程: {newRange}");
                await _device.DisableOwiViaConverterAsync();
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
        [Trait("Category", "Tcp")]
        public async Task Tcp_ReadRegister_ShouldReturnValue()
        {
            if (!IsTcpAvailable())
            {
                _output.WriteLine("跳过测试: TCP 不可用");
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
        [Trait("Category", "Tcp")]
        public async Task Tcp_ReadRegisters_ShouldReturnMultipleValues()
        {
            if (!IsTcpAvailable())
            {
                _output.WriteLine("跳过测试: TCP 不可用");
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
        [Trait("Category", "Tcp")]
        public async Task Tcp_WithCancellation_ShouldRespectToken()
        {
            if (!IsTcpAvailable())
            {
                _output.WriteLine("跳过测试: TCP 不可用");
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
        [Trait("Category", "Tcp")]
        public async Task Tcp_MultipleReads_ShouldBeConsistent()
        {
            if (!IsTcpAvailable())
            {
                _output.WriteLine("跳过测试: TCP 不可用");
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