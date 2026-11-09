# DeviceLink.Pipeline

## 概述

`DeviceLink.Pipeline` 是 DeviceLink 框架的**通信管道构建器**，通过 Builder 模式将传输层、数据链路层、会话层、协议层灵活组合成完整的通信管道。提供预定义配置，简化常见通信场景的初始化代码。

## 主要职责

1. **管道构建** - 通过 Builder 模式构建通信管道
2. **层组合** - 灵活组合 OSI 各层，支持跳过中间层
3. **预定义配置** - 提供常用通信场景的开箱即用配置（串口 / TCP / Modbus RTU / SCPI / MQTT）
4. **自动组装** - 自动创建缺失的中间层（如自动生成数据链路层和会话层）
5. **资源管理** - 管理管道生命周期和资源释放

## 关键接口/类

### `CommunicationPipelineBuilder`

通信管道构建器，使用链式 API：

| 方法 | 描述 |
|------|------|
| `UseTransport(IPhysicalTransport)` | 设置物理传输层 |
| `UseDataLink(IFrameStrategy, DataLinkOptions? options = null)` | 设置数据链路层（通过帧策略自动创建，需先设置传输层） |
| `UseDataLink(IDataLink)` | 直接注入数据链路层 |
| `UseSession(ISession)` | 设置会话层 |
| `UseProtocol(IProtocolCodec)` | 设置协议层 |
| `UseLoggerFactory(ILoggerFactory)` | 设置日志工厂 |
| `Configure(Action<CommunicationPipeline>)` | 添加自定义配置 |
| `Build()` | 构建通信管道 |

**`Build()` 自动组装逻辑**:

1. 若未设置会话层：
   - 已设置数据链路层 → 自动创建 `DirectSession(dataLink)`
   - 已设置传输层 → 自动创建 `DelimiterFrameStrategy(\0)` + `DirectDataLink` + `DirectSession`
   - 两者均未设置 → 抛 `InvalidOperationException("必须设置物理传输层或会话层")`
2. 若未设置协议层 → 抛 `InvalidOperationException("必须设置协议层")`
3. 依次执行所有 `Configure` 回调

### `CommunicationPipeline`

通信管道，封装完整的通信栈：

| 属性/方法 | 描述 |
|-----------|------|
| `Transport` | 物理传输层（可能为 null，如 MQTT 管道） |
| `DataLink` | 数据链路层（可能为 null，如 MQTT 管道） |
| `Session` | 会话层 |
| `Protocol` | 协议层 |
| `LoggerFactory` | 日志工厂（可能为 null） |
| `OpenAsync(ct)` | 打开管道 |
| `CloseAsync()` | 关闭管道 |
| `SendAndReceiveAsync(request, ct)` | 发送原始字节并接收响应 |
| `OsiReport` | 构建时生成的 OSI 层次校验报告（无画像时为 `null`） |
| `Dispose()` / `DisposeAsync()` | 释放资源。所有权链：Session → DataLink → Transport；仅当没有 DataLink 时 Pipeline 才直接释放 Transport |

> **协议编码不在这里做。** 管道只处理字节；"逻辑命令 → 字节"与"错误码判定"属于设备层
> （`DeviceBase.SendAsync` 会调用 `IProtocolCodec` 并抛 `DeviceException`）。
> 历史上这里还有一个 `SendCommandAsync`，它与 `DeviceBase.SendAsync` 重复实现了错误检查、
> 抛的却是另一种异常，且全仓库无人调用 —— 已删除。

> **注意**: `CommunicationPipeline` 的构造函数是 `internal`，只能通过 `CommunicationPipelineBuilder.Build()` 创建，不能直接 `new`。

### `PipelinePresets`

预定义管道配置工厂方法（静态类）：

| 方法 | 管道配置 |
|------|---------|
| `CreateSerialPortPipeline(string portName, int baudRate = 9600, byte address = 255)` | 串口 + ConST 协议（`DelimiterFrameStrategy(\0)` + `ConSTCodec(address)`） |
| `CreateTcpPipeline(string host, int port, byte address = 255)` | TCP + ConST 协议（`DelimiterFrameStrategy(\0)` + `ConSTCodec(address)`） |
| `CreateModbusRtuPipeline(string portName, int baudRate = 9600, byte slaveAddress = 1)` | 串口 + Modbus RTU（`ModbusRtuFrameStrategy()` + `ModbusRtuCodec(slaveAddress)`） |
| `CreateScpiPipeline(string host, int port)` | TCP + SCPI（`DelimiterFrameStrategy("\n")` + `ScpiCodec()`） |
| `CreateMqttPipeline(string brokerHost, int brokerPort, string requestTopic, string responseTopic, byte address = 255, int requestTimeoutMs = 5000)` | MQTT + ConST（`MqttSession` + `ConSTCodec(address)`，无 Transport / DataLink 层） |

## 依赖关系

- **项目依赖**（引用所有 Core 层项目）:
  - `DeviceLink.Transport` - 物理传输层
  - `DeviceLink.DataLink` - 数据链路层
  - `DeviceLink.Session` - 会话层
  - `DeviceLink.Protocol` - 协议层
- **NuGet 依赖**:
  - `Microsoft.Extensions.Logging.Abstractions` 6.0.0 - 日志抽象

## 使用示例

### 自定义管道构建

```csharp
// ConST 串口通信
var pipeline = new CommunicationPipelineBuilder()
    .UseTransport(new SerialPortTransport("COM3", 9600))
    .UseDataLink(new DelimiterFrameStrategy(new byte[] { 0 }))
    .UseProtocol(new ConSTCodec(255))
    .Build();

await pipeline.OpenAsync();
var response = await pipeline.SendAndReceiveAsync(Encoding.ASCII.GetBytes("PRES?"));
await pipeline.CloseAsync();
```

### 预定义配置

```csharp
// 一行代码创建完整管道
using var pipeline = PipelinePresets.CreateSerialPortPipeline("COM3", 9600, 255);
await pipeline.OpenAsync();
var response = await pipeline.SendAndReceiveAsync(Encoding.ASCII.GetBytes("PRES?"));
```

### Modbus RTU 管道

```csharp
using var pipeline = PipelinePresets.CreateModbusRtuPipeline("COM3", 9600, 1);
await pipeline.OpenAsync();
// Modbus RTU 读保持寄存器：从站 1 / 功能码 03 / 起始 0x000A / 数量 1 / CRC16
var response = await pipeline.SendAndReceiveAsync(
    new byte[] { 0x01, 0x03, 0x00, 0x0A, 0x00, 0x01, 0x65, 0xC9 });
```

### MQTT 管道

```csharp
// MQTT 管道跳过传输层和数据链路层，直接使用 MQTT 会话
using var pipeline = PipelinePresets.CreateMqttPipeline(
    "192.168.1.50", 1883, "devicelink/request", "devicelink/response");
await pipeline.OpenAsync();
var response = await pipeline.SendAndReceiveAsync(Encoding.ASCII.GetBytes("PRES?"));
```

## 设计原则

1. **Builder 模式** - 链式 API，清晰直观
2. **灵活组合** - 支持跳过中间层，适应不同通信场景
3. **开箱即用** - 预定义配置覆盖常见场景
4. **自动推导** - 自动创建缺失的中间层
5. **资源管理** - 实现 `IDisposable`，确保资源正确释放

## 注意事项

1. 必须设置协议层，否则 `Build()` 会抛出异常
2. 必须设置物理传输层或会话层，否则 `Build()` 会抛出异常
3. 如果未设置会话层，会根据已设置的传输层/数据链路层自动创建
4. 如果未设置数据链路层，会使用默认的 `DelimiterFrameStrategy`
5. `UseDataLink(IFrameStrategy)` 依赖先设置传输层，否则会抛异常
6. `Dispose()` 会释放 Session 和 Transport；MQTT 管道（无 Transport）仅释放 Session
7. 使用完毕后应及时释放管道资源
