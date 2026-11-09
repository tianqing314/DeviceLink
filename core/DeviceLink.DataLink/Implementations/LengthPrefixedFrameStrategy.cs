using System;

namespace DeviceLink.DataLink
{
    /// <summary>
    /// 长度前缀帧策略 —— 用帧内的长度字段定界，适用于 MBAP（Modbus TCP）、
    /// 各类 TLV 与私有二进制协议。
    ///
    /// 与"分隔符定界"的本质区别：二进制协议的载荷里随时可能出现 0x00，
    /// 用分隔符定界必然误切，必须靠长度字段。
    ///
    /// 安全约定（这是本策略存在的主要理由）：
    /// · 长度字段是<b>不可信输入</b>，不是对方的承诺；
    /// · 只有通过 <see cref="MaxFrameSize"/> 校验后才会分配内存；
    /// · 声明长度非法时不按它分配，而是滑动一字节重新寻找帧起点，
    ///   因此一个 0xFFFF 的伪造长度最多只会让解析器滑动，不会造成 OOM。
    /// </summary>
    public sealed class LengthPrefixedFrameStrategy : IFrameStrategy
    {
        /// <summary>默认帧长上限（64 KiB）。实际使用时应按业务最大报文设置得更小。</summary>
        public const int DefaultMaxFrameSize = 64 * 1024;

        private readonly int _lengthFieldOffset;
        private readonly int _lengthFieldSize;
        private readonly bool _bigEndian;
        private readonly int _lengthAdjustment;
        private readonly int _maxFrameSize;
        private readonly int _minPayloadLength;

        /// <summary>
        /// 创建长度前缀帧策略。
        /// </summary>
        /// <param name="lengthFieldOffset">长度字段相对帧起点的字节偏移，默认 0</param>
        /// <param name="lengthFieldSize">长度字段宽度，支持 1 / 2 / 4 字节，默认 2</param>
        /// <param name="bigEndian">长度字段是否大端（网络字节序），默认 true</param>
        /// <param name="lengthAdjustment">
        /// 长度值与实际帧长的修正量。约定：长度值 = 长度字段之后的字节数 + lengthAdjustment。
        /// MBAP 场景取 0；若协议把长度字段自身也算进去，可用负值修正。
        /// </param>
        /// <param name="maxFrameSize">整帧字节上限，超过即视为坏帧，默认 64 KiB</param>
        /// <param name="minPayloadLength">
        /// 长度字段允许的最小值，默认 1（拒绝长度为 0 的空帧）。
        /// 设 0 表示允许空载荷；MBAP 场景实际最小为 2（单元标识 + 功能码）。
        /// </param>
        public LengthPrefixedFrameStrategy(
            int lengthFieldOffset = 0,
            int lengthFieldSize = 2,
            bool bigEndian = true,
            int lengthAdjustment = 0,
            int maxFrameSize = DefaultMaxFrameSize,
            int minPayloadLength = 1)
        {
            if (lengthFieldOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(lengthFieldOffset), "长度字段偏移不能为负");
            if (lengthFieldSize != 1 && lengthFieldSize != 2 && lengthFieldSize != 4)
                throw new ArgumentOutOfRangeException(nameof(lengthFieldSize), "长度字段宽度只支持 1 / 2 / 4 字节");
            if (maxFrameSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxFrameSize), "帧长上限必须大于 0");
            if (minPayloadLength < 0)
                throw new ArgumentOutOfRangeException(nameof(minPayloadLength), "最小载荷长度不能为负");

            _lengthFieldOffset = lengthFieldOffset;
            _lengthFieldSize = lengthFieldSize;
            _bigEndian = bigEndian;
            _lengthAdjustment = lengthAdjustment;
            _maxFrameSize = maxFrameSize;
            _minPayloadLength = minPayloadLength;
        }

        /// <inheritdoc/>
        public string Name =>
            $"LengthPrefixed(offset={_lengthFieldOffset},size={_lengthFieldSize},max={_maxFrameSize})";

        /// <summary>整帧字节上限。</summary>
        public int MaxFrameSize => _maxFrameSize;

        /// <summary>长度字段之后的固定头部字节数（帧起点到载荷起点）。</summary>
        private int HeaderSize => _lengthFieldOffset + _lengthFieldSize;

        /// <inheritdoc/>
        public byte[] BuildFrame(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Length < _lengthFieldOffset)
                throw new ArgumentException(
                    $"载荷长度 {data.Length} 小于长度字段偏移 {_lengthFieldOffset}，无法插入长度字段",
                    nameof(data));

            long payloadLength = (long)data.Length - _lengthFieldOffset + _lengthAdjustment;
            if (payloadLength < 0)
                throw new ArgumentException("长度字段值为负，请检查 lengthAdjustment 配置", nameof(data));

            var frame = new byte[data.Length + _lengthFieldSize];

            // 长度字段之前的头部原样保留
            if (_lengthFieldOffset > 0)
                Array.Copy(data, 0, frame, 0, _lengthFieldOffset);

            WriteLength(frame, _lengthFieldOffset, payloadLength);

            // 长度字段之后的内容整体后移
            int tail = data.Length - _lengthFieldOffset;
            if (tail > 0)
                Array.Copy(data, _lengthFieldOffset, frame, HeaderSize, tail);

            return frame;
        }

        /// <inheritdoc/>
        public bool TryParseFrame(byte[] accumulated, out int frameLength, out byte[] frameData)
        {
            frameLength = 0;
            frameData = Array.Empty<byte>();

            if (accumulated == null) return false;

            int headerSize = HeaderSize;
            if (accumulated.Length < headerSize) return false;

            int maxScan = accumulated.Length - headerSize;

            for (int scan = 0; scan <= maxScan; scan++)
            {
                long declared = ReadLength(accumulated, scan);
                long total = headerSize + declared - _lengthAdjustment;

                // ① 先判合法范围，再谈分配 —— 顺序反了就是 OOM 漏洞
                if (declared < _minPayloadLength || total < headerSize || total > _maxFrameSize)
                    continue;   // 长度不可信：滑动一字节重新同步

                // ② 长度合法但数据尚未收齐：等待更多字节，不分配
                if (accumulated.Length - scan < total)
                    return false;

                // ③ 校验通过，此时才分配
                frameLength = scan + (int)total;
                frameData = new byte[total];
                Array.Copy(accumulated, scan, frameData, 0, (int)total);
                return true;
            }

            return false;
        }

        private long ReadLength(byte[] buffer, int frameStart)
        {
            int pos = frameStart + _lengthFieldOffset;

            switch (_lengthFieldSize)
            {
                case 1:
                    return buffer[pos];

                case 2:
                    return _bigEndian
                        ? (long)((buffer[pos] << 8) | buffer[pos + 1])
                        : (long)((buffer[pos + 1] << 8) | buffer[pos]);

                default:
                    uint v = _bigEndian
                        ? ((uint)buffer[pos] << 24) | ((uint)buffer[pos + 1] << 16) |
                          ((uint)buffer[pos + 2] << 8) | buffer[pos + 3]
                        : ((uint)buffer[pos + 3] << 24) | ((uint)buffer[pos + 2] << 16) |
                          ((uint)buffer[pos + 1] << 8) | buffer[pos];
                    return v;
            }
        }

        private void WriteLength(byte[] buffer, int pos, long value)
        {
            switch (_lengthFieldSize)
            {
                case 1:
                    if (value > byte.MaxValue)
                        throw new ArgumentException($"长度 {value} 超出 1 字节字段上限 {byte.MaxValue}");
                    buffer[pos] = (byte)value;
                    break;

                case 2:
                    if (value > ushort.MaxValue)
                        throw new ArgumentException($"长度 {value} 超出 2 字节字段上限 {ushort.MaxValue}");
                    if (_bigEndian)
                    {
                        buffer[pos] = (byte)(value >> 8);
                        buffer[pos + 1] = (byte)value;
                    }
                    else
                    {
                        buffer[pos] = (byte)value;
                        buffer[pos + 1] = (byte)(value >> 8);
                    }
                    break;

                default:
                    if (value > uint.MaxValue)
                        throw new ArgumentException($"长度 {value} 超出 4 字节字段上限 {uint.MaxValue}");
                    var v = (uint)value;
                    if (_bigEndian)
                    {
                        buffer[pos] = (byte)(v >> 24);
                        buffer[pos + 1] = (byte)(v >> 16);
                        buffer[pos + 2] = (byte)(v >> 8);
                        buffer[pos + 3] = (byte)v;
                    }
                    else
                    {
                        buffer[pos] = (byte)v;
                        buffer[pos + 1] = (byte)(v >> 8);
                        buffer[pos + 2] = (byte)(v >> 16);
                        buffer[pos + 3] = (byte)(v >> 24);
                    }
                    break;
            }
        }
    }
}
