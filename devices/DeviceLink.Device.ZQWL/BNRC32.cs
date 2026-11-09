using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Net;
using DeviceLink.Session;

namespace DeviceLink.Device.ZQWL;

/// <summary>
/// 智嵌物联 BNRC32 网络继电器控制器（32 路）。
///
/// 通信参数：115200, 8, 1, None
/// 协议格式：[48 3A] [addr] [func] [data x8] [checksum] [45 44]
///
/// 数据区编码（BNRC32）：<b>每字节 4 路（quad）</b>，
/// 数据从 raw[2] 开始，打开全部时每字节为 0x55。
/// </summary>
public class BNRC32 : BnrcAnalogRelayBase
{
    private static readonly string[] OpenAll =
        { "55", "55", "55", "55", "55", "55", "55", "55" };

    /// <summary>初始化串口通信（完整参数）。</summary>
    /// <param name="serialPortName">串口号（如 COM3）</param>
    /// <param name="baudRate">波特率（默认 115200）</param>
    /// <param name="dataBits">数据位（默认 8）</param>
    /// <param name="stopBits">停止位（默认 1）</param>
    /// <param name="parity">校验位（默认 None）</param>
    /// <param name="address">设备地址（默认 1）</param>
    public BNRC32(string serialPortName, int baudRate = 115200, int dataBits = 8,
        StopBits stopBits = StopBits.One, Parity parity = Parity.None, byte address = 1)
        : base(serialPortName, baudRate, dataBits, stopBits, parity, address)
    { }

    /// <summary>初始化串口通信（默认配置 115200,8,1,None）。</summary>
    /// <param name="serialPortName">串口号</param>
    /// <param name="address">设备地址（默认 1）</param>
    public BNRC32(string serialPortName, byte address = 1)
        : base(serialPortName, address)
    { }

    /// <summary>初始化 TCP 通信。</summary>
    /// <param name="ipAddress">IP 地址</param>
    /// <param name="port">端口号</param>
    /// <param name="address">设备地址（默认 1）</param>
    public BNRC32(IPAddress ipAddress, int port, byte address = 1)
        : base(ipAddress, port, address)
    { }

    /// <summary>初始化（测试用，直接注入会话层）。</summary>
    /// <param name="session">会话层实例</param>
    /// <param name="address">设备地址（默认 1）</param>
    public BNRC32(ISession session, byte address = 1)
        : base(session, address)
    { }

    /// <inheritdoc/>
    protected override string ModelName => "BNRC32";

    /// <inheritdoc/>
    protected override string[] OpenAllData => OpenAll;

    /// <inheritdoc/>
    protected override bool DecodeInput(byte[] raw, int channel)
    {
        // 响应格式：[addr][func][data x8] = 10 字节，数据从 raw[2] 开始
        if (raw.Length < 10) return false;

        int index = channel - 1;                 // 转成 0-based
        if (index < 0 || index >= 32) return false;

        return DecodeBit(raw[2 + index / 4], index % 4);
    }

    /// <inheritdoc/>
    protected override bool DecodeOutput(byte[] raw, int channel)
        => raw.Length >= 10 && raw[5] == 0x01;

    /// <inheritdoc/>
    protected override List<bool> DecodeAllStatuses(byte[] raw)
    {
        var result = new List<bool>();
        if (raw.Length < 10) return result;

        for (int i = 0; i < 32; i++)
            result.Add(DecodeBit(raw[2 + i / 4], i % 4));

        return result;
    }

    /// <summary>
    /// 取出一个字节里指定位序的状态。
    ///
    /// 位序约定（由 0x55 = 4 路全开、8 字节 × 4 路 = 32 路反推得到）：
    /// bitIndex 0/1 取低半字节（hex[1]）的第 0、2 位，2/3 取高半字节（hex[0]）的第 0、2 位。
    ///
    /// <b>注意</b>：此前 <c>GetInputAsync</c> 用的是另一套映射（channel % 4 决定半字节），
    /// 与 <c>GetAllStatusesAsync</c> 互相矛盾 —— 同一路在单读与批量读里会落到不同 bit 上。
    /// 现已统一为本方法，两者不再可能给出不同答案。
    /// </summary>
    /// <param name="dataByte">数据区的一个字节</param>
    /// <param name="bitIndex">字节内的路序（0~3）</param>
    private static bool DecodeBit(byte dataByte, int bitIndex)
    {
        string hex = dataByte.ToString("X2");

        return bitIndex switch
        {
            0 => hex[1] == '1' || hex[1] == '5',
            1 => hex[1] == '4' || hex[1] == '5',
            2 => hex[0] == '1' || hex[0] == '5',
            3 => hex[0] == '4' || hex[0] == '5',
            _ => false
        };
    }
}
