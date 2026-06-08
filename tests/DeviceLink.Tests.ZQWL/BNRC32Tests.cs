using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DeviceLink.Device.ZQWL;
using DeviceLink.Protocol;
using DeviceLink.Session;
using Moq;
using Xunit;

namespace DeviceLink.Tests.ZQWL
{
    public class BNRC32Tests
    {
        #region ZqwlCodec 编码测试

        [Fact]
        public void ZqwlCodec_Encode_GetAnalogInput_Channel0_ShouldReturnCorrectFrame()
        {
            var codec = new ZqwlCodec(address: 1);
            var encoded = codec.Encode(Command.Read("GetAnalogInput.0"));
            Assert.Equal(10, encoded.Length);
            Assert.Equal(0x0A, encoded[1]);
            Assert.Equal(0, encoded[2]);
        }

        [Fact]
        public void ZqwlCodec_Encode_GetAnalogInput_Channel3_ShouldReturnCorrectFrame()
        {
            var codec = new ZqwlCodec(address: 1);
            var encoded = codec.Encode(Command.Read("GetAnalogInput.3"));
            Assert.Equal(0x0A, encoded[1]);
            Assert.Equal(3, encoded[2]);
        }

        #endregion

        #region BNRC32 数据编码测试

        [Fact]
        public void BNRC32_OpenAll_Data_ShouldBeCorrect()
        {
            var codec = new ZqwlCodec(address: 1);
            var encoded = codec.Encode(Command.Write("OpenAll", "55", "55", "55", "55", "55", "55", "55", "55"));
            for (int i = 2; i < 10; i++)
                Assert.Equal(0x55, encoded[i]);
        }

        [Fact]
        public void BNRC32_CloseAll_Data_ShouldBeCorrect()
        {
            var codec = new ZqwlCodec(address: 1);
            var encoded = codec.Encode(Command.Write("CloseAll", "00", "00", "00", "00", "00", "00", "00", "00"));
            for (int i = 2; i < 10; i++)
                Assert.Equal(0x00, encoded[i]);
        }

        [Fact]
        public void BNRC32_BitEncoding_Channel1_ShouldUseBit0()
        {
            var codec = new ZqwlCodec(address: 1);
            var encoded = codec.Encode(Command.Write("SetOutput.1.1"));
            Assert.Equal(1, encoded[2]);
            Assert.Equal(1, encoded[3]);
        }

        [Fact]
        public void BNRC32_BitEncoding_Channel4_ShouldUseBit3()
        {
            var codec = new ZqwlCodec(address: 1);
            var encoded = codec.Encode(Command.Write("SetOutput.4.1"));
            Assert.Equal(4, encoded[2]);
            Assert.Equal(1, encoded[3]);
        }

        #endregion

        #region ZqwlCodec 解码测试

        [Fact]
        public void ZqwlCodec_ExtractAnalogValue_LargeValue_ShouldExtractCorrectly()
        {
            var codec = new ZqwlCodec(address: 1);
            var raw = new byte[] { 0x01, 0x0A, 0x00, 0x00, 0x00, 0xA0, 0x0F, 0x00, 0x00, 0x00 };
            Assert.Equal(4000, codec.ExtractAnalogValue(raw));
        }

        [Fact]
        public void ZqwlCodec_ExtractVersion_V2Format_ShouldExtractCorrectly()
        {
            var codec = new ZqwlCodec(address: 1);
            var raw = new byte[] { 0x01, 0x66, 0x00, 0x00, 0x42, 0x4E, 0x2D, 0x33, 0x32, 0x2D, 0x56, 0x32, 0x00 };
            Assert.Equal("BN-32-V2", codec.ExtractVersion(raw));
        }

        [Fact]
        public void ZqwlCodec_ExtractVersion_TooShort_ShouldReturnEmpty()
        {
            var codec = new ZqwlCodec(address: 1);
            Assert.Equal(string.Empty, codec.ExtractVersion(new byte[] { 0x01, 0x66 }));
        }

        #endregion

        #region ZqwlFrameStrategy 帧策略测试

        [Fact]
        public void ZqwlFrameStrategy_BuildFrame_OpenAll_ShouldBuildCorrectFrame()
        {
            var strategy = new DeviceLink.DataLink.ZqwlFrameStrategy();
            var codec = new ZqwlCodec(address: 1);
            var data = codec.Encode(Command.Write("OpenAll", "55", "55", "55", "55", "55", "55", "55", "55"));
            var frame = strategy.BuildFrame(data);
            Assert.Equal(15, frame.Length);
            Assert.Equal(0x48, frame[0]);
            Assert.Equal(0x3A, frame[1]);
            Assert.Equal(0x45, frame[13]);
            Assert.Equal(0x44, frame[14]);
            for (int i = 4; i < 12; i++)
                Assert.Equal(0x55, frame[i]);
        }

        [Fact]
        public void ZqwlFrameStrategy_RoundTrip_ShouldPreserveData()
        {
            var strategy = new DeviceLink.DataLink.ZqwlFrameStrategy();
            var codec = new ZqwlCodec(address: 1);
            var originalData = codec.Encode(Command.Write("SetOutput.16.1"));
            var frame = strategy.BuildFrame(originalData);
            bool parsed = strategy.TryParseFrame(frame, out int frameLength, out byte[] parsedData);
            Assert.True(parsed);
            Assert.Equal(15, frameLength);
            Assert.Equal(originalData.Length, parsedData.Length);
            for (int i = 0; i < originalData.Length; i++)
                Assert.Equal(originalData[i], parsedData[i]);
        }

        [Fact]
        public void ZqwlFrameStrategy_TryParseFrame_PartialData_ShouldReturnFalse()
        {
            var strategy = new DeviceLink.DataLink.ZqwlFrameStrategy();
            Assert.False(strategy.TryParseFrame(new byte[] { 0x48, 0x3A, 0x01, 0x52 }, out _, out _));
        }

        [Fact]
        public void ZqwlFrameStrategy_TryParseFrame_EmptyData_ShouldReturnFalse()
        {
            var strategy = new DeviceLink.DataLink.ZqwlFrameStrategy();
            Assert.False(strategy.TryParseFrame(Array.Empty<byte>(), out _, out _));
        }

        #endregion

        #region 不同地址测试

        [Fact]
        public void ZqwlCodec_Encode_Address10_ShouldUseCorrectAddress()
        {
            var codec = new ZqwlCodec(address: 10);
            var encoded = codec.Encode(Command.Read("GetInput"));
            Assert.Equal(10, encoded[0]);
            Assert.Equal(0x52, encoded[1]);
        }

        [Fact]
        public void ZqwlCodec_Encode_Address255_ShouldUseCorrectAddress()
        {
            var codec = new ZqwlCodec(address: 255);
            var encoded = codec.Encode(Command.Read("GetVersion"));
            Assert.Equal(255, encoded[0]);
            Assert.Equal(0x66, encoded[1]);
        }

        #endregion

        #region 边界条件测试

        [Fact]
        public void ZqwlCodec_Encode_SetOutput_Channel32_ShouldEncodeCorrectly()
        {
            var codec = new ZqwlCodec(address: 1);
            var encoded = codec.Encode(Command.Write("SetOutput.32.1"));
            Assert.Equal(32, encoded[2]);
            Assert.Equal(1, encoded[3]);
        }

        [Fact]
        public void ZqwlCodec_Encode_SetOutput_Off_ShouldEncodeCorrectly()
        {
            var codec = new ZqwlCodec(address: 1);
            var encoded = codec.Encode(Command.Write("SetOutput.1.0"));
            Assert.Equal(1, encoded[2]);
            Assert.Equal(0, encoded[3]);
        }

        [Fact]
        public void ZqwlCodec_IsErrorResponse_TooShort_ShouldReturnFalse()
        {
            var codec = new ZqwlCodec(address: 1);
            Assert.False(codec.IsErrorResponse(new byte[] { 0x01 }, out _));
        }

        [Fact]
        public void ZqwlCodec_IsErrorResponse_Null_ShouldReturnFalse()
        {
            var codec = new ZqwlCodec(address: 1);
            Assert.False(codec.IsErrorResponse(null!, out _));
        }

        #endregion

        #region BNRC32 设备方法测试（使用 Moq 模拟 ISession）

        [Fact]
        public async Task BNRC32_GetInputAsync_Channel1_ShouldExtractFromBit0()
        {
            var mockSession = new Mock<ISession>();
            // BNRC32: ExtractQuadInput requires raw.Length >= 12
            // channel=1, dataIndex = floor(1/4.0) = 0, raw[4+0]=raw[4]
            // channel%4=1 => hex[1] upper => val/4 >= 1 => raw[4]=0x04
            var response = new byte[] { 0x01, 0x52, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            mockSession.Setup(s => s.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(response);

            var device = new BNRC32(mockSession.Object, address: 1);
            bool result = await device.GetInputAsync(1);

            Assert.True(result);
        }

        [Fact]
        public async Task BNRC32_GetInputAsync_ChannelOff_ShouldReturnFalse()
        {
            var mockSession = new Mock<ISession>();
            var response = new byte[] { 0x01, 0x52, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            mockSession.Setup(s => s.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(response);

            var device = new BNRC32(mockSession.Object, address: 1);
            bool result = await device.GetInputAsync(1);

            Assert.False(result);
        }

        [Fact]
        public async Task BNRC32_SetOutputAsync_ShouldSendCorrectCommand()
        {
            var mockSession = new Mock<ISession>();
            mockSession.Setup(s => s.SendOnlyAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

            var device = new BNRC32(mockSession.Object, address: 1);
            await device.SetOutputAsync(1, true);

            mockSession.Verify(s => s.SendOnlyAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task BNRC32_GetOutputAsync_ShouldReturnCorrectState()
        {
            var mockSession = new Mock<ISession>();
            var response = new byte[] { 0x01, 0x72, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00 };
            mockSession.Setup(s => s.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(response);

            var device = new BNRC32(mockSession.Object, address: 1);
            bool result = await device.GetOutputAsync(1);

            Assert.True(result);
        }

        [Fact]
        public async Task BNRC32_GetOutputAsync_ChannelOff_ShouldReturnFalse()
        {
            var mockSession = new Mock<ISession>();
            var response = new byte[] { 0x01, 0x72, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 };
            mockSession.Setup(s => s.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(response);

            var device = new BNRC32(mockSession.Object, address: 1);
            bool result = await device.GetOutputAsync(1);

            Assert.False(result);
        }

        [Fact]
        public async Task BNRC32_CloseAllAsync_ShouldSendCorrectCommand()
        {
            var mockSession = new Mock<ISession>();
            mockSession.Setup(s => s.SendOnlyAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

            var device = new BNRC32(mockSession.Object, address: 1);
            await device.CloseAllAsync();

            mockSession.Verify(s => s.SendOnlyAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task BNRC32_OpenAllAsync_ShouldSendCorrectCommand()
        {
            var mockSession = new Mock<ISession>();
            mockSession.Setup(s => s.SendOnlyAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

            var device = new BNRC32(mockSession.Object, address: 1);
            await device.OpenAllAsync();

            mockSession.Verify(s => s.SendOnlyAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task BNRC32_GetVersionAsync_ShouldReturnVersionString()
        {
            var mockSession = new Mock<ISession>();
            var response = new byte[] { 0x01, 0x66, 0x00, 0x00,
                                        0x42, 0x4E, 0x2D, 0x33, 0x32, 0x2D, 0x56, 0x32, 0x00 };
            mockSession.Setup(s => s.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(response);

            var device = new BNRC32(mockSession.Object, address: 1);
            string version = await device.GetVersionAsync();

            Assert.Equal("BN-32-V2", version);
        }

        [Fact]
        public async Task BNRC32_IsExistAsync_VersionContainsBN_ShouldReturnTrue()
        {
            var mockSession = new Mock<ISession>();
            var response = new byte[] { 0x01, 0x66, 0x00, 0x00,
                                        0x42, 0x4E, 0x2D, 0x33, 0x32, 0x2D, 0x56, 0x32, 0x00 };
            mockSession.Setup(s => s.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(response);

            var device = new BNRC32(mockSession.Object, address: 1);
            Assert.True(await device.IsExistAsync());
        }

        [Fact]
        public async Task BNRC32_IsExistAsync_VersionContainsIO_ShouldReturnTrue()
        {
            var mockSession = new Mock<ISession>();
            var response = new byte[] { 0x01, 0x66, 0x00, 0x00,
                                        0x49, 0x4F, 0x2D, 0x33, 0x32, 0x2D, 0x30, 0x30, 0x00 };
            mockSession.Setup(s => s.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(response);

            var device = new BNRC32(mockSession.Object, address: 1);
            Assert.True(await device.IsExistAsync());
        }

        [Fact]
        public async Task BNRC32_IsExistAsync_SessionThrows_ShouldReturnFalse()
        {
            var mockSession = new Mock<ISession>();
            mockSession.Setup(s => s.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new Exception("连接失败"));

            var device = new BNRC32(mockSession.Object, address: 1);
            Assert.False(await device.IsExistAsync());
        }

        [Fact]
        public async Task BNRC32_GetAllStatusesAsync_ShouldReturn32States()
        {
            var mockSession = new Mock<ISession>();
            // BNRC32: 每字节4路, raw[4+i/4], 8字节=32路
            // data[0]=0x55 = 0101_0101 => hex="55"
            // channel 1 (bitIndex=0): hex[1]='5', '5'=='1'||'5' => true
            // channel 2 (bitIndex=1): hex[1]='5', '5'=='4'||'5' => true
            // channel 3 (bitIndex=2): hex[0]='5', '5'=='1'||'5' => true
            // channel 4 (bitIndex=3): hex[0]='5', '5'=='4'||'5' => true
            // data[1]=0xAA = 1010_1010 => hex="AA"
            // channel 5 (bitIndex=0): hex[1]='A', 'A'!='1'&&'A'!='5' => false
            var response = new byte[] { 0x01, 0x53, 0x00, 0x00,
                                        0x55, 0xAA, 0x55, 0xAA, 0x55, 0xAA, 0x55, 0xAA };
            mockSession.Setup(s => s.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(response);

            var device = new BNRC32(mockSession.Object, address: 1);
            var statuses = await device.GetAllStatusesAsync();

            Assert.Equal(32, statuses.Count);
            Assert.True(statuses[0]);  // 路1
            Assert.True(statuses[1]);  // 路2
            Assert.True(statuses[2]);  // 路3
            Assert.True(statuses[3]);  // 路4
            Assert.False(statuses[4]); // 路5
        }

        [Fact]
        public async Task BNRC32_GetAnalogInputAsync_ShouldReturnAnalogValue()
        {
            var mockSession = new Mock<ISession>();
            var response = new byte[] { 0x01, 0x0A, 0x00, 0x00, 0x00, 0xA0, 0x0F, 0x00, 0x00, 0x00 };
            mockSession.Setup(s => s.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(response);

            var device = new BNRC32(mockSession.Object, address: 1);
            int value = await device.GetAnalogInputAsync(1);

            Assert.Equal(4000, value);
        }

        [Fact]
        public async Task BNRC32_OpenAsync_ShouldCallSessionOpen()
        {
            var mockSession = new Mock<ISession>();
            mockSession.Setup(s => s.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            mockSession.Setup(s => s.IsOpen).Returns(true);

            var device = new BNRC32(mockSession.Object, address: 1);
            await device.OpenAsync();

            mockSession.Verify(s => s.OpenAsync(It.IsAny<CancellationToken>()), Times.Once);
            Assert.True(device.IsOpen);
        }

        [Fact]
        public async Task BNRC32_CloseAsync_ShouldCallSessionClose()
        {
            var mockSession = new Mock<ISession>();
            mockSession.Setup(s => s.CloseAsync()).Returns(Task.CompletedTask);
            mockSession.Setup(s => s.IsOpen).Returns(false);

            var device = new BNRC32(mockSession.Object, address: 1);
            await device.CloseAsync();

            mockSession.Verify(s => s.CloseAsync(), Times.Once);
            Assert.False(device.IsOpen);
        }

        [Fact]
        public void BNRC32_Name_ShouldBeBNRC32()
        {
            var mockSession = new Mock<ISession>();
            var device = new BNRC32(mockSession.Object, address: 1);
            Assert.Equal("BNRC32", device.Name);
        }

        [Fact]
        public void BNRC32_Constructor_WithSession_ShouldCreateInstance()
        {
            var mockSession = new Mock<ISession>();
            var device = new BNRC32(mockSession.Object, address: 5);
            Assert.NotNull(device);
            Assert.Equal("BNRC32", device.Name);
        }

        #endregion
    }
}
