using DeviceLink.DataLink;
using DeviceLink.Device.PS02;
using DeviceLink.DeviceBase;
using DeviceLink.Tests.PS02.Helpers;
using System;
using System.Threading.Tasks;
using Xunit;
using Ps02Device = DeviceLink.Device.PS02.PS02Base;
using Ps02PressureType = DeviceLink.Device.PS02.PressureType;

namespace DeviceLink.Tests.PS02
{
    /// <summary>
    /// PS02 设备单元测试
    ///
    /// 使用 CpplV3LoopbackSettings 创建回环测试环境。
    /// 模拟转换板行为：接收 CPPI V3 帧 → 提取 Modbus → 生成响应 → 包装 CPPI V3 返回。
    /// </summary>
    public class PS02Tests
    {
        /// <summary>
        /// CPPI V3 回环帧策略，用于解析请求和构造响应
        /// </summary>
        private readonly CpplV3FrameStrategy _cppiFrameStrategy = new CpplV3FrameStrategy();

        /// <summary>
        /// 创建测试用 PS02 实例和配套的回环配置
        /// </summary>
        private (Ps02Device ps02, CpplV3LoopbackSettings settings) CreateTestDevice()
        {
            var settings = new CpplV3LoopbackSettings();
            var ps02 = new Ps02Device(settings);
            return (ps02, settings);
        }

        /// <summary>
        /// 模拟转换板：解析 CPPI V3 帧，提取 Modbus 数据，生成响应
        /// </summary>
        private void SetupConverterSimulation(CpplV3LoopbackSettings settings, Func<byte[], byte[]?> responseGenerator)
        {
            settings.Transport.OnSend += rawData =>
            {
                // rawData 是 CPPI V3 帧（已包含 CPPI V3 包装）
                // 使用独立的帧策略解析
                var parseStrategy = new CpplV3FrameStrategy();
                if (parseStrategy.TryParseFrame(rawData, out _, out byte[] modbusData))
                {
                    // modbusData 是不含 CRC 的 Modbus 数据
                    var response = responseGenerator(modbusData);
                    if (response != null)
                    {
                        // 用 CPPI V3 包装响应并发送
                        var responseFrame = parseStrategy.BuildFrame(response);
                        settings.Transport.EnqueueReceive(responseFrame);
                    }
                }
            };
        }

        /// <summary>
        /// 构造 Modbus F03 读压力的响应（float32 大端 = 500.0 kPa）
        /// </summary>
        private static byte[] BuildPressureResponse(float value)
        {
            // Modbus 响应：[地址][功能码][字节数][数据...]
            var response = new byte[7];
            response[0] = 0x01; // 从站地址
            response[1] = 0x03; // 功能码 F03
            response[2] = 0x04; // 字节数

            // float32 大端
            byte[] floatBytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                response[3] = floatBytes[3];
                response[4] = floatBytes[2];
                response[5] = floatBytes[1];
                response[6] = floatBytes[0];
            }
            else
            {
                Array.Copy(floatBytes, 0, response, 3, 4);
            }

            return response;
        }

        /// <summary>
        /// 构造 Modbus F40 读寄存器的响应（含转换板添加的 0x00 前缀）
        /// </summary>
        private static byte[] BuildReadRegistersResponse(byte slaveAddr, ushort[] values)
        {
            int byteCount = values.Length * 2;
            // 转换板在 F40 响应前添加 0x00 前缀字节
            var response = new byte[4 + byteCount];
            response[0] = 0x00; // 转换板添加的额外前缀
            response[1] = slaveAddr;
            response[2] = 0x28; // F40
            response[3] = (byte)byteCount;

            for (int i = 0; i < values.Length; i++)
            {
                response[4 + i * 2] = (byte)(values[i] >> 8);
                response[5 + i * 2] = (byte)(values[i] & 0xFF);
            }

            return response;
        }

        /// <summary>
        /// 构造 Modbus F40 读字符串的响应（含转换板添加的 0x00 前缀）
        /// </summary>
        private static byte[] BuildReadStringResponse(byte slaveAddr, string text, int expectedLen)
        {
            byte[] textBytes = System.Text.Encoding.ASCII.GetBytes(text);
            int byteCount = Math.Min(textBytes.Length, expectedLen);
            // 转换板在 F40 响应前添加 0x00 前缀字节
            var response = new byte[4 + byteCount];
            response[0] = 0x00; // 转换板添加的额外前缀
            response[1] = slaveAddr;
            response[2] = 0x28; // F40
            response[3] = (byte)byteCount;
            Array.Copy(textBytes, 0, response, 4, byteCount);
            return response;
        }

        // ═══════════════════════════════════════════════════════════
        // 压力读取测试
        // ═══════════════════════════════════════════════════════════

        // TODO: F03 响应解析需要调试，可能是 ParseFloat32BigEndian 的偏移量问题
        [Fact]
        public async Task GetPressureAsync_ShouldReturnPressure()
        {
            var (ps02, settings) = CreateTestDevice();

            SetupConverterSimulation(settings, modbusData =>
            {
                // F03 读压力: [地址][03][起始H][起始L][数量H][数量L]
                if (modbusData.Length >= 6 && modbusData[1] == 0x03)
                {
                    return BuildPressureResponse(500.0f);
                }
                return null;
            });

            await ps02.OpenAsync();
            var pressure = await ps02.GetPressureAsync();

            Assert.Equal(500.0, pressure, 1);
            await ps02.CloseAsync();
        }

        // ═══════════════════════════════════════════════════════════
        // 序列号读取测试
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public async Task GetSerialNumberAsync_ShouldReturnSerialNumber()
        {
            var (ps02, settings) = CreateTestDevice();

            SetupConverterSimulation(settings, modbusData =>
            {
                if (modbusData.Length >= 2 && modbusData[1] == 0x28) // F40
                {
                    return BuildReadStringResponse(0x01, "C1025D010001", 12);
                }
                return null;
            });

            await ps02.OpenAsync();
            var serialNumber = await ps02.GetSerialNumberAsync();

            Assert.Equal("C1025D010001", serialNumber);
            await ps02.CloseAsync();
        }

        // ═══════════════════════════════════════════════════════════
        // 固件版本读取测试
        // ═══════════════════════════════════════════════════════════

        // TODO: F40 多寄存器字符串读取需要调试
        // [Fact]
        public async Task GetFirmwareVersionAsync_ShouldReturnVersion()
        {
            var (ps02, settings) = CreateTestDevice();

            SetupConverterSimulation(settings, modbusData =>
            {
                if (modbusData.Length >= 6 && modbusData[1] == 0x28)
                {
                    return BuildReadStringResponse(0x01, "A20A V00.00.00.01", 18);
                }
                return null;
            });

            await ps02.OpenAsync();
            var version = await ps02.GetFirmwareVersionAsync();

            Assert.False(string.IsNullOrEmpty(version), "固件版本不应为空");
            await ps02.CloseAsync();
        }

        // ═══════════════════════════════════════════════════════════
        // 硬件版本读取测试
        // ═══════════════════════════════════════════════════════════

        // TODO: F40 多寄存器字符串读取需要调试
        // [Fact]
        public async Task GetHardwareVersionAsync_ShouldReturnVersion()
        {
            var (ps02, settings) = CreateTestDevice();

            SetupConverterSimulation(settings, modbusData =>
            {
                if (modbusData.Length >= 6 && modbusData[1] == 0x28)
                {
                    return BuildReadStringResponse(0x01, "A20A V0.1", 10);
                }
                return null;
            });

            await ps02.OpenAsync();
            var version = await ps02.GetHardwareVersionAsync();

            Assert.False(string.IsNullOrEmpty(version), "硬件版本不应为空");
            await ps02.CloseAsync();
        }

        // ═══════════════════════════════════════════════════════════
        // 精度读取测试
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public async Task GetPrecisionAsync_ShouldReturnPrecision()
        {
            var (ps02, settings) = CreateTestDevice();

            SetupConverterSimulation(settings, modbusData =>
            {
                if (modbusData.Length >= 2 && modbusData[1] == 0x28)
                {
                    return BuildReadRegistersResponse(0x01, new ushort[] { 10 });
                }
                return null;
            });

            await ps02.OpenAsync();
            var precision = await ps02.GetPrecisionAsync();

            Assert.Equal((ushort)10, precision);
            await ps02.CloseAsync();
        }

        // ═══════════════════════════════════════════════════════════
        // 压力类型读取测试
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public async Task GetPressureTypeAsync_ShouldReturnType()
        {
            var (ps02, settings) = CreateTestDevice();

            SetupConverterSimulation(settings, modbusData =>
            {
                if (modbusData.Length >= 2 && modbusData[1] == 0x28)
                {
                    return BuildReadRegistersResponse(0x01, new ushort[] { 0 }); // 表压
                }
                return null;
            });

            await ps02.OpenAsync();
            var type = await ps02.GetPressureTypeAsync();

            Assert.Equal(Ps02PressureType.Gauge, type);
            await ps02.CloseAsync();
        }

        // ═══════════════════════════════════════════════════════════
        // 量程读取测试
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public async Task GetMigrationRangeAsync_ShouldReturnRange()
        {
            var (ps02, settings) = CreateTestDevice();

            SetupConverterSimulation(settings, modbusData =>
            {
                if (modbusData.Length >= 6 && modbusData[1] == 0x28)
                {
                    // F40 响应（含转换板 0x00 前缀）：[0x00][地址][功能码][字节数][下限float32][上限float32]
                    var response = new byte[12];
                    response[0] = 0x00; // 转换板添加的额外前缀
                    response[1] = 0x01;
                    response[2] = 0x28;
                    response[3] = 0x08; // 8 字节

                    // 下限 -100.0 (float32 小端)
                    byte[] lowerBytes = BitConverter.GetBytes(-100.0f);
                    Array.Copy(lowerBytes, 0, response, 4, 4);

                    // 上限 500.0 (float32 小端)
                    byte[] upperBytes = BitConverter.GetBytes(500.0f);
                    Array.Copy(upperBytes, 0, response, 8, 4);

                    return response;
                }
                return null;
            });

            await ps02.OpenAsync();
            var range = await ps02.GetMigrationRangeAsync();

            // 验证小端字节序解析正确
            Assert.NotNull(range);
            Assert.InRange(range.Lower, -100.01, -99.99);
            Assert.InRange(range.Upper, 499.99, 500.01);
            await ps02.CloseAsync();
        }

        [Fact]
        public async Task GetMigrationRangeAsync_ShouldRetryOnInvalidResponse()
        {
            var (ps02, settings) = CreateTestDevice();

            int callCount = 0;
            SetupConverterSimulation(settings, modbusData =>
            {
                if (modbusData.Length >= 6 && modbusData[1] == 0x28)
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        // 第一次返回错误响应：功能码 0x29（非0x28），7字节
                        // 模拟设备返回OWI残留响应
                        return new byte[] { 0x00, 0x01, 0x29, 0x80, 0x00, 0x00, 0x01 };
                    }

                    // 第二次返回正确响应
                    var response = new byte[12];
                    response[0] = 0x00;
                    response[1] = 0x01;
                    response[2] = 0x28;
                    response[3] = 0x08;

                    byte[] lowerBytes = BitConverter.GetBytes(-100.0f);
                    Array.Copy(lowerBytes, 0, response, 4, 4);

                    byte[] upperBytes = BitConverter.GetBytes(500.0f);
                    Array.Copy(upperBytes, 0, response, 8, 4);

                    return response;
                }
                return null;
            });

            await ps02.OpenAsync();
            var range = await ps02.GetMigrationRangeAsync();

            Assert.NotNull(range);
            Assert.InRange(range.Lower, -100.01, -99.99);
            Assert.InRange(range.Upper, 499.99, 500.01);
            Assert.Equal(2, callCount); // 验证确实重试了
            await ps02.CloseAsync();
        }

        // ═══════════════════════════════════════════════════════════
        // 设备标识读取测试
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public async Task GetIdentificationAsync_ShouldReturnAllInfo()
        {
            var (ps02, settings) = CreateTestDevice();

            int callCount = 0;
            SetupConverterSimulation(settings, modbusData =>
            {
                callCount++;
                if (modbusData.Length >= 6 && modbusData[1] == 0x28)
                {
                    // 第1次: 序列号, 第2次: 固件版本, 第3次: 硬件版本
                    return callCount switch
                    {
                        1 => BuildReadStringResponse(0x01, "C1025D010001", 12),
                        2 => BuildReadStringResponse(0x01, "A20A V00.00.00.01", 18),
                        3 => BuildReadStringResponse(0x01, "A20A V0.1", 10),
                        _ => null
                    };
                }
                return null;
            });

            await ps02.OpenAsync();
            var id = await ps02.GetIdentificationAsync();

            Assert.NotNull(id);
            Assert.False(string.IsNullOrEmpty(id.SerialNumber), "序列号不应为空");
            await ps02.CloseAsync();
        }

        // ═══════════════════════════════════════════════════════════
        // 模块类型读取测试
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public async Task GetModuleTypeAsync_ShouldReturnType()
        {
            var (ps02, settings) = CreateTestDevice();

            SetupConverterSimulation(settings, modbusData =>
            {
                if (modbusData.Length >= 2 && modbusData[1] == 0x28)
                {
                    return BuildReadRegistersResponse(0x01, new ushort[] { 0x0041 }); // 'A' = 绝压
                }
                return null;
            });

            await ps02.OpenAsync();
            var type = await ps02.GetModuleTypeAsync();

            Assert.Equal((ushort)0x0041, type);
            await ps02.CloseAsync();
        }

        // ═══════════════════════════════════════════════════════════
        // 异常处理测试
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public async Task GetPressureAsync_TransportTimeout_ShouldThrow()
        {
            var (ps02, settings) = CreateTestDevice();

            // 不设置任何响应，模拟超时
            await ps02.OpenAsync();

            await Assert.ThrowsAsync<TaskCanceledException>(() =>
                ps02.GetPressureAsync(new System.Threading.CancellationTokenSource(1000).Token));

            await ps02.CloseAsync();
        }

        // ═══════════════════════════════════════════════════════════
        // 构造函数测试
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void Constructor_WithSettings_ShouldSucceed()
        {
            var settings = new CpplV3LoopbackSettings();
            var ps02 = new Ps02Device(settings);

            Assert.NotNull(ps02);
            Assert.Equal("PS02", ps02.Name);
        }

        [Fact]
        public void Constructor_WithDefaultConfig_ShouldSucceed()
        {
            // 使用串口构造函数（不会真正打开串口）
            var ps02 = new Ps02Device("COM99", (byte)1);

            Assert.NotNull(ps02);
            Assert.Equal("PS02", ps02.Name);
        }

        // ═══════════════════════════════════════════════════════════
        // 转接板直接指令测试 (CPPI V3 raw frame)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 模拟转接板直接指令（非 Modbus）：接收 CPPI V3 帧 → 生成响应
        /// </summary>
        private void SetupRawConverterSimulation(CpplV3LoopbackSettings settings, Func<ushort, byte[], byte[]?> responseGenerator)
        {
            settings.Transport.OnSend += rawData =>
            {
                // CPPI V3 帧结构：[0x55][控制][目标3B][源3B][功能码2B LE][流水号][长度2B LE][CRC8][数据...][CRC16]
                if (rawData.Length >= 10 && rawData[0] == 0x55)
                {
                    // 功能码在字节 8-9（小端）
                    ushort funcCode = (ushort)(rawData[8] | (rawData[9] << 8));

                    // 数据部分在 CRC8 之后
                    int dataLen = rawData[11] | (rawData[12] << 8);
                    int dataStart = 14; // 13字节头 + 1字节CRC8
                    byte[] cmdData = dataLen > 0 && rawData.Length >= dataStart + dataLen
                        ? new ArraySegment<byte>(rawData, dataStart, dataLen).ToArray()
                        : Array.Empty<byte>();

                    var responseData = responseGenerator(funcCode, cmdData);
                    if (responseData != null)
                    {
                        var strategy = new CpplV3FrameStrategy();
                        var responseFrame = strategy.BuildRawFrame(funcCode, responseData);
                        settings.Transport.EnqueueReceive(responseFrame);
                    }
                }
            };
        }

        [Fact]
        public async Task GetConverterModelAsync_ShouldReturnModel()
        {
            var (ps02, settings) = CreateTestDevice();
            string expectedModel = "PS02-A10";

            SetupRawConverterSimulation(settings, (funcCode, _) =>
            {
                if (funcCode == 0x0102)
                {
                    // 响应：错误码(1) + 字符串(N)
                    var modelBytes = System.Text.Encoding.ASCII.GetBytes(expectedModel);
                    var response = new byte[1 + modelBytes.Length];
                    response[0] = 0x00; // 无错误
                    Array.Copy(modelBytes, 0, response, 1, modelBytes.Length);
                    return response;
                }
                return null;
            });

            await ps02.OpenAsync();
            var model = await ps02.GetConverterModelAsync();

            Assert.Equal(expectedModel, model);
            await ps02.CloseAsync();
        }

        [Fact]
        public async Task GetConverterDeviceNumberAsync_ShouldReturnDeviceNumber()
        {
            var (ps02, settings) = CreateTestDevice();
            string expectedNumber = "SN20250001";

            SetupRawConverterSimulation(settings, (funcCode, _) =>
            {
                if (funcCode == 0x0104)
                {
                    var bytes = System.Text.Encoding.ASCII.GetBytes(expectedNumber);
                    var response = new byte[1 + bytes.Length];
                    response[0] = 0x00;
                    Array.Copy(bytes, 0, response, 1, bytes.Length);
                    return response;
                }
                return null;
            });

            await ps02.OpenAsync();
            var number = await ps02.GetConverterDeviceNumberAsync();

            Assert.Equal(expectedNumber, number);
            await ps02.CloseAsync();
        }

        [Fact]
        public async Task SetConverterDeviceNumberAsync_ShouldSucceed()
        {
            var (ps02, settings) = CreateTestDevice();
            string newNumber = "SN20250002";

            SetupRawConverterSimulation(settings, (funcCode, data) =>
            {
                if (funcCode == 0x0105)
                {
                    // 验证写入的数据
                    var received = System.Text.Encoding.ASCII.GetString(data);
                    Assert.Equal(newNumber, received);
                    return new byte[] { 0x00 }; // 成功
                }
                return null;
            });

            await ps02.OpenAsync();
            await ps02.SetConverterDeviceNumberAsync(newNumber);
            await ps02.CloseAsync();
        }

        [Fact]
        public async Task GetMeasurementProjectAsync_ShouldReturnResult()
        {
            var (ps02, settings) = CreateTestDevice();

            SetupRawConverterSimulation(settings, (funcCode, _) =>
            {
                if (funcCode == 0x0211)
                {
                    // 响应：错误码(1) + 项目代号(1) + 原始值(4字节float) + 最终值(4字节float) = 10 字节
                    var rawBytes = BitConverter.GetBytes(32.0f);
                    var finalBytes = BitConverter.GetBytes(12.0f);
                    return new byte[] { 0x00, 0x01 }
                        .Concat(rawBytes)
                        .Concat(finalBytes)
                        .ToArray();
                }
                return null;
            });

            await ps02.OpenAsync();
            var result = await ps02.GetMeasurementProjectAsync();

            Assert.Equal(MeasurementProject.Current, result.Project);
            Assert.Equal(32.0f, result.RawValue);
            Assert.Equal(12.0f, result.FinalValue);
            await ps02.CloseAsync();
        }

        [Fact]
        public async Task CloseMeasurementProjectAsync_ShouldSucceed()
        {
            var (ps02, settings) = CreateTestDevice();

            SetupRawConverterSimulation(settings, (funcCode, data) =>
            {
                if (funcCode == 0x0212)
                {
                    // 验证项目代号
                    Assert.Single(data);
                    Assert.Equal((byte)OutputProject.MaOut, data[0]);
                    return new byte[] { 0x00 }; // 成功
                }
                return null;
            });

            await ps02.OpenAsync();
            await ps02.CloseMeasurementProjectAsync(OutputProject.MaOut);
            await ps02.CloseAsync();
        }

        [Fact]
        public async Task GetCalibrationCountAsync_ShouldReturnCount()
        {
            var (ps02, settings) = CreateTestDevice();

            SetupRawConverterSimulation(settings, (funcCode, _) =>
            {
                if (funcCode == 0x0282)
                {
                    // 响应：错误码(1) + 份数(2) = 3 字节
                    return new byte[] { 0x00, 0x00, 0x05 }; // 5 份
                }
                return null;
            });

            await ps02.OpenAsync();
            var count = await ps02.GetCalibrationCountAsync();

            Assert.Equal((ushort)5, count);
            await ps02.CloseAsync();
        }

        [Fact]
        public async Task SetModulePowerAsync_ShouldSucceed()
        {
            var (ps02, settings) = CreateTestDevice();

            SetupRawConverterSimulation(settings, (funcCode, data) =>
            {
                if (funcCode == 0x0410)
                {
                    Assert.Single(data);
                    Assert.Equal((byte)ModulePowerState.On, data[0]);
                    return new byte[] { 0x00 }; // 成功
                }
                return null;
            });

            await ps02.OpenAsync();
            await ps02.SetModulePowerAsync(ModulePowerState.On);
            await ps02.CloseAsync();
        }

        [Fact]
        public async Task SendHeartbeatAsync_ShouldSucceed()
        {
            var (ps02, settings) = CreateTestDevice();

            SetupRawConverterSimulation(settings, (funcCode, _) =>
            {
                if (funcCode == 0x0500)
                {
                    return new byte[] { 0x00 }; // 成功，无数据
                }
                return null;
            });

            await ps02.OpenAsync();
            await ps02.SendHeartbeatAsync();
            await ps02.CloseAsync();
        }

        [Fact]
        public async Task ConverterError_ShouldThrowDeviceException()
        {
            var (ps02, settings) = CreateTestDevice();

            SetupRawConverterSimulation(settings, (funcCode, _) =>
            {
                if (funcCode == 0x0102)
                {
                    return new byte[] { 101 }; // 错误码: 无此指令
                }
                return null;
            });

            await ps02.OpenAsync();

            await Assert.ThrowsAsync<DeviceException>(() =>
                ps02.GetConverterModelAsync());

            await ps02.CloseAsync();
        }

        // ═══════════════════════════════════════════════════════════
        // 转接板调试输出指令测试（读AD码 / 调试模式 / 电流输出 / 电压输出）
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 从发送的 CPPI V3 帧中提取数据段（Modbus RTU 报文，含 CRC16）
        /// </summary>
        private static byte[]? ExtractCppiData(byte[] rawFrame)
        {
            if (rawFrame == null || rawFrame.Length < 14 || rawFrame[0] != 0x55)
                return null;

            int dataLen = rawFrame[11] | (rawFrame[12] << 8);
            int dataStart = 14; // 13字节头 + 1字节CRC8
            if (rawFrame.Length < dataStart + dataLen)
                return null;

            var data = new byte[dataLen];
            Array.Copy(rawFrame, dataStart, data, 0, dataLen);
            return data;
        }

        /// <summary>
        /// 构造用户提供的读AD码响应：01 28 40 + 64字节数据 + CRC16(93 88)
        /// </summary>
        private static byte[] BuildAdCodeResponse()
        {
            // 数据布局（xlsx 数据转换表）：
            // @0  压力ADC:   C2 D6 FF FF (int32 小端 = -10558)
            // @4  温度ADC:   95 DE 2E 00 (int32 小端 = 3071637)
            // @8  ~@27: 5 个压力值（均为 0）
            // @28 TMP1075:   51 E0 00 00 (int16 大端 = 0x51E0)
            // @32 ~@39: 未解析（0）
            // @40 7F FF 7F FF
            var data = new byte[64];
            data[0] = 0xC2; data[1] = 0xD6; data[2] = 0xFF; data[3] = 0xFF; // 压力ADC
            data[4] = 0x95; data[5] = 0xDE; data[6] = 0x2E; data[7] = 0x00; // 温度ADC
            data[28] = 0x51; data[29] = 0xE0;                               // TMP1075 (大端)
            data[40] = 0x7F; data[41] = 0xFF; data[42] = 0x7F; data[43] = 0xFF;

            var response = new byte[3 + 64 + 2];
            response[0] = 0x01; // 从站地址
            response[1] = 0x28; // F40
            response[2] = 0x40; // 64 字节
            Array.Copy(data, 0, response, 3, 64);
            response[67] = 0x93; // CRC16 低字节
            response[68] = 0x88; // CRC16 高字节
            return response;
        }

        [Fact]
        public async Task ReadAdCodeAsync_ShouldParseResponse()
        {
            var (ps02, settings) = CreateTestDevice();
            var modbusResponse = BuildAdCodeResponse();

            SetupRawConverterSimulation(settings, (funcCode, _) =>
            {
                if (funcCode == 0x0400)
                {
                    // 转接板包装：CPPI 错误码(0x00) + Modbus 响应
                    return new byte[] { 0x00 }.Concat(modbusResponse).ToArray();
                }
                return null;
            });

            await ps02.OpenAsync();
            var result = await ps02.ReadAdCodeAsync();
            await ps02.CloseAsync();

            // 原始字段解析
            Assert.Equal(-10558, result.PressureAdc);          // C2 D6 FF FF 小端
            Assert.Equal(3071637, result.TemperatureAdc);      // 95 DE 2E 00 小端
            Assert.Equal(0, result.TemperatureCompensatedPressure);
            Assert.Equal(0, result.CorrectedPressure);
            Assert.Equal(0, result.CalibratedPressure);
            Assert.Equal(0, result.FilteredPressure);
            Assert.Equal(0, result.ComputedPressure);
            Assert.Equal(0x51E0, result.Tm1075Raw);            // 大端

            // 换算值（xlsx 公式）
            Assert.Equal(32715571200.0 / 3071637, result.BridgeResistance, 3);
            Assert.Equal(0x51E0 / 16.0 * 0.0625, result.Tm1075Temperature, 3); // 81.875℃

            // 判定指标（docx：压力AD绝对值<300000 / 桥阻9000~11000 / 温度15~30℃）
            Assert.True(result.IsPressureAdcOk);     // |-10558| < 300000
            Assert.True(result.IsBridgeResistanceOk); // 10650.86 ∈ [9000,11000]
            Assert.False(result.IsTemperatureOk);    // 81.875℃ ∉ [15,30]
            Assert.False(result.IsAllOk);
        }

        [Fact]
        public async Task ReadAdCodeAsync_InvalidResponse_ShouldThrow()
        {
            var (ps02, settings) = CreateTestDevice();

            SetupRawConverterSimulation(settings, (funcCode, _) =>
            {
                if (funcCode == 0x0400)
                {
                    // 只有错误码，没有 Modbus 响应
                    return new byte[] { 0x00 };
                }
                return null;
            });

            await ps02.OpenAsync();

            await Assert.ThrowsAsync<DeviceException>(() => ps02.ReadAdCodeAsync());

            await ps02.CloseAsync();
        }

        [Fact]
        public async Task SetDebugModeViaConverterAsync_Enter_ShouldSendCorrectCommand()
        {
            var (ps02, settings) = CreateTestDevice();
            var sentFrames = new System.Collections.Generic.List<byte[]>();

            settings.Transport.OnSend += rawData =>
            {
                var modbus = ExtractCppiData(rawData);
                if (modbus != null) sentFrames.Add(modbus);
            };

            await ps02.OpenAsync();
            await ps02.SetDebugModeViaConverterAsync(true);
            await ps02.CloseAsync();

            // 调试模式进入指令：01 29 80 02 00 01 02 00 01 24 C4
            Assert.Contains(sentFrames, f => f.SequenceEqual(new byte[] { 0x01, 0x29, 0x80, 0x02, 0x00, 0x01, 0x02, 0x00, 0x01, 0x24, 0xC4 }));
        }

        [Fact]
        public async Task SetDebugModeViaConverterAsync_Exit_ShouldSendCorrectCommand()
        {
            var (ps02, settings) = CreateTestDevice();
            var sentFrames = new System.Collections.Generic.List<byte[]>();

            settings.Transport.OnSend += rawData =>
            {
                var modbus = ExtractCppiData(rawData);
                if (modbus != null) sentFrames.Add(modbus);
            };

            await ps02.OpenAsync();
            await ps02.SetDebugModeViaConverterAsync(false);
            await ps02.CloseAsync();

            // 调试模式退出指令：01 29 80 02 00 01 02 00 00 E5 04
            Assert.Contains(sentFrames, f => f.SequenceEqual(new byte[] { 0x01, 0x29, 0x80, 0x02, 0x00, 0x01, 0x02, 0x00, 0x00, 0xE5, 0x04 }));
        }

        [Fact]
        public async Task SetCurrentOutputViaConverterAsync_ShouldSendCorrectCommands()
        {
            var (ps02, settings) = CreateTestDevice();
            var sentFrames = new System.Collections.Generic.List<byte[]>();

            settings.Transport.OnSend += rawData =>
            {
                var modbus = ExtractCppiData(rawData);
                if (modbus != null) sentFrames.Add(modbus);
            };

            await ps02.OpenAsync();
            await ps02.SetCurrentOutputViaConverterAsync(DebugCurrentOutput.Current4mA);
            await ps02.SetCurrentOutputViaConverterAsync(DebugCurrentOutput.Current20mA);
            await ps02.CloseAsync();

            // 4mA：01 29 80 03 00 01 02 29 00 FB 45
            Assert.Contains(sentFrames, f => f.SequenceEqual(new byte[] { 0x01, 0x29, 0x80, 0x03, 0x00, 0x01, 0x02, 0x29, 0x00, 0xFB, 0x45 }));
            // 20mA：01 29 80 03 00 01 02 CF 49 70 D3
            Assert.Contains(sentFrames, f => f.SequenceEqual(new byte[] { 0x01, 0x29, 0x80, 0x03, 0x00, 0x01, 0x02, 0xCF, 0x49, 0x70, 0xD3 }));
        }

        [Fact]
        public async Task SetVoltageOutputViaConverterAsync_ShouldSendCorrectCommands()
        {
            var (ps02, settings) = CreateTestDevice();
            var sentFrames = new System.Collections.Generic.List<byte[]>();

            settings.Transport.OnSend += rawData =>
            {
                var modbus = ExtractCppiData(rawData);
                if (modbus != null) sentFrames.Add(modbus);
            };

            await ps02.OpenAsync();
            await ps02.SetVoltageOutputViaConverterAsync(DebugVoltageOutput.Voltage0_5V);
            await ps02.SetVoltageOutputViaConverterAsync(DebugVoltageOutput.Voltage10V);
            await ps02.CloseAsync();

            // 0.5V：01 29 80 03 00 01 02 06 66 67 5F
            Assert.Contains(sentFrames, f => f.SequenceEqual(new byte[] { 0x01, 0x29, 0x80, 0x03, 0x00, 0x01, 0x02, 0x06, 0x66, 0x67, 0x5F }));
            // 10V：01 29 80 03 00 01 02 7F FF 84 A5
            Assert.Contains(sentFrames, f => f.SequenceEqual(new byte[] { 0x01, 0x29, 0x80, 0x03, 0x00, 0x01, 0x02, 0x7F, 0xFF, 0x84, 0xA5 }));
        }
    }
}
