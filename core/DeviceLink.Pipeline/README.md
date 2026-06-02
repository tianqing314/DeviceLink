# DeviceLink.Pipeline

## 概述

`DeviceLink.Pipeline` 是 DeviceLink 框架的**通信管道构建器**，通过 Builder 模式将传输层、数据链路层、会话层、协议层灵活组合成完整的通信管道。提供预定义配置，简化常见通信场景的初始化代码。

## 主要职责

1. **管道构建** - 通过 Builder 模式构建通信管道
2. **层组合** - 灵活组合 OSI 各层，支持跳过中间层
3. **预定义配置** - 提供常用通信场景的开箱即用配置
4. **自动组装** - 自动创建缺失的中间层（如自动生成会话层）
5. **资源管理** - 管理管道生命周期和资源释放

## 关键接口/类

### `CommunicationPipelineBuilder`
通信管道构建器，使用链式 API：

| 方法 | 描述 |
|------|------|
| `UseTransport(IPhysicalTransport)` | 设置物理传输层 |
| `UseDataLink(IFrameStrategy, DataLinkOptions?)` | 设置数据链路层（通过帧策略自动创建） |
| `UseDataLink(IDataLink)` | 直接注入数据链路层 |
| `UseSession(ISession)` | 设置会话层 |
| `UseProtocol(IProtocolCodec)` | 设置协议层 |
| `UseLoggerFactory(ILoggerFactory)` | 设置日志工厂 |
| `Configure(Action<CommunicationPipeline>)` | 添加自定义配置 |
| `Build()` | 构建通信管道 |

### `CommunicationPipeline`
通信管道，封装完整的通信栈：

| 属性/方法 | 描述 |
|-----------|------|
| `Transport` | 物理传输层（可能为 null） |
| `DataLink` | 数据链路层（可能为 null） |
| `Session` | 会话层 |
| `Protocol` | 协议层 |
| `OpenAsync(ct)` | 打开管道 |
| `CloseAsync()` | 关闭管道 |
| `SendAndReceiveAsync(request, ct)` | 发送原始字节并接收响应 |
| `SendCommandAsync(command, ct)` | 发送逻辑命令并接收响应（含错误检查） |

### `PipelinePresets`
预定义管道配置工厂方法：

| 方法 | 描述 |
|------|------|
| `CreateSerialPortPipeline(portName, baudRate, address)` | 创建 ConST 串口管道 |
| `CreateTcpPipeline(host, port, address)` | 创建 ConST TCP 管道 |
| `CreateModbusRtuPipeline(portName, baudRate, slaveAddress)` | 创建 Modbus RTU 串口管道 |
| `CreateScpiPipeline(host, port)` | 创建 SCPI TCP 管道 |

## 依赖关系

- **项目依赖**（引用所有 Core 层项目）:
  - `DeviceLink.Transport` - 物理传输层
  - `DeviceLink.DataLink` - 数据链路层
  - `DeviceLink.Session` - 会话层
  - `DeviceLink.Protocol` - 协议层
- **NuGet 依赖**:
  - `Microsoft.Extensions.Logging.Abstractions` - 日志抽象

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
var response = await pipeline.SendCommandAsync(Command.Read("PRES"));
await pipeline.CloseAsync();
```

### 预定义配置

```csharp
// 一行代码创建完整管道
using var pipeline = PipelinePresets.CreateSerialPortPipeline("COM3", 9600, 255);
await pipeline.OpenAsync();
var response = await pipeline.SendCommandAsync(Command.Read("PRES"));
```

### Modbus RTU 管道

```csharp
using var pipeline = PipelinePresets.CreateModbusRtuPipeline("COM3", 9600, 1);
await pipeline.OpenAsync();
var response = await pipeline.SendCommandAsync(Command.Read("40001", "10"));
```

## 设计原则

1. **Builder 模式** - 链式 API，清晰直观
2. **灵活组合** - 支持跳过中间层，适应不同通信场景
3. **开箱即用** - 预定义配置覆盖常见场景
4. **自动推导** - 自动创建缺失的中间层
5. **资源管理** - 实现 `IDisposable`，确保资源正确释放

## 注意事项

1. 必须设置协议层，否则 `Build()` 会抛出异常
2. 如果未设置会话层，会根据已设置的传输层/数据链路层自动创建
3. 如果未设置数据链路层，会使用默认的 `DelimiterFrameStrategy`
4. 管道中各层的生命周期由管道统一管理
5. 使用完毕后应及时释放管道资源