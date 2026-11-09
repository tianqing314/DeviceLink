using DeviceLink.DataLink;
using DeviceLink.Protocol;
using Xunit;

namespace DeviceLink.Tests
{
    /// <summary>
    /// Modbus TCP 编解码测试。
    ///
    /// 重点验证分层边界：
    /// · L7 的 Codec 只输出 [TID][PID][UID][PDU]，不填长度字段；
    /// · L2 的 LengthPrefixedFrameStrategy 负责插入长度字段，拼成完整 MBAP ADU。
    /// </summary>
    public class ModbusTcpCodecTests
    {
        [Fact]
        public void Encode_ProducesAduWithoutLengthField()
        {
            var codec = new ModbusTcpCodec(unitId: 1);

            var body = codec.Encode(Command.Read("3.0.10"));

            // [TID 2][PID 2][UID 1][PDU 5]，长度字段刻意缺失
            Assert.Equal(10, body.Length);
            Assert.Equal(0x00, body[2]);   // 协议标识
            Assert.Equal(0x00, body[3]);
            Assert.Equal(0x01, body[4]);   // 单元标识
            Assert.Equal(0x03, body[5]);   // 功能码
            Assert.Equal(0x00, body[6]);   // 起始地址高字节
            Assert.Equal(0x00, body[7]);   // 起始地址低字节
            Assert.Equal(0x00, body[8]);   // 数量高字节
            Assert.Equal(0x0A, body[9]);   // 数量低字节 = 10
        }

        [Fact]
        public void FrameStrategy_InsertsMbapLength_ToFormCompleteAdu()
        {
            var codec = new ModbusTcpCodec(unitId: 1);
            var strategy = new LengthPrefixedFrameStrategy(
                lengthFieldOffset: ModbusTcpCodec.LengthFieldOffset,
                lengthFieldSize: 2);

            var frame = strategy.BuildFrame(codec.Encode(Command.Read("3.0.10")));

            // 完整 ADU = MBAP 7 + PDU 5
            Assert.Equal(12, frame.Length);

            Assert.Equal(0x00, frame[2]);   // 协议标识
            Assert.Equal(0x00, frame[3]);
            Assert.Equal(0x00, frame[4]);   // 长度高字节
            Assert.Equal(0x06, frame[5]);   // 长度 = 单元标识(1) + PDU(5)
            Assert.Equal(0x01, frame[6]);   // 单元标识
            Assert.Equal(0x03, frame[7]);   // 功能码

            // 帧策略能把完整 ADU 原样切出来
            Assert.True(strategy.TryParseFrame(frame, out int frameLength, out byte[] parsed));
            Assert.Equal(12, frameLength);
            Assert.Equal(frame, parsed);
        }

        [Fact]
        public void ExtractRegisters_SkipsMbapHeader()
        {
            var codec = new ModbusTcpCodec(unitId: 1);

            // [TID][PID][LEN=7][UID=1][FC=03][ByteCount=04][0064][00C8]
            var adu = new byte[]
            {
                0x00, 0x01,
                0x00, 0x00,
                0x00, 0x07,
                0x01,
                0x03, 0x04,
                0x00, 0x64,
                0x00, 0xC8
            };

            var registers = codec.ExtractRegisters(adu);

            Assert.Equal(2, registers.Length);
            Assert.Equal(100, registers[0]);
            Assert.Equal(200, registers[1]);
        }

        [Fact]
        public void IsErrorResponse_ReadsFunctionCodeFromPdu()
        {
            var codec = new ModbusTcpCodec(unitId: 1);

            // [TID][PID][LEN=3][UID=1][FC=0x83][异常码=02]
            var adu = new byte[]
            {
                0x00, 0x01,
                0x00, 0x00,
                0x00, 0x03,
                0x01,
                0x83, 0x02
            };

            Assert.True(codec.IsErrorResponse(adu, out string message));
            Assert.Contains("非法数据地址", message);
        }

        [Fact]
        public void MatchesPendingRequest_RejectsStaleTransactionId()
        {
            var codec = new ModbusTcpCodec(unitId: 1);

            var body = codec.Encode(Command.Read("3.0.0"));
            ushort tid = codec.LastTransactionId;

            // 用当前事务标识构造响应头
            var matching = new byte[] { (byte)(tid >> 8), (byte)tid, 0x00, 0x00, 0x01, 0x03, 0x02 };
            Assert.True(codec.MatchesPendingRequest(matching, out _));

            // 伪造一个陈旧的事务标识
            var stale = new byte[] { 0xFF, 0xFE, 0x00, 0x00, 0x01, 0x03, 0x02 };
            Assert.False(codec.MatchesPendingRequest(stale, out string reason));
            Assert.Contains("事务标识不匹配", reason);

            Assert.NotEmpty(body);
        }
    }
}
