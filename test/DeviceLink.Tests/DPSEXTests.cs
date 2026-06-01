using System;
using System.Text;
using System.Threading.Tasks;
using DeviceLink.DataLink;
using DeviceLink.Devices;
using DeviceLink.Protocol;
using DeviceLink.Session;
using DeviceLink.Transport;
using Xunit;

namespace DeviceLink.Tests
{
    /// <summary>
    /// DPSEX设备测试
    /// </summary>
    public class DPSEXTests
    {
        [Fact]
        public async Task DPSEX_GetPressureAsync_ShouldReturnPressure()
        {
            // Arrange
            using var transport = new LoopbackTransport();
            var frameStrategy = new DelimiterFrameStrategy(new byte[] { 0 });
            using var dataLink = new DirectDataLink(transport, frameStrategy);
            using var session = new DirectSession(dataLink);
            var codec = new ConSTCodec(255);
            using var dpsex = new DPSEX(session, codec);

            // 设置回环响应
            transport.OnSend += data =>
            {
                var text = Encoding.ASCII.GetString(data);
                if (text.Contains("MRMD"))
                {
                    var response = Encoding.ASCII.GetBytes("255:F:MRMD:1.23456\0");
                    transport.EnqueueReceive(response);
                }
            };

            await dpsex.OpenAsync();

            // Act
            var pressure = await dpsex.GetPressureAsync();

            // Assert
            Assert.Equal(1.23456, pressure);
        }

        [Fact]
        public async Task DPSEX_GetVersionAsync_ShouldReturnVersion()
        {
            // Arrange
            using var transport = new LoopbackTransport();
            var frameStrategy = new DelimiterFrameStrategy(new byte[] { 0 });
            using var dataLink = new DirectDataLink(transport, frameStrategy);
            using var session = new DirectSession(dataLink);
            var codec = new ConSTCodec(255);
            using var dpsex = new DPSEX(session, codec);

            // 设置回环响应
            transport.OnSend += data =>
            {
                var text = Encoding.ASCII.GetString(data);
                if (text.Contains("OVER"))
                {
                    var response = Encoding.ASCII.GetBytes("255:F:OVER:V1.0.0\0");
                    transport.EnqueueReceive(response);
                }
            };

            await dpsex.OpenAsync();

            // Act
            var version = await dpsex.GetVersionAsync();

            // Assert
            Assert.Equal("V1.0.0", version);
        }

        [Fact]
        public async Task DPSEX_GetSerialNumberAsync_ShouldReturnSerialNumber()
        {
            // Arrange
            using var transport = new LoopbackTransport();
            var frameStrategy = new DelimiterFrameStrategy(new byte[] { 0 });
            using var dataLink = new DirectDataLink(transport, frameStrategy);
            using var session = new DirectSession(dataLink);
            var codec = new ConSTCodec(255);
            using var dpsex = new DPSEX(session, codec);

            // 设置回环响应
            transport.OnSend += data =>
            {
                var text = Encoding.ASCII.GetString(data);
                if (text.Contains("OTYPE"))
                {
                    var response = Encoding.ASCII.GetBytes("255:F:OTYPE:SN123456\0");
                    transport.EnqueueReceive(response);
                }
            };

            await dpsex.OpenAsync();

            // Act
            var serialNumber = await dpsex.GetSerialNumberAsync();

            // Assert
            Assert.Equal("SN123456", serialNumber);
        }

        [Fact]
        public async Task DPSEX_PressureZeroAsync_ShouldSendCommand()
        {
            // Arrange
            using var transport = new LoopbackTransport();
            var frameStrategy = new DelimiterFrameStrategy(new byte[] { 0 });
            using var dataLink = new DirectDataLink(transport, frameStrategy);
            using var session = new DirectSession(dataLink);
            var codec = new ConSTCodec(255);
            using var dpsex = new DPSEX(session, codec);

            bool commandSent = false;
            transport.OnSend += data =>
            {
                var text = Encoding.ASCII.GetString(data);
                if (text.Contains("OZERO"))
                {
                    commandSent = true;
                    var response = Encoding.ASCII.GetBytes("255:F:OZERO:\0");
                    transport.EnqueueReceive(response);
                }
            };

            await dpsex.OpenAsync();

            // Act
            await dpsex.PressureZeroAsync();

            // Assert
            Assert.True(commandSent);
        }

        [Fact]
        public async Task DPSEX_SetAddressAsync_ShouldSendCommand()
        {
            // Arrange
            using var transport = new LoopbackTransport();
            var frameStrategy = new DelimiterFrameStrategy(new byte[] { 0 });
            using var dataLink = new DirectDataLink(transport, frameStrategy);
            using var session = new DirectSession(dataLink);
            var codec = new ConSTCodec(255);
            using var dpsex = new DPSEX(session, codec);

            bool commandSent = false;
            transport.OnSend += data =>
            {
                var text = Encoding.ASCII.GetString(data);
                if (text.Contains("OADD") && text.Contains("100"))
                {
                    commandSent = true;
                    var response = Encoding.ASCII.GetBytes("255:F:OADD:\0");
                    transport.EnqueueReceive(response);
                }
            };

            await dpsex.OpenAsync();

            // Act
            await dpsex.SetAddressAsync(100);

            // Assert
            Assert.True(commandSent);
        }

        [Fact]
        public async Task DPSEX_DeviceError_ShouldThrowException()
        {
            // Arrange
            using var transport = new LoopbackTransport();
            var frameStrategy = new DelimiterFrameStrategy(new byte[] { 0 });
            using var dataLink = new DirectDataLink(transport, frameStrategy);
            using var session = new DirectSession(dataLink);
            var codec = new ConSTCodec(255);
            using var dpsex = new DPSEX(session, codec);

            // 设置回环响应为错误
            transport.OnSend += data =>
            {
                var response = Encoding.ASCII.GetBytes("255:E:ERR_OVER\0");
                transport.EnqueueReceive(response);
            };

            await dpsex.OpenAsync();

            // Act & Assert
            await Assert.ThrowsAsync<DeviceException>(() => dpsex.GetPressureAsync());
        }

        [Fact]
        public async Task DPSEX_WithLoopbackTransport_ShouldWork()
        {
            // Arrange
            using var transport = new LoopbackTransport();
            var frameStrategy = new DelimiterFrameStrategy(new byte[] { 0 });
            using var dataLink = new DirectDataLink(transport, frameStrategy);
            using var session = new DirectSession(dataLink);
            var codec = new ConSTCodec(255);
            using var dpsex = new DPSEX(session, codec);

            // 设置回环响应
            transport.OnSend += data =>
            {
                var text = Encoding.ASCII.GetString(data);
                if (text.Contains("MRMD"))
                {
                    var response = Encoding.ASCII.GetBytes("255:F:MRMD:2.34567\0");
                    transport.EnqueueReceive(response);
                }
            };

            await dpsex.OpenAsync();

            // Act
            var pressure = await dpsex.GetPressureAsync();

            // Assert
            Assert.Equal(2.34567, pressure);
        }
    }
}
