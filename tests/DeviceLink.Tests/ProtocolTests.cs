using System;
using System.Text;
using DeviceLink.Protocol;
using Xunit;

namespace DeviceLink.Tests
{
    /// <summary>
    /// 协议层测试
    /// </summary>
    public class ProtocolTests
    {
        [Fact]
        public void ConSTCodec_Encode_ReadCommand_ShouldEncodeCorrectly()
        {
            // Arrange
            var codec = new ConSTCodec(255);
            var command = Command.Read("MRMD");

            // Act
            var encoded = codec.Encode(command);

            // Assert
            var text = Encoding.ASCII.GetString(encoded);
            Assert.Equal("255:R:MRMD:\0", text);
        }

        [Fact]
        public void ConSTCodec_Encode_WriteCommand_ShouldEncodeCorrectly()
        {
            // Arrange
            var codec = new ConSTCodec(255);
            var command = Command.Write("OAV", "64");

            // Act
            var encoded = codec.Encode(command);

            // Assert
            var text = Encoding.ASCII.GetString(encoded);
            Assert.Equal("255:W:OAV:64:\0", text);
        }

        [Fact]
        public void ConSTCodec_DecodeText_ShouldDecodeCorrectly()
        {
            // Arrange
            var codec = new ConSTCodec(255);
            var data = Encoding.ASCII.GetBytes("255:F:MRMD:1.23456\0");

            // Act
            var text = codec.DecodeText(data);

            // Assert
            Assert.Equal("255:F:MRMD:1.23456", text);
        }

        [Fact]
        public void ConSTCodec_IsErrorResponse_ShouldDetectError()
        {
            // Arrange
            var codec = new ConSTCodec(255);
            var data = Encoding.ASCII.GetBytes("255:E:ERR_OVER\0");

            // Act
            bool isError = codec.IsErrorResponse(data, out var errorMessage);

            // Assert
            Assert.True(isError);
            Assert.Equal("ERR_OVER", errorMessage);
        }

        [Fact]
        public void ConSTCodec_IsErrorResponse_ShouldNotDetectSuccessAsError()
        {
            // Arrange
            var codec = new ConSTCodec(255);
            var data = Encoding.ASCII.GetBytes("255:F:MRMD:1.23456\0");

            // Act
            bool isError = codec.IsErrorResponse(data, out var errorMessage);

            // Assert
            Assert.False(isError);
            Assert.Empty(errorMessage);
        }

        [Fact]
        public void ConSTCodec_ExtractField_ShouldExtractCorrectly()
        {
            // Arrange
            var codec = new ConSTCodec(255);
            var data = Encoding.ASCII.GetBytes("255:F:MRMD:1.23456\0");

            // Act
            var field = codec.ExtractField(data, 3);

            // Assert
            Assert.Equal("1.23456", field);
        }

        [Fact]
        public void ConSTCodec_ExtractFields_ShouldExtractCorrectly()
        {
            // Arrange
            var codec = new ConSTCodec(255);
            var data = Encoding.ASCII.GetBytes("255:F:ORAN:0:100:G:0.05\0");

            // Act
            var fields = codec.ExtractFields(data, 3);

            // Assert
            Assert.Equal(4, fields.Length);
            Assert.Equal("0", fields[0]);
            Assert.Equal("100", fields[1]);
            Assert.Equal("G", fields[2]);
            Assert.Equal("0.05", fields[3]);
        }

        [Fact]
        public void ScpiCodec_Encode_ReadCommand_ShouldEncodeCorrectly()
        {
            // Arrange
            var codec = new ScpiCodec();
            var command = Command.Read("*IDN");

            // Act
            var encoded = codec.Encode(command);

            // Assert
            var text = Encoding.ASCII.GetString(encoded);
            Assert.Equal("*IDN?\n", text);
        }

        [Fact]
        public void ScpiCodec_Encode_WriteCommand_ShouldEncodeCorrectly()
        {
            // Arrange
            var codec = new ScpiCodec();
            var command = Command.Write("MEAS:VOLT", "DC");

            // Act
            var encoded = codec.Encode(command);

            // Assert
            var text = Encoding.ASCII.GetString(encoded);
            Assert.Equal("MEAS:VOLT DC\n", text);
        }

        [Fact]
        public void ScpiCodec_IsErrorResponse_ShouldDetectError()
        {
            // Arrange
            var codec = new ScpiCodec();
            var data = Encoding.ASCII.GetBytes("-100,\"Command error\"\n");

            // Act
            bool isError = codec.IsErrorResponse(data, out var errorMessage);

            // Assert
            Assert.True(isError);
            Assert.Equal("Command error", errorMessage);
        }

        [Fact]
        public void ModbusRtuCodec_Encode_ReadCommand_ShouldEncodeCorrectly()
        {
            // Arrange
            var codec = new ModbusRtuCodec(1);
            var command = Command.Read("3.0.10");

            // Act
            var encoded = codec.Encode(command);

            // Assert
            Assert.Equal(9, encoded.Length); // 从站地址(1) + 功能码(1) + 地址(2) + 数量(2) + CRC(2)
            Assert.Equal(1, encoded[0]); // 从站地址
            Assert.Equal(3, encoded[1]); // 功能码
        }

        [Fact]
        public void ModbusRtuCodec_IsErrorResponse_ShouldDetectError()
        {
            // Arrange
            var codec = new ModbusRtuCodec(1);
            var data = new byte[] { 1, 0x83, 0x02, 0xC0, 0xF1 }; // 错误响应

            // Act
            bool isError = codec.IsErrorResponse(data, out var errorMessage);

            // Assert
            Assert.True(isError);
            Assert.Equal("非法数据地址", errorMessage);
        }
    }
}
