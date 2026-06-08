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
    /// 智嵌物联 BNRC8 网络继电器控制器（8路）
    /// 
    /// 通信参数：
    ///   - 波特率：115200
    ///   - 数据位：8
    ///   - 停止位：1
    ///   - 校验：None
    /// 
    /// 协议格式：[48 3A] [addr] [func] [data x8] [checksum] [45 44]
    /// 
    /// 数据区编码（BNRC8）：
    ///   每字节对应 1 路（1 字节 = 1 路），0x00=关，0x01=开
    /// 
    /// 使用示例：
    ///   var relay = new BNRC8("COM3");
    ///   await relay.OpenAsync();
    ///   await relay.SetOutputAsync(1, true);   // 打开第 1 路
    ///   var state = await relay.GetInputAsync(1); // 读取第 1 路输入
    /// </summary>
    public class BNRC8 : DeviceBase.DeviceBase
    {
        private readonly ZqwlCodec _codec;

        #region 构造函数

        /// <summary>
        /// 构造函数（串口通讯，完整参数）
        /// </summary>
        /// <param name="serialPortName">串口号（如 COM3）</param>
        /// <param name="baudRate">波特率（默认 115200）</param>
        /// <param name="dataBits">数据位（默认 8）</param>
        /// <param name="stopBits">停止位（默认 1）</param>
        /// <param name="parity">校验位（默认 None）</param>
        /// <param name="address">设备地址（默认 1）</param>
        public BNRC8(string serialPortName, int baudRate = 115200, int dataBits = 8,
            StopBits stopBits = StopBits.One, Parity parity = Parity.None, byte address = 1)
            : base(serialPortName, baudRate, dataBits, stopBits, parity,
                  new ZqwlCodec(address))
        {
            _codec = (ZqwlCodec)Codec;
        }

        /// <summary>
        /// 构造函数（串口通讯，默认配置：115200,8,1,None）
        /// </summary>
        /// <param name="serialPortName">串口号</param>
        /// <param name="address">设备地址（默认 1）</param>
        public BNRC8(string serialPortName, byte address = 1)
            : base(serialPortName, new ZqwlCodec(address))
        {
            _codec = (ZqwlCodec)Codec;
        }

        /// <summary>
        /// 构造函数（TCP 通讯）
        /// </summary>
        /// <param name="ipAddress">IP 地址</param>
        /// <param name="port">端口号</param>
        /// <param name="address">设备地址（默认 1）</param>
        public BNRC8(IPAddress ipAddress, int port, byte address = 1)
            : base(ipAddress, port, new ZqwlCodec(address))
        {
            _codec = (ZqwlCodec)Codec;
        }

        /// <summary>
        /// 构造函数（测试用，直接注入会话层）
        /// </summary>
        /// <param name="session">会话层实例</param>
        /// <param name="address">设备地址（默认 1）</param>
        public BNRC8(ISession session, byte address = 1)
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
            Name = "BNRC8";
        }

        #endregion

        // ═══════════════════════════════════════════════════════════
        // 输入读取
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 读取指定路的输入状态
        /// </summary>
        /// <param name="channel">通道号（1~8）</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>true=开，false=关</returns>
        public async Task<bool> GetInputAsync(int channel, CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read($"GetInput"),
                raw => _codec.ExtractInputState(raw, channel),
                ct);
        }

        // ═══════════════════════════════════════════════════════════
        // 输出控制
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 设置指定路的输出状态
        /// </summary>
        /// <param name="channel">通道号（1~8）</param>
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
        /// <param name="channel">通道号（1~8）</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>true=开，false=关</returns>
        public async Task<bool> GetOutputAsync(int channel, CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read($"GetOutput.{channel}"),
                raw =>
                {
                    // 响应中 data[4+channel] 表示该路状态
                    if (raw.Length >= 10)
                        return raw[1 + channel] == 0x01;
                    return false;
                },
                ct);
        }

        /// <summary>
        /// 关闭全部输出
        /// </summary>
        /// <param name="ct">取消令牌</param>
        public async Task CloseAllAsync(CancellationToken ct = default)
        {
            // BNRC8 关闭全部：8 字节全填 0x00
            await SendNonQueryAsync(
                Command.Write("CloseAll", "00", "00", "00", "00", "00", "00", "00", "00"),
                ct);
        }

        /// <summary>
        /// 打开全部输出
        /// </summary>
        /// <param name="ct">取消令牌</param>
        public async Task OpenAllAsync(CancellationToken ct = default)
        {
            // BNRC8 打开全部：8 字节全填 0x01
            await SendNonQueryAsync(
                Command.Write("OpenAll", "01", "01", "01", "01", "01", "01", "01", "01"),
                ct);
        }

        // ═══════════════════════════════════════════════════════════
        // 版本与状态
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 读取设备版本号
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>版本号字符串，如 "IO-04-00"</returns>
        public async Task<string> GetVersionAsync(CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("GetVersion"),
                raw => _codec.ExtractVersion(raw),
                ct);
        }

        /// <summary>
        /// 检查设备是否存在（通过读取版本号判断）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>true 表示设备存在</returns>
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
        /// 读取全部 8 路输出状态
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>8 路状态列表（true=开，false=关）</returns>
        public async Task<List<bool>> GetAllStatusesAsync(CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("GetAllStatuses"),
                raw =>
                {
                    var result = new List<bool>();
                    if (raw.Length >= 10)
                    {
                        for (int i = 1; i <= 8; i++)
                        {
                            result.Add(raw[1 + i] == 0x01);
                        }
                    }
                    return result;
                },
                ct);
        }
    }
}
