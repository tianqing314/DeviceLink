namespace DeviceLink.Device.PS02;

/// <summary>
/// PS02 压力变送器 Modbus RTU 寄存器地址定义
/// 
/// 通信参数：
/// - 波特率：9600（默认）/ 115200（可配置）
/// - 数据位：8位
/// - 校验：NONE（默认）
/// - 停止位：1位（默认）
/// - 功能码：F03(0x03) 读保持寄存器 / F40(0x28) 读寄存器 / F41(0x29) 写寄存器
/// </summary>
public static class PS02Registers
{
    // ═══════════════════════════════════════════════════════════
    // 寄存器地址空间1（系统参数，掉电保存）
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 量程下限（float32，大端）
    /// </summary>
    public const ushort RangeLower = 0x0000;

    /// <summary>
    /// 量程上限（float32，大端）
    /// </summary>
    public const ushort RangeUpper = 0x0001;

    /// <summary>
    /// 实时压力值（float32，大端，只读）
    /// </summary>
    public const ushort Pressure = 0x0002;

    // ═══════════════════════════════════════════════════════════
    // 寄存器地址空间2（只读参数）
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 精度（uint16，×100，如0.1%=10）
    /// </summary>
    public const ushort Precision = 0x5136;

    /// <summary>
    /// 压力类型（uint16：0=表压, 2=绝压, 3=差压）
    /// </summary>
    public const ushort PressureType = 0x5137;

    /// <summary>
    /// 迁移量程下限（float32，大端）
    /// </summary>
    public const ushort MigrationRangeLower = 0x513E;

    /// <summary>
    /// 迁移量程上限（float32，大端）
    /// </summary>
    public const ushort MigrationRangeUpper = 0x5140;

    /// <summary>
    /// 序列号起始地址（string，12字节 ASCII）
    /// </summary>
    public const ushort SerialNumber = 0x51A0;

    /// <summary>
    /// 软件更新年
    /// </summary>
    public const ushort SoftwareYear = 0x800C;

    /// <summary>
    /// 软件更新月
    /// </summary>
    public const ushort SoftwareMonth = 0x800E;

    /// <summary>
    /// 软件更新日
    /// </summary>
    public const ushort SoftwareDay = 0x800F;

    /// <summary>
    /// 固件版本起始地址（string，多字节）
    /// </summary>
    public const ushort FirmwareVersion = 0x8010;

    /// <summary>
    /// 硬件版本起始地址（string，多字节）
    /// </summary>
    public const ushort HardwareVersion = 0x801A;

    /// <summary>
    /// 模块类型（uint16：A=绝对压力, V=真空, D=差压）
    /// </summary>
    public const ushort ModuleType = 0x8020;

    // ═══════════════════════════════════════════════════════════
    // 寄存器地址空间3（调试用，掉电丢失）
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// OWI通信使能（0x0000=DAC模式, 0x0001=OWI通信模式）
    /// </summary>
    public const ushort OwiEnable = 0x8000;

    /// <summary>
    /// 调试模式配置（0x0000=变送输出, 0x0001=调试模式）
    /// </summary>
    public const ushort DebugMode = 0x8002;

    /// <summary>
    /// 调试模式DAC值
    /// </summary>
    public const ushort DebugDacValue = 0x8003;

    /// <summary>
    /// 恒流源0配置
    /// </summary>
    public const ushort CurrentSource0 = 0x8006;

    /// <summary>
    /// 恒流源1配置
    /// </summary>
    public const ushort CurrentSource1 = 0x8007;

    /// <summary>
    /// ADC采样率配置
    /// </summary>
    public const ushort AdcSampleRate = 0x800A;
}
