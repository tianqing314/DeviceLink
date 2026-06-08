namespace DeviceLink.Device.UMC1200
{
    /// <summary>
    /// UMC1200 Modbus RTU 寄存器地址定义
    /// 
    /// 通信参数：
    /// - 波特率：9600
    /// - 数据位：8位
    /// - 校验：NONE
    /// - 停止位：1
    /// - 地址位：1
    /// - 功能码：0x03（读保持寄存器）/ 0x10（写多个寄存器）
    /// </summary>
    public static class UMC1200Registers
    {
        // ═══════════════════════════════════════════════════════════
        // 测量值（ReadOnly）
        // ═══════════════════════════════════════════════════════════

        /// <summary>温度PV值（过程值）</summary>
        public const ushort TemperaturePV = 0;

        /// <summary>湿度PV值（过程值）</summary>
        public const ushort HumidityPV = 1;

        /// <summary>温度MV值（操纵值/输出值）</summary>
        public const ushort TemperatureMV = 2;

        /// <summary>湿度MV值（操纵值/输出值）</summary>
        public const ushort HumidityMV = 3;

        // ═══════════════════════════════════════════════════════════
        // 工艺信息（ReadOnly）
        // ═══════════════════════════════════════════════════════════

        /// <summary>当前工艺步数</summary>
        public const ushort ProcessStepCount = 6;

        /// <summary>当前工艺步号（从0开始计算）</summary>
        public const ushort ProcessStepNumber = 7;

        /// <summary>当前工艺已循环数（从0开始计算）</summary>
        public const ushort ProcessLoopCurrent = 16;

        /// <summary>当前工艺总循环数</summary>
        public const ushort ProcessLoopTotal = 17;

        /// <summary>当前工艺总时间（小时）</summary>
        public const ushort ProcessTotalTimeHour = 18;

        /// <summary>当前工艺总时间（分钟）</summary>
        public const ushort ProcessTotalTimeMinute = 19;

        /// <summary>当前工艺已运行时间（小时）</summary>
        public const ushort ProcessElapsedTimeHour = 20;

        /// <summary>当前工艺已运行时间（分钟）</summary>
        public const ushort ProcessElapsedTimeMinute = 21;

        // ═══════════════════════════════════════════════════════════
        // 当前步信息（ReadOnly）
        // ═══════════════════════════════════════════════════════════

        /// <summary>当前步总时间（小时）</summary>
        public const ushort CurrentStepTotalTimeHour = 22;

        /// <summary>当前步总时间（分钟）</summary>
        public const ushort CurrentStepTotalTimeMinute = 23;

        /// <summary>当前步已运行时间（小时）</summary>
        public const ushort CurrentStepElapsedTimeHour = 24;

        /// <summary>当前步已运行时间（分钟）</summary>
        public const ushort CurrentStepElapsedTimeMinute = 25;

        // ═══════════════════════════════════════════════════════════
        // 连接信息（ReadOnly）
        // ═══════════════════════════════════════════════════════════

        /// <summary>当前连接总时间（小时）</summary>
        public const ushort ConnectionTotalTimeHour = 26;

        /// <summary>当前连接总时间（分钟）</summary>
        public const ushort ConnectionTotalTimeMinute = 27;

        /// <summary>当前连接已运行时间（小时）</summary>
        public const ushort ConnectionElapsedTimeHour = 28;

        /// <summary>当前连接已运行时间（分钟）</summary>
        public const ushort ConnectionElapsedTimeMinute = 29;

        // ═══════════════════════════════════════════════════════════
        // 系统时间（ReadOnly）
        // ═══════════════════════════════════════════════════════════

        /// <summary>系统时间（年）</summary>
        public const ushort SystemTimeYear = 31;

        /// <summary>系统时间（月）</summary>
        public const ushort SystemTimeMonth = 32;

        /// <summary>系统时间（日）</summary>
        public const ushort SystemTimeDay = 33;

        /// <summary>系统时间（时）</summary>
        public const ushort SystemTimeHour = 34;

        /// <summary>系统时间（分）</summary>
        public const ushort SystemTimeMinute = 35;

        /// <summary>系统时间（秒）</summary>
        public const ushort SystemTimeSecond = 36;

        // ═══════════════════════════════════════════════════════════
        // 定值信息（ReadOnly）
        // ═══════════════════════════════════════════════════════════

        /// <summary>定值已运行时间（小时）</summary>
        public const ushort SetValueElapsedTimeHour = 39;

        /// <summary>定值已运行时间（分钟）</summary>
        public const ushort SetValueElapsedTimeMinute = 40;

        /// <summary>当前PID区域（从0开始计算）</summary>
        public const ushort CurrentPIDZone = 41;

        // ═══════════════════════════════════════════════════════════
        // 设定值（ReadWrite）
        // ═══════════════════════════════════════════════════════════

        /// <summary>温度定值SV（设定值）</summary>
        public const ushort TemperatureSV = 43;

        /// <summary>湿度定值SV（设定值）</summary>
        public const ushort HumiditySV = 44;

        /// <summary>定值定时运行时间</summary>
        public const ushort SetValueRunTime = 45;

        // ═══════════════════════════════════════════════════════════
        // 控制命令（ReadWrite）
        // ═══════════════════════════════════════════════════════════

        /// <summary>定值控制：1=启动, 0=停止, 2=保持, 3=运行中</summary>
        public const ushort SetValueControl = 47;

        /// <summary>程序控制：1=启动, 0=停止, 2=保持, 4=跳步</summary>
        public const ushort ProgramControl = 48;

        // ═══════════════════════════════════════════════════════════
        // 当前设定值（ReadOnly）
        // ═══════════════════════════════════════════════════════════

        /// <summary>当前温度SV（设定值）</summary>
        public const ushort CurrentTemperatureSV = 51;

        /// <summary>当前湿度SV（设定值）</summary>
        public const ushort CurrentHumiditySV = 52;

        /// <summary>当前湿球PV（过程值）</summary>
        public const ushort WetBulbPV = 53;

        /// <summary>当前湿球SV（设定值）</summary>
        public const ushort WetBulbSV = 54;

        // ═══════════════════════════════════════════════════════════
        // 斜率设置（ReadWrite）
        // ═══════════════════════════════════════════════════════════

        /// <summary>定值温度斜率值</summary>
        public const ushort TemperatureSlope = 55;

        /// <summary>定值湿度斜率值</summary>
        public const ushort HumiditySlope = 56;

        // ═══════════════════════════════════════════════════════════
        // 冷输出（ReadOnly）
        // ═══════════════════════════════════════════════════════════

        /// <summary>温度冷MV（操纵值）</summary>
        public const ushort TemperatureColdMV = 57;

        /// <summary>湿度冷MV（操纵值）</summary>
        public const ushort HumidityColdMV = 58;

        // ═══════════════════════════════════════════════════════════
        // 定值控制枚举
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 定值控制命令枚举
        /// </summary>
        public enum SetValueControlCommand : ushort
        {
            /// <summary>停止</summary>
            Stop = 0,
            /// <summary>启动</summary>
            Start = 1,
            /// <summary>保持</summary>
            Hold = 2,
            /// <summary>运行中（状态）</summary>
            Running = 3
        }

        /// <summary>
        /// 程序控制命令枚举
        /// </summary>
        public enum ProgramControlCommand : ushort
        {
            /// <summary>停止</summary>
            Stop = 0,
            /// <summary>启动</summary>
            Start = 1,
            /// <summary>保持</summary>
            Hold = 2,
            /// <summary>跳步</summary>
            Skip = 4
        }
    }
}
