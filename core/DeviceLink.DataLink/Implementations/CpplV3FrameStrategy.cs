using System;

namespace DeviceLink.DataLink
{
    /// <summary>
    /// CPPI V3 帧策略（多板卡通信协议 V3.1）。
    /// 
    /// 帧格式（V3.1，3字节地址）：
    ///   [帧头 0x55][控制 1B][目标地址 3B][源地址 3B][功能码 2B][流水号 1B]
    ///   [数据长度 2B][头部CRC8 1B][数据 N B][数据CRC16 2B]
    /// 
    /// 控制字段：
    ///   bit0:    1=发送帧(Tx), 0=响应帧(Rx)
    ///   bit[1:3]: 000=V3.0(2B地址), 001=V3.1(3B地址)
    /// 
    /// 多字节数据采用小端模式。
    /// CRC8 计算范围：帧头～数据长度（共13字节），多项式 0x07，初始值 0x00。
    /// CRC16 计算范围：数据内容（N字节），CRC16-CCITT-FALSE（多项式 0x1021，初始值 0xFFFF，不反射）。
    /// </summary>
    public class CpplV3FrameStrategy : IFrameStrategy
    {
        private const byte FrameHeader = 0x55;
        private const int HeaderSize = 13; // 帧头到数据长度（不含CRC8）
        private const int Crc8Size = 1;
        private const int Crc16Size = 2;
        private const int MinFrameSize = HeaderSize + Crc8Size + Crc16Size; // 16字节（无数据时）

        /// <summary>帧头到数据长度的固定字节数（用于CRC8计算）</summary>
        private const int Crc8Length = 13;

        /// <summary>默认目标地址（转换板）：26 01 00 (小端)</summary>
        private static readonly byte[] DefaultTargetAddress = new byte[] { 0x26, 0x01, 0x00 };

        /// <summary>默认源地址（PC）：36 22 11 (小端)</summary>
        private static readonly byte[] DefaultSourceAddress = new byte[] { 0x36, 0x22, 0x11 };

        private readonly byte[] _targetAddress;
        private readonly byte[] _sourceAddress;
        private readonly ushort _sendFunctionCode;
        private readonly ushort _recvFunctionCode;
        private byte _sequenceNumber;

        /// <inheritdoc/>
        public string Name => "CpplV3";

        /// <summary>
        /// 创建 CPPI V3 帧策略（使用默认地址和功能码）
        /// </summary>
        public CpplV3FrameStrategy()
            : this(DefaultTargetAddress, DefaultSourceAddress, 0x0400, 0x4900, 0x10)
        {
        }

        /// <summary>
        /// 创建 CPPI V3 帧策略
        /// </summary>
        /// <param name="targetAddress">目标地址（3字节，小端）</param>
        /// <param name="sourceAddress">源地址（3字节，小端）</param>
        /// <param name="sendFunctionCode">发送帧功能码</param>
        /// <param name="recvFunctionCode">响应帧功能码</param>
        /// <param name="initialSequenceNumber">初始流水号（默认0x10）</param>
        public CpplV3FrameStrategy(
            byte[] targetAddress,
            byte[] sourceAddress,
            ushort sendFunctionCode = 0x0400,
            ushort recvFunctionCode = 0x4900,
            byte initialSequenceNumber = 0x10)
        {
            _targetAddress = targetAddress ?? throw new ArgumentNullException(nameof(targetAddress));
            _sourceAddress = sourceAddress ?? throw new ArgumentNullException(nameof(sourceAddress));
            if (_targetAddress.Length != 3)
                throw new ArgumentException("目标地址必须为3字节", nameof(targetAddress));
            if (_sourceAddress.Length != 3)
                throw new ArgumentException("源地址必须为3字节", nameof(sourceAddress));
            _sendFunctionCode = sendFunctionCode;
            _recvFunctionCode = recvFunctionCode;
            _sequenceNumber = initialSequenceNumber;
        }

        /// <inheritdoc/>
        public byte[] BuildFrame(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            // data = Modbus 命令（不含CRC），需要先添加 Modbus CRC16
            byte[] modbusFrame;
            if (data.Length >= 2)
            {
                // 检查是否已经包含 Modbus CRC（通过检查最后2字节是否为有效CRC）
                // 简单起见：如果调用方传入的数据不含CRC，我们添加
                ushort calcCrc = CalculateModbusCrc16(data, data.Length);
                modbusFrame = new byte[data.Length + 2];
                Array.Copy(data, 0, modbusFrame, 0, data.Length);
                modbusFrame[data.Length] = (byte)(calcCrc & 0xFF);
                modbusFrame[data.Length + 1] = (byte)((calcCrc >> 8) & 0xFF);
            }
            else
            {
                modbusFrame = data;
            }

            // CPPI V3 帧：头部(13) + CRC8(1) + 数据(N) + CRC16(2) = 16 + N
            int frameSize = MinFrameSize + modbusFrame.Length;
            var frame = new byte[frameSize];

            // 帧头
            frame[0] = FrameHeader;

            // 控制字段：bit0=1(Tx), bit[1:3]=001(V3.1)
            frame[1] = 0x03;

            // 目标地址（3B，小端）
            frame[2] = _targetAddress[0];
            frame[3] = _targetAddress[1];
            frame[4] = _targetAddress[2];

            // 源地址（3B，小端）
            frame[5] = _sourceAddress[0];
            frame[6] = _sourceAddress[1];
            frame[7] = _sourceAddress[2];

            // 功能码（2B，小端）
            frame[8] = (byte)(_sendFunctionCode & 0xFF);
            frame[9] = (byte)((_sendFunctionCode >> 8) & 0xFF);

            // 流水号
            frame[10] = _sequenceNumber++;

            // 数据长度（2B，小端）
            ushort dataLen = (ushort)modbusFrame.Length;
            frame[11] = (byte)(dataLen & 0xFF);
            frame[12] = (byte)((dataLen >> 8) & 0xFF);

            // 头部 CRC8（计算前13字节）
            frame[13] = CalculateCrc8(frame, Crc8Length);

            // 数据内容
            Array.Copy(modbusFrame, 0, frame, HeaderSize + Crc8Size, modbusFrame.Length);

            // 数据 CRC16（计算数据部分的CRC）
            ushort dataCrc = CalculateCrc16(modbusFrame, 0, modbusFrame.Length);
            frame[frameSize - 2] = (byte)(dataCrc & 0xFF);
            frame[frameSize - 1] = (byte)((dataCrc >> 8) & 0xFF);

            return frame;
        }

        /// <inheritdoc/>
        public bool TryParseFrame(byte[] accumulated, out int frameLength, out byte[] frameData)
        {
            frameLength = 0;
            frameData = Array.Empty<byte>();

            if (accumulated == null || accumulated.Length < MinFrameSize)
                return false;

            // 查找帧头 0x55
            int headerPos = -1;
            for (int i = 0; i <= accumulated.Length - MinFrameSize; i++)
            {
                if (accumulated[i] == FrameHeader)
                {
                    headerPos = i;
                    break;
                }
            }

            if (headerPos < 0)
                return false;

            // 检查是否有足够的头部数据
            if (accumulated.Length < headerPos + HeaderSize)
                return false;

            // 解析数据长度（小端）
            ushort dataLen = (ushort)(accumulated[headerPos + 11] | (accumulated[headerPos + 12] << 8));

            // 计算完整帧长度
            int totalFrameLen = HeaderSize + Crc8Size + dataLen + Crc16Size;

            // 检查是否有足够的数据
            if (accumulated.Length < headerPos + totalFrameLen)
                return false;

            // 验证头部 CRC8
            byte receivedCrc8 = accumulated[headerPos + HeaderSize];
            byte calculatedCrc8 = CalculateCrc8(accumulated, Crc8Length, headerPos);
            if (receivedCrc8 != calculatedCrc8)
                return false;

            // 验证数据 CRC16
            if (dataLen > 0)
            {
                int dataStart = headerPos + HeaderSize + Crc8Size;
                ushort receivedCrc16 = (ushort)(accumulated[headerPos + totalFrameLen - 2]
                    | (accumulated[headerPos + totalFrameLen - 1] << 8));
                ushort calculatedCrc16 = CalculateCrc16(accumulated, dataStart, dataLen);
                if (receivedCrc16 != calculatedCrc16)
                    return false;
            }

            // 提取数据内容（Modbus 响应，含CRC）
            frameLength = headerPos + totalFrameLen;

            if (dataLen > 2)
            {
                // 去掉 Modbus CRC16，返回不含CRC的Modbus数据
                int dataStart = headerPos + HeaderSize + Crc8Size;
                frameData = new byte[dataLen - 2];
                Array.Copy(accumulated, dataStart, frameData, 0, dataLen - 2);
            }
            else if (dataLen > 0)
            {
                int dataStart = headerPos + HeaderSize + Crc8Size;
                frameData = new byte[dataLen];
                Array.Copy(accumulated, dataStart, frameData, 0, dataLen);
            }

            return true;
        }

        /// <summary>
        /// 计算 CRC8（多项式 0x07）
        /// </summary>
        private static byte CalculateCrc8(byte[] data, int length, int offset = 0)
        {
            byte crc = 0x00;
            for (int i = 0; i < length; i++)
            {
                crc ^= data[offset + i];
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x80) != 0)
                    {
                        crc = (byte)((crc << 1) ^ 0x07);
                    }
                    else
                    {
                        crc = (byte)(crc << 1);
                    }
                }
            }
            return crc;
        }

        /// <summary>
        /// 计算 CRC16-CCITT-FALSE（多项式 0x1021，初始值 0xFFFF，不反射）
        /// </summary>
        private static ushort CalculateCrc16(byte[] data, int offset, int length)
        {
            ushort crc = 0xFFFF;
            for (int i = 0; i < length; i++)
            {
                crc ^= (ushort)(data[offset + i] << 8);
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x8000) != 0)
                    {
                        crc = (ushort)((crc << 1) ^ 0x1021);
                    }
                    else
                    {
                        crc = (ushort)(crc << 1);
                    }
                }
            }
            return crc;
        }

        /// <summary>
        /// 计算 Modbus CRC16（多项式 0xA001）
        /// </summary>
        private static ushort CalculateModbusCrc16(byte[] data, int length)
        {
            ushort crc = 0xFFFF;
            for (int i = 0; i < length; i++)
            {
                crc ^= data[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x0001) != 0)
                    {
                        crc >>= 1;
                        crc ^= 0xA001;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }
            return crc;
        }
    }
}
