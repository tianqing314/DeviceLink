using System;
using System.Globalization;
using System.IO.Ports;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DeviceLink.DataLink;
using DeviceLink.Protocol;
using DeviceLink.Session;
using DeviceLink.Transport;

namespace DeviceLink.Devices
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
    ///   var transport = new SerialPortTransport("COM3", 9600);
    ///   var frameStrategy = new DelimiterFrameStrategy(new byte[]{0});
    ///   var dataLink = new DirectDataLink(transport, frameStrategy);
    ///   var session = new DirectSession(dataLink);
    ///   var codec = new ConSTCodec(255);
    ///   var dpsex = new DPSEX(session, codec);
    ///   await dpsex.OpenAsync();
    ///   var pressure = await dpsex.GetPressureAsync();
    /// </summary>
    public class DPSEX : DeviceBase
    {
        private readonly ConSTCodec _codec;

        #region 构造函数

        /// <summary>
        /// 创建一个 DPSEX 设备实例（注入会话和编解码器，适用于测试、MQTT 等场景）。
        /// </summary>
        /// <param name="session">会话层</param>
        /// <param name="codec">协议编解码器</param>
        public DPSEX(ISession session, ConSTCodec codec)
            : base(session, codec)
        {
            _codec = codec;
            Name = "DPSEX";
        }

        /// <summary>
        /// 构造函数（串口通讯方式使用）
        /// </summary>
        /// <param name="serialPortName">串口号（如 COM3）</param>
        /// <param name="baudRate">波特率</param>
        /// <param name="dataBits">数据位</param>
        /// <param name="stopBits">停止位</param>
        /// <param name="parity">校验位</param>
        /// <param name="address">ConST 设备地址（默认 255）</param>
        public DPSEX(string serialPortName, int baudRate = 9600, int dataBits = 8,
            StopBits stopBits = StopBits.One, Parity parity = Parity.None, byte address = 255)
            : this(CreateSerialSession(serialPortName, baudRate, dataBits, stopBits, parity), new ConSTCodec(address))
        {
        }

        /// <summary>
        /// 构造函数（TCP 通讯方式使用）
        /// </summary>
        /// <param name="ipAddress">IP 地址</param>
        /// <param name="port">端口号</param>
        /// <param name="address">ConST 设备地址（默认 255）</param>
        public DPSEX(IPAddress ipAddress, int port, byte address = 255)
            : this(CreateTcpSession(ipAddress, port), new ConSTCodec(address))
        {
        }

        /// <summary>
        /// 创建串口会话
        /// </summary>
        private static ISession CreateSerialSession(
            string portName, int baudRate, int dataBits,
            StopBits stopBits, Parity parity)
        {
            var transport = new SerialPortTransport(portName, baudRate, dataBits, stopBits, parity);
            var frameStrategy = new DelimiterFrameStrategy(new byte[] { 0 });
            var dataLink = new DirectDataLink(transport, frameStrategy);
            return new DirectSession(dataLink);
        }

        /// <summary>
        /// 创建TCP会话
        /// </summary>
        private static ISession CreateTcpSession(IPAddress ipAddress, int port)
        {
            var transport = new TcpTransport(ipAddress.ToString(), port);
            var frameStrategy = new DelimiterFrameStrategy(new byte[] { 0 });
            var dataLink = new DirectDataLink(transport, frameStrategy);
            return new DirectSession(dataLink);
        }

        #endregion 构造函数

        // ═══════════════════════════════════════════════════════════
        // 测量
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 读取当前压力值（MRMD 指令）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>压力值</returns>
        public async Task<double> GetPressureAsync(CancellationToken ct = default)
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
                ct);
        }

        /// <summary>
        /// 读取无修正的原始测量数据（MRMN 指令）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>原始测量数据数组</returns>
        public async Task<string[]> GetRawMeasurementAsync(CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("MRMN"),
                raw => _codec.ExtractFields(raw, 3),
                ct);
        }

        /// <summary>
        /// 读取设备内部温度（OTEMP 指令）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>温度值</returns>
        public async Task<double> GetTemperatureAsync(CancellationToken ct = default)
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
                ct);
        }

        /// <summary>
        /// 读取传感器的激励电流/电压及 mV 输出（ORIV 指令）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>传感器激励数据数组</returns>
        public async Task<string[]> GetSensorExcitationAsync(CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("ORIV"),
                raw => _codec.ExtractFields(raw, 3),
                ct);
        }

        // ═══════════════════════════════════════════════════════════
        // 设备信息（读）
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 获取软件版本号（OVER 指令）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>版本号</returns>
        public async Task<string> GetVersionAsync(CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("OVER"),
                raw => _codec.ExtractField(raw, 3),
                ct);
        }

        /// <summary>
        /// 获取硬件版本号（OHVER 指令）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>硬件版本号</returns>
        public async Task<string> GetHardwareVersionAsync(CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("OHVER"),
                raw => _codec.ExtractField(raw, 3),
                ct);
        }

        /// <summary>
        /// 获取设备序列号（OTYPE 指令）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>序列号</returns>
        public async Task<string> GetSerialNumberAsync(CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("OTYPE"),
                raw => _codec.ExtractField(raw, 3),
                ct);
        }

        /// <summary>
        /// 读取生产日期（ODATE 指令）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>生产日期</returns>
        public async Task<string> GetProductionDateAsync(CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("ODATE"),
                raw => _codec.ExtractField(raw, 3),
                ct);
        }

        /// <summary>
        /// 读取仪器编号（OTYPE/OCODE 指令）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>仪器编号</returns>
        public async Task<string> GetInstrumentCodeAsync(CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("OTYPE"),
                raw => _codec.ExtractField(raw, 3),
                ct);
        }

        /// <summary>
        /// 读取设备地址（OADD 指令）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>设备地址</returns>
        public async Task<int> GetAddressAsync(CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("OADD"),
                raw =>
                {
                    var text = _codec.ExtractField(raw, 3);
                    return int.TryParse(text, out var v) ? v : -1;
                },
                ct);
        }

        /// <summary>
        /// 读取校准日期（ODCAL 指令）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>校准日期</returns>
        public async Task<string> GetCalibrationDateAsync(CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("ODCAL"),
                raw => _codec.ExtractField(raw, 3),
                ct);
        }

        /// <summary>
        /// 读 TAG 标签（TAG 指令，参数为长度）
        /// </summary>
        /// <param name="length">标签长度（默认48）</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>TAG标签</returns>
        public async Task<string> GetTagAsync(int length = 48, CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("TAG", length.ToString()),
                raw => _codec.ExtractField(raw, 3),
                ct);
        }

        /// <summary>
        /// 读量程信息（ORAN 指令）。
        /// 返回：[零点值, 满度值, 传感器类型, 准确度]
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>量程信息数组</returns>
        public async Task<string[]> GetRangeAsync(CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("ORAN"),
                raw => _codec.ExtractFields(raw, 3),
                ct);
        }

        /// <summary>
        /// 读允许使用的单位信息（OUNIT 指令）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>单位信息</returns>
        public async Task<int> GetUnitInfoAsync(CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("OUNIT"),
                raw =>
                {
                    var text = _codec.ExtractField(raw, 3);
                    return int.TryParse(text, out var v) ? v : -1;
                },
                ct);
        }

        /// <summary>
        /// 读温补/线性/校准标志状态（OSTAT 指令）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>状态信息</returns>
        public async Task<string> GetStatusAsync(CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("OSTAT"),
                raw => _codec.ExtractField(raw, 3),
                ct);
        }

        /// <summary>
        /// 读工作模式（MWORK 指令）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>工作模式</returns>
        public async Task<int> GetWorkModeAsync(CancellationToken ct = default)
             {
            return await SendForResultAsync(
                Command.Read("MWORK"),
                raw =>
                {
                    var text = _codec.ExtractField(raw, 3);
                    return int.TryParse(text, out var v) ? v : -1;
                },
                ct);
        }

        // ═══════════════════════════════════════════════════════════
        // 设备信息（写）
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 写生产日期（ODATE 指令）
        /// </summary>
        /// <param name="date">日期</param>
        /// <param name="ct">取消令牌</param>
        public async Task SetProductionDateAsync(string date, CancellationToken ct = default)
        {
            await SendNonQueryAsync(Command.Write("ODATE", date), ct);
        }

        /// <summary>
        /// 写仪器编号（OTYPE 指令）
        /// </summary>
        /// <param name="code">编号</param>
        /// <param name="ct">取消令牌</param>
        public async Task SetInstrumentCodeAsync(string code, CancellationToken ct = default)
        {
            await SendNonQueryAsync(Command.Write("OTYPE", code), ct);
        }

        /// <summary>
        /// 写设备地址（OADD 指令，1~255）
        /// </summary>
        /// <param name="newAddress">新地址</param>
        /// <param name="ct">取消令牌</param>
        public async Task SetAddressAsync(byte newAddress, CancellationToken ct = default)
        {
            await SendNonQueryAsync(Command.Write("OADD", newAddress.ToString()), ct);
        }

        /// <summary>
        /// 写校准日期（ODCAL 指令）
        /// </summary>
        /// <param name="date">日期</param>
        /// <param name="ct">取消令牌</param>
        public async Task SetCalibrationDateAsync(string date, CancellationToken ct = default)
        {
            await SendNonQueryAsync(Command.Write("ODCAL", date), ct);
        }

        /// <summary>
        /// 写 TAG 标签（TAG 指令）
        /// </summary>
        /// <param name="tag">标签</param>
        /// <param name="ct">取消令牌</param>
        public async Task SetTagAsync(string tag, CancellationToken ct = default)
        {
            await SendNonQueryAsync(Command.Write("TAG", tag.Length.ToString(), tag), ct);
        }

        // ═══════════════════════════════════════════════════════════
        // 配置
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 压力零点校准（OZERO 指令）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        public async Task PressureZeroAsync(CancellationToken ct = default)
        {
            await SendNonQueryAsync(Command.Write("OZERO"), ct);
        }

        /// <summary>
        /// 清零（OZERO 指令）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        public async Task ZeroAsync(CancellationToken ct = default)
        {
            await SendNonQueryAsync(Command.Write("OZERO"), ct);
        }

        /// <summary>
        /// 写放大倍数（OAV 指令，参数 1/2/4/8/16/32/64/128）
        /// </summary>
        /// <param name="value">放大倍数</param>
        /// <param name="ct">取消令牌</param>
        public async Task SetAmplificationAsync(int value, CancellationToken ct = default)
        {
            await SendNonQueryAsync(Command.Write("OAV", value.ToString()), ct);
        }

        /// <summary>
        /// 读放大倍数（OAV 指令）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>放大倍数</returns>
        public async Task<int> GetAmplificationAsync(CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("OAV"),
                raw =>
                {
                    var text = _codec.ExtractField(raw, 3);
                    return int.TryParse(text, out var v) ? v : -1;
                },
                ct);
        }

        /// <summary>
        /// 写恒流/恒压值（OIS 指令，1-9=恒流，30=恒压）
        /// </summary>
        /// <param name="value">恒流/恒压值</param>
        /// <param name="ct">取消令牌</param>
        public async Task SetConstantCurrentAsync(int value, CancellationToken ct = default)
        {
            await SendNonQueryAsync(Command.Write("OIS", value.ToString()), ct);
        }

        /// <summary>
        /// 读恒流/恒压值（OIS 指令）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>恒流/恒压值</returns>
        public async Task<int> GetConstantCurrentAsync(CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("OIS"),
                raw =>
                {
                    var text = _codec.ExtractField(raw, 3);
                    return int.TryParse(text, out var v) ? v : -1;
                },
                ct);
        }

        /// <summary>
        /// 写工作模式（MWORK 指令，0=普通, 1=峰值, 2=线性, 3=线性+峰值）
        /// </summary>
        /// <param name="mode">工作模式</param>
        /// <param name="ct">取消令牌</param>
        public async Task SetWorkModeAsync(int mode, CancellationToken ct = default)
        {
            await SendNonQueryAsync(Command.Write("MWORK", mode.ToString()), ct);
        }

        /// <summary>
        /// 写压力单位（OUNIT 指令，1~12 对应不同单位）
        /// </summary>
        /// <param name="unitCode">单位代码</param>
        /// <param name="ct">取消令牌</param>
        public async Task SetUnitAsync(int unitCode, CancellationToken ct = default)
        {
            await SendNonQueryAsync(Command.Write("OUNIT", unitCode.ToString()), ct);
        }

        /// <summary>
        /// 写波特率（OBAUQ 指令：1200/2400/4800/9600/19200/38400）
        /// </summary>
        /// <param name="baudRate">波特率</param>
        /// <param name="ct">取消令牌</param>
        public async Task SetBaudRateAsync(int baudRate, CancellationToken ct = default)
        {
            await SendNonQueryAsync(Command.Write("OBAUQ", baudRate.ToString()), ct);
        }

        /// <summary>
        /// 写传感器类型（OSENS 指令，G=表压, A=绝压, D=差压）
        /// </summary>
        /// <param name="type">传感器类型</param>
        /// <param name="ct">取消令牌</param>
        public async Task SetSensorTypeAsync(string type, CancellationToken ct = default)
        {
            await SendNonQueryAsync(Command.Write("OSENS", type), ct);
        }

        /// <summary>
        /// 写压力数据输出速度（SPEED 指令，0=低速, 1=高速）
        /// </summary>
        /// <param name="speed">速度</param>
        /// <param name="ct">取消令牌</param>
        public async Task SetSpeedAsync(int speed, CancellationToken ct = default)
        {
            await SendNonQueryAsync(Command.Write("SPEED", speed.ToString()), ct);
        }

        /// <summary>
        /// 读压力数据输出速度（SPEED 指令，0=低速, 1=高速）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>速度</returns>
        public async Task<int> GetSpeedAsync(CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("SPEED"),
                raw =>
                {
                    var text = _codec.ExtractField(raw, 3);
                    return int.TryParse(text, out var v) ? v : -1;
                },
                ct);
        }

        /// <summary>
        /// 恢复出厂设置（OFALT 指令）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        public async Task RestoreFactoryAsync(CancellationToken ct = default)
        {
            await SendNonQueryAsync(Command.Write("OFALT"), ct);
        }

        /// <summary>
        /// 仪表软复位（ORPP 指令）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        public async Task SoftResetAsync(CancellationToken ct = default)
        {
            await SendNonQueryAsync(Command.Write("ORPP"), ct);
        }

        /// <summary>
        /// 设置连续输出模式（OCONT 指令，0=关闭, 1/2/3=不同上传格式）
        /// </summary>
        /// <param name="mode">模式</param>
        /// <param name="ct">取消令牌</param>
        public async Task SetContinuousOutputAsync(int mode, CancellationToken ct = default)
        {
            await SendNonQueryAsync(Command.Write("OCONT", mode.ToString()), ct);
        }

        /// <summary>
        /// 写测量速率（MRATE 指令，7/15/30/60/120/240 次/分钟）
        /// </summary>
        /// <param name="rate">速率</param>
        /// <param name="ct">取消令牌</param>
        public async Task SetMeasurementRateAsync(int rate, CancellationToken ct = default)
        {
            await SendNonQueryAsync(Command.Write("MRATE", rate.ToString()), ct);
        }
    }
}
