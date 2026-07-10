using DeviceLink.Device.ConST171A;
using DeviceLink.DeviceBase;
using DeviceLink.Transport;
using Xunit;

using ConST171ADevice = DeviceLink.Device.ConST171A.ConST171Base;

namespace DeviceLink.Tests.ConST171A
{
    /// <summary>
    /// ConST171A 压力控制器蓝牙通讯测试
    /// 
    /// 注意：这些测试需要实际的 ConST171A 设备通过蓝牙连接。
    /// 蓝牙设备地址：68:0a:e2:de:a5:2e
    /// 设备名称：ConST171A
    /// 
    /// 如果蓝牙不可用或设备未连接，测试将被跳过。
    /// </summary>
    public class ConST171ABluetoothTests
    {
        // 蓝牙设备配置
        private const string DeviceAddress = "68:0a:e2:de:a5:2e";
        private const string DeviceName = "ConST171A";

        /// <summary>
        /// 检查蓝牙是否可用
        /// </summary>
        private static bool IsBluetoothAvailable()
        {
            try
            {
                using var client = new InTheHand.Net.Sockets.BluetoothClient();
                return true;
            }
            catch (PlatformNotSupportedException)
            {
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 创建蓝牙连接的 ConST171A 设备
        /// </summary>
        private ConST171ADevice CreateBluetoothDevice()
        {
            // 配置蓝牙选项
            var bluetoothOptions = new BluetoothOptions
            {
                DeviceAddress = DeviceAddress,
                ServiceUuid = InTheHand.Net.Bluetooth.BluetoothService.SerialPort,
                ConnectTimeoutMs = 15000,  // 连接超时 15秒
                AutoPair = false,          // 已配对，不需要自动配对
            };

            // 创建蓝牙设置，增加超时时间
            var settings = new BluetoothSettings
            {
                BluetoothOptions = bluetoothOptions,
                ReceiveTimeoutMs = 10000,      // 接收超时 10秒
                ReceiveIdleTimeoutMs = 100,    // 空闲超时 100ms
                MaxRetryCount = 2,             // 重试2次
                RetryDelayMs = 500,            // 重试延迟 500ms
            };

            return new ConST171ADevice(settings);
        }

        [Fact]
        public async Task GetIdentificationAsync_ShouldReturnIdentification()
        {
            // 检查蓝牙是否可用
            if (!IsBluetoothAvailable())
            {
                return; // 跳过测试
            }

            // Arrange
            using var device = CreateBluetoothDevice();
            await device.OpenAsync();

            try
            {
                // Act
                var id = await device.GetIdentificationAsync();

                // Assert
                Assert.NotNull(id);
                Assert.NotEmpty(id.Manufacturer);
                Assert.NotEmpty(id.Model);

                Console.WriteLine($"设备标识: {id.Manufacturer}, {id.Model}, {id.SerialNumber}");
            }
            finally
            {
                await device.CloseAsync();
            }
        }

        [Fact]
        public async Task GetPressureAsync_ShouldReturnDualPressure()
        {
            // 检查蓝牙是否可用
            if (!IsBluetoothAvailable())
            {
                return;
            }

            // Arrange
            using var device = CreateBluetoothDevice();
            await device.OpenAsync();

            try
            {
                // Act
                var result = await device.GetPressureAsync();

                // Assert
                Assert.True(result.IsValid, "应返回有效的双气源压力值");
                Assert.False(double.IsNaN(result.PositiveValue), "正压值应有效");
                Assert.False(double.IsNaN(result.VacuumValue), "真空值应有效");
                Assert.NotEmpty(result.PositiveUnit);
                Assert.NotEmpty(result.VacuumUnit);

                Console.WriteLine($"正压: {result.PositiveValue} {result.PositiveUnit}");
                Console.WriteLine($"真空: {result.VacuumValue} {result.VacuumUnit}");
            }
            finally
            {
                await device.CloseAsync();
            }
        }

        [Fact]
        public async Task GetPressureAsync_WithSourcePressure_ShouldReturnPressure()
        {
            // 检查蓝牙是否可用
            if (!IsBluetoothAvailable())
            {
                return;
            }

            // Arrange
            using var device = CreateBluetoothDevice();
            await device.OpenAsync();

            try
            {
                // Act
                var result = await device.GetPressureAsync(SourceModule.Pressure);

                // Assert
                Assert.False(double.IsNaN(result.Value));
                Assert.NotNull(result.Unit);
                Assert.NotEmpty(result.Unit);

                Console.WriteLine($"正压值: {result.Value} {result.Unit}");
            }
            finally
            {
                await device.CloseAsync();
            }
        }

        [Fact]
        public async Task GetPressureAsync_WithSourceVacuum_ShouldReturnPressure()
        {
            // 检查蓝牙是否可用
            if (!IsBluetoothAvailable())
            {
                return;
            }

            // Arrange
            using var device = CreateBluetoothDevice();
            await device.OpenAsync();

            try
            {
                // Act
                var result = await device.GetPressureAsync(SourceModule.Vacuum);

                // Assert
                Assert.False(double.IsNaN(result.Value));
                Assert.NotNull(result.Unit);
                Assert.NotEmpty(result.Unit);

                Console.WriteLine($"真空值: {result.Value} {result.Unit}");
            }
            finally
            {
                await device.CloseAsync();
            }
        }

        [Fact]
        public async Task GetPressureUnitAsync_ShouldReturnUnit()
        {
            // 检查蓝牙是否可用
            if (!IsBluetoothAvailable())
            {
                return;
            }

            // Arrange
            using var device = CreateBluetoothDevice();
            await device.OpenAsync();

            try
            {
                // Act
                var pressureUnit = await device.GetPressureUnitAsync(SourceModule.Pressure);
                var vacuumUnit = await device.GetPressureUnitAsync(SourceModule.Vacuum);

                // Assert
                Assert.NotNull(pressureUnit);
                Assert.NotEmpty(pressureUnit);
                Assert.NotNull(vacuumUnit);
                Assert.NotEmpty(vacuumUnit);

                Console.WriteLine($"正压单位: {pressureUnit}");
                Console.WriteLine($"真空单位: {vacuumUnit}");
            }
            finally
            {
                await device.CloseAsync();
            }
        }

        [Fact]
        public async Task GetPressureRangeAsync_ShouldReturnPressureRange()
        {
            // 检查蓝牙是否可用
            if (!IsBluetoothAvailable())
            {
                return;
            }

            // Arrange
            using var device = CreateBluetoothDevice();
            await device.OpenAsync();

            try
            {
                // Act
                var range = await device.GetPressureRangeAsync(SourceModule.Pressure);

                // Assert
                Assert.NotNull(range);
                Assert.True(range.IsValid, "压力范围应有效");
                Assert.True(range.Max >= range.Min, "上限应大于等于下限");

                Console.WriteLine($"压力范围: {range.Min} - {range.Max}");
            }
            finally
            {
                await device.CloseAsync();
            }
        }

        [Fact]
        public async Task GetSerialNumberAsync_ShouldReturnSerialNumber()
        {
            // 检查蓝牙是否可用
            if (!IsBluetoothAvailable())
            {
                return;
            }

            // Arrange
            using var device = CreateBluetoothDevice();
            await device.OpenAsync();

            try
            {
                // Act
                var serialNumber = await device.GetSerialNumberAsync();

                // Assert
                Assert.NotNull(serialNumber);
                Assert.NotEmpty(serialNumber);

                Console.WriteLine($"设备序列号: {serialNumber}");
            }
            finally
            {
                await device.CloseAsync();
            }
        }

        [Fact]
        public async Task GetModelAsync_ShouldReturnModel()
        {
            // 检查蓝牙是否可用
            if (!IsBluetoothAvailable())
            {
                return;
            }

            // Arrange
            using var device = CreateBluetoothDevice();
            await device.OpenAsync();

            try
            {
                // Act
                var model = await device.GetModelAsync();

                // Assert
                Assert.NotNull(model);
                Assert.NotEmpty(model);

                Console.WriteLine($"设备型号: {model}");
            }
            finally
            {
                await device.CloseAsync();
            }
        }
    }
}
