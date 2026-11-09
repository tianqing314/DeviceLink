using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Net;
using DeviceLink.Session;

namespace DeviceLink.Device.ZQWL;

/// <summary>
/// 智嵌物联 BNRC16 网络继电器控制器（16 路）。
///
/// 通信参数：115200, 8, 1, None
/// 协议格式：[48 3A] [addr] [func] [data x8] [checksum] [45 44]
///
/// 数据区编码（BNRC16）：<b>每字节 2 路（nibble）</b>，
/// 低 4 位 = 奇数路，高 4 位 = 偶数路，数据从 raw[2] 开始。
/// 打开全部时每字节为 0x11。
/// </summary>
public class BNRC16 : BnrcAnalogRelayBase
{
    private static readonly string[] OpenAll =
        { "11", "11", "11", "11", "11", "11", "11", "11" };

    /// <summary>初始化串口通信（完整参数）。</summary>
    /// <param name="serialPortName">串口号（如 COM3）</param>
    /// <param name="baudRate">波特率（默认 115200）</param>
    /// <param name="dataBits">数据位（默认 8）</param>
    /// <param name="stopBits">停止位（默认 1）</param>
    /// <param name="parity">校验位（默认 None）</param>
    /// <param name="address">设备地址（默认 1）</param>
    public BNRC16(string serialPortName, int baudRate = 115200, int dataBits = 8,
        StopBits stopBits = StopBits.One, Parity parity = Parity.None, byte address = 1)
        : base(serialPortName, baudRate, dataBits, stopBits, parity, address)
    { }

    /// <summary>初始化串口通信（默认配置 115200,8,1,None）。</summary>
    /// <param name="serialPortName">串口号</param>
    /// <param name="address">设备地址（默认 1）</param>
    public BNRC16(string serialPortName, byte address = 1)
        : base(serialPortName, address)
    { }

    /// <summary>初始化 TCP 通信。</summary>
    /// <param name="ipAddress">IP 地址</param>
    /// <param name="port">端口号</param>
    /// <param name="address">设备地址（默认 1）</param>
    public BNRC16(IPAddress ipAddress, int port, byte address = 1)
        : base(ipAddress, port, address)
    { }

    /// <summary>初始化（测试用，直接注入会话层）。</summary>
    /// <param name="session">会话层实例</param>
    /// <param name="address">设备地址（默认 1）</param>
    public BNRC16(ISession session, byte address = 1)
        : base(session, address)
    { }

    /// <inheritdoc/>
    protected override string ModelName => "BNRC16";

    /// <inheritdoc/>
    protected override string[] OpenAllData => OpenAll;

    /// <inheritdoc/>
    protected override bool DecodeInput(byte[] raw, int channel)
    {
        // 响应格式：[addr][func][data x8] = 10 字节，数据从 raw[2] 开始
        if (raw.Length < 10) return false;

        int dataIndex = (channel - 1) / 2;
        byte dataByte = raw[2 + dataIndex];

        return channel % 2 == 1
            ? (dataByte & 0x0F) >= 1   // 奇数路取低 4 位
            : (dataByte >> 4) >= 1;    // 偶数路取高 4 位
    }

    /// <inheritdoc/>
    protected override bool DecodeOutput(byte[] raw, int channel)
    {
        // raw[0]=addr, raw[1]=func, raw[2]=channel, raw[3]=state
        return raw.Length >= 4 && raw[3] == 0x01;
    }

    /// <inheritdoc/>
    protected override List<bool> DecodeAllStatuses(byte[] raw)
    {
        var result = new List<bool>();
        if (raw.Length >= 10)
        {
            for (int i = 0; i < 16; i++)
            {
                int byteIndex = 2 + i / 2;
                result.Add(i % 2 == 1
                    ? raw[byteIndex] >= 16      // 高 4 位
                    : raw[byteIndex] % 16 >= 1); // 低 4 位
            }
        }
        return result;
    }
}
