using DeviceLink.DataLink;
using System;
using Xunit;

namespace DeviceLink.Tests.PS02
{
    /// <summary>
    /// CpplV3FrameStrategy 单元测试
    /// </summary>
    public class CpplV3FrameStrategyTests
    {
        private readonly CpplV3FrameStrategy _strategy = new CpplV3FrameStrategy();

        [Fact]
        public void BuildFrame_ShouldContainHeader0x55()
        {
            var data = new byte[] { 0x01, 0x03, 0x00, 0x02, 0x00, 0x02 };
            var frame = _strategy.BuildFrame(data);

            Assert.Equal(0x55, frame[0]);
        }

        [Fact]
        public void BuildFrame_ControlField_ShouldBe0x03()
        {
            var data = new byte[] { 0x01, 0x03, 0x00, 0x02, 0x00, 0x02 };
            var frame = _strategy.BuildFrame(data);

            // 控制字段: bit0=1(Tx), bits[1:3]=001(V3.1) = 0x03
            Assert.Equal(0x03, frame[1]);
        }

        [Fact]
        public void BuildFrame_ShouldHaveCorrectTotalLength()
        {
            var data = new byte[] { 0x01, 0x03, 0x00, 0x02, 0x00, 0x02 }; // 6 bytes Modbus
            var frame = _strategy.BuildFrame(data);

            // Modbus CRC16 added: 6 + 2 = 8 bytes
            // CPPI V3: 14 (header+CRC8) + 8 (data) + 2 (CRC16) = 24 bytes
            Assert.Equal(24, frame.Length);
        }

        [Fact]
        public void BuildFrame_ShouldContainTargetAddress()
        {
            var data = new byte[] { 0x01, 0x03, 0x00, 0x02, 0x00, 0x02 };
            var frame = _strategy.BuildFrame(data);

            // 默认目标地址: 23 01 00 (小端)
            Assert.Equal(0x23, frame[2]);
            Assert.Equal(0x01, frame[3]);
            Assert.Equal(0x00, frame[4]);
        }

        [Fact]
        public void BuildFrame_ShouldContainSourceAddress()
        {
            var data = new byte[] { 0x01, 0x03, 0x00, 0x02, 0x00, 0x02 };
            var frame = _strategy.BuildFrame(data);

            // 默认源地址: 36 22 11 (小端)
            Assert.Equal(0x36, frame[5]);
            Assert.Equal(0x22, frame[6]);
            Assert.Equal(0x11, frame[7]);
        }

        [Fact]
        public void BuildFrame_ShouldContainFunctionCode()
        {
            var data = new byte[] { 0x01, 0x03, 0x00, 0x02, 0x00, 0x02 };
            var frame = _strategy.BuildFrame(data);

            // 默认发送功能码: 0x4900 (小端: 00 49)
            Assert.Equal(0x00, frame[8]);
            Assert.Equal(0x49, frame[9]);
        }

        [Fact]
        public void BuildFrame_SequenceNumber_ShouldIncrement()
        {
            var data = new byte[] { 0x01, 0x03, 0x00, 0x02, 0x00, 0x02 };

            var frame1 = _strategy.BuildFrame(data);
            var frame2 = _strategy.BuildFrame(data);

            Assert.Equal(frame1[10] + 1, frame2[10]);
        }

        [Fact]
        public void BuildFrame_DataLength_ShouldMatchModbusWithCRC()
        {
            var data = new byte[] { 0x01, 0x03, 0x00, 0x02, 0x00, 0x02 }; // 6 bytes
            var frame = _strategy.BuildFrame(data);

            // 数据长度 = Modbus数据(6) + Modbus CRC(2) = 8 bytes (小端: 08 00)
            ushort dataLen = (ushort)(frame[11] | (frame[12] << 8));
            Assert.Equal(8, dataLen);
        }

        [Fact]
        public void TryParseFrame_ValidFrame_ShouldReturnTrue()
        {
            var data = new byte[] { 0x01, 0x03, 0x04, 0x43, 0xFA, 0x00, 0x00 };
            var frame = _strategy.BuildFrame(data);

            var result = _strategy.TryParseFrame(frame, out int frameLength, out byte[] frameData);

            Assert.True(result);
            Assert.Equal(frame.Length, frameLength);
            Assert.NotNull(frameData);
        }

        [Fact]
        public void TryParseFrame_TooShort_ShouldReturnFalse()
        {
            var shortData = new byte[10]; // 小于最小帧长度
            var result = _strategy.TryParseFrame(shortData, out _, out _);

            Assert.False(result);
        }

        [Fact]
        public void TryParseFrame_NoHeader_ShouldReturnFalse()
        {
            var data = new byte[20];
            data[0] = 0x00; // 不是 0x55
            var result = _strategy.TryParseFrame(data, out _, out _);

            Assert.False(result);
        }

        [Fact]
        public void TryParseFrame_InvalidCrc8_ShouldReturnFalse()
        {
            var data = new byte[] { 0x01, 0x03, 0x04, 0x43, 0xFA, 0x00, 0x00 };
            var frame = _strategy.BuildFrame(data);

            // 篡改 CRC8
            frame[13] ^= 0xFF;

            var result = _strategy.TryParseFrame(frame, out _, out _);
            Assert.False(result);
        }

        [Fact]
        public void TryParseFrame_InvalidCrc16_ShouldReturnFalse()
        {
            var data = new byte[] { 0x01, 0x03, 0x04, 0x43, 0xFA, 0x00, 0x00 };
            var frame = _strategy.BuildFrame(data);

            // 篡改 CRC16（最后字节）
            frame[^1] ^= 0xFF;

            var result = _strategy.TryParseFrame(frame, out _, out _);
            Assert.False(result);
        }

        [Fact]
        public void BuildParse_RoundTrip_ShouldPreserveData()
        {
            var originalModbusData = new byte[] { 0x01, 0x03, 0x04, 0x43, 0xFA, 0x00, 0x00 };
            var frame = _strategy.BuildFrame(originalModbusData);

            var result = _strategy.TryParseFrame(frame, out _, out byte[] parsedData);

            Assert.True(result);
            // 解析出的数据应该是原始 Modbus 数据（不含 CRC）
            Assert.Equal(originalModbusData.Length, parsedData.Length);
            for (int i = 0; i < originalModbusData.Length; i++)
            {
                Assert.Equal(originalModbusData[i], parsedData[i]);
            }
        }

        [Fact]
        public void BuildFrame_WithCustomAddresses_ShouldUseProvidedValues()
        {
            var target = new byte[] { 0xAA, 0xBB, 0xCC };
            var source = new byte[] { 0x11, 0x22, 0x33 };
            var strategy = new CpplV3FrameStrategy(target, source, 0x1234, 0x5678);

            var data = new byte[] { 0x01, 0x03 };
            var frame = strategy.BuildFrame(data);

            Assert.Equal(0xAA, frame[2]);
            Assert.Equal(0xBB, frame[3]);
            Assert.Equal(0xCC, frame[4]);
            Assert.Equal(0x11, frame[5]);
            Assert.Equal(0x22, frame[6]);
            Assert.Equal(0x33, frame[7]);
        }

        [Fact]
        public void Name_ShouldReturnCpplV3()
        {
            Assert.Equal("CpplV3", _strategy.Name);
        }

        [Fact]
        public void BuildFrame_NullData_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _strategy.BuildFrame(null!));
        }
    }
}
