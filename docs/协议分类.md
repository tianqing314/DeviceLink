# DeviceLink 工业通讯协议分层分析

## 1. 项目架构概述

DeviceLink 框架基于 OSI 七层模型简化为五层架构：

```
┌─────────────────────────────────────────────────────────────────┐
│                  Layer 5: 应用层 (DeviceLink.Devices)           │
│                DeviceBase → DPSEX, DPG, 自定义设备               │
├──────────────────────────┬──────────────────────────────────────┤
│  Layer 3: 会话层          │  Layer 4: 协议层                    │
│   DeviceLink.Session      │  DeviceLink.Protocol                │
│  ISession→DirectSession   │  IProtocolCodec→ConST/SCPI/Modbus   │
├──────────────────────────┴──────────────────────────────────────┤
│                  Layer 2: 数据链路层 (DeviceLink.DataLink)      │
│     IFrameStrategy → 分隔符帧, 定长帧, Modbus RTU 帧             │
├─────────────────────────────────────────────────────────────────┤
│                  Layer 1: 物理传输层 (DeviceLink.Transport)     │
│   IPhysicalTransport → 串口, TCP, UDP, USB, 回环                │
└─────────────────────────────────────────────────────────────────┘
```

### 核心接口

| 层级 | 接口 | 职责 |
|------|------|------|
| 物理传输层 | `IPhysicalTransport` | 底层字节传输，无协议/分帧概念 |
| 数据链路层 | `IDataLink` / `IFrameStrategy` | 帧组装、解析和边界检测 |
| 会话层 | `ISession` | 请求-响应会话管理，超时重试 |
| 协议层 | `IProtocolCodec` | 命令编码/响应解码 |
| 应用层 | `DeviceBase` | 设备业务逻辑封装 |

## 2. 已实现协议分析

### 2.1 物理传输层 (DeviceLink.Transport)

| 实现类 | 协议 | 说明 | 状态 |
|--------|------|------|------|
| `SerialPortTransport` | RS-232/RS-485 | 串口通信，支持波特率、数据位、停止位配置 | ✅ 已实现 |
| `TcpTransport` | TCP/IP | 面向连接的网络通信 | ✅ 已实现 |
| `UdpTransport` | UDP | 无连接网络通信 | ✅ 已实现 |
| `UsbTransport` | USB | USB 设备通信（需第三方库） | ✅ 已实现 |
| `LoopbackTransport` | 回环 | 内存测试传输，用于单元测试 | ✅ 已实现 |

### 2.2 数据链路层 (DeviceLink.DataLink)

| 实现类 | 帧策略 | 说明 | 状态 |
|--------|--------|------|------|
| `DelimiterFrameStrategy` | 分隔符帧 | 使用特定字节序列标识帧边界 | ✅ 已实现 |
| `FixedLengthFrameStrategy` | 定长帧 | 固定长度的帧 | ✅ 已实现 |
| `ModbusRtuFrameStrategy` | Modbus RTU 帧 | 包含 CRC16 校验的 Modbus 帧 | ✅ 已实现 |

### 2.3 协议层 (DeviceLink.Protocol)

| 实现类 | 协议 | 说明 | 状态 |
|--------|------|------|------|
| `ConSTCodec` | ConST | ConST 设备协议（地址:R/W:命令:参数:\0） | ✅ 已实现 |
| `ScpiCodec` | SCPI | 标准仪器控制协议 | ✅ 已实现 |
| `ModbusRtuCodec` | Modbus RTU | 工业标准 Modbus RTU 协议 | ✅ 已实现 |

### 2.4 会话层 (DeviceLink.Session)

| 实现类 | 说明 | 状态 |
|--------|------|------|
| `DirectSession` | 直连会话，支持超时重试、线程安全 | ✅ 已实现 |
| `MqttSession` | MQTT 会话（远程通信） | 🔄 计划中 |

## 3. 工业通讯协议分层映射

### 3.1 协议分类矩阵

| 协议 | 物理层 | 数据链路层 | 协议层 | 会话层 | 复杂度 |
|------|--------|------------|--------|--------|--------|
| **蓝牙** | ✅ BluetoothTransport | 分隔符帧 | 自定义 | DirectSession | A类 |
| **CAN总线** | ✅ CanTransport | ✅ CanFrameStrategy | CANopen | DirectSession | B类 |
| **GPIB/IEEE-488** | ✅ GpibTransport | 分隔符帧 | SCPI | DirectSession | A类 |
| **HART** | ✅ HartTransport | ✅ HartFrameStrategy | ✅ HartCodec | DirectSession | B类 |
| **PROFIBUS** | ✅ ProfibusTransport | ✅ ProfibusFrameStrategy | ✅ ProfibusCodec | DirectSession | B类 |
| **PROFINET** | ✅ ProfinetTransport | Ethernet帧 | PROFINET协议 | ✅ ProfinetSession | C类 |
| **EtherCAT** | ✅ EtherCatTransport | ✅ EtherCatFrameStrategy | ✅ EtherCatCodec | DirectSession | B类 |
| **OPC UA** | TCP/HTTP | - | ✅ OpcUaCodec | ✅ OpcUaSession | C类 |
| **光纤** | 光电转换器→TCP/串口 | 复用现有 | 复用现有 | 复用现有 | 透明 |
| **Zigbee** | ✅ ZigbeeTransport | 分隔符帧 | AT指令 | DirectSession | A类 |
| **LoRa** | ✅ LoRaTransport | 分隔符帧 | AT指令 | DirectSession | A类 |
| **NB-IoT** | ✅ NbIotTransport | 分隔符帧 | AT指令 | DirectSession | A类 |
| **Modbus TCP** | TcpTransport | 分隔符帧 | ModbusTcpCodec | DirectSession | A类 |

### 3.2 复杂度分类说明

#### A类：仅需物理层扩展（复用现有帧策略和协议）

这些协议只需要实现新的 `IPhysicalTransport`，可以完全复用现有的 `DelimiterFrameStrategy` 和 `DirectSession`。

**代表协议**：
- **蓝牙**：使用 `InTheHand.Net.Bluetooth` (32feet.NET) 库
- **GPIB**：使用 `NI-488.2` 或 `linux-gpib` 库
- **Zigbee/LoRa/NB-IoT**：通常使用 AT 指令集，通过串口或 TCP 通信
- **Modbus TCP**：复用现有 `ModbusRtuCodec`，仅需 TCP 传输

**实现示例**：
```csharp
// 蓝牙传输层实现
public class BluetoothTransport : IPhysicalTransport
{
    private BluetoothClient _client;
    
    public string Name => $"Bluetooth:{_deviceAddress}";
    public bool IsOpen => _client?.Connected ?? false;
    
    public async Task ConnectAsync(CancellationToken ct = default)
    {
        _client = new BluetoothClient();
        await _client.ConnectAsync(_deviceAddress, BluetoothService.SerialPort);
    }
    
    // ... 其他接口实现
}
```

### Zigbee协议实现详情

Zigbee协议已实现模块化架构，支持多种厂商的Zigbee模块：

#### 架构设计

```
┌─────────────────────────────────────────────────────────────────┐
│                    ZigbeeTransport                              │
│              (实现 IPhysicalTransport)                           │
├─────────────────────────────────────────────────────────────────┤
│                       IZigbeeModule                             │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐               │
│  │ XBeeModule  │ │ CC2530Module│ │  ZM32Module  │               │
│  │   (Digi)    │ │    (TI)     │ │   (ZLG)     │               │
│  └─────────────┘ └─────────────┘ └─────────────┘               │
├─────────────────────────────────────────────────────────────────┤
│               SerialPortTransport (复用)                        │
└─────────────────────────────────────────────────────────────────┘
```

#### 支持的模块

| 模块 | 厂商 | AT指令格式 | 数据帧格式 | 特点 |
|------|------|------------|------------|------|
| **XBee** | Digi | `ATID1234` | API帧/透明模式 | 支持API模式和透明模式 |
| **CC2530** | TI | `AT+PANID=1234` | 透明传输 | 始终处于AT命令模式 |
| **ZM32** | ZLG | `AT+PANID=1234` | 完全透明传输 | 3线制串口全透明 |

#### 核心接口

```csharp
/// <summary>
/// Zigbee模块抽象接口
/// </summary>
public interface IZigbeeModule
{
    string Name { get; }
    
    // 配置相关
    Task EnterCommandModeAsync(IPhysicalTransport transport, CancellationToken ct = default);
    Task ExitCommandModeAsync(IPhysicalTransport transport, CancellationToken ct = default);
    Task ConfigurePanIdAsync(IPhysicalTransport transport, ushort panId, CancellationToken ct = default);
    Task ConfigureChannelAsync(IPhysicalTransport transport, byte channel, CancellationToken ct = default);
    Task ConfigureDestinationAsync(IPhysicalTransport transport, ulong destAddress, CancellationToken ct = default);
    
    // 数据帧处理
    byte[] BuildDataFrame(byte[] data, string? destination = null);
    bool TryParseDataFrame(byte[] frame, out byte[] data, out string? source);
}
```

#### 使用示例

```csharp
// 创建Zigbee传输层
var options = new ZigbeeOptions
{
    ModuleType = ZigbeeModuleType.ZM32,
    PortName = "COM5",
    BaudRate = 9600,
    PanId = 0x1234,
    Channel = 0x0B
};

var transport = new ZigbeeTransport(options);
await transport.ConnectAsync();

// 使用现有框架组件
var dataLink = new DirectDataLink(transport, new DelimiterFrameStrategy("\r\n"));
var session = new DirectSession(dataLink);
var codec = new ConSTCodec(1);
var device = new DPSEX(session, codec);
```

#### 配置选项

```csharp
public class ZigbeeOptions : SerialPortOptions
{
    public ZigbeeModuleType ModuleType { get; set; } = ZigbeeModuleType.ZM32;
    public ushort PanId { get; set; } = 0x1234;
    public byte Channel { get; set; } = 0x0B; // Channel 11
    public ulong DestinationAddress { get; set; } = 0;
    public bool UseApiMode { get; set; } = false; // XBee专用
    public int GuardTimeMs { get; set; } = 1000;
    public int CommandTimeoutMs { get; set; } = 2000;
    
    // ZM32特有参数
    public ushort ZM32_TargetNetworkAddress { get; set; } = 0x0000;
    public byte ZM32_SendMode { get; set; } = 0x01; // 0x01=单播
    public byte ZM32_DeviceType { get; set; } = 0x00; // 0=协调器, 1=路由器, 2=终端
    public bool ZM32_EnableAutoNetwork { get; set; } = false;
    public ushort ZM32_TargetGroupNumber { get; set; } = 0x0001;
}
```

#### 无感设计（Transparent Design）

Zigbee 传输层的核心设计原则是 **应用层无感**：上层设备（如 DPSEX、DPG）无需感知底层通讯介质的变化。只需在构造函数中替换传输层实例，即可实现串口直连与 Zigbee 无线通讯的无缝切换。

```csharp
// 方式一：串口直连
var serialTransport = new SerialPortTransport(serialOptions);
var device = new DPSEX(new DirectSession(new DirectDataLink(serialTransport, frameStrategy)), codec);

// 方式二：Zigbee 无线（仅替换传输层，其余代码完全相同）
var zigbeeTransport = new ZigbeeTransport(zigbeeOptions);
var device = new DPSEX(new DirectSession(new DirectDataLink(zigbeeTransport, frameStrategy)), codec);
```

**架构流程**：

```
┌──────────────────────────────────────────────────────────────────────────┐
│  应用层 (DPSEX)          发送: "READ:PRESSURE\0"                        │
│       ↓                                                                │
│  协议层 (ConSTCodec)     编码: "1:R:READ:PRESSURE:\0"                   │
│       ↓                                                                │
│  会话层 (DirectSession)  请求-响应管理, 超时重试                          │
│       ↓                                                                │
│  数据链路层 (Delimiter)  组帧: [STX] + data + [ETX]                     │
│       ↓                                                                │
│  物理传输层 (Zigbee)     自动配置ZM32 → 透明透传数据                      │
│       ↓                                                                │
│  串口 (SerialPort)       → ZM32协调器 → Zigbee无线 → ZM32终端 → 传感器   │
└──────────────────────────────────────────────────────────────────────────┘
```

**自动配置流程**（`ConnectAsync` 时自动完成，用户无感）：

1. 打开串口连接
2. ZM32 进入命令模式（`+++`）
3. 配置 PAN ID（`AT+PANID=xxxx`）
4. 配置信道（`AT+CHANNEL=xx`）
5. 配置目标网络地址（`DE DF EF D2 + addr`）
6. 配置发送模式（`DE DF EF D9 + mode`）
7. 配置自组网（`AB BC CD 27 + config + AA`）
8. 退出命令模式（`AT+EXIT`）
9. 进入透明传输模式

#### B类：需要物理层+数据链路层扩展

这些协议需要同时实现新的 `IPhysicalTransport` 和 `IFrameStrategy`，因为它们有特殊的帧格式。

**代表协议**：
- **CAN总线**：需要实现 CAN 帧格式（标准帧/扩展帧）
- **HART**：需要实现 HART 协议帧（前导码、定界符、地址、命令、数据、校验）
- **PROFIBUS**：需要实现 PROFIBUS 帧格式
- **EtherCAT**：需要实现 EtherCAT 帧格式

**实现示例**：
```csharp
// CAN 总线帧策略
public class CanFrameStrategy : IFrameStrategy
{
    public string Name => "CAN";
    
    public byte[] BuildFrame(byte[] data)
    {
        // 构建 CAN 帧：ID + DLC + Data + CRC
        var frame = new byte[13]; // 标准帧最大长度
        // ... 实现帧组装
        return frame;
    }
    
    public bool TryParseFrame(byte[] accumulated, out int frameLength, out byte[] frameData)
    {
        // 解析 CAN 帧
        // ... 实现帧解析
    }
}
```

#### C类：需要全新会话层或协议层

这些协议有复杂的会话管理或协议栈，需要实现新的 `ISession` 或 `IProtocolCodec`。

**代表协议**：
- **PROFINET**：需要实时以太网会话管理
- **OPC UA**：需要复杂的安全会话和订阅机制

**实现示例**：
```csharp
// OPC UA 会话层
public class OpcUaSession : ISession
{
    private OpcUaClient _client;
    private Session _session;
    
    public async Task<byte[]> SendAndReceiveAsync(byte[] request, CancellationToken ct = default)
    {
        // OPC UA 有复杂的安全握手和会话建立过程
        // ... 实现 OPC UA 调用
    }
}
```

## 4. 扩展需求分析

### 4.1 第三方库依赖

| 协议 | 推荐库 | NuGet 包 | 说明 |
|------|--------|----------|------|
| 蓝牙 | 32feet.NET | `InTheHand.Net.Bluetooth` | 跨平台蓝牙通信 |
| CAN总线 | PCAN-Basic | `PCANBasic` | Peak CAN 适配器 API |
| GPIB | NI-488.2 | `NationalInstruments.NI4882` | NI GPIB 适配器 |
| OPC UA | OPC Foundation | `OPCFoundation.NetStandard.Opc.Ua` | 官方 OPC UA SDK |
| HART | 自定义实现 | - | HART 协议相对简单 |

### 4.2 架构扩展点

#### 1. 传输层工厂模式

```csharp
public static class TransportFactory
{
    public static IPhysicalTransport Create(TransportType type, TransportOptions options)
    {
        return type switch
        {
            TransportType.SerialPort => new SerialPortTransport(options as SerialPortOptions),
            TransportType.Tcp => new TcpTransport(options as TcpOptions),
            TransportType.Bluetooth => new BluetoothTransport(options as BluetoothOptions),
            TransportType.CAN => new CanTransport(options as CanOptions),
            _ => throw new NotSupportedException($"Transport type {type} not supported")
        };
    }
}
```

#### 2. 帧策略注册机制

```csharp
public static class FrameStrategyRegistry
{
    private static readonly Dictionary<string, Func<IFrameStrategy>> _strategies = new();
    
    public static void Register(string name, Func<IFrameStrategy> factory)
    {
        _strategies[name] = factory;
    }
    
    public static IFrameStrategy Create(string name)
    {
        if (_strategies.TryGetValue(name, out var factory))
            return factory();
        throw new NotSupportedException($"Frame strategy '{name}' not registered");
    }
}

// 注册自定义帧策略
FrameStrategyRegistry.Register("CAN", () => new CanFrameStrategy());
FrameStrategyRegistry.Register("HART", () => new HartFrameStrategy());
```

#### 3. 协议插件化

```csharp
public interface IProtocolPlugin
{
    string Name { get; }
    IProtocolCodec CreateCodec(ProtocolOptions options);
    IFrameStrategy? CreateFrameStrategy(); // 可选
}

// 动态加载协议插件
var plugins = LoadPlugins("./plugins");
foreach (var plugin in plugins)
{
    ProtocolRegistry.Register(plugin.Name, plugin);
}
```

### 4.3 配置扩展

#### 传输层配置基类

```csharp
public abstract class TransportOptions
{
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan ReadTimeout { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan WriteTimeout { get; set; } = TimeSpan.FromSeconds(1);
}

public class BluetoothOptions : TransportOptions
{
    public string DeviceAddress { get; set; }
    public BluetoothService Service { get; set; } = BluetoothService.SerialPort;
    public bool SecureConnection { get; set; } = true;
}

public class CanOptions : TransportOptions
{
    public string Channel { get; set; } = "PCAN_USBBUS1";
    public uint BaudRate { get; set; } = 500000;
    public uint ReceiveEvent { get; set; }
}
```

## 5. 实施路线图

### 第一阶段：高优先级（1-2个月）

| 序号 | 协议 | 工作量 | 依赖库 | 说明 |
|------|------|--------|--------|------|
| 1 | Modbus TCP | 1周 | 无 | 复用现有 Modbus RTU，仅需 TCP 传输 |
| 2 | HART | 2周 | 自定义 | 工业过程控制核心协议 |
| 3 | 蓝牙 | 2周 | InTheHand.Net.Bluetooth | 物联网和移动设备支持 |

**第一阶段交付物**：
- `ModbusTcpCodec` 实现
- `HartTransport` + `HartFrameStrategy` + `HartCodec`
- `BluetoothTransport` + `BluetoothOptions`

### 第二阶段：中优先级（2-3个月）

| 序号 | 协议 | 工作量 | 依赖库 | 说明 |
|------|------|--------|--------|------|
| 4 | CAN总线 | 3周 | PCANBasic | 汽车和工业自动化 |
| 5 | GPIB | 2周 | NI-488.2 | 仪器控制标准 |
| 6 | OPC UA | 4周 | OPCFoundation.NetStandard.Opc.Ua | 工业4.0标准 |

**第二阶段交付物**：
- `CanTransport` + `CanFrameStrategy` + `CanopenCodec`
- `GpibTransport`（复用 SCPI 协议）
- `OpcUaCodec` + `OpcUaSession`

### 第三阶段：低优先级（3-6个月）

| 序号 | 协议 | 工作量 | 依赖库 | 说明 |
|------|------|--------|--------|------|
| 7 | PROFIBUS | 3周 | 自定义 | 传统 PLC 通讯 |
| 8 | PROFINET | 4周 | 自定义 | 实时以太网 |
| 9 | EtherCAT | 4周 | 自定义 | 高端运动控制 |
| 10 | Zigbee/LoRa/NB-IoT | 2周 | 自定义 | 物联网长距离通讯 |

**第三阶段交付物**：
- 完整的工业以太网协议支持
- 物联网低功耗广域网支持

## 6. 技术决策建议

### 6.1 向后兼容性策略

1. **接口不变**：所有现有 `IPhysicalTransport`、`IDataLink`、`ISession`、`IProtocolCodec` 接口保持不变
2. **新接口继承**：新协议接口继承现有基础接口
3. **适配器模式**：通过适配器支持新旧协议混合使用

### 6.2 测试策略

1. **LoopbackTransport 扩展**：为每种新协议创建专用的回环传输
2. **协议模拟器**：创建协议模拟器用于集成测试
3. **硬件在环测试**：保留可选的真实硬件测试

### 6.3 文档和示例

1. **协议集成指南**：每种新协议提供详细的集成文档
2. **示例项目**：创建独立的示例项目展示每种协议的使用
3. **API 参考**：更新 API 文档，包含新协议的说明

## 7. 总结

DeviceLink 框架的分层架构为扩展新的工业通讯协议提供了良好的基础：

- **A类协议**（蓝牙、GPIB、Zigbee等）可以快速实现，只需新增传输层
- **B类协议**（CAN、HART、PROFIBUS等）需要中等工作量，需新增传输层和数据链路层
- **C类协议**（PROFINET、OPC UA等）需要较大工作量，需重新设计会话层

建议按照优先级分阶段实施，先实现高价值的工业协议（Modbus TCP、HART、蓝牙），再逐步扩展其他协议。同时，建立完善的扩展机制（工厂模式、注册机制、插件化）可以降低后续协议集成的复杂度。