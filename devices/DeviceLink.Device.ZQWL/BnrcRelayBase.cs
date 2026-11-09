using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DeviceLink.Protocol;
using DeviceLink.Session;

namespace DeviceLink.Device.ZQWL;

/// <summary>
/// 智嵌物联 BNRC 系列网络继电器控制器的公共基类。
///
/// 三个型号（BNRC8 / BNRC16 / BNRC32）的通信协议完全一致，
/// 差别只在<b>数据区编码</b>：
/// · BNRC8　：1 字节 = 1 路，数据从 raw[1] 开始；
/// · BNRC16 ：1 字节 = 2 路（nibble），数据从 raw[2] 开始；
/// · BNRC32 ：1 字节 = 4 路（quad），数据从 raw[2] 开始。
///
/// 协议格式：[0x48 0x3A] [addr] [func] [data x8] [checksum] [0x45 0x44]
///
/// 约定：基类只承载"三个型号一模一样"的命令流与状态机，
/// 编码差异通过 <see cref="DecodeInput"/> / <see cref="DecodeOutput"/> /
/// <see cref="DecodeAllStatuses"/> / <see cref="OpenAllData"/> 暴露给子类。
/// 子类实现必须与历史行为逐位一致 —— 这几处索引本身就是协议的一部分。
/// </summary>
public abstract class BnrcRelayBase : DeviceBase.DeviceBase
{
    /// <summary>关闭全部输出时的数据区：8 字节全 0。</summary>
    private static readonly string[] ClosedAllData =
        { "00", "00", "00", "00", "00", "00", "00", "00" };

    /// <summary>ZQWL 私有协议编解码器。</summary>
    protected ZqwlCodec Zqwl { get; }

    /// <summary>初始化串口通信（完整参数）。</summary>
    protected BnrcRelayBase(string serialPortName, int baudRate, int dataBits,
        StopBits stopBits, Parity parity, byte address)
        : base(serialPortName, baudRate, dataBits, stopBits, parity, new ZqwlCodec(address))
        => Zqwl = (ZqwlCodec)Codec;

    /// <summary>初始化串口通信（默认配置）。</summary>
    protected BnrcRelayBase(string serialPortName, byte address)
        : base(serialPortName, new ZqwlCodec(address))
        => Zqwl = (ZqwlCodec)Codec;

    /// <summary>初始化 TCP 通信。</summary>
    protected BnrcRelayBase(IPAddress ipAddress, int port, byte address)
        : base(ipAddress, port, new ZqwlCodec(address))
        => Zqwl = (ZqwlCodec)Codec;

    /// <summary>初始化（测试用，直接注入会话层）。</summary>
    protected BnrcRelayBase(ISession session, byte address)
        : base(session, new ZqwlCodec(address))
        => Zqwl = (ZqwlCodec)Codec;

    /// <summary>型号名（同时作为设备名），如 "BNRC16"。</summary>
    protected abstract string ModelName { get; }

    /// <summary>版本号中用于识别型号的片段，默认同 <see cref="ModelName"/>。</summary>
    protected virtual string VersionCode => ModelName;

    /// <summary>打开全部输出时的数据区（型号相关编码）。</summary>
    protected abstract string[] OpenAllData { get; }

    /// <summary>解码某一路的输入状态。</summary>
    protected abstract bool DecodeInput(byte[] raw, int channel);

    /// <summary>解码某一路的输出状态。</summary>
    protected abstract bool DecodeOutput(byte[] raw, int channel);

    /// <summary>解码全部通道的输出状态。</summary>
    protected abstract List<bool> DecodeAllStatuses(byte[] raw);

    /// <summary>配置构造函数默认信息。</summary>
    protected override void ConstructDefaultInfo()
    {
        base.ConstructDefaultInfo();
        Name = ModelName;
    }

    // ───────────────────────── 输入读取 ─────────────────────────

    /// <summary>读取指定路的输入状态（true=开，false=关）。</summary>
    /// <param name="channel">通道号（1-based）</param>
    /// <param name="ct">取消令牌</param>
    public Task<bool> GetInputAsync(int channel, CancellationToken ct = default)
        => SendForResultAsync(Command.Read("GetInput"), raw => DecodeInput(raw, channel), ct);

    // ───────────────────────── 输出控制 ─────────────────────────

    /// <summary>设置指定路的输出状态（true=开，false=关）。</summary>
    /// <param name="channel">通道号（1-based）</param>
    /// <param name="state">目标状态</param>
    /// <param name="ct">取消令牌</param>
    public Task SetOutputAsync(int channel, bool state, CancellationToken ct = default)
        => SendNonQueryAsync(
            Command.Write($"SetOutput.{channel}.{(state ? 1 : 0)}"), ct);

    /// <summary>读取指定路的输出状态（true=开，false=关）。</summary>
    /// <param name="channel">通道号（1-based）</param>
    /// <param name="ct">取消令牌</param>
    public Task<bool> GetOutputAsync(int channel, CancellationToken ct = default)
        => SendForResultAsync(Command.Read($"GetOutput.{channel}"), raw => DecodeOutput(raw, channel), ct);

    /// <summary>关闭全部输出。</summary>
    /// <param name="ct">取消令牌</param>
    public Task CloseAllAsync(CancellationToken ct = default)
        => SendNonQueryAsync(Command.Write("CloseAll", ClosedAllData), ct);

    /// <summary>打开全部输出。</summary>
    /// <param name="ct">取消令牌</param>
    public Task OpenAllAsync(CancellationToken ct = default)
        => SendNonQueryAsync(Command.Write("OpenAll", OpenAllData), ct);

    // ───────────────────────── 版本与状态 ─────────────────────────

    /// <summary>读取设备版本号（如 "1BNRC16"、"IO-04-00"）。</summary>
    /// <param name="ct">取消令牌</param>
    public Task<string> GetVersionAsync(CancellationToken ct = default)
        => SendForResultAsync(Command.Read("GetVersion"), raw => Zqwl.ExtractVersion(raw), ct);

    /// <summary>
    /// 检查设备是否存在且型号匹配（通过读取版本号判断）。
    /// 任何异常都视为"不存在"，便于产线扫描时逐个探测。
    /// </summary>
    /// <param name="ct">取消令牌</param>
    public async Task<bool> IsExistAsync(CancellationToken ct = default)
    {
        try
        {
            var version = await GetVersionAsync(ct).ConfigureAwait(false);
            return !string.IsNullOrEmpty(version) && version.Contains(VersionCode);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>读取全部通道的输出状态。</summary>
    /// <param name="ct">取消令牌</param>
    public Task<List<bool>> GetAllStatusesAsync(CancellationToken ct = default)
        => SendForResultAsync(Command.Read("GetAllStatuses"), DecodeAllStatuses, ct);
}

/// <summary>
/// 带模拟量输入的 BNRC 型号基类（BNRC16 / BNRC32）。
/// BNRC8 没有模拟量通道，故不在此列。
/// </summary>
public abstract class BnrcAnalogRelayBase : BnrcRelayBase
{
    /// <summary>初始化串口通信（完整参数）。</summary>
    protected BnrcAnalogRelayBase(string serialPortName, int baudRate, int dataBits,
        StopBits stopBits, Parity parity, byte address)
        : base(serialPortName, baudRate, dataBits, stopBits, parity, address)
    { }

    /// <summary>初始化串口通信（默认配置）。</summary>
    protected BnrcAnalogRelayBase(string serialPortName, byte address)
        : base(serialPortName, address)
    { }

    /// <summary>初始化 TCP 通信。</summary>
    protected BnrcAnalogRelayBase(IPAddress ipAddress, int port, byte address)
        : base(ipAddress, port, address)
    { }

    /// <summary>初始化（测试用，直接注入会话层）。</summary>
    protected BnrcAnalogRelayBase(ISession session, byte address)
        : base(session, address)
    { }

    /// <summary>读取模拟量输入原始值（20mA 量程）。</summary>
    /// <param name="channel">通道号（1-based，内部转换为协议使用的 0-based）</param>
    /// <param name="ct">取消令牌</param>
    public Task<int> GetAnalogInputAsync(int channel, CancellationToken ct = default)
    {
        // 协议中的模拟量通道地址从 0 开始
        int channelIndex = channel <= 1 ? 0 : channel - 1;
        return SendForResultAsync(
            Command.Read($"GetAnalogInput.{channelIndex}"),
            raw => Zqwl.ExtractAnalogValue(raw),
            ct);
    }
}
