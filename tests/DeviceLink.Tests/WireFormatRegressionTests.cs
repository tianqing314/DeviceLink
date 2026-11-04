using System.Text;
using DeviceLink.DataLink;
using DeviceLink.Protocol;
using Xunit;

namespace DeviceLink.Tests
{
    /// <summary>
    /// 线上帧格式回归测试 —— 锁定设备侧实际收到的字节序列。
    /// 默认策略为 SkipIfAlreadyTerminated：编解码器已自带终止符时不再重复追加。
    /// </summary>
    public class WireFormatRegressionTests
    {
        [Fact]
        public void ConST_DefaultWireBytes_SingleTerminator()
        {
            var codec = new ConSTCodec();                 // 默认地址 255，终止符 \0
            var strategy = new DelimiterFrameStrategy(new byte[] { 0 });

            var payload = codec.Encode(Command.Read("PRES"));
            var frame = strategy.BuildFrame(payload);

            // 编码器已含终止符，链路层去重 -> "255:R:PRES:\0"
            Assert.Equal(Encoding.ASCII.GetBytes("255:R:PRES:\0"), frame);
        }

        [Fact]
        public void Scpi_DefaultWireBytes_SingleTerminator()
        {
            var codec = new ScpiCodec();                  // 默认终止符 \n
            var strategy = new DelimiterFrameStrategy("\n");

            var payload = codec.Encode(Command.NonQuery("*IDN?"));
            var frame = strategy.BuildFrame(payload);

            // 编码器已含终止符，链路层去重 -> "*IDN?\n"
            Assert.Equal(Encoding.ASCII.GetBytes("*IDN?\n"), frame);
        }

        [Fact]
        public void ModbusRtu_CurrentWireBytes_Unchanged()
        {
            var codec = new ModbusRtuCodec(1);
            var strategy = new ModbusRtuFrameStrategy();

            var payload = codec.Encode(Command.Read("3.0.10"));
            var frame = strategy.BuildFrame(payload);

            // 从站1 / 功能码03 / 起始地址0 / 数量10 / CRC16=0xC5CD（低字节在前）
            var expected = new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A, 0xC5, 0xCD };
            Assert.Equal(expected, frame);
        }

        [Fact]
        public void Codec_PayloadIncludesTerminator_DeclaredPerProtocol()
        {
            // 协议层声明“载荷是否自带终止符”，链路层据此在默认策略下去重
            Assert.True(new ConSTCodec().PayloadIncludesTerminator);
            Assert.True(new ScpiCodec().PayloadIncludesTerminator);
            Assert.False(new ModbusRtuCodec(1).PayloadIncludesTerminator);
            Assert.False(new ZqwlCodec(1).PayloadIncludesTerminator);
        }

        [Fact]
        public void AppendAlwaysPolicy_EmitsDoubleTerminator()
        {
            var codec = new ConSTCodec();
            var strategy = new DelimiterFrameStrategy(
                new byte[] { 0 }, 1, FrameTerminationPolicy.AppendAlways);

            var frame = strategy.BuildFrame(codec.Encode(Command.Read("PRES")));

            // 兼容逃逸口：仍可按历史行为发送双终止符 "255:R:PRES:\0\0"
            Assert.Equal(Encoding.ASCII.GetBytes("255:R:PRES:\0\0"), frame);
        }

        [Fact]
        public void PayloadWithoutTerminator_StillGetsOneAppended()
        {
            var strategy = new DelimiterFrameStrategy("\r\n");
            var frame = strategy.BuildFrame(Encoding.ASCII.GetBytes("AT"));

            // 载荷不带终止符时，默认策略仍会补一个，不会漏加
            Assert.Equal(Encoding.ASCII.GetBytes("AT\r\n"), frame);
        }
    }
}
