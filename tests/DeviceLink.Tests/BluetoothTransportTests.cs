using System;
using System.Threading.Tasks;
using DeviceLink.DeviceBase;
using DeviceLink.Pipeline;
using DeviceLink.Protocol;
using DeviceLink.Transport;
using Moq;
using Xunit;

namespace DeviceLink.Tests
{
    /// <summary>
    /// 蓝牙传输层测试
    /// </summary>
    public class BluetoothTransportTests
    {
        [Fact]
        public void BluetoothOptions_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var options = new BluetoothOptions();

            // Assert
            Assert.Equal(string.Empty, options.DeviceAddress);
            Assert.Equal(new Guid("00001101-0000-1000-8000-00805F9B34FB"), options.ServiceUuid);
            Assert.Equal(10000, options.ConnectTimeoutMs);
            Assert.Equal(4096, options.ReadBufferSize);
            Assert.Equal(2048, options.WriteBufferSize);
            Assert.True(options.AutoPair);
            Assert.Null(options.PinCode);
            Assert.True(options.UseClassicBluetooth);
            Assert.Equal(5000, options.DiscoveryTimeoutMs);
            Assert.False(options.AutoDiscover);
            Assert.Null(options.DeviceClassFilter);
            Assert.True(options.EnableAuthentication);
            Assert.False(options.EnableEncryption);
        }

        [Fact]
        public void BluetoothOptions_SetProperties_ShouldWork()
        {
            // Arrange
            var options = new BluetoothOptions();

            // Act
            options.DeviceAddress = "00:11:22:33:44:55";
            options.ConnectTimeoutMs = 5000;
            options.AutoPair = false;
            options.PinCode = "1234";

            // Assert
            Assert.Equal("00:11:22:33:44:55", options.DeviceAddress);
            Assert.Equal(5000, options.ConnectTimeoutMs);
            Assert.False(options.AutoPair);
            Assert.Equal("1234", options.PinCode);
        }

        [Fact]
        public void BluetoothTransport_Constructor_ShouldThrowOnNullOptions()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new BluetoothTransport(null));
        }

        [Fact]
        public void BluetoothTransport_Name_ShouldReturnFormattedString()
        {
            // Arrange
            var options = new BluetoothOptions { DeviceAddress = "00:11:22:33:44:55" };
            var transport = new BluetoothTransport(options);

            // Act
            var name = transport.Name;

            // Assert
            Assert.Equal("Bluetooth(00:11:22:33:44:55)", name);
        }

        [Fact]
        public void BluetoothTransport_IsOpen_ShouldBeFalseInitially()
        {
            // Arrange
            var options = new BluetoothOptions { DeviceAddress = "00:11:22:33:44:55" };
            var transport = new BluetoothTransport(options);

            // Act
            var isOpen = transport.IsOpen;

            // Assert
            Assert.False(isOpen);
        }

        [Fact]
        public void BluetoothSettings_DefaultConstructor_ShouldWork()
        {
            // Arrange & Act
            var settings = new BluetoothSettings();

            // Assert
            Assert.NotNull(settings.BluetoothOptions);
            Assert.Equal(string.Empty, settings.BluetoothOptions.DeviceAddress);
            Assert.NotNull(settings.Delimiter);
            Assert.Single(settings.Delimiter);
            Assert.Equal(0, settings.Delimiter[0]);
            Assert.Null(settings.FrameStrategy);
        }

        [Fact]
        public void BluetoothSettings_WithDeviceAddress_ShouldWork()
        {
            // Arrange
            var deviceAddress = "00:11:22:33:44:55";

            // Act
            var settings = new BluetoothSettings(deviceAddress);

            // Assert
            Assert.NotNull(settings.BluetoothOptions);
            Assert.Equal(deviceAddress, settings.BluetoothOptions.DeviceAddress);
        }

        [Fact]
        public void BluetoothSettings_WithOptions_ShouldWork()
        {
            // Arrange
            var options = new BluetoothOptions
            {
                DeviceAddress = "00:11:22:33:44:55",
                ConnectTimeoutMs = 5000
            };

            // Act
            var settings = new BluetoothSettings(options);

            // Assert
            Assert.Equal(options, settings.BluetoothOptions);
            Assert.Equal("00:11:22:33:44:55", settings.BluetoothOptions.DeviceAddress);
            Assert.Equal(5000, settings.BluetoothOptions.ConnectTimeoutMs);
        }

        [Fact]
        public void BluetoothSettings_WithNullOptions_ShouldThrow()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new BluetoothSettings((BluetoothOptions)null));
        }

        [Fact]
        public void BluetoothSettings_CreatePipeline_ShouldCreateValidPipeline()
        {
            // Arrange
            var settings = new BluetoothSettings(new BluetoothOptions { DeviceAddress = "00:11:22:33:44:55" });
            var mockCodec = new Mock<IProtocolCodec>();

            // Act - 使用反射调用protected internal方法
            var method = typeof(BluetoothSettings).GetMethod("CreatePipeline", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var pipeline = method?.Invoke(settings, new object[] { mockCodec.Object }) as CommunicationPipeline;

            // Assert
            Assert.NotNull(pipeline);
        }
    }
}