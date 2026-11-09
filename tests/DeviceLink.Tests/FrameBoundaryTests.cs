using System.Linq;
using DeviceLink.DataLink;
using Xunit;

namespace DeviceLink.Tests
{
    /// <summary>
    /// 帧定界测试 —— 验证 L2 按协议规范定界，以及长度字段不可信时的内存安全。
    /// </summary>
    public class FrameBoundaryTests
    {
        // ───────────────── Modbus RTU：按功能码定长 ─────────────────

        [Fact]
        public void Modbus_ReadHoldingRegistersResponse_UsesByteCountToBoundFrame()
        {
            var strategy = new ModbusRtuFrameStrategy();

            // [地址][0x03][字节数=4][0000][0064][CRC]
            var payload = new byte[] { 0x01, 0x03, 0x04, 0x00, 0x64, 0x00, 0xC8 };
            var frame = strategy.BuildFrame(payload);

            Assert.Equal(9, frame.Length);   // 3 + 4 + 2

            Assert.True(strategy.TryParseFrame(frame, out int frameLength, out byte[] data));
            Assert.Equal(9, frameLength);
            Assert.Equal(payload, data);
        }

        [Fact]
        public void Modbus_ExceptionResponse_IsFiveBytes()
        {
            var strategy = new ModbusRtuFrameStrategy();

            // [地址][0x83 = 0x03|0x80][异常码=02][CRC]
            var payload = new byte[] { 0x01, 0x83, 0x02 };
            var frame = strategy.BuildFrame(payload);

            Assert.Equal(5, frame.Length);

            Assert.True(strategy.TryParseFrame(frame, out int frameLength, out byte[] data));
            Assert.Equal(5, frameLength);
            Assert.Equal(payload, data);
        }

        [Fact]
        public void Modbus_IncompleteFrame_WaitsInsteadOfGuessing()
        {
            var strategy = new ModbusRtuFrameStrategy();

            var frame = strategy.BuildFrame(new byte[] { 0x01, 0x03, 0x04, 0x00, 0x64, 0x00, 0xC8 });
            var partial = frame.Take(6).ToArray();

            // 数据未收齐必须返回 false 等待，不能凭空切帧
            Assert.False(strategy.TryParseFrame(partial, out _, out _));
        }

        [Fact]
        public void Modbus_RecoversAfterLeadingGarbageByte()
        {
            var strategy = new ModbusRtuFrameStrategy();

            var frame = strategy.BuildFrame(new byte[] { 0x01, 0x03, 0x02, 0x00, 0x64 });
            var dirty = new byte[] { 0xAA }.Concat(frame).ToArray();

            Assert.True(strategy.TryParseFrame(dirty, out int frameLength, out byte[] data));

            // 应滑动到 offset=1 重新对齐，并跳过前面那个垃圾字节
            Assert.Equal(dirty.Length, frameLength);
            Assert.Equal(frame.Length - 2, data.Length);
        }

        // ───────────────── 长度前缀：先校验再分配 ─────────────────

        [Fact]
        public void LengthPrefixed_RoundTrip()
        {
            var strategy = new LengthPrefixedFrameStrategy();

            var frame = strategy.BuildFrame(new byte[] { 0x12, 0x34, 0x56 });

            // 长度字段 = 3，整帧 = 2 + 3
            Assert.Equal(new byte[] { 0x00, 0x03, 0x12, 0x34, 0x56 }, frame);

            Assert.True(strategy.TryParseFrame(frame, out int frameLength, out byte[] data));
            Assert.Equal(5, frameLength);
            Assert.Equal(frame, data);
        }

        [Fact]
        public void LengthPrefixed_MbapLayout_InsertsLengthAfterTransactionHeader()
        {
            // MBAP: 长度字段位于 offset=4，值 = 单元标识 + PDU 的字节数
            var strategy = new LengthPrefixedFrameStrategy(lengthFieldOffset: 4, lengthFieldSize: 2);

            // 事务ID(2) + 协议ID(2) + [长度(2) 由策略插入] + 单元ID(1) + PDU(2)
            var body = new byte[] { 0x00, 0x01, 0x00, 0x00, 0x01, 0x03, 0x00 };
            var frame = strategy.BuildFrame(body);

            Assert.Equal(9, frame.Length);
            Assert.Equal(0x00, frame[4]);
            Assert.Equal(0x03, frame[5]);   // 1(单元ID) + 2(PDU)

            Assert.True(strategy.TryParseFrame(frame, out int frameLength, out _));
            Assert.Equal(9, frameLength);
        }

        [Fact]
        public void LengthPrefixed_ForgedHugeLength_DoesNotAllocate()
        {
            // 一个 2 字节长度字段写 0xFFFF，绝不能触发 64KB 以上的分配
            var strategy = new LengthPrefixedFrameStrategy(maxFrameSize: 1024);

            var forged = new byte[] { 0xFF, 0xFF, 0x00, 0x00 };

            Assert.False(strategy.TryParseFrame(forged, out _, out _));
        }

        [Fact]
        public void LengthPrefixed_SplitArrival_WaitsForFullFrame()
        {
            var strategy = new LengthPrefixedFrameStrategy();
            var frame = strategy.BuildFrame(new byte[] { 0xAA, 0xBB });

            Assert.False(strategy.TryParseFrame(frame.Take(3).ToArray(), out _, out _));

            Assert.True(strategy.TryParseFrame(frame, out int frameLength, out byte[] data));
            Assert.Equal(frame.Length, frameLength);
            Assert.Equal(frame, data);
        }

        [Fact]
        public void LengthPrefixed_BigEndianAndLittleEndianAreDistinguished()
        {
            var big = new LengthPrefixedFrameStrategy(lengthFieldSize: 2, bigEndian: true);
            var little = new LengthPrefixedFrameStrategy(lengthFieldSize: 2, bigEndian: false);

            // 同一段字节，大小端解释不同：大端=0x0102(258)，小端=0x0201(513)
            var raw = new byte[] { 0x01, 0x02, 0x09, 0x09 };

            // 大端声明 258 字节，超出默认上限内的可取数据，且远未收齐
            Assert.False(big.TryParseFrame(raw, out _, out _));

            // 小端声明 513 字节，同样未收齐
            Assert.False(little.TryParseFrame(raw, out _, out _));

            // 用足够小的声明值证明解析方向正确
            var bigFrame = new byte[] { 0x00, 0x02, 0xAA, 0xBB };
            Assert.True(big.TryParseFrame(bigFrame, out _, out _));

            var littleFrame = new byte[] { 0x02, 0x00, 0xAA, 0xBB };
            Assert.True(little.TryParseFrame(littleFrame, out _, out _));
        }
    }
}
