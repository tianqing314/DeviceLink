using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DeviceLink.Device.ZQWL;
using DeviceLink.Session;
using Xunit;

namespace DeviceLink.Tests
{
    /// <summary>
    /// BNRC 系列继电器驱动的不依赖硬件测试。
    ///
    /// 为什么需要这组测试：BNRC8 / BNRC16 / BNRC32 的数据区编码各不相同
    /// （1 字节 1 路 / 2 路 / 4 路），而这正是三个驱动唯一真正有差异的地方。
    /// 过去这三个类各写一份，只能靠真机验证；现在抽出 <c>BnrcRelayBase</c> 后，
    /// 用注入 <see cref="ISession"/> 的方式就能把每种编码的边界钉死。
    ///
    /// 注入点：<c>BNRC8/16/32(ISession session, byte address)</c> 构造函数。
    /// </summary>
    public class BnrcRelayTests
    {
        /// <summary>构造一帧"已解析"的响应：[addr][func][data x8]，共 10 字节。</summary>
        private static byte[] Response(byte func, params byte[] data)
        {
            var raw = new byte[10];
            raw[0] = 0x01;
            raw[1] = func;
            Array.Copy(data, 0, raw, 2, Math.Min(data.Length, 8));
            return raw;
        }

        // ─────────────────── BNRC8：1 字节 1 路 ───────────────────

        [Fact]
        public async Task BNRC8_GetInput_MapsOneBytePerChannel()
        {
            // BNRC8 的索引基准与 16/32 不同：解码读 raw[1 + channel]，
            // 而数据区从 raw[2] 起，等价于 data[channel - 1]。
            var data = new byte[] { 0x01 };

            using var relay = new BNRC8(new StubSession(Response(0x52, data)));

            Assert.True(await relay.GetInputAsync(1));
            Assert.False(await relay.GetInputAsync(2));
        }

        [Fact]
        public async Task BNRC8_GetInput_LastChannelUsesLastDataByte()
        {
            var data = new byte[8];
            data[7] = 0x01;   // 第 8 路

            using var relay = new BNRC8(new StubSession(Response(0x52, data)));

            Assert.True(await relay.GetInputAsync(8));
            Assert.False(await relay.GetInputAsync(7));
        }

        [Fact]
        public async Task BNRC8_GetAllStatuses_ReturnsEightChannels()
        {
            var data = new byte[] { 0x01, 0x00, 0x01, 0x00, 0x01, 0x00, 0x01, 0x00 };

            using var relay = new BNRC8(new StubSession(Response(0x53, data)));
            var statuses = await relay.GetAllStatusesAsync();

            Assert.Equal(8, statuses.Count);
            Assert.True(statuses[0]);
            Assert.False(statuses[1]);
            Assert.True(statuses[6]);
            Assert.False(statuses[7]);
        }

        [Fact]
        public async Task BNRC8_OpenAll_SendsOneBytePerChannel()
        {
            var session = new StubSession(Response(0x57));
            using var relay = new BNRC8(session);

            await relay.OpenAllAsync();

            Assert.Equal(0x57, session.LastRequest![1]);
            for (int i = 2; i < 10; i++)
                Assert.Equal(0x01, session.LastRequest[i]);
        }

        // ─────────────────── BNRC16：nibble（1 字节 2 路） ───────────────────

        [Theory]
        [InlineData(1, true)]   // 低 4 位
        [InlineData(2, false)]  // 高 4 位
        public async Task BNRC16_GetInput_SplitsNibbles(int channel, bool expected)
        {
            // data[0] = 0x01 → 低 4 位=1（奇数路开），高 4 位=0（偶数路关）
            var data = new byte[] { 0x01 };

            using var relay = new BNRC16(new StubSession(Response(0x52, data)));
            Assert.Equal(expected, await relay.GetInputAsync(channel));
        }

        [Fact]
        public async Task BNRC16_GetInput_HighNibbleDecodedAsChannelTwo()
        {
            var data = new byte[] { 0x10 };

            using var relay = new BNRC16(new StubSession(Response(0x52, data)));

            Assert.False(await relay.GetInputAsync(1));
            Assert.True(await relay.GetInputAsync(2));
        }

        [Fact]
        public async Task BNRC16_GetAllStatuses_ReturnsSixteenChannels()
        {
            // 0x11 → 两路都开
            var data = new byte[] { 0x11, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            using var relay = new BNRC16(new StubSession(Response(0x53, data)));
            var statuses = await relay.GetAllStatusesAsync();

            Assert.Equal(16, statuses.Count);
            Assert.True(statuses[0]);
            Assert.True(statuses[1]);
            Assert.False(statuses[2]);
            Assert.False(statuses[3]);
        }

        [Fact]
        public async Task BNRC16_OpenAll_SendsNibblePattern()
        {
            var session = new StubSession(Response(0x57));
            using var relay = new BNRC16(session);

            await relay.OpenAllAsync();

            for (int i = 2; i < 10; i++)
                Assert.Equal(0x11, session.LastRequest![i]);
        }

        // ─────────────────── BNRC32：quad（1 字节 4 路） ───────────────────

        [Fact]
        public async Task BNRC32_GetInput_TreatsEachByteAsFourChannels()
        {
            // 0x55 = 0101 0101 → 每字节 4 路全开
            var data = new byte[] { 0x55 };

            using var relay = new BNRC32(new StubSession(Response(0x52, data)));

            Assert.True(await relay.GetInputAsync(1));
            Assert.True(await relay.GetInputAsync(2));
            Assert.True(await relay.GetInputAsync(3));
            Assert.True(await relay.GetInputAsync(4));
        }

        [Fact]
        public async Task BNRC32_GetInput_ZeroByteMeansAllOff()
        {
            var data = new byte[] { 0x00 };

            using var relay = new BNRC32(new StubSession(Response(0x52, data)));

            Assert.False(await relay.GetInputAsync(1));
            Assert.False(await relay.GetInputAsync(2));
            Assert.False(await relay.GetInputAsync(3));
            Assert.False(await relay.GetInputAsync(4));
        }

        [Fact]
        public async Task BNRC32_GetAllStatuses_ReturnsThirtyTwoChannels()
        {
            var data = new byte[] { 0x55, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            using var relay = new BNRC32(new StubSession(Response(0x53, data)));
            var statuses = await relay.GetAllStatusesAsync();

            Assert.Equal(32, statuses.Count);
            Assert.True(statuses[0]);
            Assert.True(statuses[3]);
            Assert.False(statuses[4]);
        }

        [Fact]
        public async Task BNRC32_OpenAll_SendsQuadPattern()
        {
            var session = new StubSession(Response(0x57));
            using var relay = new BNRC32(session);

            await relay.OpenAllAsync();

            for (int i = 2; i < 10; i++)
                Assert.Equal(0x55, session.LastRequest![i]);
        }

        // ─────────────────── 共性行为 ───────────────────

        [Fact]
        public async Task SetOutput_EncodesChannelAndState()
        {
            var session = new StubSession(Response(0x70));
            using var relay = new BNRC16(session);

            await relay.SetOutputAsync(7, true);

            var request = session.LastRequest!;
            Assert.Equal(0x70, request[1]);  // 功能码：设置单路
            Assert.Equal(7, request[2]);     // 通道
            Assert.Equal(1, request[3]);     // 状态
        }

        [Fact]
        public async Task CloseAll_SendsZeroData()
        {
            var session = new StubSession(Response(0x57));
            using var relay = new BNRC32(session);

            await relay.CloseAllAsync();

            for (int i = 2; i < 10; i++)
                Assert.Equal(0x00, session.LastRequest![i]);
        }

        [Fact]
        public async Task IsExist_MatchesModelCode_InVersionString()
        {
            var version = new byte[8];
            System.Text.Encoding.ASCII.GetBytes("1BNRC16").CopyTo(version, 0);

            using var relay = new BNRC16(new StubSession(Response(0x66, version)));
            Assert.True(await relay.IsExistAsync());

            using var other = new BNRC32(new StubSession(Response(0x66, version)));
            Assert.False(await other.IsExistAsync());
        }

        [Fact]
        public async Task IsExist_ReturnsFalse_WhenResponseIsErrorCode()
        {
            // 功能码 0xFF 被编解码器判定为错误响应 → 抛 DeviceException → IsExist 返回 false
            using var relay = new BNRC8(new StubSession(Response(0xFF)));

            Assert.False(await relay.IsExistAsync());
        }

        /// <summary>
        /// 只回放固定响应的会话替身 —— 让驱动测试完全脱离硬件。
        /// </summary>
        private sealed class StubSession : ISession
        {
            private readonly byte[] _response;

            public StubSession(byte[] response) => _response = response;

            public string Name => "StubSession";
            public bool IsOpen { get; private set; } = true;
            public byte[]? LastRequest { get; private set; }

            public Task OpenAsync(CancellationToken ct = default)
            {
                IsOpen = true;
                return Task.CompletedTask;
            }

            public Task CloseAsync()
            {
                IsOpen = false;
                return Task.CompletedTask;
            }

            public Task<byte[]> SendAndReceiveAsync(byte[] request, CancellationToken ct = default)
            {
                LastRequest = request;
                return Task.FromResult(_response);
            }

            public Task SendOnlyAsync(byte[] request, CancellationToken ct = default)
            {
                LastRequest = request;
                return Task.CompletedTask;
            }

            public Task<byte[]> ReceiveOnlyAsync(CancellationToken ct = default)
                => Task.FromResult(_response);

            public void Dispose() { }

            public ValueTask DisposeAsync() => default;
        }
    }
}
