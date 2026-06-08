using System;
using System.IO.Ports;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DeviceLink.DeviceBase;
using DeviceLink.Protocol;
using DeviceLink.Session;

namespace DeviceLink.Device.UMC1200
{
    /// <summary>
    /// UMC1200 温湿度控制器设备类
    /// 
    /// 支持 Modbus RTU 协议，通过串口/TCP/MQTT 任意通道连接。
    /// 
    /// 协议格式：[从站地址][功能码][数据...][CRC16]
    /// - 功能码 0x03：读保持寄存器
    /// - 功能码 0x10：写多个寄存器
    /// 
    /// 通信参数：
    /// - 波特率：9600
    /// - 数据位：8位
    /// - 校验：NONE
    /// - 停止位：1
    /// 
    /// 使用示例：
    ///   var umc1200 = new UMC1200("COM3");
    ///   await umc1200.OpenAsync();
    ///   var temp = await umc1200.GetTemperaturePVAsync();
    /// </summary>
    public class UMC1200 : DeviceBase.DeviceBase
    {
        private readonly ModbusRtuCodec _codec;
        private readonly byte _slaveAddress;

        #region 构造函数

        /// <summary>
        /// 构造函数（串口通讯方式使用）
        /// </summary>
        /// <param name="serialPortName">串口号（如 COM3）</param>
        /// <param name="baudRate">波特率（默认9600）</param>
        /// <param name="dataBits">数据位（默认8）</param>
        /// <param name="stopBits">停止位（默认1）</param>
        /// <param name="parity">校验位（默认None）</param>
        /// <param name="slaveAddress">Modbus从站地址（默认1）</param>
        public UMC1200(string serialPortName, int baudRate = 9600, int dataBits = 8,
            StopBits stopBits = StopBits.One, Parity parity = Parity.None, byte slaveAddress = 1)
            : base(serialPortName, baudRate, dataBits, stopBits, parity, new ModbusRtuCodec(slaveAddress))
        {
            _codec = (ModbusRtuCodec)Codec;
            _slaveAddress = slaveAddress;
        }

        /// <summary>
        /// 构造函数（串口通讯方式使用，默认配置）
        /// </summary>
        /// <param name="serialPortName">串口号（如 COM3）</param>
        /// <param name="slaveAddress">Modbus从站地址（默认1）</param>
        public UMC1200(string serialPortName, byte slaveAddress = 1)
            : base(serialPortName, new ModbusRtuCodec(slaveAddress))
        {
            _codec = (ModbusRtuCodec)Codec;
            _slaveAddress = slaveAddress;
        }

        /// <summary>
        /// 构造函数（TCP 通讯方式使用）
        /// </summary>
        /// <param name="ipAddress">IP 地址</param>
        /// <param name="port">端口号</param>
        /// <param name="slaveAddress">Modbus从站地址（默认1）</param>
        public UMC1200(IPAddress ipAddress, int port, byte slaveAddress = 1)
            : base(ipAddress, port, new ModbusRtuCodec(slaveAddress))
        {
            _codec = (ModbusRtuCodec)Codec;
            _slaveAddress = slaveAddress;
        }

        /// <summary>
        /// 构造函数（通信设置实例方式适用）
        /// </summary>
        /// <param name="settings">通信配置</param>
        /// <param name="slaveAddress">Modbus从站地址（默认1）</param>
        public UMC1200(DeviceCommSettings settings, byte slaveAddress = 1)
            : base(settings, new ModbusRtuCodec(slaveAddress))
        {
            _codec = (ModbusRtuCodec)Codec;
            _slaveAddress = slaveAddress;
        }

        /// <summary>
        /// 构造函数（MQTT 通讯方式使用）
        /// </summary>
        /// <param name="brokerHost">MQTT Broker 地址</param>
        /// <param name="brokerPort">MQTT Broker 端口号</param>
        /// <param name="requestTopic">请求主题（设备接收命令的主题）</param>
        /// <param name="responseTopic">响应主题（设备发送响应的主题）</param>
        /// <param name="slaveAddress">Modbus从站地址（默认1）</param>
        /// <param name="requestTimeoutMs">请求超时时间（毫秒，默认 5000）</param>
        public UMC1200(string brokerHost, int brokerPort, string requestTopic, string responseTopic,
            byte slaveAddress = 1, int requestTimeoutMs = 5000)
            : base(new MqttSession(new MqttSessionOptions
            {
                BrokerHost = brokerHost,
                BrokerPort = brokerPort,
                RequestTopic = requestTopic,
                ResponseTopic = responseTopic,
                RequestTimeoutMs = requestTimeoutMs
            }), new ModbusRtuCodec(slaveAddress))
        {
            _codec = (ModbusRtuCodec)Codec;
            _slaveAddress = slaveAddress;
        }

        /// <summary>
        /// 配置构造函数默认信息
        /// </summary>
        protected override void ConstructDefaultInfo()
        {
            base.ConstructDefaultInfo();
            Name = "UMC1200";
        }

        #endregion 构造函数

        // ═══════════════════════════════════════════════════════════
        // 通用读写方法
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 读取单个寄存器值
        /// </summary>
        /// <param name="registerAddress">寄存器地址</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>寄存器值</returns>
        public async Task<ushort> ReadRegisterAsync(ushort registerAddress, CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read($"3.{registerAddress}.1"),
                raw =>
                {
                    var registers = _codec.ExtractRegisters(raw);
                    return registers.Length > 0 ? registers[0] : (ushort)0;
                },
                ct);
        }

        /// <summary>
        /// 读取多个连续寄存器值
        /// </summary>
        /// <param name="startAddress">起始寄存器地址</param>
        /// <param name="count">寄存器数量</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>寄存器值数组</returns>
        public async Task<ushort[]> ReadRegistersAsync(ushort startAddress, ushort count, CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read($"3.{startAddress}.{count}"),
                raw => _codec.ExtractRegisters(raw),
                ct);
        }

        /// <summary>
        /// 写入单个寄存器值
        /// </summary>
        /// <param name="registerAddress">寄存器地址</param>
        /// <param name="value">写入值</param>
        /// <param name="ct">取消令牌</param>
        public async Task WriteRegisterAsync(ushort registerAddress, ushort value, CancellationToken ct = default)
        {
            await SendNonQueryAsync(Command.Write($"6.{registerAddress}", value.ToString()), ct);
        }

        /// <summary>
        /// 写入多个连续寄存器值
        /// </summary>
        /// <param name="startAddress">起始寄存器地址</param>
        /// <param name="values">写入值数组</param>
        /// <param name="ct">取消令牌</param>
        public async Task WriteRegistersAsync(ushort startAddress, ushort[] values, CancellationToken ct = default)
        {
            var parameters = new string[values.Length + 1];
            parameters[0] = values.Length.ToString();
            for (int i = 0; i < values.Length; i++)
            {
                parameters[i + 1] = values[i].ToString();
            }
            await SendNonQueryAsync(Command.Write($"16.{startAddress}", parameters), ct);
        }

        // ═══════════════════════════════════════════════════════════
        // 测量值读取（ReadOnly）
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 获取温度PV值（过程值）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>温度值（单位：0.1℃）</returns>
        public async Task<short> GetTemperaturePVAsync(CancellationToken ct = default)
        {
            ushort raw = await ReadRegisterAsync(UMC1200Registers.TemperaturePV, ct);
            return (short)raw;
        }

        /// <summary>
        /// 获取湿度PV值（过程值）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>湿度值（单位：0.1%RH）</returns>
        public async Task<short> GetHumidityPVAsync(CancellationToken ct = default)
        {
            ushort raw = await ReadRegisterAsync(UMC1200Registers.HumidityPV, ct);
            return (short)raw;
        }

        /// <summary>
        /// 获取温度MV值（操纵值/输出值）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>温度MV值</returns>
        public async Task<short> GetTemperatureMVAsync(CancellationToken ct = default)
        {
            ushort raw = await ReadRegisterAsync(UMC1200Registers.TemperatureMV, ct);
            return (short)raw;
        }

        /// <summary>
        /// 获取湿度MV值（操纵值/输出值）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>湿度MV值</returns>
        public async Task<short> GetHumidityMVAsync(CancellationToken ct = default)
        {
            ushort raw = await ReadRegisterAsync(UMC1200Registers.HumidityMV, ct);
            return (short)raw;
        }

        // ═══════════════════════════════════════════════════════════
        // 工艺信息读取（ReadOnly）
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 获取当前工艺步数
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>工艺步数</returns>
        public async Task<ushort> GetProcessStepCountAsync(CancellationToken ct = default)
        {
            return await ReadRegisterAsync(UMC1200Registers.ProcessStepCount, ct);
        }

        /// <summary>
        /// 获取当前工艺步号（从0开始）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>工艺步号</returns>
        public async Task<ushort> GetProcessStepNumberAsync(CancellationToken ct = default)
        {
            return await ReadRegisterAsync(UMC1200Registers.ProcessStepNumber, ct);
        }

        /// <summary>
        /// 获取当前工艺已循环数（从0开始）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>已循环数</returns>
        public async Task<ushort> GetProcessLoopCurrentAsync(CancellationToken ct = default)
        {
            return await ReadRegisterAsync(UMC1200Registers.ProcessLoopCurrent, ct);
        }

        /// <summary>
        /// 获取当前工艺总循环数
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>总循环数</returns>
        public async Task<ushort> GetProcessLoopTotalAsync(CancellationToken ct = default)
        {
            return await ReadRegisterAsync(UMC1200Registers.ProcessLoopTotal, ct);
        }

        /// <summary>
        /// 获取当前工艺总时间（小时和分钟）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>（小时，分钟）元组</returns>
        public async Task<(ushort Hour, ushort Minute)> GetProcessTotalTimeAsync(CancellationToken ct = default)
        {
            var registers = await ReadRegistersAsync(UMC1200Registers.ProcessTotalTimeHour, 2, ct);
            return (registers[0], registers[1]);
        }

        /// <summary>
        /// 获取当前工艺已运行时间（小时和分钟）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>（小时，分钟）元组</returns>
        public async Task<(ushort Hour, ushort Minute)> GetProcessElapsedTimeAsync(CancellationToken ct = default)
        {
            var registers = await ReadRegistersAsync(UMC1200Registers.ProcessElapsedTimeHour, 2, ct);
            return (registers[0], registers[1]);
        }

        // ═══════════════════════════════════════════════════════════
        // 当前步信息读取（ReadOnly）
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 获取当前步总时间（小时和分钟）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>（小时，分钟）元组</returns>
        public async Task<(ushort Hour, ushort Minute)> GetCurrentStepTotalTimeAsync(CancellationToken ct = default)
        {
            var registers = await ReadRegistersAsync(UMC1200Registers.CurrentStepTotalTimeHour, 2, ct);
            return (registers[0], registers[1]);
        }

        /// <summary>
        /// 获取当前步已运行时间（小时和分钟）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>（小时，分钟）元组</returns>
        public async Task<(ushort Hour, ushort Minute)> GetCurrentStepElapsedTimeAsync(CancellationToken ct = default)
        {
            var registers = await ReadRegistersAsync(UMC1200Registers.CurrentStepElapsedTimeHour, 2, ct);
            return (registers[0], registers[1]);
        }

        // ═══════════════════════════════════════════════════════════
        // 连接信息读取（ReadOnly）
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 获取当前连接总时间（小时和分钟）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>（小时，分钟）元组</returns>
        public async Task<(ushort Hour, ushort Minute)> GetConnectionTotalTimeAsync(CancellationToken ct = default)
        {
            var registers = await ReadRegistersAsync(UMC1200Registers.ConnectionTotalTimeHour, 2, ct);
            return (registers[0], registers[1]);
        }

        /// <summary>
        /// 获取当前连接已运行时间（小时和分钟）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>（小时，分钟）元组</returns>
        public async Task<(ushort Hour, ushort Minute)> GetConnectionElapsedTimeAsync(CancellationToken ct = default)
        {
            var registers = await ReadRegistersAsync(UMC1200Registers.ConnectionElapsedTimeHour, 2, ct);
            return (registers[0], registers[1]);
        }

        // ═══════════════════════════════════════════════════════════
        // 系统时间读取（ReadOnly）
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 获取系统时间（完整日期时间）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>DateTime对象</returns>
        public async Task<DateTime> GetSystemTimeAsync(CancellationToken ct = default)
        {
            var registers = await ReadRegistersAsync(UMC1200Registers.SystemTimeYear, 6, ct);
            try
            {
                return new DateTime(
                    registers[0],  // 年
                    registers[1],  // 月
                    registers[2],  // 日
                    registers[3],  // 时
                    registers[4],  // 分
                    registers[5]); // 秒
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // 定值信息读取（ReadOnly）
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 获取定值已运行时间（小时和分钟）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>（小时，分钟）元组</returns>
        public async Task<(ushort Hour, ushort Minute)> GetSetValueElapsedTimeAsync(CancellationToken ct = default)
        {
            var registers = await ReadRegistersAsync(UMC1200Registers.SetValueElapsedTimeHour, 2, ct);
            return (registers[0], registers[1]);
        }

        /// <summary>
        /// 获取当前PID区域（从0开始）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>PID区域号</returns>
        public async Task<ushort> GetCurrentPIDZoneAsync(CancellationToken ct = default)
        {
            return await ReadRegisterAsync(UMC1200Registers.CurrentPIDZone, ct);
        }

        // ═══════════════════════════════════════════════════════════
        // 设定值读写（ReadWrite）
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 获取温度定值SV（设定值）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>温度设定值（单位：0.1℃）</returns>
        public async Task<short> GetTemperatureSVAsync(CancellationToken ct = default)
        {
            ushort raw = await ReadRegisterAsync(UMC1200Registers.TemperatureSV, ct);
            return (short)raw;
        }

        /// <summary>
        /// 设置温度定值SV（设定值）
        /// </summary>
        /// <param name="value">温度设定值（单位：0.1℃）</param>
        /// <param name="ct">取消令牌</param>
        public async Task SetTemperatureSVAsync(short value, CancellationToken ct = default)
        {
            await WriteRegisterAsync(UMC1200Registers.TemperatureSV, (ushort)value, ct);
        }

        /// <summary>
        /// 获取湿度定值SV（设定值）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>湿度设定值（单位：0.1%RH）</returns>
        public async Task<short> GetHumiditySVAsync(CancellationToken ct = default)
        {
            ushort raw = await ReadRegisterAsync(UMC1200Registers.HumiditySV, ct);
            return (short)raw;
        }

        /// <summary>
        /// 设置湿度定值SV（设定值）
        /// </summary>
        /// <param name="value">湿度设定值（单位：0.1%RH）</param>
        /// <param name="ct">取消令牌</param>
        public async Task SetHumiditySVAsync(short value, CancellationToken ct = default)
        {
            await WriteRegisterAsync(UMC1200Registers.HumiditySV, (ushort)value, ct);
        }

        /// <summary>
        /// 获取定值定时运行时间
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>运行时间（分钟）</returns>
        public async Task<ushort> GetSetValueRunTimeAsync(CancellationToken ct = default)
        {
            return await ReadRegisterAsync(UMC1200Registers.SetValueRunTime, ct);
        }

        /// <summary>
        /// 设置定值定时运行时间
        /// </summary>
        /// <param name="minutes">运行时间（分钟）</param>
        /// <param name="ct">取消令牌</param>
        public async Task SetSetValueRunTimeAsync(ushort minutes, CancellationToken ct = default)
        {
            await WriteRegisterAsync(UMC1200Registers.SetValueRunTime, minutes, ct);
        }

        // ═══════════════════════════════════════════════════════════
        // 控制命令（ReadWrite）
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 获取定值控制状态
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>控制状态值</returns>
        public async Task<UMC1200Registers.SetValueControlCommand> GetSetValueControlAsync(CancellationToken ct = default)
        {
            ushort raw = await ReadRegisterAsync(UMC1200Registers.SetValueControl, ct);
            return (UMC1200Registers.SetValueControlCommand)raw;
        }

        /// <summary>
        /// 设置定值控制命令
        /// </summary>
        /// <param name="command">控制命令</param>
        /// <param name="ct">取消令牌</param>
        public async Task SetSetValueControlAsync(UMC1200Registers.SetValueControlCommand command, CancellationToken ct = default)
        {
            await WriteRegisterAsync(UMC1200Registers.SetValueControl, (ushort)command, ct);
        }

        /// <summary>
        /// 启动定值控制
        /// </summary>
        /// <param name="ct">取消令牌</param>
        public async Task StartSetValueAsync(CancellationToken ct = default)
        {
            await SetSetValueControlAsync(UMC1200Registers.SetValueControlCommand.Start, ct);
        }

        /// <summary>
        /// 停止定值控制
        /// </summary>
        /// <param name="ct">取消令牌</param>
        public async Task StopSetValueAsync(CancellationToken ct = default)
        {
            await SetSetValueControlAsync(UMC1200Registers.SetValueControlCommand.Stop, ct);
        }

        /// <summary>
        /// 保持定值控制
        /// </summary>
        /// <param name="ct">取消令牌</param>
        public async Task HoldSetValueAsync(CancellationToken ct = default)
        {
            await SetSetValueControlAsync(UMC1200Registers.SetValueControlCommand.Hold, ct);
        }

        /// <summary>
        /// 获取程序控制状态
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>控制状态值</returns>
        public async Task<UMC1200Registers.ProgramControlCommand> GetProgramControlAsync(CancellationToken ct = default)
        {
            ushort raw = await ReadRegisterAsync(UMC1200Registers.ProgramControl, ct);
            return (UMC1200Registers.ProgramControlCommand)raw;
        }

        /// <summary>
        /// 设置程序控制命令
        /// </summary>
        /// <param name="command">控制命令</param>
        /// <param name="ct">取消令牌</param>
        public async Task SetProgramControlAsync(UMC1200Registers.ProgramControlCommand command, CancellationToken ct = default)
        {
            await WriteRegisterAsync(UMC1200Registers.ProgramControl, (ushort)command, ct);
        }

        /// <summary>
        /// 启动程序控制
        /// </summary>
        /// <param name="ct">取消令牌</param>
        public async Task StartProgramAsync(CancellationToken ct = default)
        {
            await SetProgramControlAsync(UMC1200Registers.ProgramControlCommand.Start, ct);
        }

        /// <summary>
        /// 停止程序控制
        /// </summary>
        /// <param name="ct">取消令牌</param>
        public async Task StopProgramAsync(CancellationToken ct = default)
        {
            await SetProgramControlAsync(UMC1200Registers.ProgramControlCommand.Stop, ct);
        }

        /// <summary>
        /// 保持程序控制
        /// </summary>
        /// <param name="ct">取消令牌</param>
        public async Task HoldProgramAsync(CancellationToken ct = default)
        {
            await SetProgramControlAsync(UMC1200Registers.ProgramControlCommand.Hold, ct);
        }

        /// <summary>
        /// 跳步程序控制
        /// </summary>
        /// <param name="ct">取消令牌</param>
        public async Task SkipProgramStepAsync(CancellationToken ct = default)
        {
            await SetProgramControlAsync(UMC1200Registers.ProgramControlCommand.Skip, ct);
        }

        // ═══════════════════════════════════════════════════════════
        // 当前设定值读取（ReadOnly）
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 获取当前温度SV（设定值）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>当前温度设定值（单位：0.1℃）</returns>
        public async Task<short> GetCurrentTemperatureSVAsync(CancellationToken ct = default)
        {
            ushort raw = await ReadRegisterAsync(UMC1200Registers.CurrentTemperatureSV, ct);
            return (short)raw;
        }

        /// <summary>
        /// 获取当前湿度SV（设定值）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>当前湿度设定值（单位：0.1%RH）</returns>
        public async Task<short> GetCurrentHumiditySVAsync(CancellationToken ct = default)
        {
            ushort raw = await ReadRegisterAsync(UMC1200Registers.CurrentHumiditySV, ct);
            return (short)raw;
        }

        /// <summary>
        /// 获取当前湿球PV（过程值）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>湿球温度值（单位：0.1℃）</returns>
        public async Task<short> GetWetBulbPVAsync(CancellationToken ct = default)
        {
            ushort raw = await ReadRegisterAsync(UMC1200Registers.WetBulbPV, ct);
            return (short)raw;
        }

        /// <summary>
        /// 获取当前湿球SV（设定值）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>湿球设定值（单位：0.1℃）</returns>
        public async Task<short> GetWetBulbSVAsync(CancellationToken ct = default)
        {
            ushort raw = await ReadRegisterAsync(UMC1200Registers.WetBulbSV, ct);
            return (short)raw;
        }

        // ═══════════════════════════════════════════════════════════
        // 斜率设置（ReadWrite）
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 获取定值温度斜率值
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>温度斜率值</returns>
        public async Task<ushort> GetTemperatureSlopeAsync(CancellationToken ct = default)
        {
            return await ReadRegisterAsync(UMC1200Registers.TemperatureSlope, ct);
        }

        /// <summary>
        /// 设置定值温度斜率值
        /// </summary>
        /// <param name="value">温度斜率值</param>
        /// <param name="ct">取消令牌</param>
        public async Task SetTemperatureSlopeAsync(ushort value, CancellationToken ct = default)
        {
            await WriteRegisterAsync(UMC1200Registers.TemperatureSlope, value, ct);
        }

        /// <summary>
        /// 获取定值湿度斜率值
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>湿度斜率值</returns>
        public async Task<ushort> GetHumiditySlopeAsync(CancellationToken ct = default)
        {
            return await ReadRegisterAsync(UMC1200Registers.HumiditySlope, ct);
        }

        /// <summary>
        /// 设置定值湿度斜率值
        /// </summary>
        /// <param name="value">湿度斜率值</param>
        /// <param name="ct">取消令牌</param>
        public async Task SetHumiditySlopeAsync(ushort value, CancellationToken ct = default)
        {
            await WriteRegisterAsync(UMC1200Registers.HumiditySlope, value, ct);
        }

        // ═══════════════════════════════════════════════════════════
        // 冷输出读取（ReadOnly）
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 获取温度冷MV（操纵值）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>温度冷MV值</returns>
        public async Task<short> GetTemperatureColdMVAsync(CancellationToken ct = default)
        {
            ushort raw = await ReadRegisterAsync(UMC1200Registers.TemperatureColdMV, ct);
            return (short)raw;
        }

        /// <summary>
        /// 获取湿度冷MV（操纵值）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>湿度冷MV值</returns>
        public async Task<short> GetHumidityColdMVAsync(CancellationToken ct = default)
        {
            ushort raw = await ReadRegisterAsync(UMC1200Registers.HumidityColdMV, ct);
            return (short)raw;
        }

        // ═══════════════════════════════════════════════════════════
        // 批量读取方法
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 获取所有测量值（温度PV、湿度PV、温度MV、湿度MV）
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>（温度PV，湿度PV，温度MV，湿度MV）元组</returns>
        public async Task<(short TempPV, short HumiPV, short TempMV, short HumiMV)> GetAllMeasurementsAsync(CancellationToken ct = default)
        {
            var registers = await ReadRegistersAsync(UMC1200Registers.TemperaturePV, 4, ct);
            return ((short)registers[0], (short)registers[1], (short)registers[2], (short)registers[3]);
        }

        /// <summary>
        /// 获取工艺状态摘要
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>工艺状态信息</returns>
        public async Task<ProcessStatus> GetProcessStatusAsync(CancellationToken ct = default)
        {
            var registers = await ReadRegistersAsync(UMC1200Registers.ProcessStepCount, 18, ct);
            return new ProcessStatus
            {
                StepCount = registers[0],
                StepNumber = registers[1],
                LoopCurrent = registers[8],
                LoopTotal = registers[9],
                TotalTimeHour = registers[10],
                TotalTimeMinute = registers[11],
                ElapsedTimeHour = registers[12],
                ElapsedTimeMinute = registers[13],
                CurrentStepTotalHour = registers[14],
                CurrentStepTotalMinute = registers[15],
                CurrentStepElapsedHour = registers[16],
                CurrentStepElapsedMinute = registers[17]
            };
        }
    }

    /// <summary>
    /// 工艺状态信息
    /// </summary>
    public class ProcessStatus
    {
        /// <summary>当前工艺步数</summary>
        public ushort StepCount { get; set; }
        /// <summary>当前工艺步号</summary>
        public ushort StepNumber { get; set; }
        /// <summary>当前工艺已循环数</summary>
        public ushort LoopCurrent { get; set; }
        /// <summary>当前工艺总循环数</summary>
        public ushort LoopTotal { get; set; }
        /// <summary>当前工艺总时间（小时）</summary>
        public ushort TotalTimeHour { get; set; }
        /// <summary>当前工艺总时间（分钟）</summary>
        public ushort TotalTimeMinute { get; set; }
        /// <summary>当前工艺已运行时间（小时）</summary>
        public ushort ElapsedTimeHour { get; set; }
        /// <summary>当前工艺已运行时间（分钟）</summary>
        public ushort ElapsedTimeMinute { get; set; }
        /// <summary>当前步总时间（小时）</summary>
        public ushort CurrentStepTotalHour { get; set; }
        /// <summary>当前步总时间（分钟）</summary>
        public ushort CurrentStepTotalMinute { get; set; }
        /// <summary>当前步已运行时间（小时）</summary>
        public ushort CurrentStepElapsedHour { get; set; }
        /// <summary>当前步已运行时间（分钟）</summary>
        public ushort CurrentStepElapsedMinute { get; set; }
    }
}
