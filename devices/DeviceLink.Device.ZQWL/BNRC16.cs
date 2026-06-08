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
    /// 智嵌物联 BNRC16 网络继电器控制器（16路）
    /// 
    /// 通信参数：
    ///   - 波特率：115200
    ///   - 数据位：8
    ///   - 停止位：1
    ///   - 校验：None
    /// 
    /// 协议格式：[48 3A] [addr] [func] [data x8] [checksum] [45 44]
    /// 
    /// 数据区编码（BNRC16）：
    ///   每字节表示 2 路：低 4 位 = 奇数路，高 4 位 = 偶数路
    ///   data[0] = 路1(低4位) + 路2(高4位)
    ///   data[1] = 路3(低4位) + 路4(高4位)
    ///   ...
    ///   data[7] = 路15(低4位) + 路16(高4位)
    /// 
    /// 使用示例：
    ///   var relay = new BNRC16("COM3");
    ///   await relay.OpenAsync();
    ///   await relay.SetOutputAsync(1, true);   // 打开第 1 路
    ///   var state = await relay.GetInputAsync(1); // 读取第 1 路输入
    /// </summary>
    public class BNRC16 : DeviceBase.DeviceBase
    {
        private readonly ZqwlCodec _codec;

        #region 构造函数

        /// <summary>
        /// 构造函数（串口通讯，完整参数）
        /// </summary>
        public BNRC16(string serialPortName, int baudRate = 115200, int dataBits = 8,
            StopBits stopBits = StopBits.One, Parity parity = Parity.None, byte address = 1)
            : base(serialPortName, baudRate, dataBits, stopBits, parity,
                  new ZqwlCodec(address))
        {
            _codec = (ZqwlCodec)Codec;
        }

        /// <summary>
        /// 构造函数（串口通讯，默认配置）
        /// </summary>
        public BNRC16(string serialPortName, byte address = 1)
            : base(serialPortName, new ZqwlCodec(address))
        {
            _codec = (ZqwlCodec)Codec;
        }

        /// <summary>
        /// 构造函数（TCP 通讯）
        /// </summary>
        public BNRC16(IPAddress ipAddress, int port, byte address = 1)
            : base(ipAddress, port, new ZqwlCodec(address))
        {
            _codec = (ZqwlCodec)Codec;
        }

        /// <summary>
        /// 构造函数（测试用，直接注入会话层）
        /// </summary>
        public BNRC16(ISession session, byte address = 1)
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
            Name = "BNRC16";
        }

        #endregion

        // ═══════════════════════════════════════════════════════════
        // 输入读取
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 读取指定路的输入状态（BNRC16 使用 nibble 编码）
        /// </summary>
        /// <param name="channel">通道号（1~16）</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>true=开，false=关</returns>
        public async Task<bool> GetInputAsync(int channel, CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("GetInput"),
                raw => ExtractNibbleInput(raw, channel),
                ct);
        }

        // ═══════════════════════════════════════════════════════════
        // 输出控制
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 设置指定路的输出状态
        /// </summary>
        /// <param name="channel">通道号（1~16）</param>
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
        /// <param name="channel">通道号（1~16）</param>
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
                        // 响应格式：data[4]=channel, data[5]=state
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
            // BNRC16 关闭全部：8 字节全填 0x00
            await SendNonQueryAsync(
                Command.Write("CloseAll", "00", "00", "00", "00", "00", "00", "00", "00"),
                ct);
        }

        /// <summary>
        /// 打开全部输出
        /// </summary>
        public async Task OpenAllAsync(CancellationToken ct = default)
        {
            // BNRC16 打开全部：8 字节全填 0x11（每字节高4位=1, 低4位=1，表示两路都开）
            await SendNonQueryAsync(
                Command.Write("OpenAll", "11", "11", "11", "11", "11", "11", "11", "11"),
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
        /// 读取全部 16 路输出状态
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
                        for (int i = 0; i < 16; i++)
                        {
                            int byteIndex = 4 + i / 2;
                            if (i % 2 == 1)
                                result.Add(raw[byteIndex] >= 16); // 高4位
                            else
                                result.Add(raw[byteIndex] % 16 >= 1); // 低4位
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
        /// <param name="channel">通道号（1-based，内部转换为 0-based）</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>模拟量原始值（单位 mA）</returns>
        public async Task<int> GetAnalogInputAsync(int channel, CancellationToken ct = default)
        {
            // 实际地址从 0 开始
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
        /// 从 nibble 编码的响应中提取输入状态（BNRC16 专用）
        /// 每字节表示 2 路：低4位 = 奇数路，高4位 = 偶数路
        /// </summary>
        private static bool ExtractNibbleInput(byte[] raw, int channel)
        {
            if (raw.Length < 12)
                return false;

            // raw[0]=addr, raw[1]=func, raw[2..9]=data (8 bytes)
            int dataIndex = (int)Math.Ceiling(channel / 2.0);
            byte dataByte = raw[1 + dataIndex];

            if (channel % 2 == 1)
            {
                // 奇数路：取低 4 位
                return (dataByte & 0x0F) >= 1;
            }
            else
            {
                // 偶数路：取高 4 位
                return (dataByte >> 4) >= 1;
            }
        }
    }
}
