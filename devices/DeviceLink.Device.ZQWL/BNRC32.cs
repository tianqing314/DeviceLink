using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DeviceLink.DeviceBase;
using DeviceLink.Protocol;
using DeviceLink.Session;

namespace DeviceLink.Device.ZQWL
{
    /// <summary>
    /// 智嵌物联 BNRC32 网络继电器控制器（32路）
    /// 
    /// 通信参数：
    ///   - 波特率：115200
    ///   - 数据位：8
    ///   - 停止位：1
    ///   - 校验：None
    /// 
    /// 协议格式：[48 3A] [addr] [func] [data x8] [checksum] [45 44]
    /// 
    /// 数据区编码（BNRC32）：
    ///   每字节表示 4 路，由高位到低位分别为第 4,3,2,1 排序位
    ///   data[0] = 路1~4（bit3=路4, bit2=路3, bit1=路2, bit0=路1）
    ///   data[1] = 路5~8
    ///   ...
    ///   data[7] = 路29~32
    /// 
    /// 使用示例：
    ///   var relay = new BNRC32("COM3");
    ///   await relay.OpenAsync();
    ///   await relay.SetOutputAsync(1, true);   // 打开第 1 路
    ///   var state = await relay.GetInputAsync(1); // 读取第 1 路输入
    /// </summary>
    public class BNRC32 : DeviceBase.DeviceBase
    {
        private readonly ZqwlCodec _codec;

        #region 构造函数

        /// <summary>
        /// 构造函数（串口通讯，完整参数）
        /// </summary>
        public BNRC32(string serialPortName, int baudRate = 115200, int dataBits = 8,
            StopBits stopBits = StopBits.One, Parity parity = Parity.None, byte address = 1)
            : base(serialPortName, baudRate, dataBits, stopBits, parity,
                  new ZqwlCodec(address))
        {
            _codec = (ZqwlCodec)Codec;
        }

        /// <summary>
        /// 构造函数（串口通讯，默认配置）
        /// </summary>
        public BNRC32(string serialPortName, byte address = 1)
            : base(serialPortName, new ZqwlCodec(address))
        {
            _codec = (ZqwlCodec)Codec;
        }

        /// <summary>
        /// 构造函数（TCP 通讯）
        /// </summary>
        public BNRC32(IPAddress ipAddress, int port, byte address = 1)
            : base(ipAddress, port, new ZqwlCodec(address))
        {
            _codec = (ZqwlCodec)Codec;
        }

        /// <summary>
        /// 构造函数（测试用，直接注入会话层）
        /// </summary>
        public BNRC32(ISession session, byte address = 1)
            : base(session, new ZqwlCodec(address))
        {
            _codec = (ZqwlCodec)Codec;
        }

        /// <summary>
        /// 配置构造函数默认信息
        /// </summary>
        protected override void ConstructDefaultInfo()
        {
            base.ConstructDefaultInfo();
            Name = "BNRC32";
        }

        #endregion

        // ═══════════════════════════════════════════════════════════
        // 输入读取
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 读取指定路的输入状态（BNRC32 使用 4-bit 编码）
        /// </summary>
        /// <param name="channel">通道号（1~32）</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>true=开，false=关</returns>
        public async Task<bool> GetInputAsync(int channel, CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("GetInput"),
                raw => ExtractQuadInput(raw, channel),
                ct);
        }

        // ═══════════════════════════════════════════════════════════
        // 输出控制
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 设置指定路的输出状态
        /// </summary>
        /// <param name="channel">通道号（1~32）</param>
        /// <param name="state">true=开，false=关</param>
        /// <param name="ct">取消令牌</param>
        public async Task SetOutputAsync(int channel, bool state, CancellationToken ct = default)
        {
            await SendNonQueryAsync(
                Command.Write($"SetOutput.{channel}.{(state ? 1 : 0)}"),
                ct);
        }

        /// <summary>
        /// 读取指定路的输出状态
        /// </summary>
        /// <param name="channel">通道号（1~32）</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>true=开，false=关</returns>
        public async Task<bool> GetOutputAsync(int channel, CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read($"GetOutput.{channel}"),
                raw =>
                {
                    if (raw.Length >= 10)
                    {
                        return raw[5] == 0x01;
                    }
                    return false;
                },
                ct);
        }

        /// <summary>
        /// 关闭全部输出
        /// </summary>
        public async Task CloseAllAsync(CancellationToken ct = default)
        {
            // BNRC32 关闭全部：8 字节全填 0x00
            await SendNonQueryAsync(
                Command.Write("CloseAll", "00", "00", "00", "00", "00", "00", "00", "00"),
                ct);
        }

        /// <summary>
        /// 打开全部输出
        /// </summary>
        public async Task OpenAllAsync(CancellationToken ct = default)
        {
            // BNRC32 打开全部：8 字节全填 0x55（每字节 4 位都为 1，表示 4 路全开）
            await SendNonQueryAsync(
                Command.Write("OpenAll", "55", "55", "55", "55", "55", "55", "55", "55"),
                ct);
        }

        // ═══════════════════════════════════════════════════════════
        // 版本与状态
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 读取设备版本号
        /// </summary>
        public async Task<string> GetVersionAsync(CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("GetVersion"),
                raw => _codec.ExtractVersion(raw),
                ct);
        }

        /// <summary>
        /// 检查设备是否存在
        /// </summary>
        public async Task<bool> IsExistAsync(CancellationToken ct = default)
        {
            try
            {
                var version = await GetVersionAsync(ct);
                return !string.IsNullOrEmpty(version) &&
                       (version.Contains("IO") || version.Contains("BN"));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 读取全部 32 路输出状态
        /// </summary>
        public async Task<List<bool>> GetAllStatusesAsync(CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("GetAllStatuses"),
                raw =>
                {
                    var result = new List<bool>();
                    if (raw.Length >= 12)
                    {
                        for (int i = 0; i < 32; i++)
                        {
                            int byteIndex = 4 + i / 4;
                            int bitIndex = i % 4;
                            byte dataByte = raw[byteIndex];
                            string hex = dataByte.ToString("X2");
                            if (hex.Length == 1) hex = "0" + hex;

                            bool isOn;
                            switch (bitIndex)
                            {
                                case 0:
                                    isOn = hex[1] == '1' || hex[1] == '5';
                                    break;
                                case 1:
                                    isOn = hex[1] == '4' || hex[1] == '5';
                                    break;
                                case 2:
                                    isOn = hex[0] == '1' || hex[0] == '5';
                                    break;
                                case 3:
                                    isOn = hex[0] == '4' || hex[0] == '5';
                                    break;
                                default:
                                    isOn = false;
                                    break;
                            }
                            result.Add(isOn);
                        }
                    }
                    return result;
                },
                ct);
        }

        // ═══════════════════════════════════════════════════════════
        // 模拟量读取
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 读取模拟量输入值（20mA 量程）
        /// </summary>
        /// <param name="channel">通道号（1-based）</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>模拟量原始值</returns>
        public async Task<int> GetAnalogInputAsync(int channel, CancellationToken ct = default)
        {
            int channelIndex = channel <= 1 ? 0 : channel - 1;
            return await SendForResultAsync(
                Command.Read($"GetAnalogInput.{channelIndex}"),
                raw => _codec.ExtractAnalogValue(raw),
                ct);
        }

        // ═══════════════════════════════════════════════════════════
        // 私有辅助方法
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 从 4-bit 编码的响应中提取输入状态（BNRC32 专用）
        /// 每字节表示 4 路：由高位到低位分别为第 4,3,2,1 排序位
        /// </summary>
        private static bool ExtractQuadInput(byte[] raw, int channel)
        {
            if (raw.Length < 12)
                return false;

            int dataIndex = (int)Math.Floor(channel / 4.0);
            byte dataByte = raw[4 + dataIndex];
            string hex = dataByte.ToString("X2");
            if (hex.Length == 1) hex = "0" + hex;

            switch (channel % 4)
            {
                case 0:
                    return GetBitFromHex(hex[1], HexCharPart.Lower);
                case 1:
                    return GetBitFromHex(hex[1], HexCharPart.Upper);
                case 2:
                    return GetBitFromHex(hex[0], HexCharPart.Lower);
                case 3:
                    return GetBitFromHex(hex[0], HexCharPart.Upper);
                default:
                    return false;
            }
        }

        private enum HexCharPart { Lower, Upper }

        private static bool GetBitFromHex(char hexChar, HexCharPart part)
        {
            int val = 0;
            if (hexChar >= '0' && hexChar <= '9')
                val = hexChar - '0';
            else if (hexChar >= 'A' && hexChar <= 'F')
                val = hexChar - 'A' + 10;
            else if (hexChar >= 'a' && hexChar <= 'f')
                val = hexChar - 'a' + 10;

            switch (part)
            {
                case HexCharPart.Lower:
                    return val % 4 >= 1;
                case HexCharPart.Upper:
                    return val / 4 >= 1;
                default:
                    return false;
            }
        }
    }
}
