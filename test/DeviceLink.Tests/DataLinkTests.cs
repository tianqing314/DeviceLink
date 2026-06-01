using System;
using System.Text;
using System.Threading.Tasks;
using DeviceLink.DataLink;
using DeviceLink.Transport;
using Xunit;

namespace DeviceLink.Tests
{
    /// <summary>
    /// 数据链路层测试
    /// </summary>
    public class DataLinkTests
    {
        [Fact]
        public void DelimiterFrameStrategy_BuildFrame_ShouldAddDelimiter()
        {
            // Arrange
            var strategy = new DelimiterFrameStrategy(new byte[] { 0 });
            var data = Encoding.ASCII.GetBytes("Hello");

            // Act
            var frame = strategy.BuildFrame(data);

            // Assert
            Assert.Equal(data.Length + 1, frame.Length);
            Assert.Equal(0, frame[frame.Length - 1]); // 分隔符
        }

        [Fact]
        public void DelimiterFrameStrategy_TryParseFrame_ShouldParseCorrectly()
        {
            // Arrange
            var strategy = new DelimiterFrameStrategy(new byte[] { 0 });
            var accumulated = Encoding.ASCII.GetBytes("Hello\0World\0");

            // Act
            bool result = strategy.TryParseFrame(accumulated, out int frameLength, out byte[] frameData);

            // Assert
            Assert.True(result);
            Assert.Equal(6, frameLength); // "Hello\0"
            Assert.Equal("Hello", Encoding.ASCII.GetString(frameData));
        }

        [Fact]
        public void DelimiterFrameStrategy_TryParseFrame_ShouldReturnFalseWhenNoDelimiter()
        {
            // Arrange
            var strategy = new DelimiterFrameStrategy(new byte[] { 0 });
            var accumulated = Encoding.ASCII.GetBytes("Hello");

            // Act
            bool result = strategy.TryParseFrame(accumulated, out int frameLength, out byte[] frameData);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void FixedLengthFrameStrategy_BuildFrame_ShouldCreateFixedLength()
        {
            // Arrange
            var strategy = new FixedLengthFrameStrategy(10);
            var data = Encoding.ASCII.GetBytes("Hello");

            // Act
            var frame = strategy.BuildFrame(data);

            // Assert
            Assert.Equal(10, frame.Length);
            Assert.Equal('H', (char)frame[0]);
            Assert.Equal('e', (char)frame[1]);
            Assert.Equal('l', (char)frame[2]);
            Assert.Equal('l', (char)frame[3]);
            Assert.Equal('o', (char)frame[4]);
            Assert.Equal(0, frame[5]); // 补零
        }

        [Fact]
        public void FixedLengthFrameStrategy_TryParseFrame_ShouldParseCorrectly()
        {
            // Arrange
            var strategy = new FixedLengthFrameStrategy(5);
            var accumulated = Encoding.ASCII.GetBytes("HelloWorld");

            // Act
            bool result = strategy.TryParseFrame(accumulated, out int frameLength, out byte[] frameData);

            // Assert
            Assert.True(result);
            Assert.Equal(5, frameLength);
            Assert.Equal("Hello", Encoding.ASCII.GetString(frameData));
        }

        [Fact]
        public void ModbusRtuFrameStrategy_BuildFrame_ShouldAddCrc()
        {
            // Arrange
            var strategy = new ModbusRtuFrameStrategy();
            var data = new byte[] { 1, 3, 0, 0, 0, 10 }; // 读取10个寄存器

            // Act
            var frame = strategy.BuildFrame(data);

            // Assert
            Assert.Equal(data.Length + 2, frame.Length); // 数据 + CRC
        }

        [Fact]
        public async Task DirectDataLink_SendAndReceiveFrameAsync_ShouldWork()
        {
            // Arrange
            using var transport = new LoopbackTransport();
            var frameStrategy = new DelimiterFrameStrategy(new byte[] { 0 });
            using var dataLink = new DirectDataLink(transport, frameStrategy);

            // 设置回环响应
            transport.OnSend += data =>
            {
                // 模拟设备响应
                var response = Encoding.ASCII.GetBytes("Response\0");
                transport.EnqueueReceive(response);
            };

            await dataLink.OpenAsync();

            var request = Encoding.ASCII.GetBytes("Request");

            // Act
            var response = await dataLink.SendAndReceiveFrameAsync(request);

            // Assert
            Assert.Equal("Response", Encoding.ASCII.GetString(response));
        }
    }
}
