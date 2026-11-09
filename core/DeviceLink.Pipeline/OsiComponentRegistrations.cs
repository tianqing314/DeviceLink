using System.Threading;
using DeviceLink.DataLink;
using DeviceLink.Osi;
using DeviceLink.Protocol;
using DeviceLink.Session;
using DeviceLink.Transport;

namespace DeviceLink.Pipeline
{
    /// <summary>
    /// 既有组件的 OSI 层归属登记 —— 全框架唯一的"谁在哪一层"清单。
    ///
    /// 设计取舍：不修改既有组件的类定义（避免破坏 20+ 个项目与测试），
    /// 而是集中登记层归属，由 <see cref="OsiAssembler"/> 在装配阶段校验。
    /// 未登记的类型会直接抛异常，不存在"默认层"这种蒙混方式。
    ///
    /// 每条登记都必须在 Reason 里写明层归属依据；若组件当前存在越界行为，
    /// 也必须在 Reason 里如实标注，不允许粉饰。
    /// </summary>
    public static class OsiComponentRegistrations
    {
        private static int _initialized;

        /// <summary>幂等登记全部内置组件。</summary>
        public static void EnsureRegistered()
        {
            if (Interlocked.Exchange(ref _initialized, 1) != 0) return;
            RegisterPhysicalLayer();
            RegisterDataLinkLayer();
            RegisterSessionLayer();
            RegisterPresentationAndApplicationLayer();
        }

        // ───────────────────────── L1 物理层 ─────────────────────────

        private static void RegisterPhysicalLayer()
        {
            OsiLayerRegistry.Register<SerialPortTransport>(
                "SerialPortTransport", "L1",
                reason: "只搬运字节并控制电气参数（波特率/数据位/DTR/RTS），不识别帧边界，不做校验。");

            OsiLayerRegistry.Register<LoopbackTransport>(
                "LoopbackTransport", "L1",
                reason: "内存回环的字节通道，仅用于测试，不具备任何协议语义。");

            OsiLayerRegistry.Register<UsbTransport>(
                "UsbTransport", "L1",
                reason: "USB 报文分帧由驱动与端点完成，本组件只做端点读写。");

            OsiLayerRegistry.Register<TcpTransport>(
                "TcpTransport", "L1-L4",
                reason: "封装 TCP 连接：连接管理属 L4、端到端可靠传输属 L4、IP 寻址属 L3、以太网帧属 L1-L2，均由系统协议栈提供。");

            OsiLayerRegistry.Register<UdpTransport>(
                "UdpTransport", "L1-L4",
                reason: "封装 UDP 数据报，同样由系统协议栈覆盖 L1-L4。");

            OsiLayerRegistry.Register<BluetoothTransport>(
                "BluetoothTransport", "L1-L4",
                reason: "SPP/RFCOMM 已由蓝牙协议栈提供链路层到传输层，本组件只是套接字封装。");

            OsiLayerRegistry.Register<ZigbeeTransport>(
                "ZigbeeTransport", "L1,L2,L7",
                reason: "L1 为串口字节通道；L7 为 ZM32 的 AT/私有配置协议（业务面）。" +
                        "注意：WriteAsync 中的 BuildDataFrame 属于模块侧的报文封装，是越界候选项，" +
                        "目标态迁往 ZigbeeFrameStrategy（见 docs/OSI层次归属.md 迁移项 M2）。");
        }

        // ───────────────────────── L2 数据链路层 ─────────────────────────

        private static void RegisterDataLinkLayer()
        {
            OsiLayerRegistry.Register<DelimiterFrameStrategy>(
                "DelimiterFrameStrategy", "L2",
                reason: "以结束符定界，负责帧边界识别与终止符去重。");

            OsiLayerRegistry.Register<FixedLengthFrameStrategy>(
                "FixedLengthFrameStrategy", "L2",
                reason: "以固定长度定界，负责帧切分。");

            OsiLayerRegistry.Register<ModbusRtuFrameStrategy>(
                "ModbusRtuFrameStrategy", "L2",
                reason: "负责 Modbus RTU 帧定界、CRC16 计算与校验；不解释功能码语义（那是 L7）。");

            OsiLayerRegistry.Register<LengthPrefixedFrameStrategy>(
                "LengthPrefixedFrameStrategy", "L2",
                reason: "以长度字段定界，强制校验长度上限后才分配内存（防 OOM），适用 MBAP 与各类二进制协议。");

            OsiLayerRegistry.Register<ZqwlFrameStrategy>(
                "ZqwlFrameStrategy", "L2",
                reason: "ZWQL 私有帧定界与校验。");

            OsiLayerRegistry.Register<CpplV3FrameStrategy>(
                "CpplV3FrameStrategy", "L2",
                reason: "CPPL V3 帧定界、CRC8/CRC16 双层校验。帧头流水号是协议字段，" +
                        "仅用于对端匹配，不构成本框架的会话层服务。");

            OsiLayerRegistry.Register<DirectDataLink>(
                "DirectDataLink", "L2",
                reason: "只负责组帧发送与接收循环中的帧解析。" +
                        "重试已全部上移到 DirectSession（M1 已关闭），" +
                        "本层不再持有 MaxRetryCount；SemaphoreSlim 仅用于保护单次收发的原子性。");
        }

        // ───────────────────────── L5 会话层 ─────────────────────────

        private static void RegisterSessionLayer()
        {
            OsiLayerRegistry.Register<DirectSession>(
                "DirectSession", "L5",
                reason: "请求-响应配对、超时控制、异常映射。当前重试实际下沉在 DirectDataLink，属待收敛项。");

            OsiLayerRegistry.Register<MqttSession>(
                "MqttSession", "L1-L5,L7",
                reason: "MQTT 自身是应用层协议（topic 路由属 L7），其 session/keepalive/QoS 承担 L5；" +
                        "它内部封装 TCP/IP（L1-L4），因此完整覆盖 L1-L5 与 L7，唯独不做 L6 表示层转换。");
        }

        // ─────────────────────── L6 表示层 / L7 应用层 ───────────────────────

        private static void RegisterPresentationAndApplicationLayer()
        {
            OsiLayerRegistry.Register<ConSTCodec>(
                "ConSTCodec", "L6,L7",
                reason: "L6：文本编码与 \\0 终止符处理；L7：地址/读写/命令的业务语法。");

            OsiLayerRegistry.Register<ScpiCodec>(
                "ScpiCodec", "L6,L7",
                reason: "L6：ASCII 编解码与终止符剥离；L7：SCPI 命令树与错误码语义。");

            OsiLayerRegistry.Register<ModbusRtuCodec>(
                "ModbusRtuCodec", "L6,L7",
                reason: "L6：寄存器大端字节序解释（ExtractRegisters 把字节还原为 ushort）；" +
                        "L7：功能码 PDU 组装与异常码语义。CRC 已上移到 L2，此处不再负责。");

            OsiLayerRegistry.Register<ZqwlCodec>(
                "ZqwlCodec", "L6,L7",
                reason: "L6：私有载荷的字节序与类型转换；L7：命令与状态语义。");

            OsiLayerRegistry.Register<ModbusTcpCodec>(
                "ModbusTcpCodec", "L6,L7",
                reason: "L6：寄存器大端字节序解释；L7：MBAP 事务标识/单元标识与功能码 PDU。" +
                        "长度字段属 L2，由 LengthPrefixedFrameStrategy 填充，本类不参与分帧。" +
                        "事务标识只做请求配对，不提供会话能力。");
        }
    }
}
