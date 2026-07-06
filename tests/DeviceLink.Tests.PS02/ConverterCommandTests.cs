using DeviceLink.DataLink;
using System;
using Xunit;
using Xunit.Abstractions;

namespace DeviceLink.Tests.PS02
{
    /// <summary>
    /// 转接板 CPPI V3 指令帧构建测试
    /// 
    /// 验证 BuildRawFrame 方法生成的帧与文档中的示例一致。
    /// </summary>
    public class ConverterCommandTests
    {
        private readonly ITestOutputHelper _output;

        public ConverterCommandTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// 创建转接板专用帧策略（目标地址 0x000123，源地址 0x112236）
        /// </summary>
        private CpplV3FrameStrategy CreateConverterStrategy(byte initialSequenceNumber = 0x01)
        {
            return new CpplV3FrameStrategy(
                targetAddress: new byte[] { 0x23, 0x01, 0x00 },
                sourceAddress: new byte[] { 0x36, 0x22, 0x11 },
                initialSequenceNumber: initialSequenceNumber);
        }

        /// <summary>
        /// 创建 Modbus 转发帧策略（目标地址 0x000126，源地址 0x112236）
        /// 用于 Modbus RTU 转发指令（功能码 0x0400）
        /// </summary>
        private CpplV3FrameStrategy CreateModbusForwardStrategy(byte initialSequenceNumber = 0x01)
        {
            return new CpplV3FrameStrategy(
                targetAddress: new byte[] { 0x26, 0x01, 0x00 },
                sourceAddress: new byte[] { 0x36, 0x22, 0x11 },
                initialSequenceNumber: initialSequenceNumber);
        }

        /// <summary>
        /// 将字节数组转换为十六进制字符串（用于调试输出）
        /// </summary>
        private static string ToHexString(byte[] data)
        {
            return BitConverter.ToString(data).Replace("-", " ");
        }

        /// <summary>
        /// 验证 CRC8 计算（头部校验）
        /// </summary>
        private static byte CalculateCrc8(byte[] data, int length)
        {
            byte crc = 0x00;
            for (int i = 0; i < length; i++)
            {
                crc ^= data[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x80) != 0)
                        crc = (byte)((crc << 1) ^ 0x07);
                    else
                        crc = (byte)(crc << 1);
                }
            }
            return crc;
        }

        /// <summary>
        /// 验证 CRC16-CCITT-FALSE 计算（数据校验）
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
                        crc = (ushort)((crc << 1) ^ 0x1021);
                    else
                        crc = (ushort)(crc << 1);
                }
            }
            return crc;
        }

        /// <summary>
        /// 验证帧的基本结构
        /// </summary>
        private void ValidateFrameStructure(byte[] frame, ushort expectedFunctionCode, byte expectedSeqNum)
        {
            // 帧头
            Assert.Equal(0x55, frame[0]);

            // 控制字段：bit0=1(Tx), bit[1:3]=001(V3.1)
            Assert.Equal(0x03, frame[1]);

            // 目标地址：0x000123 (23 01 00)
            Assert.Equal(0x23, frame[2]);
            Assert.Equal(0x01, frame[3]);
            Assert.Equal(0x00, frame[4]);

            // 源地址：0x112236 (36 22 11)
            Assert.Equal(0x36, frame[5]);
            Assert.Equal(0x22, frame[6]);
            Assert.Equal(0x11, frame[7]);

            // 功能码（小端）
            Assert.Equal((byte)(expectedFunctionCode & 0xFF), frame[8]);
            Assert.Equal((byte)((expectedFunctionCode >> 8) & 0xFF), frame[9]);

            // 流水号
            Assert.Equal(expectedSeqNum, frame[10]);

            // 头部 CRC8（校验前 13 字节）
            byte expectedCrc8 = CalculateCrc8(frame, 13);
            Assert.Equal(expectedCrc8, frame[13]);
        }

        // ═══════════════════════════════════════════════════════════
        // 测试用例：与文档中的实际抓包数据对比
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void BuildRawFrame_ScanDevice_ShouldMatchDocumentSample()
        {
            // 文档指令 1：扫描从设备
            // 发送：55 03 23 01 00 36 22 11 00 03 01 01 00 D1 01 D1 F1
            var expected = new byte[] { 0x55, 0x03, 0x23, 0x01, 0x00, 0x36, 0x22, 0x11, 0x00, 0x03, 0x01, 0x01, 0x00, 0xD1, 0x01, 0xD1, 0xF1 };

            var strategy = CreateConverterStrategy(0x01);
            var actual = strategy.BuildRawFrame(0x0300, new byte[] { 0x01 });

            _output.WriteLine($"期望: {ToHexString(expected)}");
            _output.WriteLine($"实际: {ToHexString(actual)}");

            Assert.Equal(expected.Length, actual.Length);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void BuildRawFrame_ReadFirmwareVersion_ShouldMatchDocumentSample()
        {
            // 文档指令 2：读取设备固件版本
            // 发送：55 03 23 01 00 36 22 11 06 01 02 00 00 1E FF FF
            var expected = new byte[] { 0x55, 0x03, 0x23, 0x01, 0x00, 0x36, 0x22, 0x11, 0x06, 0x01, 0x02, 0x00, 0x00, 0x1E, 0xFF, 0xFF };

            var strategy = CreateConverterStrategy(0x02);
            var actual = strategy.BuildRawFrame(0x0106);

            _output.WriteLine($"期望: {ToHexString(expected)}");
            _output.WriteLine($"实际: {ToHexString(actual)}");

            Assert.Equal(expected.Length, actual.Length);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void BuildRawFrame_ReadHardwareVersion_ShouldMatchDocumentSample()
        {
            // 文档指令 4：读取设备硬件版本
            // 发送：55 03 23 01 00 36 22 11 08 01 04 00 00 31 FF FF
            var expected = new byte[] { 0x55, 0x03, 0x23, 0x01, 0x00, 0x36, 0x22, 0x11, 0x08, 0x01, 0x04, 0x00, 0x00, 0x31, 0xFF, 0xFF };

            var strategy = CreateConverterStrategy(0x04);
            var actual = strategy.BuildRawFrame(0x0108);

            _output.WriteLine($"期望: {ToHexString(expected)}");
            _output.WriteLine($"实际: {ToHexString(actual)}");

            Assert.Equal(expected.Length, actual.Length);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void BuildRawFrame_SetOutputProject_ShouldMatchDocumentSample()
        {
            // 文档指令 5：设定输出项目（关闭所有输出）
            // 发送：55 03 23 01 00 36 22 11 10 02 05 02 00 61 00 00 0F 1D
            var expected = new byte[] { 0x55, 0x03, 0x23, 0x01, 0x00, 0x36, 0x22, 0x11, 0x10, 0x02, 0x05, 0x02, 0x00, 0x61, 0x00, 0x00, 0x0F, 0x1D };

            var strategy = CreateConverterStrategy(0x05);
            var actual = strategy.BuildRawFrame(0x0210, new byte[] { 0x00, 0x00 });

            _output.WriteLine($"期望: {ToHexString(expected)}");
            _output.WriteLine($"实际: {ToHexString(actual)}");

            Assert.Equal(expected.Length, actual.Length);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void BuildRawFrame_ReadParameter_ShouldMatchDocumentSample()
        {
            // 文档指令 7：读取参数 #1
            // 发送：55 03 23 01 00 36 22 11 01 03 07 00 00 DB FF FF
            var expected = new byte[] { 0x55, 0x03, 0x23, 0x01, 0x00, 0x36, 0x22, 0x11, 0x01, 0x03, 0x07, 0x00, 0x00, 0xDB, 0xFF, 0xFF };

            var strategy = CreateConverterStrategy(0x07);
            var actual = strategy.BuildRawFrame(0x0301);

            _output.WriteLine($"期望: {ToHexString(expected)}");
            _output.WriteLine($"实际: {ToHexString(actual)}");

            Assert.Equal(expected.Length, actual.Length);
            Assert.Equal(expected, actual);
        }

        // ═══════════════════════════════════════════════════════════
        // 测试用例：验证帧结构正确性
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void BuildRawFrame_NoData_ShouldHaveCorrectCrc16()
        {
            // 无数据时，CRC16 应为 0xFFFF
            var strategy = CreateConverterStrategy(0x01);
            var frame = strategy.BuildRawFrame(0x0106);

            // CRC16 是最后两个字节
            ushort crc16 = (ushort)(frame[frame.Length - 2] | (frame[frame.Length - 1] << 8));
            Assert.Equal(0xFFFF, crc16);
        }

        [Fact]
        public void BuildRawFrame_WithData_ShouldHaveCorrectCrc16()
        {
            // 有数据时，CRC16 应该正确计算
            var strategy = CreateConverterStrategy(0x01);
            var data = new byte[] { 0x01 };
            var frame = strategy.BuildRawFrame(0x0300, data);

            // 提取数据部分（从索引 14 开始，长度为数据长度）
            ushort dataLen = (ushort)(frame[11] | (frame[12] << 8));
            Assert.Equal(1, dataLen);

            // 计算期望的 CRC16
            ushort expectedCrc16 = CalculateCrc16(frame, 14, dataLen);
            ushort actualCrc16 = (ushort)(frame[frame.Length - 2] | (frame[frame.Length - 1] << 8));

            Assert.Equal(expectedCrc16, actualCrc16);
        }

        [Fact]
        public void BuildRawFrame_SequenceNumber_ShouldIncrement()
        {
            var strategy = CreateConverterStrategy(0x01);

            var frame1 = strategy.BuildRawFrame(0x0106);
            var frame2 = strategy.BuildRawFrame(0x0108);
            var frame3 = strategy.BuildRawFrame(0x0300, new byte[] { 0x01 });

            Assert.Equal(0x01, frame1[10]);
            Assert.Equal(0x02, frame2[10]);
            Assert.Equal(0x03, frame3[10]);
        }

        // ═══════════════════════════════════════════════════════════
        // 测试用例：验证响应解析
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void TryParseFrame_ValidResponse_ShouldParseCorrectly()
        {
            // 文档指令 2 的响应：
            // 55 02 36 22 11 23 01 00 06 01 02 14 00 AB 00 41 32 30 2D 39 38 20 56 30 30 2E 30 30 2E 30 30 2E 30 37 AA B7
            var response = new byte[] {
                0x55, 0x02, 0x36, 0x22, 0x11, 0x23, 0x01, 0x00,
                0x06, 0x01, 0x02, 0x14, 0x00, 0xAB,
                0x00, 0x41, 0x32, 0x30, 0x2D, 0x39, 0x38, 0x20, 0x56, 0x30, 0x30, 0x2E, 0x30, 0x30, 0x2E, 0x30, 0x30, 0x2E, 0x30, 0x37,
                0xAA, 0xB7
            };

            var strategy = CreateConverterStrategy();
            bool parsed = strategy.TryParseRawFrame(response, out int frameLength, out byte[] frameData);

            Assert.True(parsed);
            Assert.Equal(response.Length, frameLength);

            // 数据应包含错误码 + 版本字符串（不含 CRC16）
            // 20 字节数据 = 1 字节错误码 + 19 字节版本字符串
            Assert.Equal(20, frameData.Length);

            // 错误码 = 0x00
            Assert.Equal(0x00, frameData[0]);

            // 版本字符串 = "A20-98 V00.00.00.07"
            var version = System.Text.Encoding.ASCII.GetString(frameData, 1, frameData.Length - 1);
            Assert.Equal("A20-98 V00.00.00.07", version);
        }

        [Fact]
        public void TryParseFrame_ScanDeviceResponse_ShouldParseCorrectly()
        {
            // 文档指令 1 的响应：
            // 55 02 36 22 11 23 01 00 00 03 01 01 00 67 00 F0 E1
            var response = new byte[] {
                0x55, 0x02, 0x36, 0x22, 0x11, 0x23, 0x01, 0x00,
                0x00, 0x03, 0x01, 0x01, 0x00, 0x67,
                0x00,
                0xF0, 0xE1
            };

            var strategy = CreateConverterStrategy();
            bool parsed = strategy.TryParseRawFrame(response, out int frameLength, out byte[] frameData);

            Assert.True(parsed);

            // 数据应包含错误码
            Assert.Single(frameData);

            // 错误码 = 0x00
            Assert.Equal(0x00, frameData[0]);
        }

        // ═══════════════════════════════════════════════════════════
        // 测试用例：Modbus RTU 转发指令（端口38）
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void BuildRawFrame_EnableOwi_ShouldMatchDocumentSample()
        {
            // 文档指令 13：启用 OWI 通信模式
            // 发送：55 03 26 01 00 36 22 11 00 04 0D 0B 00 A8 01 29 80 00 00 01 02 00 01 25 26 53 24
            var expected = new byte[] {
                0x55, 0x03, 0x26, 0x01, 0x00, 0x36, 0x22, 0x11,
                0x00, 0x04, 0x0D, 0x0B, 0x00, 0xA8,
                0x01, 0x29, 0x80, 0x00, 0x00, 0x01, 0x02, 0x00, 0x01, 0x25, 0x26,
                0x53, 0x24
            };

            // 构建 Modbus RTU 帧：F41 写寄存器 0x8000 = 0x0001
            // 从机地址: 0x01, 功能码: 0x29, 寄存器: 0x8000, 数量: 0x0001, 数据: 0x0001
            var modbusData = new byte[] { 0x00, 0x01 };
            var modbusFrame = BuildModbusRtuFrame(0x01, 0x29, 0x8000, 0x0001, modbusData);

            // 使用 Modbus 转发策略构建 CPPI V3 帧
            var strategy = CreateModbusForwardStrategy(0x0D);
            var actual = strategy.BuildRawFrame(0x0400, modbusFrame);

            _output.WriteLine($"期望: {ToHexString(expected)}");
            _output.WriteLine($"实际: {ToHexString(actual)}");
            _output.WriteLine($"Modbus RTU: {ToHexString(modbusFrame)}");

            Assert.Equal(expected.Length, actual.Length);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TryParseFrame_EnableOwiResponse_ShouldParseCorrectly()
        {
            // 文档指令 13 的响应：
            // 55 02 36 22 11 26 01 00 00 04 0D 09 00 08 00 01 29 80 00 00 01 B4 0C CE EA
            var response = new byte[] {
                0x55, 0x02, 0x36, 0x22, 0x11, 0x26, 0x01, 0x00,
                0x00, 0x04, 0x0D, 0x09, 0x00, 0x08,
                0x00, 0x01, 0x29, 0x80, 0x00, 0x00, 0x01, 0xB4, 0x0C,
                0xCE, 0xEA
            };

            var strategy = CreateModbusForwardStrategy();
            bool parsed = strategy.TryParseRawFrame(response, out int frameLength, out byte[] frameData);

            Assert.True(parsed);
            Assert.Equal(response.Length, frameLength);

            // 数据应包含：CPPI 错误码(1B) + Modbus 响应(8B) = 9 字节
            Assert.Equal(9, frameData.Length);

            // CPPI 错误码 = 0x00
            Assert.Equal(0x00, frameData[0]);

            // Modbus 响应：01 29 80 00 00 01 B4 0C
            Assert.Equal(0x01, frameData[1]); // 从机地址
            Assert.Equal(0x29, frameData[2]); // 功能码 F41
            Assert.Equal(0x80, frameData[3]); // 寄存器地址高字节
            Assert.Equal(0x00, frameData[4]); // 寄存器地址低字节
            Assert.Equal(0x00, frameData[5]); // 寄存器数量高字节
            Assert.Equal(0x01, frameData[6]); // 寄存器数量低字节
            Assert.Equal(0xB4, frameData[7]); // Modbus CRC 低字节
            Assert.Equal(0x0C, frameData[8]); // Modbus CRC 高字节
        }

        /// <summary>
        /// 构建完整的 Modbus RTU 帧（含 CRC16）
        /// </summary>
        private static byte[] BuildModbusRtuFrame(byte slaveAddress, byte functionCode, ushort registerAddress, ushort registerCount, byte[] data)
        {
            int dataLen = data?.Length ?? 0;
            int frameLen = 7 + dataLen + 2;
            var frame = new byte[frameLen];

            frame[0] = slaveAddress;
            frame[1] = functionCode;
            frame[2] = (byte)(registerAddress >> 8);
            frame[3] = (byte)(registerAddress & 0xFF);
            frame[4] = (byte)(registerCount >> 8);
            frame[5] = (byte)(registerCount & 0xFF);
            frame[6] = (byte)dataLen;

            if (data != null && dataLen > 0)
            {
                Array.Copy(data, 0, frame, 7, dataLen);
            }

            // 计算 Modbus CRC16
            ushort crc = CalculateModbusCrc16(frame, frameLen - 2);
            frame[frameLen - 2] = (byte)(crc & 0xFF);
            frame[frameLen - 1] = (byte)((crc >> 8) & 0xFF);

            return frame;
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
