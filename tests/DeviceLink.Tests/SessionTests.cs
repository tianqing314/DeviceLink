using System;
using System.Text;
using System.Threading.Tasks;
using DeviceLink.DataLink;
using DeviceLink.Session;
using DeviceLink.Transport;
using Xunit;

namespace DeviceLink.Tests
{
    /// <summary>
    /// 会话层测试
    /// </summary>
    public class SessionTests
    {
        [Fact]
        public async Task DirectSession_OpenAsync_ShouldOpenDataLink()
        {
            // Arrange
            using var transport = new LoopbackTransport();
            var frameStrategy = new DelimiterFrameStrategy(new byte[] { 0 });
            using var dataLink = new DirectDataLink(transport, frameStrategy);
            using var session = new DirectSession(dataLink);

            // Act
            await session.OpenAsync();

            // Assert
            Assert.True(session.IsOpen);
        }

        [Fact]
        public async Task DirectSession_CloseAsync_ShouldCloseDataLink()
        {
            // Arrange
            using var transport = new LoopbackTransport();
            var frameStrategy = new DelimiterFrameStrategy(new byte[] { 0 });
            using var dataLink = new DirectDataLink(transport, frameStrategy);
            using var session = new DirectSession(dataLink);
            await session.OpenAsync();

            // Act
            await session.CloseAsync();

            // Assert
            Assert.False(session.IsOpen);
        }

        [Fact]
        public async Task DirectSession_SendAndReceiveAsync_ShouldWork()
        {
            // Arrange
            using var transport = new LoopbackTransport();
            var frameStrategy = new DelimiterFrameStrategy(new byte[] { 0 });
            using var dataLink = new DirectDataLink(transport, frameStrategy);
            using var session = new DirectSession(dataLink);

            // 设置回环响应
            transport.OnSend += data =>
            {
                var response = Encoding.ASCII.GetBytes("Response\0");
                transport.EnqueueReceive(response);
            };

            await session.OpenAsync();

            var request = Encoding.ASCII.GetBytes("Request");

            // Act
            var response = await session.SendAndReceiveAsync(request);

            // Assert
            Assert.Equal("Response", Encoding.ASCII.GetString(response));
        }

        [Fact]
        public async Task DirectSession_SendOnlyAsync_ShouldNotThrow()
        {
            // Arrange
            using var transport = new LoopbackTransport();
            var frameStrategy = new DelimiterFrameStrategy(new byte[] { 0 });
            using var dataLink = new DirectDataLink(transport, frameStrategy);
            using var session = new DirectSession(dataLink);
            await session.OpenAsync();

            var request = Encoding.ASCII.GetBytes("Request");

            // Act & Assert
            await session.SendOnlyAsync(request); // 不应抛出异常
        }

        [Fact]
        public async Task DirectSession_ReceiveOnlyAsync_ShouldWork()
        {
            // Arrange
            using var transport = new LoopbackTransport();
            var frameStrategy = new DelimiterFrameStrategy(new byte[] { 0 });
            using var dataLink = new DirectDataLink(transport, frameStrategy);
            using var session = new DirectSession(dataLink);
            await session.OpenAsync();

            // 模拟设备主动发送数据
            var deviceData = Encoding.ASCII.GetBytes("DeviceData\0");
            transport.EnqueueReceive(deviceData);

            // Act
            var response = await session.ReceiveOnlyAsync();

            // Assert
            Assert.Equal("DeviceData", Encoding.ASCII.GetString(response));
        }
    }
}
