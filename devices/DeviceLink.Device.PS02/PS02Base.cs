using DeviceLink.DataLink;
using DeviceLink.DeviceBase;
using DeviceLink.Protocol;
using DeviceLink.Transport;
using System;
using System.IO.Ports;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceLink.Device.PS02
{
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

        #region 构造函数

        /// <summary>
        /// 构造函数（串口通讯，默认 CPPI V3 帧策略）
        /// </summary>
        /// <param name="serialPortName">串口号（如 COM3）</param>
        /// <param name="baudRate">波特率（默认9600）</param>
        /// <param name="dataBits">数据位（默认8）</param>
        /// <param name="stopBits">停止位（默认1）</param>
        /// <param name="parity">校验位（默认None）</param>
        /// <param name="slaveAddress">Modbus从站地址（默认1）</param>
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
        /// <param name="serialPortName">串口号（如 COM3）</param>
        /// <param name="slaveAddress">Modbus从站地址（默认1）</param>
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
        /// <param name="ipAddress">IP地址</param>
        /// <param name="port">端口号</param>
        /// <param name="slaveAddress">Modbus从站地址（默认1）</param>
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
        /// <param name="settings">通信配置</param>
        /// <param name="slaveAddress">Modbus从站地址（默认1）</param>
        public PS02Base(DeviceCommSettings settings, byte slaveAddress = 1)
            : base(settings, new ModbusRtuCodec(slaveAddress), new CpplV3FrameStrategy())
        {
            _codec = (ModbusRtuCodec)Codec;
            _slaveAddress = slaveAddress;
            _cppiV3FrameStrategy = new CpplV3FrameStrategy();
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
        /// 通过 F03 读取多个连续寄存器值
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
        /// 通过 F40 读取寄存器（PS02自定义功能码）
        /// </summary>
        /// <param name="registerAddress">寄存器地址</param>
        /// <param name="count">寄存器数量</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>原始响应数据（不含CRC）</returns>
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
        /// <param name="registerAddress">起始寄存器地址</param>
        /// <param name="data">写入的原始数据字节</param>
        /// <param name="ct">取消令牌</param>
        public async Task WriteRegistersF41Async(ushort registerAddress, byte[] data, CancellationToken ct = default)
        {
            ushort count = (ushort)(data.Length / 2);
            if (data.Length % 2 != 0) count++;

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
        /// <param name="ct">取消令牌</param>
        /// <returns>压力值（kPa），无效时返回 NaN</returns>
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
        /// <param name="ct">取消令牌</param>
        /// <returns>压力值（kPa），无效时返回 NaN</returns>
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
        /// <param name="ct">取消令牌</param>
        /// <returns>序列号字符串</returns>
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
        /// <param name="ct">取消令牌</param>
        /// <returns>精度值（×100）</returns>
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
        /// <param name="ct">取消令牌</param>
        /// <returns>压力类型</returns>
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
        /// 读取迁移量程（F40, 寄存器 0x513E-0x5141）
        /// 返回下限和上限，均为 float32 大端浮点数，单位 kPa
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>量程信息</returns>
        public async Task<PressureRange> GetMigrationRangeAsync(CancellationToken ct = default)
        {
            return await SendForResultAsync(
                Command.Read("40.20798.4"),
                raw =>
                {
                    if (raw == null || raw.Length < 12) // 前缀(1) + 地址(1) + 功能码(1) + 字节数(1) + 数据(8)
                        return new PressureRange();

                    return new PressureRange
                    {
                        Lower = ParseFloat32BigEndian(raw, 4),
                        Upper = ParseFloat32BigEndian(raw, 8)
                    };
                },
                ct);
        }

        /// <summary>
        /// 读取固件版本（F40, 寄存器 0x8010, 多个寄存器）
        /// 返回 ASCII 字符串，如 "A20A V00.00.00.01"
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>固件版本字符串</returns>
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
        /// <param name="ct">取消令牌</param>
        /// <returns>硬件版本字符串</returns>
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
        /// <param name="ct">取消令牌</param>
        /// <returns>模块类型值</returns>
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
        /// <param name="ct">取消令牌</param>
        /// <returns>设备标识</returns>
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
        /// 写入迁移量程（F41, 寄存器 0x513E-0x5141）
        /// 量程下限和上限均为 float32 大端浮点数，单位 kPa
        /// </summary>
        /// <param name="lower">量程下限（kPa）</param>
        /// <param name="upper">量程上限（kPa）</param>
        /// <param name="ct">取消令牌</param>
        public async Task SetMigrationRangeAsync(float lower, float upper, CancellationToken ct = default)
        {
            var data = new byte[8];
            WriteFloat32BigEndian(data, 0, lower);
            WriteFloat32BigEndian(data, 4, upper);

            await WriteRegistersF41Async(PS02Registers.MigrationRangeLower, data, ct);
        }

        // ═══════════════════════════════════════════════════════════
        // 序列号写入
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 写入序列号（F41, 寄存器 0x51A0, 6个寄存器=12字节）
        /// 序列号为 ASCII 字符串，如 "C1025D010001"
        /// </summary>
        /// <param name="serialNumber">序列号字符串（12个字符）</param>
        /// <param name="ct">取消令牌</param>
        public async Task SetSerialNumberAsync(string serialNumber, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(serialNumber))
                throw new ArgumentException("序列号不能为空", nameof(serialNumber));

            byte[] data = Encoding.ASCII.GetBytes(serialNumber);
            await WriteRegistersF41Async(PS02Registers.SerialNumber, data, ct);
        }

        // ═══════════════════════════════════════════════════════════
        // 调试/配置指令
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 配置 OWI 通信使能（F41, 寄存器 0x8000）
        /// </summary>
        /// <param name="enable">true=进入OWI通信模式, false=回到DAC工作模式</param>
        /// <param name="ct">取消令牌</param>
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
        /// <param name="enable">true=进入调试模式, false=回到变送输出模式</param>
        /// <param name="ct">取消令牌</param>
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
        /// <param name="value">DAC 输出值</param>
        /// <param name="ct">取消令牌</param>
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
        /// <param name="config">配置值（0x0000-0x000F）</param>
        /// <param name="ct">取消令牌</param>
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
        /// <param name="config">配置值（0x0000-0x000F）</param>
        /// <param name="ct">取消令牌</param>
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
        /// <param name="config">配置值（0x0000-0x0010）</param>
        /// <param name="ct">取消令牌</param>
        public async Task SetAdcSampleRateAsync(ushort config, CancellationToken ct = default)
        {
            var data = new byte[2];
            data[0] = (byte)(config >> 8);
            data[1] = (byte)(config & 0xFF);
            await WriteRegistersF41Async(PS02Registers.AdcSampleRate, data, ct);
        }

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
        /// <param name="raw">原始响应数据</param>
        /// <param name="dataOffset">数据起始偏移（跳过地址+功能码+字节数）</param>
        /// <returns>浮点数值，解析失败返回 NaN</returns>
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
                return BitConverter.ToDouble(bytes, 0);
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
        /// 从 Modbus 响应中提取 ASCII 字符串
        /// </summary>
        /// <param name="raw">原始响应数据</param>
        /// <param name="dataOffset">数据起始偏移</param>
        /// <param name="expectedLength">期望的字符串长度</param>
        /// <returns>解码后的字符串</returns>
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
}
