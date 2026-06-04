using DeviceLink.Device.DPSEX;
using DeviceLink.DeviceBase;
using DeviceLink.Tests.Helpers;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DeviceLink.Tests
{
    /// <summary>
    /// DPSEX 设备单元测试
    /// 
    /// 使用 LoopbackSettings 创建回环测试环境，不依赖物理硬件。
    /// 通过 settings.Transport.OnSend 回调模拟设备响应。
    /// </summary>
    public class DPSEXTests
    {
        /// <summary>
        /// 创建测试用 DPSEX 实例和配套的 LoopbackSettings
        /// </summary>
        /// <returns>(dpsex, settings) 元组</returns>
        private static (DPSEX dpsex, LoopbackSettings settings) CreateTestDevice()
        {
            var settings = new LoopbackSettings();
            var dpsex = new DPSEX("COM3", 4800, 8, System.IO.Ports.StopBits.Two, System.IO.Ports.Parity.None);
            return (dpsex, settings);
        }

        [Fact]
        public async Task GetPressureAsync_ShouldReturnPressure()
        {
            // Arrange
            var (dpsex, settings) = CreateTestDevice();

            settings.Transport.OnSend += data =>
            {
                var text = Encoding.ASCII.GetString(data);
                if (text.Contains("MRMD"))
                {
                    var response = Encoding.ASCII.GetBytes("255:F:MRMD:1.23456\0");
                    settings.Transport.EnqueueReceive(response);
                }
            };

            await dpsex.OpenAsync();

            // Act
            var pressure = await dpsex.GetPressureAsync();

            // Assert
            Assert.Equal(1.23456, pressure);
        }

        [Fact]
        public async Task GetVersionAsync_ShouldReturnVersion()
        {
            // Arrange
            var (dpsex, settings) = CreateTestDevice();

            settings.Transport.OnSend += data =>
            {
                var text = Encoding.ASCII.GetString(data);
                if (text.Contains("OVER"))
                {
                    var response = Encoding.ASCII.GetBytes("255:F:OVER:V1.0.0\0");
                    settings.Transport.EnqueueReceive(response);
                }
            };

            await dpsex.OpenAsync();

            // Act
            var version = await dpsex.GetVersionAsync();

            // Assert
            Assert.Equal("V1.0.0", version);
        }

        [Fact]
        public async Task GetSerialNumberAsync_ShouldReturnSerialNumber()
        {
            // Arrange
            var (dpsex, settings) = CreateTestDevice();

            settings.Transport.OnSend += data =>
            {
                var text = Encoding.ASCII.GetString(data);
                if (text.Contains("OTYPE"))
                {
                    var response = Encoding.ASCII.GetBytes("255:F:OTYPE:SN123456\0");
                    settings.Transport.EnqueueReceive(response);
                }
            };

            await dpsex.OpenAsync();

            // Act
            var serialNumber = await dpsex.GetSerialNumberAsync();

            // Assert
            Assert.Equal("SN123456", serialNumber);
        }

        [Fact]
        public async Task PressureZeroAsync_ShouldSendCommand()
        {
            // Arrange
            var (dpsex, settings) = CreateTestDevice();

            bool commandSent = false;
            settings.Transport.OnSend += data =>
            {
                var text = Encoding.ASCII.GetString(data);
                if (text.Contains("OZERO"))
                {
                    commandSent = true;
                    var response = Encoding.ASCII.GetBytes("255:F:OZERO:\0");
                    settings.Transport.EnqueueReceive(response);
                }
            };

            await dpsex.OpenAsync();

            // Act
            await dpsex.PressureZeroAsync();

            // Assert
            Assert.True(commandSent);
        }

        [Fact]
        public async Task SetAddressAsync_ShouldSendCommand()
        {
            // Arrange
            var (dpsex, settings) = CreateTestDevice();

            bool commandSent = false;
            settings.Transport.OnSend += data =>
            {
                var text = Encoding.ASCII.GetString(data);
                if (text.Contains("OADD") && text.Contains("100"))
                {
                    commandSent = true;
                    var response = Encoding.ASCII.GetBytes("255:F:OADD:\0");
                    settings.Transport.EnqueueReceive(response);
                }
            };

            await dpsex.OpenAsync();

            // Act
            await dpsex.SetAddressAsync(100);

            // Assert
            Assert.True(commandSent);
        }

        [Fact]
        public async Task DeviceError_ShouldThrowException()
        {
            // Arrange
            var (dpsex, settings) = CreateTestDevice();

            settings.Transport.OnSend += data =>
            {
                var response = Encoding.ASCII.GetBytes("255:E:ERR_OVER\0");
                settings.Transport.EnqueueReceive(response);
            };

            await dpsex.OpenAsync();

            // Act & Assert
            await Assert.ThrowsAsync<DeviceException>(() => dpsex.GetPressureAsync());
        }

        [Fact]
        public async Task GetTemperatureAsync_ShouldReturnTemperature()
        {
            // Arrange
            var (dpsex, settings) = CreateTestDevice();

            settings.Transport.OnSend += data =>
            {
                var text = Encoding.ASCII.GetString(data);
                if (text.Contains("OTEMP"))
                {
                    // 返回纯数字温度值（模拟实际设备响应）
                    var response = Encoding.ASCII.GetBytes("255:F:OTEMP:25.3\0");
                    settings.Transport.EnqueueReceive(response);
                }
            };

            await dpsex.OpenAsync();

            // Act
            var temperature = await dpsex.GetTemperatureAsync();

            // Assert
            Assert.Equal(25.3, temperature);
        }
    }
}
