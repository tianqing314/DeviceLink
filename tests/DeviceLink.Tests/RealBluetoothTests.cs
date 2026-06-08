using DeviceLink.Device.ConST326EX;
using DeviceLink.Transport;
using System;
using System.Threading.Tasks;
using Xunit;

namespace DeviceLink.Tests
{
    /// <summary>
    /// 真实蓝牙设备测试
    /// 注意：这些测试需要真实的蓝牙适配器和设备，如果蓝牙不可用将自动跳过
    /// </summary>
    public class RealBluetoothTests
    {
        // 蓝牙设备配置
        private const string DeviceName = "ConST326Ex";
        private const string DeviceAddress = "68:0a:e2:de:a5:2e";

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

        [Fact]
        public async Task ConnectToDevice_ShouldWork()
        {
            // 检查蓝牙是否可用
            if (!IsBluetoothAvailable())
            {
                // 跳过测试，而不是失败
                return;
            }

            // Arrange
            var options = new BluetoothOptions
            {
                DeviceAddress = DeviceAddress,
                ServiceUuid = InTheHand.Net.Bluetooth.BluetoothService.SerialPort,
                ConnectTimeoutMs = 10000,
                AutoPair = false
            };

            var device = new DPCEXBase(options);

            try
            {
                // Act
                await device.OpenAsync();

                // 如果连接成功，尝试获取版本
                var version = await device.GetVersion(Device.ConST326EX.Enums.VersionType.APPLication);

                // Assert
                Assert.NotNull(version);
                Assert.Contains("ConST", version, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                // 确保关闭连接
                await device.CloseAsync();
            }
        }

        [Fact]
        public async Task GetVersion_ShouldReturnVersionInfo()
        {
            // 检查蓝牙是否可用
            if (!IsBluetoothAvailable())
            {
                return;
            }

            // Arrange
            var options = new BluetoothOptions
            {
                DeviceAddress = DeviceAddress,
                ServiceUuid = InTheHand.Net.Bluetooth.BluetoothService.SerialPort,
                ConnectTimeoutMs = 10000,
                AutoPair = false
            };

            var device = new DPCEXBase(options);

            try
            {
                // 先连接
                await device.OpenAsync();

                // Act
                var version = await device.GetVersion(Device.ConST326EX.Enums.VersionType.APPLication);

                // Assert
                Assert.NotNull(version);
                Assert.NotEmpty(version);
                
                // 输出版本信息（测试中可见）
                Console.WriteLine($"设备版本: {version}");
            }
            finally
            {
                await device.CloseAsync();
            }
        }

        [Fact]
        public async Task GetApplicationVersion_ShouldReturnVersion()
        {
            // 检查蓝牙是否可用
            if (!IsBluetoothAvailable())
            {
                return;
            }

            // Arrange
            var options = new BluetoothOptions
            {
                DeviceAddress = DeviceAddress,
                ServiceUuid = InTheHand.Net.Bluetooth.BluetoothService.SerialPort,
                ConnectTimeoutMs = 10000,
                AutoPair = false
            };

            var device = new DPCEXBase(options);

            try
            {
                // 先连接
                await device.OpenAsync();

                // Act
                var version = await device.GetVersion(Device.ConST326EX.Enums.VersionType.APPLication);

                // Assert
                Assert.NotNull(version);
                Assert.NotEmpty(version);
                
                Console.WriteLine($"应用程序版本: {version}");
            }
            finally
            {
                await device.CloseAsync();
            }
        }

        [Fact]
        public async Task GetHardwareVersion_ShouldReturnVersion()
        {
            // 检查蓝牙是否可用
            if (!IsBluetoothAvailable())
            {
                return;
            }

            // Arrange
            var options = new BluetoothOptions
            {
                DeviceAddress = DeviceAddress,
                ServiceUuid = InTheHand.Net.Bluetooth.BluetoothService.SerialPort,
                ConnectTimeoutMs = 10000,
                AutoPair = false
            };

            var device = new DPCEXBase(options);

            try
            {
                // 先连接
                await device.OpenAsync();

                // Act
                var version = await device.GetVersion(Device.ConST326EX.Enums.VersionType.HARDware);

                // Assert
                Assert.NotNull(version);
                Assert.NotEmpty(version);
                
                Console.WriteLine($"硬件版本: {version}");
            }
            finally
            {
                await device.CloseAsync();
            }
        }
    }
}