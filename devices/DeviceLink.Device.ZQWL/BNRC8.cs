using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DeviceLink.Session;

namespace DeviceLink.Device.ZQWL;

/// <summary>
/// 智嵌物联 BNRC8 网络继电器控制器（8 路）。
///
/// 通信参数：115200, 8, 1, None
/// 协议格式：[48 3A] [addr] [func] [data x8] [checksum] [45 44]
///
/// 数据区编码（BNRC8）：<b>每字节对应 1 路</b>，0x00=关，0x01=开，
/// 因此数据从 raw[1] 开始（与 BNRC16/32 的 raw[2] 不同，这是协议差异，不是笔误）。
///
/// 使用示例：
///   var relay = new BNRC8("COM3");
///   await relay.OpenAsync();
///   await relay.SetOutputAsync(1, true);       // 打开第 1 路
///   var state = await relay.GetInputAsync(1);  // 读取第 1 路输入
/// </summary>
public class BNRC8 : BnrcRelayBase
{
    private static readonly string[] OpenAll =
        { "01", "01", "01", "01", "01", "01", "01", "01" };

    /// <summary>初始化串口通信（完整参数）。</summary>
    /// <param name="serialPortName">串口号（如 COM3）</param>
    /// <param name="baudRate">波特率（默认 115200）</param>
    /// <param name="dataBits">数据位（默认 8）</param>
    /// <param name="stopBits">停止位（默认 1）</param>
    /// <param name="parity">校验位（默认 None）</param>
    /// <param name="address">设备地址（默认 1）</param>
    public BNRC8(string serialPortName, int baudRate = 115200, int dataBits = 8,
        StopBits stopBits = StopBits.One, Parity parity = Parity.None, byte address = 1)
        : base(serialPortName, baudRate, dataBits, stopBits, parity, address)
    { }

    /// <summary>初始化串口通信（默认配置 115200,8,1,None）。</summary>
    /// <param name="serialPortName">串口号</param>
    /// <param name="address">设备地址（默认 1）</param>
    public BNRC8(string serialPortName, byte address = 1)
        : base(serialPortName, address)
    { }

    /// <summary>初始化 TCP 通信。</summary>
    /// <param name="ipAddress">IP 地址</param>
    /// <param name="port">端口号</param>
    /// <param name="address">设备地址（默认 1）</param>
    public BNRC8(IPAddress ipAddress, int port, byte address = 1)
        : base(ipAddress, port, address)
    { }

    /// <summary>初始化（测试用，直接注入会话层）。</summary>
    /// <param name="session">会话层实例</param>
    /// <param name="address">设备地址（默认 1）</param>
    public BNRC8(ISession session, byte address = 1)
        : base(session, address)
    { }

    /// <inheritdoc/>
    protected override string ModelName => "BNRC8";

    /// <inheritdoc/>
    protected override string VersionCode => "BNRC08";

    /// <inheritdoc/>
    protected override string[] OpenAllData => OpenAll;

    /// <inheritdoc/>
    protected override bool DecodeInput(byte[] raw, int channel)
        => Zqwl.ExtractInputState(raw, channel);

    /// <inheritdoc/>
    protected override bool DecodeOutput(byte[] raw, int channel)
    {
        // 响应中 raw[1 + channel] 表示该路状态
        return raw.Length >= 10 && raw[1 + channel] == 0x01;
    }

    /// <inheritdoc/>
    protected override List<bool> DecodeAllStatuses(byte[] raw)
    {
        var result = new List<bool>();
        if (raw.Length >= 10)
        {
            for (int i = 1; i <= 8; i++)
                result.Add(raw[1 + i] == 0x01);
        }
        return result;
    }
}
