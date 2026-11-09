namespace DeviceLink.Osi
{
    /// <summary>
    /// 内置协议栈画像库 —— 每个协议的 OSI 归属在此集中声明，全框架唯一权威。
    ///
    /// 四类层的含义：
    /// · <b>实现层</b>：必须由框架组件提供的层，装配时逐层校验；
    /// · <b>委托层</b>：由操作系统、硬件或对端模块提供的层，框架不实现；
    /// · <b>内联层</b>：职责存在但被折叠进相邻层，没有独立组件；
    /// · <b>可扩展层</b>：不属于协议本身（仍计入空缺），但框架允许补一个实现且不算越界。
    ///
    /// 关键结论（与工程实践一致）：
    /// · RS-232 / RS-485：只有 L1 物理 + L2 分帧 + L7 业务，L3-L5 天然空缺；
    /// · Modbus RTU：L1 + L2（CRC16 校验）+ L7，无 L3-L5，L6 内联进编解码器；
    /// · Modbus TCP：L1-L4 由以太网/TCP 提供，L7 为 PDU；
    ///   MBAP 事务标识只做单次请求-响应配对，<b>不构成 L5</b>，因此 L5 空缺；
    /// · MQTT：L1-L4 由 TCP/IP 提供，L5 由 MQTT 会话提供（clean session / keepalive），L7 为业务载荷；
    /// · L6 表示层在所有协议中都被内联 —— 字节序与类型解释散落在各编解码器里，
    ///   这正是工业协议需要人工维护寄存器表的根本原因。
    ///
    /// <b>关于 extendable: "L5"</b>：没有会话层的协议，框架仍会挂一个 <c>DirectSession</c>
    /// 来统一"连接生命周期 + 请求-响应配对 + 安全重连"。这是框架能力增强，
    /// 不改变"该协议没有会话层"这一事实，因此只在 extendable 中放行、不写入 implemented。
    /// </summary>
    public static class OsiProfiles
    {
        /// <summary>RS-232 串口直连（无地址、无校验，靠分隔符/静默定帧）。</summary>
        public static readonly OsiStackProfile Rs232 = new OsiStackProfile(
            name: "RS-232 串口直连",
            implemented: "L1,L2,L7",
            inlined: "L6",
            allowShared: "L2",
            extendable: "L5",
            notes: "无寻址、无 CRC；L2 依赖分隔符或空闲间隔定帧；L3-L5 全部空缺。");

        /// <summary>RS-485 多点半双工（从站地址寻址属 L2）。</summary>
        public static readonly OsiStackProfile Rs485 = new OsiStackProfile(
            name: "RS-485 多点总线",
            implemented: "L1,L2,L7",
            inlined: "L6",
            allowShared: "L2",
            extendable: "L5",
            notes: "L2 提供从站地址寻址与 CRC 校验；无 L3 路由，总线即网段。");

        /// <summary>Modbus RTU over RS-485。</summary>
        public static readonly OsiStackProfile ModbusRtu = new OsiStackProfile(
            name: "Modbus RTU over RS-485",
            implemented: "L1,L2,L7",
            inlined: "L6",
            allowShared: "L2",
            extendable: "L5",
            notes: "L2 负责 3.5 字符间隔定界与 CRC16；L7 为功能码 PDU；无会话层，靠主从轮询替代。");

        /// <summary>SCPI over RS-232（仪器控制）。</summary>
        public static readonly OsiStackProfile ScpiOverRs232 = new OsiStackProfile(
            name: "SCPI over RS-232",
            implemented: "L1,L2,L7",
            inlined: "L6",
            allowShared: "L2",
            extendable: "L5",
            notes: "L2 以 \\n 分隔符定帧；L7 为 SCPI 命令语法；L3-L5 空缺。");

        /// <summary>SCPI over TCP（仪器控制，LXI）。</summary>
        public static readonly OsiStackProfile ScpiOverTcp = new OsiStackProfile(
            name: "SCPI over TCP",
            implemented: "L1-L4,L7",
            inlined: "L6",
            allowShared: "L2",
            extendable: "L5",
            notes: "TCP 提供 L3/L4；L2 由 \\n 分隔符完成应用消息定界；无 L5 会话层。");

        /// <summary>ConST 私有文本协议 over RS-232 / RS-485。</summary>
        public static readonly OsiStackProfile ConSTOverRs232 = new OsiStackProfile(
            name: "ConST over RS-232",
            implemented: "L1,L2,L7",
            inlined: "L6",
            allowShared: "L2",
            extendable: "L5",
            notes: "L2 以 \\0 分隔符定帧；L7 为地址 + 读写命令语法；L3-L5 空缺。");

        /// <summary>ConST 私有文本协议 over TCP（串口服务器透传）。</summary>
        public static readonly OsiStackProfile ConSTOverTcp = new OsiStackProfile(
            name: "ConST over TCP",
            implemented: "L1-L4,L7",
            inlined: "L6",
            allowShared: "L2",
            extendable: "L5",
            notes: "TCP 提供 L3/L4；应用消息仍以 \\0 定界；无 L5 会话层。");

        /// <summary>Modbus TCP —— 没有会话层。</summary>
        public static readonly OsiStackProfile ModbusTcp = new OsiStackProfile(
            name: "Modbus TCP",
            implemented: "L1-L4,L7",
            inlined: "L6",
            allowShared: "L2",
            extendable: "L5",
            notes: "MBAP 事务标识只做单次请求-响应配对（correlation），不建立、不保持、不恢复会话状态，" +
                   "因此不构成 L5 —— Modbus TCP 没有会话层。L5 与独立 L6 均空缺。" +
                   "MBAP 长度字段的应用消息定界由 LengthPrefixedFrameStrategy 承担（L2 职责在 TCP 之上再次落实）。");

        /// <summary>MQTT（物联网远程通信）。</summary>
        public static readonly OsiStackProfile Mqtt = new OsiStackProfile(
            name: "MQTT over TCP",
            implemented: "L1-L5,L7",
            inlined: "L6",
            allowShared: "L2,L7",
            notes: "TCP/IP 提供 L1-L4；MQTT 的 session/keepalive/QoS 承担 L5；载荷为裸字节。");

        /// <summary>Zigbee 透传（串口 → ZM32 模块 → 无线 → 终端）。</summary>
        public static readonly OsiStackProfile ZigbeeTransparent = new OsiStackProfile(
            name: "Zigbee 透传（ZM32）",
            implemented: "L1,L7",
            delegated: "L2-L3",
            inlined: "L6",
            extendable: "L5",
            notes: "模块内部实现 Zigbee MAC/NWK（L2-L3）；本机只有串口 L1 与 AT 配置业务面 L7，不参与数据分帧。");

        /// <summary>USB HID / 虚拟串口。</summary>
        public static readonly OsiStackProfile UsbHid = new OsiStackProfile(
            name: "USB HID",
            implemented: "L1,L2,L7",
            inlined: "L6",
            allowShared: "L2",
            extendable: "L5",
            notes: "USB 传输与报文分帧由驱动完成；无 L3-L5。");

        /// <summary>蓝牙 SPP（串口透传）。</summary>
        public static readonly OsiStackProfile BluetoothSpp = new OsiStackProfile(
            name: "Bluetooth SPP",
            implemented: "L1-L4,L7",
            inlined: "L6",
            allowShared: "L2",
            extendable: "L5",
            notes: "蓝牙链路层与 L2CAP/RFCOMM 提供 L1-L4；无 L5 会话层。");

        /// <summary>Modbus RTU over TCP（串口服务器透传场景）。</summary>
        public static readonly OsiStackProfile ModbusRtuOverTcp = new OsiStackProfile(
            name: "Modbus RTU over TCP",
            implemented: "L1-L4,L7",
            inlined: "L6",
            allowShared: "L2",
            extendable: "L5",
            notes: "TCP 透传后仍按 RTU 帧（CRC16）定界 —— 以太网 L2 与 RTU L2 是两次独立的分帧职责。");

        /// <summary>全部内置画像，用于自检与文档生成。</summary>
        public static readonly System.Collections.Generic.IReadOnlyList<OsiStackProfile> All = new[]
        {
            Rs232, Rs485, ModbusRtu, ScpiOverRs232, ScpiOverTcp, ConSTOverRs232, ConSTOverTcp,
            ModbusTcp, Mqtt, ZigbeeTransparent, UsbHid, BluetoothSpp, ModbusRtuOverTcp
        };

        /// <summary>按名称查找内置画像，未命中返回 null。</summary>
        public static OsiStackProfile? Find(string name)
        {
            if (name == null || name.Trim().Length == 0) return null;

            foreach (var profile in All)
            {
                if (string.Equals(profile.Name, name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return profile;
                }
            }
            return null;
        }
    }
}
