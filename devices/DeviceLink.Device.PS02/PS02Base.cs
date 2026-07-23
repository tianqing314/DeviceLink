using DeviceLink.DataLink;
using DeviceLink.DeviceBase;
using DeviceLink.Protocol;
using DeviceLink.Transport;
using System.IO.Ports;
using System.Net;
using System.Text;

namespace DeviceLink.Device.PS02;

/// <summary>
/// PS02 压力变送器设备类。
///
/// 支持 CPPI V3 协议（通过转换板）+ Modbus RTU 协议。
/// 通信链路：PC → CPPI V3 → 转换板 → Modbus RTU → PS02 传感器。
///
/// 支持的功能码：
///   F03 (0x03): 标准读保持寄存器
///   F40 (0x28): 自定义读寄存器
///   F41 (0x29): 自定义写寄存器
///
/// 使用示例：
///   var ps02 = new PS02("COM3");
///   await ps02.OpenAsync();
///   var pressure = await ps02.GetPressureAsync();
///   var serialNumber = await ps02.GetSerialNumberAsync();
/// </summary>
public class PS02Base : DeviceBase.DeviceBase
{
    private readonly ModbusRtuCodec _codec;
    private readonly byte _slaveAddress;
    private readonly CpplV3FrameStrategy _cppiV3FrameStrategy;

    /// <summary>
    /// 转接板专用帧策略（目标地址 0x000123）
    /// </summary>
    private readonly CpplV3FrameStrategy _converterFrameStrategy;

    #region 构造函数

    /// <summary>
    /// 构造函数（串口通讯，默认 CPPI V3 帧策略）
    /// </summary>
    /// <param name="serialPortName">
    /// 串口号（如 COM3）
    /// </param>
    /// <param name="baudRate">
    /// 波特率（默认9600）
    /// </param>
    /// <param name="dataBits">
    /// 数据位（默认8）
    /// </param>
    /// <param name="stopBits">
    /// 停止位（默认1）
    /// </param>
    /// <param name="parity">
    /// 校验位（默认None）
    /// </param>
    /// <param name="slaveAddress">
    /// Modbus从站地址（默认1）
    /// </param>
    public PS02Base(string serialPortName, int baudRate = 9600, int dataBits = 8,
        StopBits stopBits = StopBits.One, Parity parity = Parity.None, byte slaveAddress = 1)
        : base(serialPortName, baudRate, dataBits, stopBits, parity,
            new ModbusRtuCodec(slaveAddress),
            new CpplV3FrameStrategy())
    {
        _codec = (ModbusRtuCodec)Codec;
        _slaveAddress = slaveAddress;
        _cppiV3FrameStrategy = new CpplV3FrameStrategy();
    }

    /// <summary>
    /// 构造函数（串口通讯，使用默认配置 9600,8,N,1）
    /// </summary>
    /// <param name="serialPortName">
    /// 串口号（如 COM3）
    /// </param>
    /// <param name="slaveAddress">
    /// Modbus从站地址（默认1）
    /// </param>
    public PS02Base(string serialPortName, byte slaveAddress = 1)
        : base(serialPortName, new ModbusRtuCodec(slaveAddress), new CpplV3FrameStrategy())
    {
        _codec = (ModbusRtuCodec)Codec;
        _slaveAddress = slaveAddress;
        _cppiV3FrameStrategy = new CpplV3FrameStrategy();
    }

    /// <summary>
    /// 构造函数（TCP通讯）
    /// </summary>
    /// <param name="ipAddress">
    /// IP地址
    /// </param>
    /// <param name="port">
    /// 端口号
    /// </param>
    /// <param name="slaveAddress">
    /// Modbus从站地址（默认1）
    /// </param>
    public PS02Base(IPAddress ipAddress, int port, byte slaveAddress = 1)
        : base(ipAddress, port, new ModbusRtuCodec(slaveAddress), new CpplV3FrameStrategy())
    {
        _codec = (ModbusRtuCodec)Codec;
        _slaveAddress = slaveAddress;
        _cppiV3FrameStrategy = new CpplV3FrameStrategy();
    }

    /// <summary>
    /// 构造函数（通信设置实例）
    /// </summary>
    /// <param name="settings">
    /// 通信配置
    /// </param>
    /// <param name="slaveAddress">
    /// Modbus从站地址（默认1）
    /// </param>
    public PS02Base(DeviceCommSettings settings, byte slaveAddress = 1)
        : base(settings, new ModbusRtuCodec(slaveAddress), new CpplV3FrameStrategy())
    {
        _codec = (ModbusRtuCodec)Codec;
        _slaveAddress = slaveAddress;
        _cppiV3FrameStrategy = new CpplV3FrameStrategy();

        // 转接板专用帧策略：目标地址 0x000123，源地址 0x112236，流水号从 0x01 开始
        _converterFrameStrategy = new CpplV3FrameStrategy(
            targetAddress: new byte[] { 0x23, 0x01, 0x00 },
            sourceAddress: new byte[] { 0x36, 0x22, 0x11 },
            initialSequenceNumber: 0x01);
    }

    /// <summary>
    /// 配置构造函数默认信息
    /// </summary>
    protected override void ConstructDefaultInfo()
    {
        base.ConstructDefaultInfo();
        Name = "PS02";
    }

    #endregion 构造函数

    // ═══════════════════════════════════════════════════════════
    // 重写发送方法 - 添加 CPPI V3 帧日志
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 发送命令并接收响应（重写基类方法，添加 CPPI V3 帧日志记录）
    /// </summary>
    protected override async Task<byte[]> SendAsync(
        Command command,
        CancellationToken ct = default)
    {
        // 编码命令
        var request = Codec.Encode(command);
        var commandString = Encoding.ASCII.GetString(request);

        // 记录发送日志（Modbus RTU 负载）
        CommunicationLogger.LogSend(Name, command.Id, command.Kind.ToString(),
            commandString, request);

        // 构建 CPPI V3 帧并记录（用于调试对比）
        try
        {
            var cppiV3Frame = _cppiV3FrameStrategy.BuildFrame(request);
            CommunicationLogger.LogRaw(Name, ">>> CPPI V3 发送帧", cppiV3Frame);
        }
        catch (Exception ex)
        {
            CommunicationLogger.LogError(Name, "构建 CPPI V3 帧失败", ex);
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        byte[] response;
        try
        {
            response = await Session.SendAndReceiveAsync(request, ct);
        }
        catch (Exception ex)
        {
            CommunicationLogger.LogError(Name, $"发送命令 [{command.Id}] 失败", ex);
            throw;
        }
        finally
        {
            sw.Stop();
        }

        // 记录接收日志
        var responseText = Codec.DecodeText(response);
        CommunicationLogger.LogReceive(Name, sw.ElapsedMilliseconds, response, responseText);

        // 检查设备错误
        if (Codec.IsErrorResponse(response, out var errMsg))
        {
            CommunicationLogger.LogError(Name, $"设备返回错误: {errMsg}");
            throw new DeviceException($"设备错误: {errMsg}");
        }

        return response;
    }

    /// <summary>
    /// 发送单向命令（重写基类方法，添加 CPPI V3 帧日志记录）
    /// </summary>
    protected override async Task SendNonQueryAsync(
        Command command,
        CancellationToken ct = default)
    {
        // 编码命令
        var request = Codec.Encode(command);
        var commandString = Encoding.ASCII.GetString(request);

        // 记录发送日志（Modbus RTU 负载）
        CommunicationLogger.LogSend(Name, command.Id, command.Kind.ToString(),
            commandString, request);

        // 构建 CPPI V3 帧并记录（用于调试对比）
        try
        {
            var cppiV3Frame = _cppiV3FrameStrategy.BuildFrame(request);
            CommunicationLogger.LogRaw(Name, ">>> CPPI V3 发送帧", cppiV3Frame);
        }
        catch (Exception ex)
        {
            CommunicationLogger.LogError(Name, "构建 CPPI V3 帧失败", ex);
        }

        try
        {
            await Session.SendOnlyAsync(request, ct);
        }
        catch (Exception ex)
        {
            CommunicationLogger.LogError(Name, $"单向发送命令 [{command.Id}] 失败", ex);
            throw;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 通用读写方法
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 通过 F03 读取单个保持寄存器值
    /// </summary>
    /// <param name="registerAddress">
    /// 寄存器地址
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 寄存器值
    /// </returns>
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
    /// 通过 F03 读取多个连续寄存器值
    /// </summary>
    /// <param name="startAddress">
    /// 起始寄存器地址
    /// </param>
    /// <param name="count">
    /// 寄存器数量
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 寄存器值数组
    /// </returns>
    public async Task<ushort[]> ReadRegistersAsync(ushort startAddress, ushort count, CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read($"3.{startAddress}.{count}"),
            raw => _codec.ExtractRegisters(raw),
            ct);
    }

    /// <summary>
    /// 通过 F40 读取寄存器（PS02自定义功能码）
    /// </summary>
    /// <param name="registerAddress">
    /// 寄存器地址
    /// </param>
    /// <param name="count">
    /// 寄存器数量
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 原始响应数据（不含CRC）
    /// </returns>
    public async Task<byte[]> ReadRegistersF40Async(ushort registerAddress, ushort count, CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read($"40.{registerAddress}.{count}"),
            raw => raw,
            ct);
    }

    /// <summary>
    /// 通过 F41 写入寄存器（PS02自定义功能码）
    /// </summary>
    /// <param name="registerAddress">
    /// 起始寄存器地址
    /// </param>
    /// <param name="data">
    /// 写入的原始数据字节
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task WriteRegistersF41Async(ushort registerAddress, byte[] data, CancellationToken ct = default)
    {
        ushort count = (ushort)(data.Length / 2);
        if (data.Length % 2 != 0)
        {
            count++;
        }

        var command = Command.Write($"41.{registerAddress}", count.ToString());
        command.Data = data;
        await SendNonQueryAsync(command, ct);
    }

    // ═══════════════════════════════════════════════════════════
    // 压力测量（只读）
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 读取实时压力值（F03, 寄存器 0x0002-0x0003）
    /// 返回 float32 大端浮点数，单位 kPa
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 压力值（kPa），无效时返回 NaN
    /// </returns>
    public async Task<double> GetPressureAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("3.2.2"),
            raw => ParseFloat32BigEndian(raw, 3),
            ct);
    }

    /// <summary>
    /// 通过 F40 读取实时压力值（寄存器 0x0002-0x0003）
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 压力值（kPa），无效时返回 NaN
    /// </returns>
    public async Task<double> GetPressureF40Async(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("40.2.2"),
            raw => ParseFloat32BigEndian(raw, 4),
            ct);
    }

    // ═══════════════════════════════════════════════════════════
    // 设备信息（只读）
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 读取序列号（F40, 寄存器 0x51A0, 6个寄存器=12字节）
    /// 序列号为 ASCII 字符串，如 "C1025D010001"
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 序列号字符串
    /// </returns>
    public async Task<string> GetSerialNumberAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("40.20896.6"),
            raw => ExtractAsciiString(raw, 4, 12),
            ct);
    }

    /// <summary>
    /// 读取精度（F40, 寄存器 0x5136）
    /// 返回值 ×100 为百分比，如 10 表示 0.1%
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 精度值（×100）
    /// </returns>
    public async Task<ushort> GetPrecisionAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("40.20790.1"),
            raw =>
            {
                var normalized = NormalizeF40Response(raw);
                var registers = _codec.ExtractRegisters(normalized);
                return registers.Length > 0 ? registers[0] : (ushort)0;
            },
            ct);
    }

    /// <summary>
    /// 读取压力类型（F40, 寄存器 0x5137）
    /// 0=表压, 2=绝压, 3=差压
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 压力类型
    /// </returns>
    public async Task<PressureType> GetPressureTypeAsync(CancellationToken ct = default)
    {
        ushort raw = await SendForResultAsync(
            Command.Read("40.20791.1"),
            raw =>
            {
                var normalized = NormalizeF40Response(raw);
                var registers = _codec.ExtractRegisters(normalized);
                return registers.Length > 0 ? registers[0] : (ushort)0;
            },
            ct);
        return (PressureType)raw;
    }

    /// <summary>
    /// 设置压力类型（F41, 寄存器 0x5137）
    /// 0=表压(Gauge), 2=绝压(Absolute), 3=差压(Differential)
    /// </summary>
    /// <param name="pressureType">
    /// 压力类型
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task SetPressureTypeAsync(PressureType pressureType, CancellationToken ct = default)
    {
        // 寄存器 0x5137，2字节大端模式
        var data = new byte[2];
        data[0] = (byte)((ushort)pressureType >> 8);    // 高字节
        data[1] = (byte)((ushort)pressureType & 0xFF);  // 低字节
        await WriteRegistersF41Async(PS02Registers.PressureType, data, ct);
    }

    /// <summary>
    /// 读取迁移量程（F40, 寄存器 0x513E-0x5141）
    /// 返回下限和上限，均为 float32 小端浮点数，单位 kPa
    /// 返回值格式：[CPPI错误码0x00][从站地址][功能码0x28][字节数0x08][下限4字节][上限4字节]
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 量程信息
    /// </returns>
    public async Task<PressureRange> GetMigrationRangeAsync(CancellationToken ct = default)
    {
        const int maxRetries = 3;
        const int retryDelayMs = 500;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await SendForResultAsync(
                    Command.Read("40.20798.4"),
                    raw =>
                    {
                        // 移除转接板添加的 0x00 前缀（CPPI 错误码）
                        var normalized = NormalizeF40Response(raw);

                        // 最小长度：地址(1) + 功能码(1) + 字节数(1) + 数据(8) = 11字节
                        if (normalized == null || normalized.Length < 11)
                        {
                            var actualLen = normalized?.Length ?? 0;
                            CommunicationLogger.LogInfo(Name,
                                $"GetMigrationRangeAsync 响应无效: 长度={actualLen}（期望≥11），原始数据={BitConverter.ToString(raw)}");
                            throw new DeviceException($"F40响应无效: 长度={actualLen}，期望≥11");
                        }

                        // 验证功能码：F40 响应功能码应为 0x28
                        if (normalized[1] != 0x28)
                        {
                            CommunicationLogger.LogInfo(Name,
                                $"GetMigrationRangeAsync 功能码错误: 0x{normalized[1]:X2}（期望0x28），原始数据={BitConverter.ToString(raw)}");
                            throw new DeviceException($"F40功能码错误: 0x{normalized[1]:X2}，期望0x28");
                        }

                        // 偏移量：地址(0) + 功能码(1) + 字节数(2) = 数据从偏移3开始
                        return new PressureRange
                        {
                            Lower = ParseFloat32LittleEndian(normalized, 3),
                            Upper = ParseFloat32LittleEndian(normalized, 7)
                        };
                    },
                    ct);
            }
            catch (FrameTimeoutException) when (attempt < maxRetries)
            {
                CommunicationLogger.LogInfo(Name, $"GetMigrationRangeAsync 超时，第 {attempt}/{maxRetries} 次重试...");
                await Task.Delay(retryDelayMs, ct);
            }
            catch (DeviceException) when (attempt < maxRetries)
            {
                CommunicationLogger.LogInfo(Name, $"GetMigrationRangeAsync 响应异常，第 {attempt}/{maxRetries} 次重试...");
                await Task.Delay(retryDelayMs, ct);
            }
        }

        // 最后一次尝试，让异常自然抛出
        return await SendForResultAsync(
            Command.Read("40.20798.4"),
            raw =>
            {
                var normalized = NormalizeF40Response(raw);
                if (normalized == null || normalized.Length < 11)
                {
                    throw new DeviceException($"F40响应无效: 长度={normalized?.Length ?? 0}，期望≥11");
                }
                if (normalized[1] != 0x28)
                {
                    throw new DeviceException($"F40功能码错误: 0x{normalized[1]:X2}，期望0x28");
                }
                return new PressureRange
                {
                    Lower = ParseFloat32LittleEndian(normalized, 3),
                    Upper = ParseFloat32LittleEndian(normalized, 7)
                };
            },
            ct);
    }

    /// <summary>
    /// 读取固件版本（F40, 寄存器 0x8010, 多个寄存器）
    /// 返回 ASCII 字符串，如 "A20A V00.00.00.01"
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 固件版本字符串
    /// </returns>
    public async Task<string> GetFirmwareVersionAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("40.32784.9"),
            raw => ExtractAsciiString(raw, 4, 18),
            ct);
    }

    /// <summary>
    /// 读取硬件版本（F40, 寄存器 0x801A, 多个寄存器）
    /// 返回 ASCII 字符串，如 "A20A V0.1"
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 硬件版本字符串
    /// </returns>
    public async Task<string> GetHardwareVersionAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("40.32794.5"),
            raw => ExtractAsciiString(raw, 4, 10),
            ct);
    }

    /// <summary>
    /// 读取模块类型（F40, 寄存器 0x8020）
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 模块类型值
    /// </returns>
    public async Task<ushort> GetModuleTypeAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("40.32800.1"),
            raw =>
            {
                var normalized = NormalizeF40Response(raw);
                var registers = _codec.ExtractRegisters(normalized);
                return registers.Length > 0 ? registers[0] : (ushort)0;
            },
            ct);
    }

    /// <summary>
    /// 读取设备标识信息（序列号 + 固件版本 + 硬件版本）
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 设备标识
    /// </returns>
    public async Task<DeviceIdentification> GetIdentificationAsync(CancellationToken ct = default)
    {
        var sn = await GetSerialNumberAsync(ct);
        var fw = await GetFirmwareVersionAsync(ct);
        var hw = await GetHardwareVersionAsync(ct);
        return new DeviceIdentification
        {
            SerialNumber = sn,
            FirmwareVersion = fw,
            HardwareVersion = hw
        };
    }

    // ═══════════════════════════════════════════════════════════
    // 量程迁移（读写）
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 写入迁移量程（F41, 寄存器 0x513E-0x5142）
    /// 量程下限和上限均为 float32 小端浮点数，单位 kPa
    /// 寄存器布局：下限(2) + 上限(2) + 使能标志(1) = 5个寄存器
    /// </summary>
    /// <param name="lower">
    /// 量程下限（kPa）
    /// </param>
    /// <param name="upper">
    /// 量程上限（kPa）
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task SetMigrationRangeAsync(float lower, float upper, CancellationToken ct = default)
    {
        // 5个寄存器 = 10字节：下限(4) + 上限(4) + 使能标志(2)
        var data = new byte[10];
        WriteFloat32LittleEndian(data, 0, lower);   // 下限，小端模式
        WriteFloat32LittleEndian(data, 4, upper);   // 上限，小端模式
        data[8] = 0x00;                           // 使能标志高字节
        data[9] = 0x01;                           // 使能标志低字节（0x0001 = 启用迁移）

        await WriteRegistersF41Async(PS02Registers.MigrationRangeLower, data, ct);
    }

    // ═══════════════════════════════════════════════════════════
    // 序列号写入
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 写入序列号（F41, 寄存器 0x51A0, 6个寄存器=12字节）
    /// 序列号为 ASCII 字符串，如 "C1025D010001"
    /// </summary>
    /// <param name="serialNumber">
    /// 序列号字符串（12个字符）
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task SetSerialNumberAsync(string serialNumber, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(serialNumber))
        {
            throw new ArgumentException("序列号不能为空", nameof(serialNumber));
        }

        byte[] data = Encoding.ASCII.GetBytes(serialNumber);
        await WriteRegistersF41Async(PS02Registers.SerialNumber, data, ct);
    }

    // ═══════════════════════════════════════════════════════════
    // 调试/配置指令
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 配置 OWI 通信使能（F41, 寄存器 0x8000）
    /// </summary>
    /// <param name="enable">
    /// true=进入OWI通信模式, false=回到DAC工作模式
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task SetOwiEnableAsync(bool enable, CancellationToken ct = default)
    {
        var data = new byte[2];
        data[0] = 0x00;
        data[1] = enable ? (byte)0x01 : (byte)0x00;
        await WriteRegistersF41Async(PS02Registers.OwiEnable, data, ct);
    }

    /// <summary>
    /// 配置调试模式（F41, 寄存器 0x8002）
    /// </summary>
    /// <param name="enable">
    /// true=进入调试模式, false=回到变送输出模式
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task SetDebugModeAsync(bool enable, CancellationToken ct = default)
    {
        var data = new byte[2];
        data[0] = 0x00;
        data[1] = enable ? (byte)0x01 : (byte)0x00;
        await WriteRegistersF41Async(PS02Registers.DebugMode, data, ct);
    }

    /// <summary>
    /// 设置调试模式 DAC 输出值（F41, 寄存器 0x8003）
    /// </summary>
    /// <param name="value">
    /// DAC 输出值
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task SetDebugDacValueAsync(ushort value, CancellationToken ct = default)
    {
        var data = new byte[2];
        data[0] = (byte)(value >> 8);
        data[1] = (byte)(value & 0xFF);
        await WriteRegistersF41Async(PS02Registers.DebugDacValue, data, ct);
    }

    /// <summary>
    /// 配置恒流源0（F41, 寄存器 0x8006）
    /// </summary>
    /// <param name="config">
    /// 配置值（0x0000-0x000F）
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task SetCurrentSource0Async(ushort config, CancellationToken ct = default)
    {
        var data = new byte[2];
        data[0] = (byte)(config >> 8);
        data[1] = (byte)(config & 0xFF);
        await WriteRegistersF41Async(PS02Registers.CurrentSource0, data, ct);
    }

    /// <summary>
    /// 配置恒流源1（F41, 寄存器 0x8007）
    /// </summary>
    /// <param name="config">
    /// 配置值（0x0000-0x000F）
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task SetCurrentSource1Async(ushort config, CancellationToken ct = default)
    {
        var data = new byte[2];
        data[0] = (byte)(config >> 8);
        data[1] = (byte)(config & 0xFF);
        await WriteRegistersF41Async(PS02Registers.CurrentSource1, data, ct);
    }

    /// <summary>
    /// 配置 ADC 采样率（F41, 寄存器 0x800A）
    /// </summary>
    /// <param name="config">
    /// 配置值（0x0000-0x0010）
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task SetAdcSampleRateAsync(ushort config, CancellationToken ct = default)
    {
        var data = new byte[2];
        data[0] = (byte)(config >> 8);
        data[1] = (byte)(config & 0xFF);
        await WriteRegistersF41Async(PS02Registers.AdcSampleRate, data, ct);
    }

    #region 转接板指令

    // ═══════════════════════════════════════════════════════════
    // 转接板 CPPI V3 指令
    // ═══════════════════════════════════════════════════════════
    //
    // 通信参数：
    //   PC（主机）源地址：0x112236 (36 22 11)
    //   转接板（从机）目标地址：0x000123 (23 01 00)
    //
    // 功能码定义：
    //   0x0106 - 读取设备固件版本（返回 ASCII 字符串）
    //   0x0108 - 读取设备硬件版本（返回 ASCII 字符串）
    //   0x0210 - 设定输出项目（参数：项目代号 + 输出值类型）
    //   0x0300 - 扫描从设备（启动扫描，参数：控制字节 0x01=开始）
    //   0x0301 - 获取扫描结果（返回接口类型：0=未连接, 1=OWI电流, 2=OWI电压, 3=485）
    //
    // 错误码定义：
    //   0x00 - 无错误
    //   100 - CRC 校验错误
    //   101 - 无此指令
    //   102 - 当前状态不支持此操作
    //   103 - 密码错误
    //   104 - 参数格式错误
    //   105 - 参数超范围
    //   106 - 执行错误
    //   107 - 参数错误

    /// <summary>
    /// 扫描从设备（功能码 0x0300）
    /// 返回下游设备的接口类型
    /// </summary>
    /// <param name="scanType">
    /// 扫描类型：0x00=标准扫描, 0x01=详细扫描
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 设备接口类型
    /// </returns>
    public async Task<DeviceInterfaceType> ScanDeviceAsync(byte scanType = 0x00, CancellationToken ct = default)
    {
        // 构建 CPPI V3 帧：功能码 0x0300，数据 [scanType]
        var frame = _converterFrameStrategy.BuildRawFrame(0x0300, new byte[] { scanType });
        CommunicationLogger.LogRaw(Name, $">>> 转接板指令: 扫描从设备 (类型:{scanType})", frame);

        var response = await SendRawFrameAsync(frame, ct);

        // 使用帧策略解析 CPPI V3 帧
        if (!_converterFrameStrategy.TryParseRawFrame(response, out _, out byte[] frameData))
        {
            throw new DeviceException($"转接板响应帧解析失败，数据: {BitConverter.ToString(response)}");
        }

        // 至少需要 1 字节错误码
        if (frameData.Length < 1)
        {
            throw new DeviceException("转接板响应数据为空");
        }

        // 检查错误码
        byte errorCode = frameData[0];
        if (errorCode != 0)
        {
            var errorName = Enum.IsDefined(typeof(ConverterErrorCode), errorCode)
                ? ((ConverterErrorCode)errorCode).ToString()
                : $"未知错误({errorCode})";
            throw new DeviceException($"转接板返回错误: {errorName}");
        }

        // 根据文档，0x0300扫描从设备的返回参数是"无"
        // 如果有接口类型数据（frameData > 1 字节），返回接口类型（兼容旧固件）
        if (frameData.Length > 1)
        {
            return (DeviceInterfaceType)frameData[1];
        }

        // 如果只有错误码（frameData == 1 字节），返回 NotConnected
        // 这是正常行为，因为0x0300不返回扫描结果
        CommunicationLogger.LogInfo(Name, "扫描指令已发送，需通过0x0301获取扫描结果");
        return DeviceInterfaceType.NotConnected;
    }

    /// <summary>
    /// 读取转接板固件版本（功能码 0x0106）
    /// 返回 ASCII 字符串，如 "A20-98 V00.00.00.07"
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 固件版本字符串
    /// </returns>
    public async Task<string> GetConverterFirmwareVersionAsync(CancellationToken ct = default)
    {
        // 构建 CPPI V3 帧：功能码 0x0106，无数据
        var frame = _converterFrameStrategy.BuildRawFrame(0x0106);
        CommunicationLogger.LogRaw(Name, ">>> 转接板指令: 读取固件版本", frame);

        var response = await SendRawFrameAsync(frame, ct);
        var data = ParseConverterResponse(response, 0);

        // 解析 ASCII 字符串（跳过错误码）
        return Encoding.ASCII.GetString(data).TrimEnd('\0');
    }

    /// <summary>
    /// 读取转接板硬件版本（功能码 0x0108）
    /// 返回 ASCII 字符串，如 "A20-98 V00.01"
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 硬件版本字符串
    /// </returns>
    public async Task<string> GetConverterHardwareVersionAsync(CancellationToken ct = default)
    {
        // 构建 CPPI V3 帧：功能码 0x0108，无数据
        var frame = _converterFrameStrategy.BuildRawFrame(0x0108);
        CommunicationLogger.LogRaw(Name, ">>> 转接板指令: 读取硬件版本", frame);

        var response = await SendRawFrameAsync(frame, ct);
        var data = ParseConverterResponse(response, 0);

        // 解析 ASCII 字符串（跳过错误码）
        return Encoding.ASCII.GetString(data).TrimEnd('\0');
    }

    /// <summary>
    /// 设定输出项目（功能码 0x0210）
    /// 控制转接板的电流/电压输出
    /// </summary>
    /// <param name="project">
    /// 输出项目代号
    /// </param>
    /// <param name="deviceCategory">
    /// 测量设备类别：0=测量OWI模块输出，1=测量标准板输出
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task SetOutputProjectAsync(OutputProject project, MeasurementDeviceCategory deviceCategory, CancellationToken ct = default)
    {
        // 构建 CPPI V3 帧：功能码 0x0210，数据 [项目代号, 测量设备类别]
        var frame = _converterFrameStrategy.BuildRawFrame(0x0210, new byte[] { (byte)project, (byte)deviceCategory });
        CommunicationLogger.LogRaw(Name, $">>> 转接板指令: 设定输出项目 ({project}, {deviceCategory})", frame);

        var response = await SendRawFrameAsync(frame, ct);
        ParseConverterResponse(response, 0); // 验证无错误
    }

    /// <summary>
    /// 关闭所有输出（功能码 0x0210 的便捷方法）
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task DisableAllOutputAsync(CancellationToken ct = default)
    {
        await SetOutputProjectAsync(OutputProject.Off, MeasurementDeviceCategory.OwiModule, ct);
    }

    /// <summary>
    /// 读取转接板内部参数（功能码 0x0301）
    /// 返回参数值（1 字节）
    /// </summary>
    /// <param name="parameterIndex">
    /// 参数索引（0-4，对应流水号 0x07-0x0B）
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 参数值
    /// </returns>
    public async Task<byte> ReadConverterParameterAsync(byte parameterIndex, CancellationToken ct = default)
    {
        // 构建 CPPI V3 帧：功能码 0x0301，无数据
        var frame = _converterFrameStrategy.BuildRawFrame(0x0301);
        CommunicationLogger.LogRaw(Name, $">>> 转接板指令: 读取参数 #{parameterIndex}", frame);

        var response = await SendRawFrameAsync(frame, ct);
        var data = ParseConverterResponse(response, 1);

        // 返回值在错误码之后的第一个字节
        return data[0];
    }

    /// <summary>
    /// 获取扫描结果（功能码 0x0301）
    /// 查询从设备扫描结果，返回连接的设备接口类型
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 设备接口类型（NotConnected, OwiCurrent, OwiVoltage, Rs485）
    /// </returns>
    public async Task<DeviceInterfaceType> GetScanResultAsync(CancellationToken ct = default)
    {
        // 构建 CPPI V3 帧：功能码 0x0301，无数据
        var frame = _converterFrameStrategy.BuildRawFrame(0x0301);
        CommunicationLogger.LogRaw(Name, ">>> 转接板指令: 获取扫描结果", frame);

        var response = await SendRawFrameAsync(frame, ct);

        // 使用帧策略解析 CPPI V3 帧
        if (!_converterFrameStrategy.TryParseRawFrame(response, out _, out byte[] frameData))
        {
            throw new DeviceException($"转接板响应帧解析失败，数据: {BitConverter.ToString(response)}");
        }

        // 至少需要 1 字节错误码 + 1 字节扫描结果
        if (frameData.Length < 2)
        {
            throw new DeviceException($"转接板响应数据长度不足: 期望至少 2 字节，实际 {frameData.Length} 字节");
        }

        // 检查错误码
        byte errorCode = frameData[0];
        if (errorCode != 0)
        {
            var errorName = Enum.IsDefined(typeof(ConverterErrorCode), errorCode)
                ? ((ConverterErrorCode)errorCode).ToString()
                : $"未知错误({errorCode})";
            throw new DeviceException($"转接板返回错误: {errorName}");
        }

        // 解析扫描结果（错误码之后的第1个字节）
        byte scanResult = frameData[1];
        CommunicationLogger.LogInfo(Name, $"扫描结果: {scanResult} ({(DeviceInterfaceType)scanResult})");

        return (DeviceInterfaceType)scanResult;
    }

    /// <summary>
    /// 通过转接板发送 Modbus RTU 指令（功能码 0x0400）
    /// 转接板将 Modbus RTU 报文透传给下游变送器
    /// </summary>
    /// <param name="modbusFrame">
    /// 完整的 Modbus RTU 报文（含 CRC16）
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// Modbus RTU 响应（不含 CPPI 错误码）
    /// </returns>
    private async Task<byte[]> SendModbusForwardAsync(byte[] modbusFrame, CancellationToken ct)
    {
        // 使用 _cppiV3FrameStrategy（端口38，功能码0x0400）构建 CPPI V3 帧
        var frame = _cppiV3FrameStrategy.BuildRawFrame(0x0400, modbusFrame);
        CommunicationLogger.LogRaw(Name, ">>> 转接板 Modbus 转发", frame);

        var response = await SendRawFrameAsync(frame, ct);

        // 使用帧策略解析 CPPI V3 响应
        if (!_converterFrameStrategy.TryParseRawFrame(response, out _, out byte[] frameData))
        {
            throw new DeviceException($"转接板响应帧解析失败，数据: {BitConverter.ToString(response)}");
        }

        // 至少需要 1 字节错误码 + Modbus 响应
        if (frameData.Length < 1)
        {
            throw new DeviceException("转接板响应数据为空");
        }

        // 检查 CPPI 错误码
        byte errorCode = frameData[0];
        if (errorCode != 0)
        {
            var errorName = Enum.IsDefined(typeof(ConverterErrorCode), errorCode)
                ? ((ConverterErrorCode)errorCode).ToString()
                : $"未知错误({errorCode})";
            throw new DeviceException($"转接板返回错误: {errorName}");
        }

        // 提取 Modbus 响应（跳过 CPPI 错误码）
        if (frameData.Length > 1)
        {
            var modbusResponse = new byte[frameData.Length - 1];
            Array.Copy(frameData, 1, modbusResponse, 0, modbusResponse.Length);
            CommunicationLogger.LogRaw(Name, "<<< Modbus 响应", modbusResponse);
            return modbusResponse;
        }

        return Array.Empty<byte>();
    }

    /// <summary>
    /// 通过转接板启用 OWI 通信模式
    /// 向下游变送器写入寄存器 0x8000 = 0x0001
    /// </summary>
    /// <param name="slaveAddress">
    /// Modbus 从机地址（默认1）
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// true: 指令执行成功，false: 指令执行失败
    /// </returns>
    public async Task<bool> EnableOwiViaConverterAsync(byte slaveAddress = 1, CancellationToken ct = default)
    {
        // 构建 Modbus RTU 帧：F41 写寄存器 0x8000 = 0x0001
        var modbusFrame = BuildModbusRtuFrame(slaveAddress, 0x29, 0x8000, 0x0001, new byte[] { 0x00, 0x01 });
        CommunicationLogger.LogRaw(Name, ">>> Modbus RTU: 启用 OWI 通信模式", modbusFrame);

        try
        {
            await SendModbusForwardAsync(modbusFrame, ct);
            CommunicationLogger.LogInfo(Name, "OWI 通信模式已启用");
            // 等待设备完成模式切换
            await Task.Delay(200, ct);
            return true;
        }
        catch (DeviceException ex)
        {
            CommunicationLogger.LogError(Name, "启用 OWI 通信模式失败", ex);
            return false;
        }
    }

    /// <summary>
    /// 通过转接板禁用 OWI 通信模式
    /// 向下游变送器写入寄存器 0x8000 = 0x0000
    /// </summary>
    /// <param name="slaveAddress">
    /// Modbus 从机地址（默认1）
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// true: 指令执行成功，false: 指令执行失败
    /// </returns>
    public async Task<bool> DisableOwiViaConverterAsync(byte slaveAddress = 1, CancellationToken ct = default)
    {
        // 构建 Modbus RTU 帧：F41 写寄存器 0x8000 = 0x0000
        var modbusFrame = BuildModbusRtuFrame(slaveAddress, 0x29, 0x8000, 0x0001, new byte[] { 0x00, 0x00 });
        CommunicationLogger.LogRaw(Name, ">>> Modbus RTU: 禁用 OWI 通信模式", modbusFrame);

        try
        {
            await SendModbusForwardAsync(modbusFrame, ct);
            CommunicationLogger.LogInfo(Name, "OWI 通信模式已禁用");
            // 等待设备完成模式切换
            await Task.Delay(200, ct);
            return true;
        }
        catch (DeviceException ex)
        {
            CommunicationLogger.LogError(Name, "禁用 OWI 通信模式失败", ex);
            return false;
        }
    }

    /// <summary>
    /// 发送原始 CPPI V3 帧并接收响应
    /// </summary>
    private async Task<byte[]> SendRawFrameAsync(byte[] frame, CancellationToken ct)
    {
        // 直接使用传输层发送和接收（绕过 BuildFrame 的 Modbus CRC 添加）
        var transport = Pipeline.Transport;
        if (transport == null || !transport.IsOpen)
        {
            throw new DeviceException("传输层未打开");
        }

        // 清空接收缓冲区
        await transport.ClearReceiveBufferAsync(ct);

        // 发送原始帧
        await transport.WriteAsync(frame, 0, frame.Length, ct);

        // 接收响应（使用转接板帧策略解析）
        var response = await ReceiveRawFrameAsync(transport, ct);

        // 记录接收日志
        CommunicationLogger.LogRaw(Name, "<<< 转接板响应", response);

        return response;
    }

    /// <summary>
    /// 接收原始 CPPI V3 帧响应
    /// </summary>
    private async Task<byte[]> ReceiveRawFrameAsync(IPhysicalTransport transport, CancellationToken ct)
    {
        var accumulated = new List<byte>();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var idleSw = System.Diagnostics.Stopwatch.StartNew();
        bool hasReceivedData = false;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            // 等待一小段时间
            await Task.Delay(10, ct);

            // 尝试读取数据
            var buffer = new byte[4096];
            int read = await transport.ReadAsync(buffer, 0, buffer.Length, ct);

            if (read > 0)
            {
                accumulated.AddRange(buffer.AsSpan(0, read).ToArray());
                hasReceivedData = true;
                idleSw.Restart();

                // 尝试解析帧
                var accArray = accumulated.ToArray();
                if (_converterFrameStrategy.TryParseRawFrame(accArray, out int frameLen, out byte[] frameData))
                {
                    // 返回完整帧（包括帧头和 CRC）
                    return accArray.AsSpan(0, frameLen).ToArray();
                }
            }

            // 首次响应超时检查（15秒）
            if (!hasReceivedData && sw.ElapsedMilliseconds > 15000)
            {
                throw new DeviceException("转接板响应超时");
            }

            // 接收空闲超时检查（100ms 无新数据）
            if (hasReceivedData && idleSw.ElapsedMilliseconds > 100)
            {
                // 返回累积的数据（可能不是完整帧）
                if (accumulated.Count > 0)
                {
                    return accumulated.ToArray();
                }

                break;
            }
        }

        throw new DeviceException("未收到转接板响应");
    }

    /// <summary>
    /// 解析转接板响应帧
    /// </summary>
    /// <param name="response">
    /// 完整的 CPPI V3 响应帧
    /// </param>
    /// <param name="expectedDataLength">
    /// 期望的数据长度（不含错误码），0 表示不检查
    /// </param>
    /// <returns>
    /// 数据内容（不含错误码）
    /// </returns>
    private byte[] ParseConverterResponse(byte[] response, int expectedDataLength)
    {
        // 使用帧策略解析 CPPI V3 帧（原始模式，不剥离 Modbus CRC）
        if (!_converterFrameStrategy.TryParseRawFrame(response, out int frameLen, out byte[] frameData))
        {
            throw new DeviceException($"转接板响应帧解析失败，数据: {BitConverter.ToString(response)}");
        }

        // 至少需要 1 字节错误码
        if (frameData.Length < 1)
        {
            throw new DeviceException("转接板响应数据为空");
        }

        // 检查错误码
        byte errorCode = frameData[0];
        if (errorCode != 0)
        {
            var errorName = Enum.IsDefined(typeof(ConverterErrorCode), errorCode)
                ? ((ConverterErrorCode)errorCode).ToString()
                : $"未知错误({errorCode})";
            throw new DeviceException($"转接板返回错误: {errorName}");
        }

        // 提取数据部分（跳过错误码）
        if (frameData.Length > 1)
        {
            var data = new byte[frameData.Length - 1];
            Array.Copy(frameData, 1, data, 0, data.Length);

            // 验证数据长度
            if (expectedDataLength > 0 && data.Length < expectedDataLength)
            {
                throw new DeviceException($"转接板响应数据长度不足: 期望 {expectedDataLength}，实际 {data.Length}");
            }

            return data;
        }

        // 只有错误码，没有额外数据
        if (expectedDataLength > 0)
        {
            throw new DeviceException($"转接板响应数据长度不足: 期望 {expectedDataLength}，实际 0");
        }

        return Array.Empty<byte>();
    }

    // ═══════════════════════════════════════════════════════════
    // 通用指令 (0x01xx)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 读取转接板设备型号（功能码 0x0102）
    /// 返回 ASCII 字符串，最大 16 字节
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 设备型号字符串
    /// </returns>
    public async Task<string> GetConverterModelAsync(CancellationToken ct = default)
    {
        var frame = _converterFrameStrategy.BuildRawFrame(0x0102);
        CommunicationLogger.LogRaw(Name, ">>> 转接板指令: 读取设备型号", frame);

        var response = await SendRawFrameAsync(frame, ct);
        var data = ParseConverterResponse(response, 0);

        return Encoding.ASCII.GetString(data).TrimEnd('\0');
    }

    /// <summary>
    /// 读取转接板设备编号（功能码 0x0104）
    /// 返回 ASCII 字符串，最大 16 字节
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 设备编号字符串
    /// </returns>
    public async Task<string> GetConverterDeviceNumberAsync(CancellationToken ct = default)
    {
        var frame = _converterFrameStrategy.BuildRawFrame(0x0104);
        CommunicationLogger.LogRaw(Name, ">>> 转接板指令: 读取设备编号", frame);

        var response = await SendRawFrameAsync(frame, ct);
        var data = ParseConverterResponse(response, 0);

        return Encoding.ASCII.GetString(data).TrimEnd('\0');
    }

    /// <summary>
    /// 设置转接板设备编号（功能码 0x0105）
    /// 写入 ASCII 字符串，最大 16 字节
    /// </summary>
    /// <param name="deviceNumber">
    /// 设备编号字符串（最大 16 字节）
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task SetConverterDeviceNumberAsync(string deviceNumber, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(deviceNumber))
            throw new ArgumentException("设备编号不能为空", nameof(deviceNumber));

        var bytes = Encoding.ASCII.GetBytes(deviceNumber);
        if (bytes.Length > 16)
            throw new ArgumentException("设备编号不能超过 16 字节", nameof(deviceNumber));

        var frame = _converterFrameStrategy.BuildRawFrame(0x0105, bytes);
        CommunicationLogger.LogRaw(Name, $">>> 转接板指令: 设置设备编号 ({deviceNumber})", frame);

        var response = await SendRawFrameAsync(frame, ct);
        ParseConverterResponse(response, 0); // 验证无错误
    }

    // ═══════════════════════════════════════════════════════════
    // 核心指令 (0x02xx)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 读取当前测量项目（功能码 0x0211）
    /// 返回当前正在进行测量和输出的项目代号、原始值和最终值
    /// 响应格式：1字节项目代号 + 4字节原始值(float32小端) + 4字节最终值(float32小端)
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 测量结果
    /// </returns>
    public async Task<MeasurementResult> GetMeasurementProjectAsync(CancellationToken ct = default)
    {
        var frame = _converterFrameStrategy.BuildRawFrame(0x0211);
        CommunicationLogger.LogRaw(Name, ">>> 转接板指令: 读取当前测量项目", frame);

        var response = await SendRawFrameAsync(frame, ct);
        var data = ParseConverterResponse(response, 9); // 1 + 4 + 4 = 9 字节

        return new MeasurementResult
        {
            Project = (MeasurementProject)data[0],
            RawValue = BitConverter.ToSingle(data, 1),    // float32 小端，偏移 1
            FinalValue = BitConverter.ToSingle(data, 5)   // float32 小端，偏移 5
        };
    }

    /// <summary>
    /// 关闭当前输出项目（功能码 0x0212）
    /// 关闭指定的输出项目
    /// </summary>
    /// <param name="project">
    /// 要关闭的项目代号
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task CloseMeasurementProjectAsync(OutputProject project, CancellationToken ct = default)
    {
        var frame = _converterFrameStrategy.BuildRawFrame(0x0212, new byte[] { (byte)project });
        CommunicationLogger.LogRaw(Name, $">>> 转接板指令: 关闭输出项目 ({project})", frame);

        var response = await SendRawFrameAsync(frame, ct);
        ParseConverterResponse(response, 0); // 验证无错误
    }

    // ═══════════════════════════════════════════════════════════
    // 校准相关 (0x0280-0x0282)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 读取校准数据份数（功能码 0x0282）
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 校准数据份数
    /// </returns>
    public async Task<ushort> GetCalibrationCountAsync(CancellationToken ct = default)
    {
        var frame = _converterFrameStrategy.BuildRawFrame(0x0282);
        CommunicationLogger.LogRaw(Name, ">>> 转接板指令: 读取校准份数", frame);

        var response = await SendRawFrameAsync(frame, ct);
        var data = ParseConverterResponse(response, 2); // 2 字节

        // 大端模式：高字节在前
        return (ushort)((data[0] << 8) | data[1]);
    }

    /// <summary>
    /// 读取校准数据（功能码 0x0281）
    /// 读取指定索引的校准数据
    /// </summary>
    /// <param name="index">
    /// 校准数据索引（1=最新，依次累加）
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    /// <returns>
    /// 校准数据
    /// </returns>
    public async Task<CalibrationData> ReadCalibrationDataAsync(ushort index, CancellationToken ct = default)
    {
        // 参数：索引（2字节大端）
        var paramData = new byte[] { (byte)(index >> 8), (byte)(index & 0xFF) };
        var frame = _converterFrameStrategy.BuildRawFrame(0x0281, paramData);
        CommunicationLogger.LogRaw(Name, $">>> 转接板指令: 读取校准数据 (索引:{index})", frame);

        var response = await SendRawFrameAsync(frame, ct);

        // 解析响应帧
        if (!_converterFrameStrategy.TryParseRawFrame(response, out _, out byte[] frameData))
            throw new DeviceException($"转接板响应帧解析失败，数据: {BitConverter.ToString(response)}");

        if (frameData.Length < 1)
            throw new DeviceException("转接板响应数据为空");

        byte errorCode = frameData[0];
        if (errorCode != 0)
        {
            var errorName = Enum.IsDefined(typeof(ConverterErrorCode), errorCode)
                ? ((ConverterErrorCode)errorCode).ToString()
                : $"未知错误({errorCode})";
            throw new DeviceException($"转接板返回错误: {errorName}");
        }

        // 校准数据在错误码之后
        var data = new byte[frameData.Length - 1];
        Array.Copy(frameData, 1, data, 0, data.Length);

        return ParseCalibrationData(data);
    }

    /// <summary>
    /// 写入校准数据（功能码 0x0280）
    /// </summary>
    /// <param name="calibrationData">
    /// 校准数据
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task WriteCalibrationDataAsync(CalibrationData calibrationData, CancellationToken ct = default)
    {
        if (calibrationData == null)
            throw new ArgumentNullException(nameof(calibrationData));

        var data = SerializeCalibrationData(calibrationData);
        var frame = _converterFrameStrategy.BuildRawFrame(0x0280, data);
        CommunicationLogger.LogRaw(Name, ">>> 转接板指令: 写入校准数据", frame);

        var response = await SendRawFrameAsync(frame, ct);
        ParseConverterResponse(response, 0); // 验证无错误
    }

    // ═══════════════════════════════════════════════════════════
    // 其他指令 (0x03xx-0x05xx)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 模块电源控制（功能码 0x0410）
    /// 控制压力模块的供电
    /// 注：扫描模式时不能关闭压力模块供电
    /// </summary>
    /// <param name="state">
    /// 电源状态
    /// </param>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task SetModulePowerAsync(ModulePowerState state, CancellationToken ct = default)
    {
        var frame = _converterFrameStrategy.BuildRawFrame(0x0410, new byte[] { (byte)state });
        CommunicationLogger.LogRaw(Name, $">>> 转接板指令: 模块电源控制 ({state})", frame);

        var response = await SendRawFrameAsync(frame, ct);
        ParseConverterResponse(response, 0); // 验证无错误
    }

    /// <summary>
    /// 发送心跳帧（功能码 0x0500）
    /// 用于保持与转接板的通信连接
    /// </summary>
    /// <param name="ct">
    /// 取消令牌
    /// </param>
    public async Task SendHeartbeatAsync(CancellationToken ct = default)
    {
        var frame = _converterFrameStrategy.BuildRawFrame(0x0500);
        CommunicationLogger.LogRaw(Name, ">>> 转接板指令: 心跳", frame);

        var response = await SendRawFrameAsync(frame, ct);
        ParseConverterResponse(response, 0); // 验证无错误
    }

    // ═══════════════════════════════════════════════════════════
    // 校准数据序列化/反序列化
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 解析校准数据字节数组为 CalibrationData 对象
    /// </summary>
    /// <param name="data">
    /// 校准数据字节数组（不含错误码）
    /// </param>
    /// <returns>
    /// 校准数据对象
    /// </returns>
    private static CalibrationData ParseCalibrationData(byte[] data)
    {
        // 校准数据格式（文档定义）：
        // 基准板 SN 号: 16 bytes
        // 基准板校准日期 (年): 2 bytes (大端)
        // 基准板校准日期 (月): 1 byte
        // 基准板校准日期 (日): 1 byte
        // 基准板校准值列表: 4*4 bytes 电压 + 4*4 bytes 电流 = 32 bytes
        // 校准日期 (年): 2 bytes (大端)
        // 校准日期 (月): 1 byte
        // 校准日期 (日): 1 byte
        // 实际值列表: 4*4 bytes 电压 + 4*4 bytes 电流 = 32 bytes
        // 电压校准系数 (K, B): 8 bytes
        // 电流校准系数 (K, B): 8 bytes
        // CRC8: 1 byte
        int expectedSize = 16 + 4 + 32 + 4 + 32 + 8 + 8 + 1;
        if (data.Length < expectedSize)
            throw new DeviceException($"校准数据长度不足: 期望 {expectedSize}，实际 {data.Length}");

        int offset = 0;
        var result = new CalibrationData();

        // 基准板 SN 号 (16 bytes)
        result.StandardBoardSn = Encoding.ASCII.GetString(data, offset, 16).TrimEnd('\0');
        offset += 16;

        // 基准板校准日期 (年 2 bytes 大端 + 月 1 byte + 日 1 byte)
        int stdYear = (data[offset] << 8) | data[offset + 1];
        int stdMonth = data[offset + 2];
        int stdDay = data[offset + 3];
        result.StandardBoardCalibrationDate = new DateTime(stdYear, stdMonth, stdDay);
        offset += 4;

        // 基准板校准值 - 电压 (4*4 bytes 小端 float32)
        for (int i = 0; i < 4; i++)
        {
            result.StandardVoltageValues[i] = BitConverter.ToSingle(data, offset);
            offset += 4;
        }

        // 基准板校准值 - 电流 (4*4 bytes 小端 float32)
        for (int i = 0; i < 4; i++)
        {
            result.StandardCurrentValues[i] = BitConverter.ToSingle(data, offset);
            offset += 4;
        }

        // 校准日期 (年 2 bytes 大端 + 月 1 byte + 日 1 byte)
        int calYear = (data[offset] << 8) | data[offset + 1];
        int calMonth = data[offset + 2];
        int calDay = data[offset + 3];
        result.CalibrationDate = new DateTime(calYear, calMonth, calDay);
        offset += 4;

        // 实际值 - 电压 (4*4 bytes 小端 float32)
        for (int i = 0; i < 4; i++)
        {
            result.ActualVoltageValues[i] = BitConverter.ToSingle(data, offset);
            offset += 4;
        }

        // 实际值 - 电流 (4*4 bytes 小端 float32)
        for (int i = 0; i < 4; i++)
        {
            result.ActualCurrentValues[i] = BitConverter.ToSingle(data, offset);
            offset += 4;
        }

        // 电压校准系数 K, B (2*4 bytes 小端 float32)
        result.VoltageK = BitConverter.ToSingle(data, offset);
        offset += 4;
        result.VoltageB = BitConverter.ToSingle(data, offset);
        offset += 4;

        // 电流校准系数 K, B (2*4 bytes 小端 float32)
        result.CurrentK = BitConverter.ToSingle(data, offset);
        offset += 4;
        result.CurrentB = BitConverter.ToSingle(data, offset);
        offset += 4;

        // 最后 1 字节 CRC8（跳过）

        return result;
    }

    /// <summary>
    /// 将 CalibrationData 对象序列化为字节数组
    /// </summary>
    /// <param name="calibrationData">
    /// 校准数据对象
    /// </param>
    /// <returns>
    /// 字节数组
    /// </returns>
    private static byte[] SerializeCalibrationData(CalibrationData calibrationData)
    {
        // 校准数据格式（文档定义）：
        // 基准板 SN 号: 16 bytes
        // 基准板校准日期 (年): 2 bytes (大端)
        // 基准板校准日期 (月): 1 byte
        // 基准板校准日期 (日): 1 byte
        // 基准板校准值列表: 4*4 bytes 电压 + 4*4 bytes 电流 = 32 bytes
        // 校准日期 (年): 2 bytes (大端)
        // 校准日期 (月): 1 byte
        // 校准日期 (日): 1 byte
        // 实际值列表: 4*4 bytes 电压 + 4*4 bytes 电流 = 32 bytes
        // 电压校准系数 (K, B): 8 bytes
        // 电流校准系数 (K, B): 8 bytes
        // CRC8: 1 byte (由转接板自动计算？文档中写 CRC8 上述所有数据同 CPPI V3 头)
        int totalSize = 16 + 4 + 32 + 4 + 32 + 8 + 8 + 1;
        var data = new byte[totalSize];
        int offset = 0;

        // 基准板 SN 号 (16 bytes)
        var snBytes = Encoding.ASCII.GetBytes(calibrationData.StandardBoardSn ?? string.Empty);
        Array.Copy(snBytes, 0, data, offset, Math.Min(snBytes.Length, 16));
        offset += 16;

        // 基准板校准日期 (年 2 bytes 大端 + 月 1 byte + 日 1 byte)
        data[offset] = (byte)(calibrationData.StandardBoardCalibrationDate.Year >> 8);
        data[offset + 1] = (byte)(calibrationData.StandardBoardCalibrationDate.Year & 0xFF);
        data[offset + 2] = (byte)calibrationData.StandardBoardCalibrationDate.Month;
        data[offset + 3] = (byte)calibrationData.StandardBoardCalibrationDate.Day;
        offset += 4;

        // 基准板校准值 - 电压 (4*4 bytes 小端 float32)
        for (int i = 0; i < 4; i++)
        {
            var bytes = BitConverter.GetBytes(calibrationData.StandardVoltageValues[i]);
            Array.Copy(bytes, 0, data, offset, 4);
            offset += 4;
        }

        // 基准板校准值 - 电流 (4*4 bytes 小端 float32)
        for (int i = 0; i < 4; i++)
        {
            var bytes = BitConverter.GetBytes(calibrationData.StandardCurrentValues[i]);
            Array.Copy(bytes, 0, data, offset, 4);
            offset += 4;
        }

        // 校准日期 (年 2 bytes 大端 + 月 1 byte + 日 1 byte)
        data[offset] = (byte)(calibrationData.CalibrationDate.Year >> 8);
        data[offset + 1] = (byte)(calibrationData.CalibrationDate.Year & 0xFF);
        data[offset + 2] = (byte)calibrationData.CalibrationDate.Month;
        data[offset + 3] = (byte)calibrationData.CalibrationDate.Day;
        offset += 4;

        // 实际值 - 电压 (4*4 bytes 小端 float32)
        for (int i = 0; i < 4; i++)
        {
            var bytes = BitConverter.GetBytes(calibrationData.ActualVoltageValues[i]);
            Array.Copy(bytes, 0, data, offset, 4);
            offset += 4;
        }

        // 实际值 - 电流 (4*4 bytes 小端 float32)
        for (int i = 0; i < 4; i++)
        {
            var bytes = BitConverter.GetBytes(calibrationData.ActualCurrentValues[i]);
            Array.Copy(bytes, 0, data, offset, 4);
            offset += 4;
        }

        // 电压校准系数 K, B (2*4 bytes 小端 float32)
        var vk = BitConverter.GetBytes(calibrationData.VoltageK);
        Array.Copy(vk, 0, data, offset, 4);
        offset += 4;
        var vb = BitConverter.GetBytes(calibrationData.VoltageB);
        Array.Copy(vb, 0, data, offset, 4);
        offset += 4;

        // 电流校准系数 K, B (2*4 bytes 小端 float32)
        var ck = BitConverter.GetBytes(calibrationData.CurrentK);
        Array.Copy(ck, 0, data, offset, 4);
        offset += 4;
        var cb = BitConverter.GetBytes(calibrationData.CurrentB);
        Array.Copy(cb, 0, data, offset, 4);
        offset += 4;

        // CRC8 占位（文档说明 CRC8 同 CPPI V3 头，由 CPPI V3 帧的 CRC8 字段覆盖）
        // data[offset] = 0; // 保留字节

        return data;
    }

    #endregion 转接板指令

    // ═══════════════════════════════════════════════════════════
    // 私有解析方法
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 移除 F40 响应中转换板添加的额外 0x00 前缀字节。
    /// 转换板在 CPPI V3 数据字段中会添加一个多余的 0x00 字节，
    /// 使标准 Modbus 响应偏移 1 字节。
    /// </summary>
    private static byte[] NormalizeF40Response(byte[] raw)
    {
        if (raw == null || raw.Length < 2)
            return raw;

        // 如果第一个字节是 0x00 且第二个字节是有效的 Modbus 从站地址（0x01），
        // 则认为是转换板添加的额外前缀
        if (raw[0] == 0x00 && raw.Length >= 4)
        {
            var result = new byte[raw.Length - 1];
            Array.Copy(raw, 1, result, 0, result.Length);
            return result;
        }
        return raw;
    }

    /// <summary>
    /// 从 Modbus 响应中解析 float32 大端浮点数
    /// </summary>
    /// <param name="raw">
    /// 原始响应数据
    /// </param>
    /// <param name="dataOffset">
    /// 数据起始偏移（跳过地址+功能码+字节数）
    /// </param>
    /// <returns>
    /// 浮点数值，解析失败返回 NaN
    /// </returns>
    private static double ParseFloat32BigEndian(byte[] raw, int dataOffset)
    {
        if (raw == null || raw.Length < dataOffset + 4)
            return double.NaN;

        // 大端模式：高字节在前
        byte[] bytes = new byte[4];
        bytes[0] = raw[dataOffset + 3]; // 低字节
        bytes[1] = raw[dataOffset + 2];
        bytes[2] = raw[dataOffset + 1];
        bytes[3] = raw[dataOffset];     // 高字节

        try
        {
            return (double)BitConverter.ToSingle(bytes, 0);
        }
        catch
        {
            return double.NaN;
        }
    }

    /// <summary>
    /// 从 Modbus 响应中解析 float32 小端浮点数
    /// </summary>
    /// <param name="raw">
    /// 原始响应数据
    /// </param>
    /// <param name="dataOffset">
    /// 数据起始偏移
    /// </param>
    /// <returns>
    /// 浮点数值，解析失败返回 NaN
    /// </returns>
    private static double ParseFloat32LittleEndian(byte[] raw, int dataOffset)
    {
        if (raw == null || raw.Length < dataOffset + 4)
            return double.NaN;

        // 小端模式：低字节在前（直接复制即可）
        byte[] bytes = new byte[4];
        Array.Copy(raw, dataOffset, bytes, 0, 4);

        try
        {
            return (double)BitConverter.ToSingle(bytes, 0);
        }
        catch
        {
            return double.NaN;
        }
    }

    /// <summary>
    /// 将 float32 大端浮点数写入字节数组
    /// </summary>
    private static void WriteFloat32BigEndian(byte[] buffer, int offset, float value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        // 大端模式：高字节在前
        buffer[offset] = bytes[3];
        buffer[offset + 1] = bytes[2];
        buffer[offset + 2] = bytes[1];
        buffer[offset + 3] = bytes[0];
    }

    /// <summary>
    /// 将 float32 小端浮点数写入字节数组
    /// </summary>
    private static void WriteFloat32LittleEndian(byte[] buffer, int offset, float value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        // 小端模式：低字节在前（BitConverter 默认就是小端）
        Array.Copy(bytes, 0, buffer, offset, 4);
    }

    /// <summary>
    /// 计算 Modbus CRC16（多项式 0xA001，初始值 0xFFFF）
    /// </summary>
    private static ushort CalculateModbusCrc16(byte[] data, int length)
    {
        ushort crc = 0xFFFF;
        for (int i = 0; i < length; i++)
        {
            crc ^= data[i];
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 0x0001) != 0)
                {
                    crc >>= 1;
                    crc ^= 0xA001;
                }
                else
                {
                    crc >>= 1;
                }
            }
        }
        return crc;
    }

    /// <summary>
    /// 构建完整的 Modbus RTU 帧（含从机地址、功能码、数据、CRC16）
    /// </summary>
    private static byte[] BuildModbusRtuFrame(byte slaveAddress, byte functionCode, ushort registerAddress, ushort registerCount, byte[] data)
    {
        // Modbus RTU 帧：地址(1) + 功能码(1) + 起始地址(2) + 寄存器数量(2) + 字节数(1) + 数据(N) + CRC(2)
        int dataLen = data?.Length ?? 0;
        int frameLen = 7 + dataLen + 2; // +2 for CRC
        var frame = new byte[frameLen];

        frame[0] = slaveAddress;
        frame[1] = functionCode;
        frame[2] = (byte)(registerAddress >> 8);   // 寄存器地址高字节
        frame[3] = (byte)(registerAddress & 0xFF); // 寄存器地址低字节
        frame[4] = (byte)(registerCount >> 8);     // 寄存器数量高字节
        frame[5] = (byte)(registerCount & 0xFF);   // 寄存器数量低字节
        frame[6] = (byte)dataLen;                  // 数据字节数

        if (data != null && dataLen > 0)
        {
            Array.Copy(data, 0, frame, 7, dataLen);
        }

        // 计算 CRC16（不含 CRC 本身）
        ushort crc = CalculateModbusCrc16(frame, frameLen - 2);
        frame[frameLen - 2] = (byte)(crc & 0xFF);
        frame[frameLen - 1] = (byte)((crc >> 8) & 0xFF);

        return frame;
    }

    /// <summary>
    /// 从 Modbus 响应中提取 ASCII 字符串
    /// </summary>
    /// <param name="raw">
    /// 原始响应数据
    /// </param>
    /// <param name="dataOffset">
    /// 数据起始偏移
    /// </param>
    /// <param name="expectedLength">
    /// 期望的字符串长度
    /// </param>
    /// <returns>
    /// 解码后的字符串
    /// </returns>
    private static string ExtractAsciiString(byte[] raw, int dataOffset, int expectedLength)
    {
        if (raw == null || raw.Length < dataOffset + expectedLength)
            return string.Empty;

        var sb = new StringBuilder();
        for (int i = 0; i < expectedLength; i++)
        {
            byte b = raw[dataOffset + i];
            if (b == 0) break; // 遇到 null 终止符
            if (b >= 0x20 && b <= 0x7E) // 可打印 ASCII
                sb.Append((char)b);
        }
        return sb.ToString().Trim();
    }
}
