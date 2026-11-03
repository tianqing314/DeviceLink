using System.Text;
using DeviceLink.DataLink;
using DeviceLink.Protocol;
using Xunit;

namespace DeviceLink.Tests
{
    /// <summary>
    /// 线上帧格式回归测试 —— 锁定设备侧实际收到的字节序列。
    /// 重构/优化时这些断言必须保持通过；任何差异都意味着交互帧格式被改变。
    /// </summary>
    public class WireFormatRegressionTests
    {
        [Fact]
        public void ConST_CurrentWireBytes_Unchanged()
        {
            var codec = new ConSTCodec();                 // 默认地址 255，终止符 \0
            var strategy = new DelimiterFrameStrategy(new byte[] { 0 });

            var payload = codec.Encode(Command.Read("PRES"));
            var frame = strategy.BuildFrame(payload);

            // 现状：编解码器已拼接收尾符，链路层再追加一次 -> "255:R:PRES:\0\0"
            Assert.Equal(Encoding.ASCII.GetBytes("255:R:PRES:\0\0"), frame);
        }

        [Fact]
        public void Scpi_CurrentWireBytes_Unchanged()
        {
            var codec = new ScpiCodec();                  // 默认终止符 \n
            var strategy = new DelimiterFrameStrategy("\n");

            var payload = codec.Encode(Command.NonQuery("*IDN?"));
            var frame = strategy.BuildFrame(payload);

            // 现状："*IDN?\n" + "\n" -> "*IDN?\n\n"
            Assert.Equal(Encoding.ASCII.GetBytes("*IDN?\n\n"), frame);
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
            // 协议层声明“载荷是否自带终止符”，供链路层按策略去重时参考
            Assert.True(new ConSTCodec().PayloadIncludesTerminator);
            Assert.True(new ScpiCodec().PayloadIncludesTerminator);
            Assert.False(new ModbusRtuCodec(1).PayloadIncludesTerminator);
            Assert.False(new ZqwlCodec(1).PayloadIncludesTerminator);
        }

        [Fact]
        public void ConST_SkipPolicy_EmitsSingleTerminator()
        {
            var codec = new ConSTCodec();
            var strategy = new DelimiterFrameStrategy(
                new byte[] { 0 }, 1, FrameTerminationPolicy.SkipIfAlreadyTerminated);

            var frame = strategy.BuildFrame(codec.Encode(Command.Read("PRES")));

            // 显式选择去重后：设备收到 "255:R:PRES:\0"（单终止符）
            Assert.Equal(Encoding.ASCII.GetBytes("255:R:PRES:\0"), frame);
        }

        [Fact]
        public void Scpi_SkipPolicy_EmitsSingleTerminator()
        {
            var codec = new ScpiCodec();
            var strategy = new DelimiterFrameStrategy(
                "\n", 1, FrameTerminationPolicy.SkipIfAlreadyTerminated);

            var frame = strategy.BuildFrame(codec.Encode(Command.NonQuery("*IDN?")));

            // 显式选择去重后："*IDN?\n"（单终止符）
            Assert.Equal(Encoding.ASCII.GetBytes("*IDN?\n"), frame);
        }
    }
}
