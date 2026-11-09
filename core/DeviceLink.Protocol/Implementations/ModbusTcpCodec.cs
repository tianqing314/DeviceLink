using System;
using System.Text;

namespace DeviceLink.Protocol
{
    /// <summary>
    /// Modbus TCP 协议编解码器（L7）。
    ///
    /// ADU 结构：[事务标识 2B][协议标识 2B][长度 2B][单元标识 1B][PDU N]
    ///
    /// 分层约定：
    /// · 本类负责 L7 —— 事务标识、协议标识、单元标识与 PDU；
    /// · <b>长度字段不由本类填充</b>，它属于 L2 的应用消息定界职责，
    ///   由 <c>LengthPrefixedFrameStrategy(lengthFieldOffset: 4)</c> 在组帧时插入；
    /// · 因此 Encode 返回 [TID][PID][UID][PDU]，帧策略插入长度后即成为完整 MBAP ADU。
    ///
    /// 关于事务标识：它只是请求-响应的配对标签，不具备会话语义。
    /// Modbus TCP 没有会话建立、会话状态与会话恢复，故本类不提供任何会话能力。
    /// </summary>
    public class ModbusTcpCodec : IProtocolCodec
    {
        /// <summary>完整 MBAP 头长度（含 2 字节长度字段）。</summary>
        public const int MbapHeaderLength = 7;

        /// <summary>长度字段在 ADU 中的偏移，供 <c>LengthPrefixedFrameStrategy</c> 直接使用。</summary>
        public const int LengthFieldOffset = 4;

        private readonly byte _unitId;
        private readonly ModbusRtuCodec _pduBuilder = new ModbusRtuCodec();
        private int _transactionId;

        /// <summary>
        /// 创建 Modbus TCP 编解码器。
        /// </summary>
        /// <param name="unitId">单元标识符。经网关连接串行从站时填从站地址，直连设备一般为 1 或 0xFF。</param>
        public ModbusTcpCodec(byte unitId = 1)
        {
            _unitId = unitId;
        }

        /// <inheritdoc/>
        public string ProtocolName => "ModbusTCP";

        /// <inheritdoc/>
        public bool PayloadIncludesTerminator => false;

        /// <summary>最近一次 Encode 分配的事务标识，供上层做响应配对校验。</summary>
        public ushort LastTransactionId { get; private set; }

        /// <inheritdoc/>
        public byte[] Encode(Command command)
        {
            // 复用 RTU 的 PDU 构建逻辑：[从站][功能码][数据...]，去掉首字节即为 PDU
            var rtu = _pduBuilder.Encode(command);
            int pduLength = rtu.Length - 1;

            ushort tid = NextTransactionId();

            // [TID 2][PID 2][UID 1][PDU N]
            // 长度字段缺失是刻意的：由 L2 帧策略在 offset=4 处插入
            var adu = new byte[4 + 1 + pduLength];
            adu[0] = (byte)(tid >> 8);
            adu[1] = (byte)tid;
            adu[2] = 0x00;          // 协议标识恒为 0
            adu[3] = 0x00;
            adu[4] = _unitId;
            Array.Copy(rtu, 1, adu, 5, pduLength);

            return adu;
        }

        /// <inheritdoc/>
        public string DecodeText(byte[] raw)
        {
            if (raw == null || raw.Length == 0)
                return string.Empty;

            var sb = new StringBuilder(raw.Length * 3);
            for (int i = 0; i < raw.Length; i++)
            {
                sb.Append(raw[i].ToString("X2"));
                if (i < raw.Length - 1) sb.Append(' ');
            }
            return sb.ToString();
        }

        /// <inheritdoc/>
        public bool IsErrorResponse(byte[] raw, out string errorMessage)
        {
            errorMessage = string.Empty;

            // 完整 ADU = MBAP(7) + PDU，异常 PDU 至少 2 字节
            if (raw == null || raw.Length < MbapHeaderLength + 2)
                return false;

            byte functionCode = raw[MbapHeaderLength];
            if ((functionCode & 0x80) == 0)
                return false;

            byte errorCode = raw[MbapHeaderLength + 1];
            errorMessage = errorCode switch
            {
                0x01 => "非法功能码",
                0x02 => "非法数据地址",
                0x03 => "非法数据值",
                0x04 => "从站设备故障",
                0x05 => "确认",
                0x06 => "从站设备忙",
                0x08 => "存储奇偶性差错",
                0x0A => "不可用网关路径",
                0x0B => "网关目标设备响应失败",
                _ => $"未知错误码: 0x{errorCode:X2}"
            };
            return true;
        }

        /// <summary>
        /// 从响应 ADU 中提取寄存器值（PDU 位于 MBAP 之后）。
        /// </summary>
        /// <param name="raw">完整响应 ADU（含 MBAP）</param>
        /// <returns>寄存器值数组；格式不合法时返回空数组</returns>
        public ushort[] ExtractRegisters(byte[] raw)
        {
            if (raw == null || raw.Length < MbapHeaderLength + 3)
                return Array.Empty<ushort>();

            int pdu = MbapHeaderLength;
            byte functionCode = raw[pdu];

            if (functionCode != 0x03 && functionCode != 0x04 && functionCode != 0x28)
                return Array.Empty<ushort>();

            int byteCount = raw[pdu + 1];

            // 防御：字节数字段来自链路，不可信，缓冲区不足时直接返回
            if (raw.Length < pdu + 2 + byteCount)
                return Array.Empty<ushort>();

            int count = byteCount / 2;
            var registers = new ushort[count];
            for (int i = 0; i < count; i++)
            {
                registers[i] = (ushort)((raw[pdu + 2 + i * 2] << 8) | raw[pdu + 3 + i * 2]);
            }
            return registers;
        }

        /// <summary>
        /// 校验响应的 MBAP 头是否与最近一次请求匹配（事务标识 + 协议标识 + 单元标识）。
        /// 不匹配通常意味着响应滞后或串包，应当丢弃而不是交给业务层。
        /// </summary>
        public bool MatchesPendingRequest(byte[] raw, out string reason)
        {
            reason = string.Empty;

            if (raw == null || raw.Length < MbapHeaderLength)
            {
                reason = "响应短于 MBAP 头长度";
                return false;
            }

            ushort tid = (ushort)((raw[0] << 8) | raw[1]);
            if (tid != LastTransactionId)
            {
                reason = $"事务标识不匹配：期望 {LastTransactionId}，实际 {tid}";
                return false;
            }

            if (raw[2] != 0x00 || raw[3] != 0x00)
            {
                reason = $"协议标识非 0：0x{raw[2]:X2}{raw[3]:X2}";
                return false;
            }

            if (raw[4] != _unitId)
            {
                reason = $"单元标识不匹配：期望 {_unitId}，实际 {raw[4]}";
                return false;
            }

            return true;
        }

        private ushort NextTransactionId()
        {
            // 事务标识只需要局部分配唯一，不要求全局唯一，也不要求递增
            var value = (ushort)System.Threading.Interlocked.Increment(ref _transactionId);
            LastTransactionId = value;
            return value;
        }
    }
}
