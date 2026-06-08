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
    public class BNRC16Tests
    {
        #region ZqwlCodec 编码测试（BNRC16 特有）

        [Fact]
        public void ZqwlCodec_Encode_GetAnalogInput_ShouldReturnCorrectFrame()
        {
            var codec = new ZqwlCodec(address: 1);
            var encoded = codec.Encode(Command.Read("GetAnalogInput.0"));
            Assert.Equal(10, encoded.Length);
            Assert.Equal(0x0A, encoded[1]);
            Assert.Equal(0, encoded[2]);
        }

        [Fact]
        public void ZqwlCodec_Encode_GetOutput_ShouldReturnCorrectFrame()
        {
            var codec = new ZqwlCodec(address: 1);
            var encoded = codec.Encode(Command.Read("GetOutput.1"));
            Assert.Equal(0x72, encoded[1]);
            Assert.Equal(1, encoded[2]);
        }

        #endregion

        #region BNRC16 数据编码测试

        [Fact]
        public void BNRC16_OpenAll_Data_ShouldBeCorrect()
        {
            var codec = new ZqwlCodec(address: 1);
            var encoded = codec.Encode(Command.Write("OpenAll", "11", "11", "11", "11", "11", "11", "11", "11"));
            for (int i = 0; i < 8; i++)
                Assert.Equal(0x11, encoded[i + 2]);
        }

        [Fact]
        public void BNRC16_CloseAll_Data_ShouldBeCorrect()
        {
            var codec = new ZqwlCodec(address: 1);
            var encoded = codec.Encode(Command.Write("CloseAll", "00", "00", "00", "00", "00", "00", "00", "00"));
            for (int i = 0; i < 8; i++)
                Assert.Equal(0x00, encoded[i + 2]);
        }

        [Fact]
        public void BNRC16_NibbleEncoding_ShouldEncodeOddChannelCorrectly()
        {
            var codec = new ZqwlCodec(address: 1);
            var encoded = codec.Encode(Command.Write("SetOutput.1.1"));
            Assert.Equal(1, encoded[2]);
            Assert.Equal(1, encoded[3]);
        }

        [Fact]
        public void BNRC16_NibbleEncoding_ShouldEncodeEvenChannelCorrectly()
        {
            var codec = new ZqwlCodec(address: 1);
            var encoded = codec.Encode(Command.Write("SetOutput.2.1"));
            Assert.Equal(2, encoded[2]);
            Assert.Equal(1, encoded[3]);
        }

        #endregion

        #region ZqwlCodec 解码测试（BNRC16 特有）

        [Fact]
        public void ZqwlCodec_ExtractAnalogValue_ShouldExtract16BitValue()
        {
            var codec = new ZqwlCodec(address: 1);
            var raw = new byte[] { 0x01, 0x0A, 0x00, 0x00, 0x00, 0xE8, 0x03, 0x00, 0x00, 0x00 };
            Assert.Equal(1000, codec.ExtractAnalogValue(raw));
        }

        [Fact]
        public void ZqwlCodec_ExtractInputState_Channel1_ShouldBeTrue()
        {
            var codec = new ZqwlCodec(address: 1);
            var raw = new byte[] { 0x01, 0x52, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            Assert.True(codec.ExtractInputState(raw, 1));
        }

        [Fact]
        public void ZqwlCodec_ExtractInputState_Channel2_ShouldBeFalse()
        {
            var codec = new ZqwlCodec(address: 1);
            var raw = new byte[] { 0x01, 0x52, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            Assert.False(codec.ExtractInputState(raw, 2));
        }

        #endregion

        #region ZqwlFrameStrategy 帧策略测试

        [Fact]
        public void ZqwlFrameStrategy_BuildFrame_SetOutput_ShouldBuildCorrectFrame()
        {
            var strategy = new DeviceLink.DataLink.ZqwlFrameStrategy();
            var codec = new ZqwlCodec(address: 1);
            var data = codec.Encode(Command.Write("SetOutput.1.1"));
            var frame = strategy.BuildFrame(data);
            Assert.Equal(15, frame.Length);
            Assert.Equal(0x48, frame[0]);
            Assert.Equal(0x3A, frame[1]);
            Assert.Equal(0x45, frame[13]);
            Assert.Equal(0x44, frame[14]);
        }

        [Fact]
        public void ZqwlFrameStrategy_TryParseFrame_SetOutput_ShouldParseCorrectly()
        {
            var strategy = new DeviceLink.DataLink.ZqwlFrameStrategy();
            var codec = new ZqwlCodec(address: 1);
            var data = codec.Encode(Command.Write("SetOutput.1.1"));
            var frame = strategy.BuildFrame(data);
            bool parsed = strategy.TryParseFrame(frame, out int frameLength, out byte[] frameData);
            Assert.True(parsed);
            Assert.Equal(15, frameLength);
            Assert.Equal(10, frameData.Length);
        }

        #endregion

        #region BNRC16 设备方法测试（使用 Moq 模拟 ISession）

        [Fact]
        public async Task BNRC16_GetInputAsync_OddChannel_ShouldUseNibbleLow4Bits()
        {
            var mockSession = new Mock<ISession>();
            // BNRC16: ExtractNibbleInput requires raw.Length >= 12
            // channel=1, dataIndex = ceil(1/2.0) = 1, raw[1+1]=raw[2]
            // 奇数路: (raw[2] & 0x0F) >= 1 => 需要 raw[2] = 0x01
            var response = new byte[] { 0x01, 0x52, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            mockSession.Setup(s => s.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(response);

            var device = new BNRC16(mockSession.Object, address: 1);
            bool result = await device.GetInputAsync(1);

            Assert.True(result);
        }

        [Fact]
        public async Task BNRC16_GetInputAsync_EvenChannel_ShouldUseNibbleHigh4Bits()
        {
            var mockSession = new Mock<ISession>();
            // channel=2, dataIndex = ceil(2/2.0) = 1, raw[1+1]=raw[2]
            // 偶数路: (raw[2] >> 4) >= 1 => 需要 raw[2] = 0x10
            var response = new byte[] { 0x01, 0x52, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            mockSession.Setup(s => s.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(response);

            var device = new BNRC16(mockSession.Object, address: 1);
            bool result = await device.GetInputAsync(2);

            Assert.True(result);
        }

        [Fact]
        public async Task BNRC16_GetInputAsync_ChannelOff_ShouldReturnFalse()
        {
            var mockSession = new Mock<ISession>();
            var response = new byte[] { 0x01, 0x52, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            mockSession.Setup(s => s.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(response);

            var device = new BNRC16(mockSession.Object, address: 1);
            bool result = await device.GetInputAsync(1);

            Assert.False(result);
        }

        [Fact]
        public async Task BNRC16_SetOutputAsync_ShouldSendCorrectCommand()
        {
            var mockSession = new Mock<ISession>();
            mockSession.Setup(s => s.SendOnlyAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

            var device = new BNRC16(mockSession.Object, address: 1);
            await device.SetOutputAsync(1, true);

            mockSession.Verify(s => s.SendOnlyAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task BNRC16_GetOutputAsync_ShouldReturnCorrectState()
        {
            var mockSession = new Mock<ISession>();
            // data[4]=channel, data[5]=state(0x01=开)
            var response = new byte[] { 0x01, 0x72, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00 };
            mockSession.Setup(s => s.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(response);

            var device = new BNRC16(mockSession.Object, address: 1);
            bool result = await device.GetOutputAsync(1);

            Assert.True(result);
        }

        [Fact]
        public async Task BNRC16_GetOutputAsync_ChannelOff_ShouldReturnFalse()
        {
            var mockSession = new Mock<ISession>();
            var response = new byte[] { 0x01, 0x72, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 };
            mockSession.Setup(s => s.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(response);

            var device = new BNRC16(mockSession.Object, address: 1);
            bool result = await device.GetOutputAsync(1);

            Assert.False(result);
        }

        [Fact]
        public async Task BNRC16_CloseAllAsync_ShouldSendCorrectCommand()
        {
            var mockSession = new Mock<ISession>();
            mockSession.Setup(s => s.SendOnlyAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

            var device = new BNRC16(mockSession.Object, address: 1);
            await device.CloseAllAsync();

            mockSession.Verify(s => s.SendOnlyAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task BNRC16_OpenAllAsync_ShouldSendCorrectCommand()
        {
            var mockSession = new Mock<ISession>();
            mockSession.Setup(s => s.SendOnlyAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

            var device = new BNRC16(mockSession.Object, address: 1);
            await device.OpenAllAsync();

            mockSession.Verify(s => s.SendOnlyAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task BNRC16_GetVersionAsync_ShouldReturnVersionString()
        {
            var mockSession = new Mock<ISession>();
            var response = new byte[] { 0x01, 0x66, 0x00, 0x00,
                                        0x42, 0x4E, 0x2D, 0x31, 0x36, 0x2D, 0x56, 0x31, 0x00 };
            mockSession.Setup(s => s.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(response);

            var device = new BNRC16(mockSession.Object, address: 1);
            string version = await device.GetVersionAsync();

            Assert.Equal("BN-16-V1", version);
        }

        [Fact]
        public async Task BNRC16_IsExistAsync_VersionContainsBN_ShouldReturnTrue()
        {
            var mockSession = new Mock<ISession>();
            var response = new byte[] { 0x01, 0x66, 0x00, 0x00,
                                        0x42, 0x4E, 0x2D, 0x31, 0x36, 0x2D, 0x56, 0x31, 0x00 };
            mockSession.Setup(s => s.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(response);

            var device = new BNRC16(mockSession.Object, address: 1);
            Assert.True(await device.IsExistAsync());
        }

        [Fact]
        public async Task BNRC16_IsExistAsync_VersionContainsIO_ShouldReturnTrue()
        {
            var mockSession = new Mock<ISession>();
            var response = new byte[] { 0x01, 0x66, 0x00, 0x00,
                                        0x49, 0x4F, 0x2D, 0x31, 0x36, 0x2D, 0x30, 0x30, 0x00 };
            mockSession.Setup(s => s.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(response);

            var device = new BNRC16(mockSession.Object, address: 1);
            Assert.True(await device.IsExistAsync());
        }

        [Fact]
        public async Task BNRC16_IsExistAsync_SessionThrows_ShouldReturnFalse()
        {
            var mockSession = new Mock<ISession>();
            mockSession.Setup(s => s.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new Exception("连接失败"));

            var device = new BNRC16(mockSession.Object, address: 1);
            Assert.False(await device.IsExistAsync());
        }

        [Fact]
        public async Task BNRC16_GetAllStatusesAsync_ShouldReturn16States()
        {
            var mockSession = new Mock<ISession>();
            // BNRC16: raw.Length >= 12 required
            // data[0]=0x01 => 路1(低4位=1)开, 路2(高4位=0)关
            // data[1]=0x10 => 路3(低4位=0)关, 路4(高4位=16)开
            var response = new byte[] { 0x01, 0x53, 0x00, 0x00,
                                        0x01, 0x10, 0x11, 0x00, 0x01, 0x10, 0x11, 0x00 };
            mockSession.Setup(s => s.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(response);

            var device = new BNRC16(mockSession.Object, address: 1);
            var statuses = await device.GetAllStatusesAsync();

            Assert.Equal(16, statuses.Count);
            Assert.True(statuses[0]);  // 路1（低4位 >= 1）
            Assert.False(statuses[1]); // 路2（高4位 < 16）
            Assert.False(statuses[2]); // 路3
            Assert.True(statuses[3]);  // 路4
        }

        [Fact]
        public async Task BNRC16_GetAnalogInputAsync_ShouldReturnAnalogValue()
        {
            var mockSession = new Mock<ISession>();
            var response = new byte[] { 0x01, 0x0A, 0x00, 0x00, 0x00,
                                        0xE8, 0x03, 0x00, 0x00, 0x00 };
            mockSession.Setup(s => s.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(response);

            var device = new BNRC16(mockSession.Object, address: 1);
            int value = await device.GetAnalogInputAsync(1);

            Assert.Equal(1000, value);
        }

        [Fact]
        public async Task BNRC16_OpenAsync_ShouldCallSessionOpen()
        {
            var mockSession = new Mock<ISession>();
            mockSession.Setup(s => s.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            mockSession.Setup(s => s.IsOpen).Returns(true);

            var device = new BNRC16(mockSession.Object, address: 1);
            await device.OpenAsync();

            mockSession.Verify(s => s.OpenAsync(It.IsAny<CancellationToken>()), Times.Once);
            Assert.True(device.IsOpen);
        }

        [Fact]
        public async Task BNRC16_CloseAsync_ShouldCallSessionClose()
        {
            var mockSession = new Mock<ISession>();
            mockSession.Setup(s => s.CloseAsync()).Returns(Task.CompletedTask);
            mockSession.Setup(s => s.IsOpen).Returns(false);

            var device = new BNRC16(mockSession.Object, address: 1);
            await device.CloseAsync();

            mockSession.Verify(s => s.CloseAsync(), Times.Once);
            Assert.False(device.IsOpen);
        }

        [Fact]
        public void BNRC16_Name_ShouldBeBNRC16()
        {
            var mockSession = new Mock<ISession>();
            var device = new BNRC16(mockSession.Object, address: 1);
            Assert.Equal("BNRC16", device.Name);
        }

        [Fact]
        public void BNRC16_Constructor_WithSession_ShouldCreateInstance()
        {
            var mockSession = new Mock<ISession>();
            var device = new BNRC16(mockSession.Object, address: 5);
            Assert.NotNull(device);
            Assert.Equal("BNRC16", device.Name);
        }

        #endregion
    }
}
