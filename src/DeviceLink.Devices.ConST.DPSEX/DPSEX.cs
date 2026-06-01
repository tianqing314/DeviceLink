using System;
using System.Globalization;
using System.IO.Ports;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DeviceLink.Core;
using DeviceLink.Core.Channel;
using DeviceLink.Core.Framing;
using DeviceLink.Core.Protocol;
using DeviceLink.Core.Transport;

namespace DeviceLink.Devices.ConST.DPSEX
{
    /// <summary>
    /// DPSEX 系列智能数字压力模块。
    /// 
    /// 支持 ConST 协议，通过串口/TCP/MQTT 任意通道连接。
    /// 
    /// 协议格式：地址:R/W:命令:参数...:\0
    /// 例：255:R:MRMD:\0 → 读取测量数据
    ///     255:W:OAV:64:\0 → 写放大倍数 64
    /// 
    /// 使用示例：
    ///   var channel = new DirectChannel(
    ///       new SerialPortTransport("COM3", 9600),
    ///       new DelimiterFrameStrategy(new byte[]{0}));
    ///   var dpsex = new DPSEX(channel);
    ///   await dpsex.OpenAsync();
    ///   var pressure = await dpsex.GetPressureAsync();
    /// </summary>
    public class DPSEX : DeviceBase
    {
        private readonly ConSTCodec _codec;

        #region 构造函数

        /// <summary>
        /// 创建一个 DPSEX 设备实例（注入通道，适用于测试、MQTT 等场景）。
        /// </summary>
        /// <param name="channel">通讯通道</param>
        /// <param name="address">ConST 设备地址（默认 255）</param>
        public DPSEX(IChannel channel, byte address = 255)
            : base(channel, new ConSTCodec(address, ':', new byte[] { 0 }))
        {
            _codec = (ConSTCodec)Codec;
            Name = "DPSEX";
        }

        /// <summary>
        /// 构造函数（TCP 通讯方式使用）
        /// </summary>
        /// <param name="ipAddress">IP 地址</param>
        /// <param name="port">端口号</param>
        /// <param name="address">ConST 设备地址（默认 255）</param>
        public DPSEX(IPAddress ipAddress, int port, byte address = 255)
            : this(CreateTcpChannel(ipAddress, port), address)
        {
        }

        /// <summary>
        /// 构造函数（RS232 通讯方式使用）
        /// </summary>
        /// <param name="serialPortName">串口号（如 COM3）</param>
        /// <param name="baudrate">波特率</param>
        /// <param name="databits">数据位</param>
        /// <param name="stopBits">停止位</param>
        /// <param name="parity">校验位</param>
        /// <param name="address">ConST 设备地址（默认 255）</param>
        public DPSEX(string serialPortName, int baudrate, int databits,
            StopBits stopBits, Parity parity, byte address = 255)
            : this(CreateSerialChannel(serialPortName, baudrate, databits, stopBits, parity), address)
        {
        }

        /// <summary>
        /// 构造函数（RS232 通讯方式使用，默认 9600/8/One/None）
        /// </summary>
        /// <param name="serialPortName">串口号（如 COM3）</param>
        /// <param name="address">ConST 设备地址（默认 255）</param>
        public DPSEX(string serialPortName, byte address = 255)
            : this(CreateSerialChannel(serialPortName, 9600, 8, StopBits.One, Parity.None), address)
        {
        }



        /// <summary>
        /// 创建 TCP 通道
        /// </summary>
        private static DirectChannel CreateTcpChannel(IPAddress ipAddress, int port)
        {
            var transport = new TcpTransport(ipAddress.ToString(), port);
            var frameStrategy = new DelimiterFrameStrategy(new byte[] { 0 });
            return new DirectChannel(transport, frameStrategy, $"DPSEX@{ipAddress}:{port}");
        }

        /// <summary>
        /// 创建串口通道
        /// </summary>
        private static DirectChannel CreateSerialChannel(
            string portName, int baudrate, int databits,
            StopBits stopBits, Parity parity)
        {
            var transport = new SerialPortTransport(portName, baudrate, databits, stopBits, parity);
            var frameStrategy = new DelimiterFrameStrategy(new byte[] { 0 });
            return new DirectChannel(transport, frameStrategy, $"DPSEX@{portName}");
        }

        #endregion 构造函数

        // ═══════════════════════════════════════════════════════════
        // 测量
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 读取当前压力值（MRMD 指令）
        /// </summary>
        public async Task<DeviceResult<double>> GetPressureAsync(
            CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("MRMD"),
                raw =>
                {
                    var text = _codec.ExtractField(raw, 3);
                    return double.TryParse(text,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out var v) ? v : double.NaN;
                },
                ct: ct);
        }

        /// <summary>
        /// 读取无修正的原始测量数据（MRMN 指令）
        /// </summary>
        public async Task<DeviceResult<string[]>> GetRawMeasurementAsync(
            CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("MRMN"),
                raw => _codec.ExtractFields(raw, 3),
                ct: ct);
        }

        /// <summary>
        /// 读取设备内部温度（OTEMP 指令）
        /// </summary>
        public async Task<DeviceResult<double>> GetTemperatureAsync(
            CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("OTEMP"),
                raw =>
                {
                    var text = _codec.ExtractField(raw, 3);
                    // 去掉尾部 "℃" 等非数字字符
                    text = text.TrimEnd('℃', '℉', ' ');
                    return double.TryParse(text,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out var v) ? v : double.NaN;
                },
                ct: ct);
        }

        /// <summary>
        /// 读取传感器的激励电流/电压及 mV 输出（ORIV 指令）
        /// </summary>
        public async Task<DeviceResult<string[]>> GetSensorExcitationAsync(
            CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("ORIV"),
                raw => _codec.ExtractFields(raw, 3),
                ct: ct);
        }

        // ═══════════════════════════════════════════════════════════
        // 设备信息（读）
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 获取软件版本号（OVER 指令）
        /// </summary>
        public async Task<DeviceResult<string>> GetVersionAsync(
            CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("OVER"),
                raw => _codec.ExtractField(raw, 3),
                ct: ct);
        }

        /// <summary>
        /// 获取硬件版本号（OHVER 指令）
        /// </summary>
        public async Task<DeviceResult<string>> GetHardwareVersionAsync(
            CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("OHVER"),
                raw => _codec.ExtractField(raw, 3),
                ct: ct);
        }

        /// <summary>
        /// 获取设备序列号（OTYPE 指令）
        /// </summary>
        public async Task<DeviceResult<string>> GetSerialNumberAsync(
            CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("OTYPE"),
                raw => _codec.ExtractField(raw, 3),
                ct: ct);
        }

        /// <summary>
        /// 读取生产日期（ODATE 指令）
        /// </summary>
        public async Task<DeviceResult<string>> GetProductionDateAsync(
            CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("ODATE"),
                raw => _codec.ExtractField(raw, 3),
                ct: ct);
        }

        /// <summary>
        /// 读取仪器编号（OTYPE/OCODE 指令）
        /// </summary>
        public async Task<DeviceResult<string>> GetInstrumentCodeAsync(
            CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("OTYPE"),
                raw => _codec.ExtractField(raw, 3),
                ct: ct);
        }

        /// <summary>
        /// 读取设备地址（OADD 指令）
        /// </summary>
        public async Task<DeviceResult<int>> GetAddressAsync(
            CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("OADD"),
                raw =>
                {
                    var text = _codec.ExtractField(raw, 3);
                    return int.TryParse(text, out var v) ? v : -1;
                },
                ct: ct);
        }

        /// <summary>
        /// 读取校准日期（ODCAL 指令）
        /// </summary>
        public async Task<DeviceResult<string>> GetCalibrationDateAsync(
            CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("ODCAL"),
                raw => _codec.ExtractField(raw, 3),
                ct: ct);
        }

        /// <summary>
        /// 读 TAG 标签（TAG 指令，参数为长度）
        /// </summary>
        public async Task<DeviceResult<string>> GetTagAsync(
            int length = 48,
            CancellationToken ct = default)
        {
            return await SendForResultAsync(
                new Command(CommandKind.Read, "TAG", length.ToString()),
                raw => _codec.ExtractField(raw, 3),
                ct: ct);
        }

        /// <summary>
        /// 读量程信息（ORAN 指令）。
        /// 返回：[零点值, 满度值, 传感器类型, 准确度]
        /// </summary>
        public async Task<DeviceResult<string[]>> GetRangeAsync(
            CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("ORAN"),
                raw => _codec.ExtractFields(raw, 3),
                ct: ct);
        }

        /// <summary>
        /// 读允许使用的单位信息（OUNIT 指令）
        /// </summary>
        public async Task<DeviceResult<int>> GetUnitInfoAsync(
            CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("OUNIT"),
                raw =>
                {
                    var text = _codec.ExtractField(raw, 3);
                    return int.TryParse(text, out var v) ? v : -1;
                },
                ct: ct);
        }

        /// <summary>
        /// 读温补/线性/校准标志状态（OSTAT 指令）
        /// </summary>
        public async Task<DeviceResult<string>> GetStatusAsync(
            CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("OSTAT"),
                raw => _codec.ExtractField(raw, 3),
                ct: ct);
        }

        /// <summary>
        /// 读工作模式（MWORK 指令）
        /// </summary>
        public async Task<DeviceResult<int>> GetWorkModeAsync(
            CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("MWORK"),
                raw =>
                {
                    var text = _codec.ExtractField(raw, 3);
                    return int.TryParse(text, out var v) ? v : -1;
                },
                ct: ct);
        }

        // ═══════════════════════════════════════════════════════════
        // 设备信息（写）
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 写生产日期（ODATE 指令）
        /// </summary>
        public async Task<DeviceResult> SetProductionDateAsync(
            string date,
            CancellationToken ct = default)
        {
            var channelResult = await SendAsync(Command.Write("ODATE", date), ct: ct);
            return channelResult.Success
                ? DeviceResult.Succeed(channelResult)
                : DeviceResult.Failed(channelResult.Error, channelResult);
        }

        /// <summary>
        /// 写仪器编号（OTYPE 指令）
        /// </summary>
        public async Task<DeviceResult> SetInstrumentCodeAsync(
            string code,
            CancellationToken ct = default)
        {
            var channelResult = await SendAsync(Command.Write("OTYPE", code), ct: ct);
            return channelResult.Success
                ? DeviceResult.Succeed(channelResult)
                : DeviceResult.Failed(channelResult.Error, channelResult);
        }

        /// <summary>
        /// 写设备地址（OADD 指令，1~255）
        /// </summary>
        public async Task<DeviceResult> SetAddressAsync(
            byte newAddress,
            CancellationToken ct = default)
        {
            var channelResult = await SendAsync(
                Command.Write("OADD", newAddress.ToString()), ct: ct);
            return channelResult.Success
                ? DeviceResult.Succeed(channelResult)
                : DeviceResult.Failed(channelResult.Error, channelResult);
        }

        /// <summary>
        /// 写校准日期（ODCAL 指令）
        /// </summary>
        public async Task<DeviceResult> SetCalibrationDateAsync(
            string date,
            CancellationToken ct = default)
        {
            var channelResult = await SendAsync(Command.Write("ODCAL", date), ct: ct);
            return channelResult.Success
                ? DeviceResult.Succeed(channelResult)
                : DeviceResult.Failed(channelResult.Error, channelResult);
        }

        /// <summary>
        /// 写 TAG 标签（TAG 指令）
        /// </summary>
        public async Task<DeviceResult> SetTagAsync(
            string tag,
            CancellationToken ct = default)
        {
            var channelResult = await SendAsync(
                Command.Write("TAG", tag.Length.ToString(), tag), ct: ct);
            return channelResult.Success
                ? DeviceResult.Succeed(channelResult)
                : DeviceResult.Failed(channelResult.Error, channelResult);
        }

        // ═══════════════════════════════════════════════════════════
        // 配置
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 压力零点校准（OZERO 指令）
        /// </summary>
        public async Task<DeviceResult> PressureZeroAsync(
            CancellationToken ct = default)
        {
            var channelResult = await SendAsync(Command.Write("OZERO"), ct: ct);
            return channelResult.Success
                ? DeviceResult.Succeed(channelResult)
                : DeviceResult.Failed(channelResult.Error, channelResult);
        }

        /// <summary>
        /// 清零（OZERO 指令）
        /// </summary>
        public async Task<DeviceResult> ZeroAsync(
            CancellationToken ct = default)
        {
            var channelResult = await SendAsync(Command.Write("OZERO"), ct: ct);
            return channelResult.Success
                ? DeviceResult.Succeed(channelResult)
                : DeviceResult.Failed(channelResult.Error, channelResult);
        }

        /// <summary>
        /// 写放大倍数（OAV 指令，参数 1/2/4/8/16/32/64/128）
        /// </summary>
        public async Task<DeviceResult> SetAmplificationAsync(
            int value,
            CancellationToken ct = default)
        {
            var channelResult = await SendAsync(
                Command.Write("OAV", value.ToString()), ct: ct);
            return channelResult.Success
                ? DeviceResult.Succeed(channelResult)
                : DeviceResult.Failed(channelResult.Error, channelResult);
        }

        /// <summary>
        /// 读放大倍数（OAV 指令）
        /// </summary>
        public async Task<DeviceResult<int>> GetAmplificationAsync(
            CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("OAV"),
                raw =>
                {
                    var text = _codec.ExtractField(raw, 3);
                    return int.TryParse(text, out var v) ? v : -1;
                },
                ct: ct);
        }

        /// <summary>
        /// 写恒流/恒压值（OIS 指令，1-9=恒流，30=恒压）
        /// </summary>
        public async Task<DeviceResult> SetConstantCurrentAsync(
            int value,
            CancellationToken ct = default)
        {
            var channelResult = await SendAsync(
                Command.Write("OIS", value.ToString()), ct: ct);
            return channelResult.Success
                ? DeviceResult.Succeed(channelResult)
                : DeviceResult.Failed(channelResult.Error, channelResult);
        }

        /// <summary>
        /// 读恒流/恒压值（OIS 指令）
        /// </summary>
        public async Task<DeviceResult<int>> GetConstantCurrentAsync(
            CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("OIS"),
                raw =>
                {
                    var text = _codec.ExtractField(raw, 3);
                    return int.TryParse(text, out var v) ? v : -1;
                },
                ct: ct);
        }

        /// <summary>
        /// 写工作模式（MWORK 指令，0=普通, 1=峰值, 2=线性, 3=线性+峰值）
        /// </summary>
        public async Task<DeviceResult> SetWorkModeAsync(
            int mode,
            CancellationToken ct = default)
        {
            var channelResult = await SendAsync(
                Command.Write("MWORK", mode.ToString()), ct: ct);
            return channelResult.Success
                ? DeviceResult.Succeed(channelResult)
                : DeviceResult.Failed(channelResult.Error, channelResult);
        }

        /// <summary>
        /// 写压力单位（OUNIT 指令，1~12 对应不同单位）
        /// </summary>
        public async Task<DeviceResult> SetUnitAsync(
            int unitCode,
            CancellationToken ct = default)
        {
            var channelResult = await SendAsync(
                Command.Write("OUNIT", unitCode.ToString()), ct: ct);
            return channelResult.Success
                ? DeviceResult.Succeed(channelResult)
                : DeviceResult.Failed(channelResult.Error, channelResult);
        }

        /// <summary>
        /// 写波特率（OBAUQ 指令：1200/2400/4800/9600/19200/38400）
        /// </summary>
        public async Task<DeviceResult> SetBaudRateAsync(
            int baudRate,
            CancellationToken ct = default)
        {
            var channelResult = await SendAsync(
                Command.Write("OBAUQ", baudRate.ToString()), ct: ct);
            return channelResult.Success
                ? DeviceResult.Succeed(channelResult)
                : DeviceResult.Failed(channelResult.Error, channelResult);
        }

        /// <summary>
        /// 写传感器类型（OSENS 指令，G=表压, A=绝压, D=差压）
        /// </summary>
        public async Task<DeviceResult> SetSensorTypeAsync(
            string type,
            CancellationToken ct = default)
        {
            var channelResult = await SendAsync(
                Command.Write("OSENS", type), ct: ct);
            return channelResult.Success
                ? DeviceResult.Succeed(channelResult)
                : DeviceResult.Failed(channelResult.Error, channelResult);
        }

        /// <summary>
        /// 写压力数据输出速度（SPEED 指令，0=低速, 1=高速）
        /// </summary>
        public async Task<DeviceResult> SetSpeedAsync(
            int speed,
            CancellationToken ct = default)
        {
            var channelResult = await SendAsync(
                Command.Write("SPEED", speed.ToString()), ct: ct);
            return channelResult.Success
                ? DeviceResult.Succeed(channelResult)
                : DeviceResult.Failed(channelResult.Error, channelResult);
        }

        /// <summary>
        /// 读压力数据输出速度（SPEED 指令，0=低速, 1=高速）
        /// </summary>
        public async Task<DeviceResult<int>> GetSpeedAsync(
            CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("SPEED"),
                raw =>
                {
                    var text = _codec.ExtractField(raw, 3);
                    return int.TryParse(text, out var v) ? v : -1;
                },
                ct: ct);
        }

        /// <summary>
        /// 恢复出厂设置（OFALT 指令）
        /// </summary>
        public async Task<DeviceResult> RestoreFactoryAsync(
            CancellationToken ct = default)
        {
            var channelResult = await SendAsync(Command.Write("OFALT"), ct: ct);
            return channelResult.Success
                ? DeviceResult.Succeed(channelResult)
                : DeviceResult.Failed(channelResult.Error, channelResult);
        }

        /// <summary>
        /// 仪表软复位（ORPP 指令）
        /// </summary>
        public async Task SoftResetAsync(
            CancellationToken ct = default)
        {
            await SendNonQueryAsync(Command.Write("ORPP"), ct);
        }

        /// <summary>
        /// 设置连续输出模式（OCONT 指令，0=关闭, 1/2/3=不同上传格式）
        /// </summary>
        public async Task<DeviceResult> SetContinuousOutputAsync(
            int mode,
            CancellationToken ct = default)
        {
            var channelResult = await SendAsync(
                Command.Write("OCONT", mode.ToString()), ct: ct);
            return channelResult.Success
                ? DeviceResult.Succeed(channelResult)
                : DeviceResult.Failed(channelResult.Error, channelResult);
        }

        /// <summary>
        /// 写测量速率（MRATE 指令，7/15/30/60/120/240 次/分钟）
        /// </summary>
        public async Task<DeviceResult> SetMeasurementRateAsync(
            int rate,
            CancellationToken ct = default)
        {
            var channelResult = await SendAsync(
                Command.Write("MRATE", rate.ToString()), ct: ct);
            return channelResult.Success
                ? DeviceResult.Succeed(channelResult)
                : DeviceResult.Failed(channelResult.Error, channelResult);
        }
    }
}