using System;
using System.Threading.Tasks;
using DeviceLink.Transport;
using Xunit;

namespace DeviceLink.Tests
{
    /// <summary>
    /// 物理传输层测试
    /// </summary>
    public class TransportTests
    {
        [Fact]
        public async Task LoopbackTransport_ConnectAsync_ShouldSetIsOpen()
        {
            // Arrange
            using var transport = new LoopbackTransport();

            // Act
            await transport.ConnectAsync();

            // Assert
            Assert.True(transport.IsOpen);
        }

        [Fact]
        public async Task LoopbackTransport_CloseAsync_ShouldResetIsOpen()
        {
            // Arrange
            using var transport = new LoopbackTransport();
            await transport.ConnectAsync();

            // Act
            await transport.CloseAsync();

            // Assert
            Assert.False(transport.IsOpen);
        }

        [Fact]
        public async Task LoopbackTransport_WriteAsync_ShouldTriggerOnSend()
        {
            // Arrange
            using var transport = new LoopbackTransport();
            await transport.ConnectAsync();

            byte[]? sentData = null;
            transport.OnSend += data => sentData = data;

            var testData = new byte[] { 1, 2, 3 };

            // Act
            await transport.WriteAsync(testData, 0, testData.Length);

            // Assert
            Assert.NotNull(sentData);
            Assert.Equal(testData, sentData);
        }

        [Fact]
        public async Task LoopbackTransport_ReadAsync_ShouldReadEnqueuedData()
        {
            // Arrange
            using var transport = new LoopbackTransport();
            await transport.ConnectAsync();

            var testData = new byte[] { 1, 2, 3, 4, 5 };
            transport.EnqueueReceive(testData);

            var buffer = new byte[10];

            // Act
            int read = await transport.ReadAsync(buffer, 0, buffer.Length);

            // Assert
            Assert.Equal(testData.Length, read);
            Assert.Equal(testData, buffer.AsSpan(0, read).ToArray());
        }

        [Fact]
        public async Task LoopbackTransport_ClearReceiveBufferAsync_ShouldClearBuffer()
        {
            // Arrange
            using var transport = new LoopbackTransport();
            await transport.ConnectAsync();

            transport.EnqueueReceive(new byte[] { 1, 2, 3 });

            // Act
            await transport.ClearReceiveBufferAsync();

            var buffer = new byte[10];
            int read = await transport.ReadAsync(buffer, 0, buffer.Length);

            // Assert
            Assert.Equal(0, read);
        }
    }
}
