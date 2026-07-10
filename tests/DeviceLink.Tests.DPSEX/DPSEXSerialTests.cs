using DeviceLink.Device.DPSEX.Datas;
using System.IO.Ports;
using Xunit;
using Xunit.Abstractions;
using DPSEXDevice = DeviceLink.Device.DPSEX.DPSEX;

namespace DeviceLink.Tests.DPSEX
{
    /// <summary>
    /// DPSEX 智能数字压力模块串口通讯测试。
    /// 
    /// 使用真实串口连接设备，验证所有方法功能。
    /// 
    /// 使用方法：
    /// 1. 连接 DPSEX 设备到串口
    /// 2. 设置环境变量：
    ///    - DPSEX_SERIAL_PORT：串口号（默认 COM3）
    ///    - DPSEX_BAUD_RATE：波特率（默认 9600）
    ///    - DPSEX_ADDRESS：设备地址（默认 255）
    /// 3. 运行：dotnet test --filter "Category=DPSEX"
    /// </summary>
    public class DPSEXSerialTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly string _portName;
        private readonly int _baudRate;
        private readonly byte _address;
        private DPSEXDevice? _device;
        private bool _disposed;

        public DPSEXSerialTests(ITestOutputHelper output)
        {
            _output = output;
            _portName = Environment.GetEnvironmentVariable("DPSEX_SERIAL_PORT") ?? "COM3";
            _baudRate = int.TryParse(Environment.GetEnvironmentVariable("DPSEX_BAUD_RATE"), out var b) ? b : 4800;
            _address = byte.TryParse(Environment.GetEnvironmentVariable("DPSEX_ADDRESS"), out var a) ? a : (byte)255;
            _output.WriteLine($"配置: {_portName}, {_baudRate}bps, 地址={_address}");
        }

        #region 辅助方法

        private bool IsPortAvailable()
        {
            try
            {
                var ports = SerialPort.GetPortNames();
                if (Array.IndexOf(ports, _portName) < 0)
                {
                    _output.WriteLine($"串口 {_portName} 不存在，可用: {string.Join(", ", ports)}");
                    return false;
                }
                using var p = new SerialPort(_portName, _baudRate);
                p.Open(); p.Close();
                return true;
            }
            catch (Exception ex)
            {
                _output.WriteLine($"串口不可用: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> OpenAsync()
        {
            try
            {
                _device = new DPSEXDevice(_portName, _baudRate, 8, StopBits.Two, Parity.None, _address);
                await _device.OpenAsync();
                _output.WriteLine("设备已打开");
                return true;
            }
            catch (Exception ex)
            {
                _output.WriteLine($"打开失败: {ex.Message}");
                _device?.Dispose(); _device = null;
                return false;
            }
        }

        private async Task CloseAsync()
        {
            if (_device != null)
            {
                try { await _device.CloseAsync(); }
                catch (Exception ex) { _output.WriteLine($"关闭异常: {ex.Message}"); }
                finally { _device.Dispose(); _device = null; }
            }
        }

        public void Dispose()
        {
            if (!_disposed) { _device?.Dispose(); _disposed = true; }
            GC.SuppressFinalize(this);
        }

        #endregion

        #region 连接测试

        [Fact, Trait("Category", "DPSEX")]
        public async Task Connect_ShouldSucceed()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                Assert.True(_device!.IsOpen);
                _output.WriteLine("连接测试通过");
            }
            finally { await CloseAsync(); }
        }

        #endregion

        #region 设备信息

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetVersion_ShouldReturnNonEmpty()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var v = await _device!.GetVersionAsync();
                _output.WriteLine($"固件版本: {v}");
                Assert.False(string.IsNullOrEmpty(v));
            }
            finally { await CloseAsync(); }
        }

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetHardwareVersion_ShouldReturnNonEmpty()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var v = await _device!.GetHardwareVersionAsync();
                _output.WriteLine($"硬件版本: {v}");
                Assert.False(string.IsNullOrEmpty(v));
            }
            finally { await CloseAsync(); }
        }

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetSerialNumber_ShouldReturnNonEmpty()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var v = await _device!.GetSerialNumberAsync();
                _output.WriteLine($"序列号: {v}");
                Assert.False(string.IsNullOrEmpty(v));
            }
            finally { await CloseAsync(); }
        }

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetProductionDate_ShouldReturnNonEmpty()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var v = await _device!.GetProductionDateAsync();
                _output.WriteLine($"生产日期: {v}");
                Assert.False(string.IsNullOrEmpty(v));
            }
            finally { await CloseAsync(); }
        }

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetInstrumentCode_ShouldReturnNonEmpty()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var v = await _device!.GetInstrumentCodeAsync();
                _output.WriteLine($"仪器编码: {v}");
                Assert.False(string.IsNullOrEmpty(v));
            }
            finally { await CloseAsync(); }
        }

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetDeviceInfo_ShouldReturnNonEmpty()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var v = await _device!.GetDeviceInfoAsync();
                _output.WriteLine($"设备标识: {v}");
                Assert.False(string.IsNullOrEmpty(v));
            }
            finally { await CloseAsync(); }
        }

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetAddress_ShouldReturnValid()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var addr = await _device!.GetAddressAsync();
                _output.WriteLine($"地址: {addr}");
                Assert.InRange(addr, 0, 255);
            }
            finally { await CloseAsync(); }
        }

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetTag_ShouldReturnString()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var tag = await _device!.GetTagAsync();
                _output.WriteLine($"标签: {tag}");
            }
            finally { await CloseAsync(); }
        }

        #endregion

        #region 压力测量

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetPressureWithUnit_ShouldReturnValid()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var p = await _device!.GetPressureWithUnitAsync();
                _output.WriteLine($"压力: {p.Value} {p.Unit}, 有效={p.IsValid}");
                Assert.NotNull(p);
                Assert.True(p.IsValid);
                Assert.False(string.IsNullOrEmpty(p.Unit));
            }
            finally { await CloseAsync(); }
        }

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetPressureUnit_ShouldReturnNonEmpty()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var u = await _device!.GetPressureUnitAsync();
                _output.WriteLine($"单位: {u}");
                Assert.False(string.IsNullOrEmpty(u));
            }
            finally { await CloseAsync(); }
        }

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetTemperature_ShouldReturnValid()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var t = await _device!.GetTemperatureAsync();
                _output.WriteLine($"温度: {t}°C");
                Assert.False(double.IsNaN(t));
            }
            finally { await CloseAsync(); }
        }

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetRawMeasurement_ShouldReturnFields()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var raw = await _device!.GetRawMeasurementAsync();
                _output.WriteLine($"原始数据 [{raw.Length}]: {string.Join(", ", raw)}");
                Assert.NotNull(raw);
                Assert.True(raw.Length > 0);
            }
            finally { await CloseAsync(); }
        }

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetSensorExcitation_ShouldReturnData()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var data = await _device!.GetSensorExcitationAsync();
                _output.WriteLine($"激励: {string.Join(", ", data)}");
                Assert.NotNull(data);
            }
            finally { await CloseAsync(); }
        }

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetCalibrationBeforeData_ShouldReturnValid()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var data = await _device!.GetCalibrationBeforeDataAsync();
                _output.WriteLine($"校准前: {data.MeasureValue} {data.Unit}, 有效={data.IsValid}");
                Assert.NotNull(data);
            }
            finally { await CloseAsync(); }
        }

        #endregion

        #region 压力范围与精度

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetPressureRangeDetailedInfo_ShouldReturnValid()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var info = await _device!.GetPressureRangeDetailedInfoAsync();
                _output.WriteLine($"范围: {info.Low} ~ {info.High} {info.Unit}");
                _output.WriteLine($"类型: {info.PressureType}, 精度指数={info.AccuracyIndex}, 精度%={info.AccuracyPercent}");
                Assert.NotNull(info);
                Assert.True(info.High > info.Low);
            }
            finally { await CloseAsync(); }
        }

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetAccuracyInfo_ShouldReturnValid()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var acc = await _device!.GetAccuracyInfoAsync();
                _output.WriteLine($"精度: {acc}");
                Assert.True(acc >= 0);
            }
            finally { await CloseAsync(); }
        }

        #endregion

        #region 工作模式

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetWorkMode_ShouldReturnValid()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var mode = await _device!.GetWorkModeEnumAsync();
                _output.WriteLine($"工作模式: {mode} ({(int)mode})");
                Assert.True(Enum.IsDefined(typeof(PressureWorkMode), mode));
            }
            finally { await CloseAsync(); }
        }

        [Fact, Trait("Category", "DPSEX")]
        public async Task SetWorkMode_Normal_ShouldSucceed()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                await _device!.SetWorkModeEnumAsync(PressureWorkMode.Normal);
                var mode = await _device.GetWorkModeEnumAsync();
                _output.WriteLine($"设置后工作模式: {mode}");
                Assert.Equal(PressureWorkMode.Normal, mode);
            }
            finally { await CloseAsync(); }
        }

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetWorkModeTestType_ShouldReturnValid()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var t = await _device!.GetWorkModeTestTypeAsync();
                _output.WriteLine($"测试类型: {t} ({(int)t})");
                Assert.True(Enum.IsDefined(typeof(WorkModeTestType), t));
            }
            finally { await CloseAsync(); }
        }

        #endregion

        #region 输出速度

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetSpeed_ShouldReturnValid()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var s = await _device!.GetSpeedEnumAsync();
                _output.WriteLine($"速度: {s} ({(int)s})");
                Assert.True(Enum.IsDefined(typeof(OutputSpeed), s));
            }
            finally { await CloseAsync(); }
        }

        [Fact, Trait("Category", "DPSEX")]
        public async Task SetSpeed_High_ShouldSucceed()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                await _device!.SetSpeedEnumAsync(OutputSpeed.High);
                var s = await _device.GetSpeedEnumAsync();
                _output.WriteLine($"设置后速度: {s}");
                Assert.Equal(OutputSpeed.High, s);
            }
            finally { await CloseAsync(); }
        }

        #endregion

        #region 校准状态

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetCalibrationState_ShouldReturnValid()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var state = await _device!.GetCalibrationStateAsync();
                _output.WriteLine($"有效={state.IsValid}, 温补={state.IsTemperatureCompensated}, 线性化={state.IsLinearized}");
                _output.WriteLine($"校准点数={state.CalibrationPointCount}, 工厂={state.IsFactoryCalibrated}, 用户={state.IsUserCalibrated}");
                Assert.NotNull(state);
            }
            finally { await CloseAsync(); }
        }

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetCalibrationDate_ShouldReturnNonEmpty()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var d = await _device!.GetCalibrationDateAsync();
                _output.WriteLine($"校准日期: {d}");
                Assert.False(string.IsNullOrEmpty(d));
            }
            finally { await CloseAsync(); }
        }

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetFactoryCalibrationDate_ShouldReturnNonEmpty()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var d = await _device!.GetFactoryCalibrationDateAsync();
                _output.WriteLine($"工厂校准日期: {d}");
                Assert.False(string.IsNullOrEmpty(d));
            }
            finally { await CloseAsync(); }
        }

        #endregion

        #region 状态与诊断

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetStatus_ShouldReturnNonEmpty()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var s = await _device!.GetStatusAsync();
                _output.WriteLine($"状态: {s}");
                Assert.False(string.IsNullOrEmpty(s));
            }
            finally { await CloseAsync(); }
        }

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetOverPressureFlag_ShouldReturnBool()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var f = await _device!.GetOverPressureFlagAsync();
                _output.WriteLine($"过压标志: {f}");
            }
            finally { await CloseAsync(); }
        }

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetSelfDiagnosis_ShouldReturnValid()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var diag = await _device!.GetSelfDiagnosisAsync();
                _output.WriteLine($"诊断项数: {diag.Items.Count}");
                foreach (var item in diag.Items)
                    _output.WriteLine($"  [{item.Sort}] 故障={item.FaultNo}, 值={item.MeasureValue}");
                Assert.NotNull(diag);
            }
            finally { await CloseAsync(); }
        }

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetDangerRecord_ShouldReturnData()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var r = await _device!.GetDangerRecordAsync();
                _output.WriteLine($"危险记录: {r}");
                Assert.NotNull(r);
            }
            finally { await CloseAsync(); }
        }

        #endregion

        #region 硬件接口

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetRTC_ShouldReturnValid()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var rtc = await _device!.GetRTCAsync();
                _output.WriteLine($"RTC: {rtc.Date} {rtc.Time}");
                Assert.NotNull(rtc);
                Assert.False(string.IsNullOrEmpty(rtc.Date));
                Assert.False(string.IsNullOrEmpty(rtc.Time));
            }
            finally { await CloseAsync(); }
        }

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetFrequency_ShouldReturnValid()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var f = await _device!.GetFrequencyAsync();
                _output.WriteLine($"频率: {f.Frequency1}, {f.Frequency2}");
                Assert.NotNull(f);
            }
            finally { await CloseAsync(); }
        }

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetActuatorBoard_ShouldReturnValid()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var ab = await _device!.GetActuatorBoardAsync();
                _output.WriteLine($"压力: {ab.PressureValue} {ab.PressureUnit}");
                _output.WriteLine($"温度: {ab.TemperatureValue} {ab.TemperatureUnit}");
                Assert.NotNull(ab);
            }
            finally { await CloseAsync(); }
        }

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetAmplification_ShouldReturnPositive()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var amp = await _device!.GetAmplificationAsync();
                _output.WriteLine($"放大倍数: {amp}");
                Assert.True(amp > 0);
            }
            finally { await CloseAsync(); }
        }

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetConstantCurrent_ShouldReturnValid()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var c = await _device!.GetConstantCurrentAsync();
                _output.WriteLine($"恒流源: {c}");
                Assert.True(c >= 0);
            }
            finally { await CloseAsync(); }
        }

        #endregion

        #region 校准验证

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetVerificationTotalNumber_ShouldReturnValid()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var total = await _device!.GetVerificationTotalNumberAsync();
                _output.WriteLine($"校准点总数: {total}");
                Assert.True(total >= 0);
            }
            finally { await CloseAsync(); }
        }

        [Fact, Trait("Category", "DPSEX")]
        public async Task GetPointVerificationInfo_Point0_ShouldReturnValid()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                var info = await _device!.GetPointVerificationInfoAsync(0);
                _output.WriteLine($"校准点0: 时间={info.VerificatiTime}");
                _output.WriteLine($"  滞后最大误差(前)={info.HysterisisMaxErrorBefore}");
                _output.WriteLine($"  TMP117={info.TMP117}, MCU={info.MCU}");
                Assert.NotNull(info);
            }
            finally { await CloseAsync(); }
        }

        #endregion

        #region 写操作测试

        [Fact, Trait("Category", "DPSEX")]
        public async Task PressureZero_ShouldSucceed()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                await _device!.PressureZeroAsync();
                _output.WriteLine("压力置零成功");
            }
            finally { await CloseAsync(); }
        }

        [Fact, Trait("Category", "DPSEX")]
        public async Task SetAmplification_ShouldSucceed()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());
                // 先读取当前值
                var before = await _device!.GetAmplificationAsync();
                _output.WriteLine($"设置前放大倍数: {before}");

                // 设置新值（使用64作为测试值）
                await _device.SetAmplificationAsync(64);
                var after = await _device.GetAmplificationAsync();
                _output.WriteLine($"设置后放大倍数: {after}");
                Assert.Equal(64, after);

                // 恢复原值
                await _device.SetAmplificationAsync(before);
                _output.WriteLine($"已恢复放大倍数: {before}");
            }
            finally { await CloseAsync(); }
        }

        #endregion

        #region 综合流程测试

        [Fact, Trait("Category", "DPSEX")]
        public async Task FullWorkflow_ReadAllInfo_ShouldSucceed()
        {
            if (!IsPortAvailable()) return;
            try
            {
                Assert.True(await OpenAsync());

                _output.WriteLine("═══ 设备信息 ═══");
                _output.WriteLine($"固件: {await _device!.GetVersionAsync()}");
                _output.WriteLine($"硬件: {await _device.GetHardwareVersionAsync()}");
                _output.WriteLine($"序列号: {await _device.GetSerialNumberAsync()}");
                _output.WriteLine($"生产日期: {await _device.GetProductionDateAsync()}");
                _output.WriteLine($"编码: {await _device.GetInstrumentCodeAsync()}");
                _output.WriteLine($"标识: {await _device.GetDeviceInfoAsync()}");
                _output.WriteLine($"地址: {await _device.GetAddressAsync()}");

                _output.WriteLine("\n═══ 压力测量 ═══");
                var p = await _device.GetPressureWithUnitAsync();
                _output.WriteLine($"压力: {p.Value} {p.Unit}");
                _output.WriteLine($"单位: {await _device.GetPressureUnitAsync()}");
                _output.WriteLine($"温度: {await _device.GetTemperatureAsync()}°C");

                _output.WriteLine("\n═══ 范围与模式 ═══");
                var range = await _device.GetPressureRangeDetailedInfoAsync();
                _output.WriteLine($"范围: {range.Low} ~ {range.High} {range.Unit} ({range.PressureType})");
                _output.WriteLine($"工作模式: {await _device.GetWorkModeEnumAsync()}");
                _output.WriteLine($"速度: {await _device.GetSpeedEnumAsync()}");

                _output.WriteLine("\n═══ 校准状态 ═══");
                var cal = await _device.GetCalibrationStateAsync();
                _output.WriteLine($"温补={cal.IsTemperatureCompensated}, 线性化={cal.IsLinearized}");
                _output.WriteLine($"工厂校准={cal.IsFactoryCalibrated}, 用户校准={cal.IsUserCalibrated}");
                _output.WriteLine($"校准点数={cal.CalibrationPointCount}");

                _output.WriteLine("\n═══ 测试完成 ═══");
            }
            finally { await CloseAsync(); }
        }

        #endregion
    }
}
