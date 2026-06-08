using DeviceLink.Device.ZQWL;
using DeviceLink.Protocol;
using DeviceLink.Session;
using Moq;
using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DeviceLink.Tests.ZQWL
{
    public class BNRC8Tests
    {
        #region ZqwlCodec 编码测试

        [Fact]
        public void ZqwlCodec_Encode_GetInput_ShouldReturnCorrectFrame()
        {
            var codec = new ZqwlCodec(address: 1);
            var command = Command.Read("GetInput");
            var encoded = codec.Encode(command);
            Assert.Equal(10, encoded.Length);
            Assert.Equal(1, encoded[0]);
            Assert.Equal(0x52, encoded[1]);
            for (int i = 2; i < 10; i++)
                Assert.Equal(0x00, encoded[i]);
        }

        [Fact]
        public void ZqwlCodec_Encode_SetOutput_ShouldReturnCorrectFrame()
        {
            var codec = new ZqwlCodec(address: 1);
            var command = Command.Write("SetOutput.1.1");
            var encoded = codec.Encode(command);
            Assert.Equal(10, encoded.Length);
            Assert.Equal(1, encoded[0]);
            Assert.Equal(0x70, encoded[1]);
            Assert.Equal(1, encoded[2]);
            Assert.Equal(1, encoded[3]);
        }

        [Fact]
        public void ZqwlCodec_Encode_CloseAll_ShouldReturnCorrectFrame()
        {
            var codec = new ZqwlCodec(address: 1);
            var command = Command.Write("CloseAll", "00", "00", "00", "00", "00", "00", "00", "00");
            var encoded = codec.Encode(command);
            Assert.Equal(10, encoded.Length);
            Assert.Equal(0x57, encoded[1]);
            for (int i = 2; i < 10; i++)
                Assert.Equal(0x00, encoded[i]);
        }

        [Fact]
        public void ZqwlCodec_Encode_OpenAll_ShouldReturnCorrectFrame()
        {
            var codec = new ZqwlCodec(address: 1);
            var command = Command.Write("OpenAll", "01", "01", "01", "01", "01", "01", "01", "01");
            var encoded = codec.Encode(command);
            Assert.Equal(0x57, encoded[1]);
            for (int i = 2; i < 10; i++)
                Assert.Equal(0x01, encoded[i]);
        }

        [Fact]
        public void ZqwlCodec_Encode_GetVersion_ShouldReturnCorrectFrame()
        {
            var codec = new ZqwlCodec(address: 1);
            var command = Command.Read("GetVersion");
            var encoded = codec.Encode(command);
            Assert.Equal(0x66, encoded[1]);
        }

        [Fact]
        public void ZqwlCodec_Encode_GetAllStatuses_ShouldReturnCorrectFrame()
        {
            var codec = new ZqwlCodec(address: 1);
            var command = Command.Read("GetAllStatuses");
            var encoded = codec.Encode(command);
            Assert.Equal(0x53, encoded[1]);
            for (int i = 2; i < 10; i++)
                Assert.Equal(0xAA, encoded[i]);
        }

        [Fact]
        public void ZqwlCodec_Encode_DifferentAddress_ShouldUseCorrectAddress()
        {
            var codec = new ZqwlCodec(address: 5);
            var encoded = codec.Encode(Command.Read("GetInput"));
            Assert.Equal(5, encoded[0]);
        }

        [Fact]
        public void ZqwlCodec_Encode_UnknownOperation_ShouldThrowException()
        {
            var codec = new ZqwlCodec(address: 1);
            Assert.Throws<ArgumentException>(() => codec.Encode(Command.Read("UnknownOperation")));
        }

        #endregion

        #region ZqwlCodec 解码测试

        [Fact]
        public void ZqwlCodec_DecodeText_ShouldReturnHexString()
        {
            var codec = new ZqwlCodec(address: 1);
            var data = new byte[] { 0x01, 0x52, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            var text = codec.DecodeText(data);
            Assert.Equal("01 52 00 01 00 00 00 00 00 00", text);
        }

        [Fact]
        public void ZqwlCodec_DecodeText_EmptyData_ShouldReturnEmpty()
        {
            var codec = new ZqwlCodec(address: 1);
            Assert.Equal(string.Empty, codec.DecodeText(Array.Empty<byte>()));
        }

        [Fact]
        public void ZqwlCodec_IsErrorResponse_ShouldDetectError()
        {
            var codec = new ZqwlCodec(address: 1);
            var data = new byte[] { 0x01, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            bool isError = codec.IsErrorResponse(data, out var errorMessage);
            Assert.True(isError);
            Assert.Equal("ZQWL设备返回错误响应", errorMessage);
        }

        [Fact]
        public void ZqwlCodec_IsErrorResponse_NormalResponse_ShouldReturnFalse()
        {
            var codec = new ZqwlCodec(address: 1);
            var data = new byte[] { 0x01, 0x52, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            Assert.False(codec.IsErrorResponse(data, out _));
        }

        #endregion

        #region ZqwlCodec 辅助方法测试

        [Fact]
        public void ZqwlCodec_ExtractInputState_ShouldExtractCorrectly()
        {
            var codec = new ZqwlCodec(address: 1);
            var raw = new byte[] { 0x01, 0x52, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            Assert.True(codec.ExtractInputState(raw, 1));
            Assert.False(codec.ExtractInputState(raw, 2));
        }

        [Fact]
        public void ZqwlCodec_ExtractVersion_ShouldExtractCorrectly()
        {
            var codec = new ZqwlCodec(address: 1);
            var raw = new byte[] { 0x01, 0x66, 0x00, 0x00,
                                   0x49, 0x4F, 0x2D, 0x30, 0x34, 0x2D, 0x30, 0x30, 0x00 };
            Assert.Equal("IO-04-00", codec.ExtractVersion(raw));
        }

        [Fact]
        public void ZqwlCodec_ExtractAnalogValue_ShouldExtractCorrectly()
        {
            var codec = new ZqwlCodec(address: 1);
            var raw = new byte[] { 0x01, 0x0A, 0x00, 0x00, 0x00, 0x64, 0x00, 0x00, 0x00, 0x00 };
            Assert.Equal(100, codec.ExtractAnalogValue(raw));
        }

        #endregion

        #region ZqwlFrameStrategy 帧策略测试

        [Fact]
        public void ZqwlFrameStrategy_BuildFrame_ShouldBuildCorrectFrame()
        {
            var strategy = new DeviceLink.DataLink.ZqwlFrameStrategy();
            var data = new byte[] { 0x01, 0x52, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            var frame = strategy.BuildFrame(data);
            Assert.Equal(15, frame.Length);
            Assert.Equal(0x48, frame[0]);
            Assert.Equal(0x3A, frame[1]);
            Assert.Equal(0x45, frame[13]);
            Assert.Equal(0x44, frame[14]);
        }

        [Fact]
        public void ZqwlFrameStrategy_TryParseFrame_ShouldParseCorrectly()
        {
            var strategy = new DeviceLink.DataLink.ZqwlFrameStrategy();
            var frame = new byte[] {
                0x48, 0x3A,
                0x01, 0x52,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, // checksum placeholder
                0x45, 0x44
            };
            int sum = 0;
            for (int i = 0; i < 12; i++) sum += frame[i];
            frame[12] = (byte)(sum & 0xFF);

            bool parsed = strategy.TryParseFrame(frame, out int frameLength, out byte[] frameData);
            Assert.True(parsed);
            Assert.Equal(15, frameLength);
            Assert.Equal(10, frameData.Length);
            Assert.Equal(0x01, frameData[0]);
            Assert.Equal(0x52, frameData[1]);
        }

        [Fact]
        public void ZqwlFrameStrategy_TryParseFrame_InvalidChecksum_ShouldReturnFalse()
        {
            var strategy = new DeviceLink.DataLink.ZqwlFrameStrategy();
            var frame = new byte[] {
                0x48, 0x3A, 0x01, 0x52,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0xFF,
                0x45, 0x44
            };
            Assert.False(strategy.TryParseFrame(frame, out _, out _));
        }

        [Fact]
        public void ZqwlFrameStrategy_TryParseFrame_InvalidHeader_ShouldReturnFalse()
        {
            var strategy = new DeviceLink.DataLink.ZqwlFrameStrategy();
            var frame = new byte[] {
                0x00, 0x00, 0x01, 0x52,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x9B, 0x45, 0x44
            };
            Assert.False(strategy.TryParseFrame(frame, out _, out _));
        }

        [Fact]
        public void ZqwlFrameStrategy_TryParseFrame_InvalidFooter_ShouldReturnFalse()
        {
            var strategy = new DeviceLink.DataLink.ZqwlFrameStrategy();
            var frame = new byte[] {
                0x48, 0x3A, 0x01, 0x52,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x9B, 0x00, 0x00
            };
            Assert.False(strategy.TryParseFrame(frame, out _, out _));
        }

        #endregion

        #region BNRC8 设备方法测试（使用 Moq 模拟 ISession）

        [Fact]
        public async Task BNRC8_GetInputAsync_ShouldSendCorrectCommand()
        {
            var mockSession = new Mock<ISession>();
            var response = new byte[] { 0x01, 0x52, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            mockSession.Setup(s => s.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(response);

            var device = new BNRC8(mockSession.Object, address: 1);
            bool result = await device.GetInputAsync(1);

            Assert.True(result);
            mockSession.Verify(s => s.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task BNRC8_GetInputAsync_ChannelOff_ShouldReturnFalse()
        {
            var mockSession = new Mock<ISession>();
            var response = new byte[] { 0x01, 0x52, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            mockSession.Setup(s => s.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(response);

            var device = new BNRC8(mockSession.Object, address: 1);
            bool result = await device.GetInputAsync(1);

            Assert.False(result);
        }

        [Fact]
        public async Task BNRC8_SetOutputAsync_ShouldSendCorrectCommand()
        {
            var mockSession = new Mock<ISession>();
            mockSession.Setup(s => s.SendOnlyAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

            var device = new BNRC8(mockSession.Object, address: 1);
            await device.SetOutputAsync(1, true);

            mockSession.Verify(s => s.SendOnlyAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task BNRC8_GetOutputAsync_ShouldReturnCorrectState()
        {
            var mockSession = new Mock<ISession>();
            // BNRC8: raw[1+channel] == 0x01 表示该路开, channel=1 => raw[2]=0x01
            var response = new byte[] { 0x01, 0x72, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            mockSession.Setup(s => s.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(response);

            var device = new BNRC8(mockSession.Object, address: 1);
            bool result = await device.GetOutputAsync(1);

            Assert.True(result);
        }

        [Fact]
        public async Task BNRC8_CloseAllAsync_ShouldSendCorrectCommand()
        {
            var mockSession = new Mock<ISession>();
            mockSession.Setup(s => s.SendOnlyAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

            var device = new BNRC8(mockSession.Object, address: 1);
            await device.CloseAllAsync();

            mockSession.Verify(s => s.SendOnlyAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task BNRC8_OpenAllAsync_ShouldSendCorrectCommand()
        {
            var mockSession = new Mock<ISession>();
            mockSession.Setup(s => s.SendOnlyAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

            var device = new BNRC8(mockSession.Object, address: 1);
            await device.OpenAllAsync();

            mockSession.Verify(s => s.SendOnlyAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task BNRC8_GetVersionAsync_ShouldReturnVersionString()
        {
            var mockSession = new Mock<ISession>();
            var response = new byte[] { 0x01, 0x66, 0x00, 0x00,
                                        0x49, 0x4F, 0x2D, 0x30, 0x34, 0x2D, 0x30, 0x30, 0x00 };
            mockSession.Setup(s => s.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(response);

            var device = new BNRC8("COM12", 115200, 8, StopBits.Two, Parity.None, address: 1);
            string version = await device.GetVersionAsync();
            Assert.Equal("IO-04-00", version);
        }

        [Fact]
        public async Task BNRC8_IsExistAsync_VersionContainsIO_ShouldReturnTrue()
        {
            var mockSession = new Mock<ISession>();
            var response = new byte[] { 0x01, 0x66, 0x00, 0x00,
                                        0x49, 0x4F, 0x2D, 0x30, 0x34, 0x2D, 0x30, 0x30, 0x00 };
            mockSession.Setup(s => s.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(response);

            var device = new BNRC8(mockSession.Object, address: 1);
            Assert.True(await device.IsExistAsync());
        }

        [Fact]
        public async Task BNRC8_IsExistAsync_VersionContainsBN_ShouldReturnTrue()
        {
            var mockSession = new Mock<ISession>();
            var response = new byte[] { 0x01, 0x66, 0x00, 0x00,
                                        0x42, 0x4E, 0x2D, 0x30, 0x38, 0x2D, 0x56, 0x31, 0x00 };
            mockSession.Setup(s => s.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(response);

            var device = new BNRC8(mockSession.Object, address: 1);
            Assert.True(await device.IsExistAsync());
        }

        [Fact]
        public async Task BNRC8_IsExistAsync_SessionThrows_ShouldReturnFalse()
        {
            var mockSession = new Mock<ISession>();
            mockSession.Setup(s => s.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new Exception("连接失败"));

            var device = new BNRC8(mockSession.Object, address: 1);
            Assert.False(await device.IsExistAsync());
        }

        [Fact]
        public async Task BNRC8_GetAllStatusesAsync_ShouldReturn8States()
        {
            var mockSession = new Mock<ISession>();
            // BNRC8: raw[1+i] == 0x01 表示第i路开, i=1..8
            var response = new byte[] { 0x01, 0x53, 0x01, 0x00, 0x01, 0x00, 0x01, 0x00, 0x01, 0x00 };
            mockSession.Setup(s => s.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(response);

            var device = new BNRC8(mockSession.Object, address: 1);
            var statuses = await device.GetAllStatusesAsync();

            Assert.Equal(8, statuses.Count);
            Assert.True(statuses[0]);
            Assert.False(statuses[1]);
            Assert.True(statuses[2]);
            Assert.False(statuses[3]);
            Assert.True(statuses[4]);
            Assert.False(statuses[5]);
            Assert.True(statuses[6]);
            Assert.False(statuses[7]);
        }

        [Fact]
        public async Task BNRC8_OpenAsync_ShouldCallSessionOpen()
        {
            var mockSession = new Mock<ISession>();
            mockSession.Setup(s => s.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            mockSession.Setup(s => s.IsOpen).Returns(true);

            var device = new BNRC8(mockSession.Object, address: 1);
            await device.OpenAsync();

            mockSession.Verify(s => s.OpenAsync(It.IsAny<CancellationToken>()), Times.Once);
            Assert.True(device.IsOpen);
        }

        [Fact]
        public async Task BNRC8_CloseAsync_ShouldCallSessionClose()
        {
            var mockSession = new Mock<ISession>();
            mockSession.Setup(s => s.CloseAsync()).Returns(Task.CompletedTask);
            mockSession.Setup(s => s.IsOpen).Returns(false);

            var device = new BNRC8(mockSession.Object, address: 1);
            await device.CloseAsync();

            mockSession.Verify(s => s.CloseAsync(), Times.Once);
            Assert.False(device.IsOpen);
        }

        [Fact]
        public void BNRC8_Name_ShouldBeBNRC8()
        {
            var mockSession = new Mock<ISession>();
            var device = new BNRC8(mockSession.Object, address: 1);
            Assert.Equal("BNRC8", device.Name);
        }

        [Fact]
        public void BNRC8_Constructor_WithSession_ShouldCreateInstance()
        {
            var mockSession = new Mock<ISession>();
            var device = new BNRC8(mockSession.Object, address: 5);
            Assert.NotNull(device);
            Assert.Equal("BNRC8", device.Name);
        }

        #endregion
    }
}
