# DeviceLink 设备通信框架

基于 OSI 七层模型（简化为 5 层）的设备通信框架，支持 Modbus RTU/TCP、SCPI、ConST 等多种通信协议。

## 层级引用顺序（从底层到顶层）

```
① DeviceLink.Transport   ← 基础层，无内部依赖，直接操作硬件/网络
② DeviceLink.Protocol    ← 基础层，无内部依赖，纯编解码逻辑
③ DeviceLink.DataLink    ← 依赖 ① Transport
④ DeviceLink.Session     ← 依赖 ① Transport + ③ DataLink
⑤ DeviceLink.Devices     ← 依赖 ④ Session + ② Protocol（汇聚层）
⑥ DeviceLink.Pipeline    ← 依赖 ①~⑤ 全部（组装工具层）

依赖关系图：
  ⑥ Pipeline ──→ ⑤ Devices ──┬──→ ④ Session ──→ ③ DataLink ──→ ① Transport
                              └──→ ② Protocol
```

## 架构概览

```
┌─────────────────────────────────────────────────────────────────┐
│                  ⑥ Pipeline: 管道构建器                           │
│               CommunicationPipelineBuilder                      │
│                    灵活组装各层                                   │
├─────────────────────────────────────────────────────────────────┤
│                  ⑤ Layer 5: 应用层                               │
│                DeviceLink.Devices                               │
│          DeviceBase → DPSEX, DPG, 自定义设备                     │
├──────────────────────────┬──────────────────────────────────────┤
│  ④ Layer 3: 会话层       │  ② Layer 4: 协议层（基础层）           │
│   DeviceLink.Session     │  DeviceLink.Protocol                 │
│  ISession→DirectSession  │  IProtocolCodec→ConST/SCPI/Modbus    │
├──────────────────────────┴──────────────────────────────────────┤
│                  ③ Layer 2: 数据链路层                           │
│                DeviceLink.DataLink                              │
│     IFrameStrategy → 分隔符帧, 定长帧, Modbus RTU 帧             │
├─────────────────────────────────────────────────────────────────┤
│                  ① Layer 1: 物理传输层（基础层）                   │
│                DeviceLink.Transport                             │
│   IPhysicalTransport → 串口, TCP, UDP, USB, 回环                │
└─────────────────────────────────────────────────────────────────┘
```

## 项目结构

```
DeviceLink/
├── src/
│   ├── DeviceLink.Transport/      # ① 物理传输层（基础层，无内部依赖）
│   ├── DeviceLink.Protocol/       # ② 协议层（基础层，无内部依赖）
│   ├── DeviceLink.DataLink/       # ③ 数据链路层（依赖 ①）
│   ├── DeviceLink.Session/        # ④ 会话层（依赖 ①③）
│   ├── DeviceLink.Devices/        # ⑤ 应用层（依赖 ④②）
│   └── DeviceLink.Pipeline/       # ⑥ 管道构建器（依赖 ①~⑤ 全部）
├── test/
│   └── DeviceLink.Tests/          # 单元测试
└── README.md
```

## 快速开始

### 1. 安装 NuGet 包

```bash
# 根据需要引用对应的层
dotnet add package DeviceLink.Transport
dotnet add package DeviceLink.DataLink
dotnet add package DeviceLink.Session
dotnet add package DeviceLink.Protocol
dotnet add package DeviceLink.Devices
dotnet add package DeviceLink.Pipeline
```

### 2. 使用管道构建器（推荐）

#### 串口通信（ConST 协议）

```csharp
using DeviceLink.Pipeline;
using DeviceLink.Protocol;

// 使用预设创建串口管道
var pipeline = PipelinePresets.CreateSerialPortPipeline("COM3", 9600, 255);

// 或者手动构建
var pipeline = new CommunicationPipelineBuilder()
    .UseTransport(new SerialPortTransport("COM3", 9600))
    .UseDataLink(new DelimiterFrameStrategy(new byte[] { 0 }))
    .UseProtocol(new ConSTCodec(255))
    .Build();

// 打开连接
await pipeline.OpenAsync();

// 发送命令
var command = Command.Read("PRESSURE");
var response = await pipeline.SendCommandAsync(command);

// 关闭连接
await pipeline.CloseAsync();
```

#### TCP 通信（Modbus TCP）

```csharp
var pipeline = new CommunicationPipelineBuilder()
    .UseTransport(new TcpTransport("192.168.1.100", 502))
    .UseSession(new DirectSession(...))
    .UseProtocol(new ModbusTcpCodec())
    .Build();
```

#### Modbus RTU 通信

```csharp
// 使用预设
var pipeline = PipelinePresets.CreateModbusRtuPipeline("COM3", 9600, 1);

// 手动构建
var pipeline = new CommunicationPipelineBuilder()
    .UseTransport(new SerialPortTransport("COM3", 9600))
    .UseDataLink(new ModbusRtuFrameStrategy())
    .UseProtocol(new ModbusRtuCodec(slaveAddress: 1))
    .Build();

// 读取寄存器
var command = Command.Read("3.0.10"); // 功能码3，起始地址0，读取10个寄存器
var response = await pipeline.SendCommandAsync(command);
```

#### SCPI 通信

```csharp
var pipeline = PipelinePresets.CreateScpiPipeline("192.168.1.100", 5024);

// 发送 SCPI 命令
var command = Command.Read("*IDN?");
var response = await pipeline.SendCommandAsync(command);
```

### 3. 使用设备类（高级封装）

```csharp
using DeviceLink.Devices;
using DeviceLink.Protocol;
using DeviceLink.Session;

// 创建 DPSEX 压力传感器
var session = new DirectSession(dataLink);
var codec = new ConSTCodec(address: 1);
var device = new DPSEX(session, codec);

// 打开设备
await device.OpenAsync();

// 读取压力
double pressure = await device.GetPressureAsync();

// 读取温度
double temperature = await device.GetTemperatureAsync();

// 获取版本
string version = await device.GetVersionAsync();

// 关闭设备
await device.CloseAsync();
```

## 各层详细说明

### ① Layer 1: 物理传输层 (DeviceLink.Transport)

负责底层字节传输，无协议/分帧/命令概念。

#### 核心接口: `IPhysicalTransport`

```csharp
public interface IPhysicalTransport : IDisposable
{
    string Name { get; }
    bool IsOpen { get; }
    Task ConnectAsync(CancellationToken ct = default);
    Task CloseAsync();
    Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct = default);
    Task WriteAsync(byte[] data, int offset, int count, CancellationToken ct = default);
    Task ClearReceiveBufferAsync(CancellationToken ct = default);
}
```

#### 内置实现

| 实现类 | 说明 | 使用场景 |
|--------|------|----------|
| `SerialPortTransport` | 串口传输 | RS232/RS485 设备 |
| `TcpTransport` | TCP 传输 | 网络设备 |
| `UdpTransport` | UDP 传输 | 无连接网络设备 |
| `UsbTransport` | USB 传输 | USB 设备（需集成第三方库） |
| `LoopbackTransport` | 回环传输 | 单元测试 |

#### 示例：串口传输

```csharp
var transport = new SerialPortTransport("COM3", 9600);
await transport.ConnectAsync();

// 发送数据
var data = Encoding.ASCII.GetBytes("Hello");
await transport.WriteAsync(data, 0, data.Length);

// 接收数据
var buffer = new byte[1024];
int bytesRead = await transport.ReadAsync(buffer, 0, buffer.Length);

await transport.CloseAsync();
```

#### 示例：TCP 传输

```csharp
var transport = new TcpTransport("192.168.1.100", 502);
await transport.ConnectAsync();

// 使用方式与串口相同
await transport.WriteAsync(data, 0, data.Length);
int bytesRead = await transport.ReadAsync(buffer, 0, buffer.Length);

await transport.CloseAsync();
```

### ③ Layer 2: 数据链路层 (DeviceLink.DataLink)

负责帧的组装、解析和边界检测。

#### 核心接口: `IDataLink`

```csharp
public interface IDataLink : IDisposable
{
    string Name { get; }
    IPhysicalTransport Transport { get; }
    bool IsOpen { get; }
    Task OpenAsync(CancellationToken ct = default);
    Task CloseAsync();
    Task<byte[]> SendAndReceiveFrameAsync(byte[] frameData, CancellationToken ct = default);
    Task SendFrameAsync(byte[] frameData, CancellationToken ct = default);
    Task<byte[]> ReceiveFrameAsync(CancellationToken ct = default);
}
```

#### 帧策略接口: `IFrameStrategy`

```csharp
public interface IFrameStrategy
{
    string Name { get; }
    byte[] BuildFrame(byte[] data);
    bool TryParseFrame(byte[] accumulated, out int frameLength, out byte[] frameData);
}
```

#### 内置帧策略

| 帧策略 | 说明 | 使用场景 |
|--------|------|----------|
| `DelimiterFrameStrategy` | 分隔符帧 | ConST (\0)、SCPI (\n)、AT 指令 (\r\n) |
| `FixedLengthFrameStrategy` | 定长帧 | 固定长度响应的设备 |
| `ModbusRtuFrameStrategy` | Modbus RTU 帧 | Modbus RTU 协议（带 CRC16 校验） |

#### 示例：分隔符帧

```csharp
// 使用 \0 作为帧结束符（ConST 协议）
var frameStrategy = new DelimiterFrameStrategy(new byte[] { 0 });

// 使用 \n 作为帧结束符（SCPI 协议）
var frameStrategy = new DelimiterFrameStrategy("\n");

// 使用 \r\n 作为帧结束符（AT 指令）
var frameStrategy = new DelimiterFrameStrategy("\r\n");

// 组装帧
byte[] frame = frameStrategy.BuildFrame(data); // data + delimiter

// 解析帧
bool success = frameStrategy.TryParseFrame(accumulated, out int frameLength, out byte[] frameData);
```

#### 示例：Modbus RTU 帧

```csharp
var frameStrategy = new ModbusRtuFrameStrategy();

// 组装帧（自动添加 CRC16）
byte[] frame = frameStrategy.BuildFrame(data);

// 解析帧（自动验证 CRC16）
bool success = frameStrategy.TryParseFrame(accumulated, out int frameLength, out byte[] frameData);
```

#### 配置选项: `DataLinkOptions`

```csharp
var options = new DataLinkOptions
{
    ReceiveTimeoutMs = 1000,      // 接收超时时间
    ReceiveIdleTimeoutMs = 50,    // 接收空闲超时时间
    MaxRetryCount = 0,            // 最大重试次数
    RetryDelayMs = 300,           // 重试延迟时间
    ReceivePollIntervalMs = 10    // 接收轮询间隔
};

var dataLink = new DirectDataLink(transport, frameStrategy, options);
```

### ④ Layer 3: 会话层 (DeviceLink.Session)

管理请求-响应会话，支持超时重试、线程安全。

#### 核心接口: `ISession`

```csharp
public interface ISession : IDisposable
{
    string Name { get; }
    bool IsOpen { get; }
    Task OpenAsync(CancellationToken ct = default);
    Task CloseAsync();
    Task<byte[]> SendAndReceiveAsync(byte[] request, CancellationToken ct = default);
    Task SendOnlyAsync(byte[] request, CancellationToken ct = default);
    Task<byte[]> ReceiveOnlyAsync(CancellationToken ct = default);
}
```

#### 内置实现

| 实现类 | 说明 | 使用场景 |
|--------|------|----------|
| `DirectSession` | 直连会话 | 基于数据链路层的直接通信 |
| `MqttSession` | MQTT 会话 | 基于 MQTT 协议的远程通信（TODO） |

#### 配置选项: `SessionOptions`

```csharp
var options = new SessionOptions
{
    RequestTimeoutMs = 1000,  // 请求超时时间
    MaxRetryCount = 0,        // 最大重试次数
    RetryDelayMs = 300        // 重试延迟时间
};

var session = new DirectSession(dataLink, options);
```

#### 示例

```csharp
var session = new DirectSession(dataLink);
await session.OpenAsync();

// 发送并接收
var response = await session.SendAndReceiveAsync(request);

// 单向发送
await session.SendOnlyAsync(request);

// 仅接收
var data = await session.ReceiveOnlyAsync();

await session.CloseAsync();
```

### ② Layer 4: 协议层 (DeviceLink.Protocol)

协议编解码器，将逻辑命令编码为字节，将响应字节解码为业务结果。

#### 核心接口: `IProtocolCodec`

```csharp
public interface IProtocolCodec
{
    string ProtocolName { get; }
    byte[] Encode(Command command);
    string DecodeText(byte[] raw);
    bool IsErrorResponse(byte[] raw, out string errorMessage);
}
```

#### 命令类型: `Command`

```csharp
// 读取命令
var readCmd = Command.Read("PRESSURE");
var readCmdWithParams = Command.Read("3.0.10"); // Modbus: 功能码.地址.数量

// 写入命令
var writeCmd = Command.Write("ADDRESS", "1");

// 无返回命令
var nonQueryCmd = Command.NonQuery("ZERO");
```

#### 内置协议

| 协议 | 说明 | 使用场景 |
|------|------|----------|
| `ConSTCodec` | ConST 协议 | ConST 设备（地址:R/W:命令:参数:\0） |
| `ScpiCodec` | SCPI 协议 | 标准 SCPI 仪器 |
| `ModbusRtuCodec` | Modbus RTU 协议 | Modbus RTU 设备 |

#### ConST 协议格式

```
格式: 地址:R/W:命令:参数...:\0

示例:
  读取: 1:R:PRESSURE:\0
  写入: 1:W:ADDRESS:2:\0
```

```csharp
var codec = new ConSTCodec(address: 1);

// 编码
var command = Command.Read("PRESSURE");
byte[] encoded = codec.Encode(command); // "1:R:PRESSURE:\0"

// 解码
string text = codec.DecodeText(response);

// 错误检查
bool isError = codec.IsErrorResponse(response, out string errorMessage);
```

#### SCPI 协议格式

```
格式: 命令\n

示例:
  查询: *IDN?\n
  设置: VOLT 10.0\n
```

```csharp
var codec = new ScpiCodec();

// 编码
var command = Command.Read("*IDN?");
byte[] encoded = codec.Encode(command); // "*IDN?\n"

// 辅助方法
double voltage = ScpiCodec.ExtractNumeric(response);
string idn = ScpiCodec.ExtractString(response);
bool enabled = ScpiCodec.ExtractBoolean(response);
```

#### Modbus RTU 协议格式

```
格式: [从站地址][功能码][数据...][CRC低][CRC高]

功能码:
  0x03: 读保持寄存器
  0x06: 写单个寄存器
  0x10: 写多个寄存器
```

```csharp
var codec = new ModbusRtuCodec(slaveAddress: 1);

// 读取寄存器
var readCmd = Command.Read("3.0.10"); // 功能码3，起始地址0，读取10个
byte[] encoded = codec.Encode(readCmd);

// 写入单个寄存器
var writeCmd = Command.Write("6.100", "1234"); // 功能码6，地址100，值1234
byte[] encoded = codec.Encode(writeCmd);

// 提取寄存器值
ushort[] registers = codec.ExtractRegisters(response);
```

### ⑤ Layer 5: 应用层 (DeviceLink.Devices)

设备基类和具体设备实现。

#### 设备基类: `DeviceBase`

```csharp
public abstract class DeviceBase : IDisposable
{
    protected ISession Session { get; }
    protected IProtocolCodec Codec { get; }
    protected ILogger Logger { get; }

    public string Name { get; set; }
    public bool IsOpen { get; }

    public virtual async Task OpenAsync(CancellationToken ct = default);
    public virtual async Task CloseAsync();

    // 发送命令并接收原始响应
    protected async Task<byte[]> SendAsync(Command command, CancellationToken ct = default);

    // 发送命令并解析响应
    protected async Task<T> SendForResultAsync<T>(Command command, Func<byte[], T> decoder, CancellationToken ct = default);

    // 单向发送
    protected async Task SendNonQueryAsync(Command command, CancellationToken ct = default);
}
```

#### DPSEX 压力传感器示例

```csharp
// 创建设备
var session = new DirectSession(dataLink);
var codec = new ConSTCodec(address: 1);
var device = new DPSEX(session, codec);

// 或使用简化构造函数
var device = new DPSEX("COM3", 9600, address: 1);

// 打开设备
await device.OpenAsync();

// 读取数据
double pressure = await device.GetPressureAsync();
double temperature = await device.GetTemperatureAsync();
string version = await device.GetVersionAsync();
string serialNumber = await device.GetSerialNumberAsync();

// 设置参数
await device.SetAddressAsync(2);
await device.SetUnitAsync("kPa");

// 校准操作
await device.PressureZeroAsync();
await device.ZeroAsync();

// 关闭设备
await device.CloseAsync();
```

## 通信场景指南

### 场景 1: 串口 + ConST 协议

```csharp
var pipeline = new CommunicationPipelineBuilder()
    .UseTransport(new SerialPortTransport("COM3", 9600))
    .UseDataLink(new DelimiterFrameStrategy(new byte[] { 0 }))
    .UseProtocol(new ConSTCodec(255))
    .Build();
```

**层组合**: Transport → DataLink → Protocol（跳过 Session）

### 场景 2: TCP + ConST 协议

```csharp
var pipeline = new CommunicationPipelineBuilder()
    .UseTransport(new TcpTransport("192.168.1.100", 502))
    .UseDataLink(new DelimiterFrameStrategy(new byte[] { 0 }))
    .UseProtocol(new ConSTCodec(255))
    .Build();
```

**层组合**: Transport → DataLink → Protocol（跳过 Session）

### 场景 3: Modbus RTU

```csharp
var pipeline = new CommunicationPipelineBuilder()
    .UseTransport(new SerialPortTransport("COM3", 9600))
    .UseDataLink(new ModbusRtuFrameStrategy())
    .UseProtocol(new ModbusRtuCodec(slaveAddress: 1))
    .Build();
```

**层组合**: Transport → DataLink → Protocol（跳过 Session）

### 场景 4: SCPI 仪器

```csharp
var pipeline = new CommunicationPipelineBuilder()
    .UseTransport(new TcpTransport("192.168.1.100", 5024))
    .UseDataLink(new DelimiterFrameStrategy("\n"))
    .UseProtocol(new ScpiCodec())
    .Build();
```

**层组合**: Transport → DataLink → Protocol（跳过 Session）

### 场景 5: 需要会话层的场景

```csharp
var transport = new TcpTransport("192.168.1.100", 502);
var dataLink = new DirectDataLink(transport, new DelimiterFrameStrategy(new byte[] { 0 }));
var session = new DirectSession(dataLink, new SessionOptions
{
    RequestTimeoutMs = 2000,
    MaxRetryCount = 3,
    RetryDelayMs = 500
});
var codec = new ConSTCodec(255);

var device = new DPSEX(session, codec);
```

**层组合**: Transport → DataLink → Session → Protocol

## 异常处理

### 异常层次

```
DeviceException (应用层)
  └── ProtocolException (协议层)
  └── SessionException (会话层)
      └── SessionTimeoutException
      └── SessionConnectionException
  └── DataLinkException (数据链路层)
      └── FrameTimeoutException
      └── FrameFormatException
  └── TransportException (物理传输层)
      └── ConnectionException
      └── TransportTimeoutException
```

### 示例

```csharp
try
{
    var pressure = await device.GetPressureAsync();
}
catch (DeviceException ex)
{
    // 设备业务错误
    Console.WriteLine($"设备错误: {ex.Message}");
}
catch (SessionTimeoutException ex)
{
    // 会话超时
    Console.WriteLine($"通信超时: {ex.Message}");
}
catch (TransportException ex)
{
    // 传输层错误
    Console.WriteLine($"传输错误: {ex.Message}");
}
```

## 测试

### 使用 LoopbackTransport 进行单元测试

```csharp
using var transport = new LoopbackTransport();
await transport.ConnectAsync();

// 设置模拟响应
transport.OnSend += data =>
{
    // 解析命令并返回模拟响应
    var response = Encoding.ASCII.GetBytes("1:R:PRESSURE:100.0:\0");
    transport.EnqueueReceive(response);
};

var dataLink = new DirectDataLink(transport, new DelimiterFrameStrategy(new byte[] { 0 }));
var session = new DirectSession(dataLink);
var codec = new ConSTCodec(1);
var device = new DPSEX(session, codec);

await device.OpenAsync();
double pressure = await device.GetPressureAsync();

Assert.Equal(100.0, pressure);
```

### 运行测试

```bash
cd test/DeviceLink.Tests
dotnet test
```

## 依赖注入

### 注册服务

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// 注册传输层
services.AddSingleton<IPhysicalTransport>(sp =>
    new SerialPortTransport("COM3", 9600));

// 注册数据链路层
services.AddSingleton<IDataLink>(sp =>
{
    var transport = sp.GetRequiredService<IPhysicalTransport>();
    return new DirectDataLink(transport, new DelimiterFrameStrategy(new byte[] { 0 }));
});

// 注册会话层
services.AddSingleton<ISession>(sp =>
{
    var dataLink = sp.GetRequiredService<IDataLink>();
    return new DirectSession(dataLink);
});

// 注册协议层
services.AddSingleton<IProtocolCodec>(new ConSTCodec(255));

// 注册设备
services.AddTransient<DPSEX>();

var provider = services.BuildServiceProvider();
var device = provider.GetRequiredService<DPSEX>();
```

## 扩展指南

### 自定义传输层

```csharp
public class BluetoothTransport : IPhysicalTransport
{
    public string Name => "Bluetooth";
    public bool IsOpen { get; }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        // 实现蓝牙连接
    }

    // ... 实现其他接口方法
}
```

### 自定义帧策略

```csharp
public class CustomFrameStrategy : IFrameStrategy
{
    public string Name => "Custom";

    public byte[] BuildFrame(byte[] data)
    {
        // 实现帧组装
    }

    public bool TryParseFrame(byte[] accumulated, out int frameLength, out byte[] frameData)
    {
        // 实现帧解析
    }
}
```

### 自定义协议

```csharp
public class CustomCodec : IProtocolCodec
{
    public string ProtocolName => "Custom";

    public byte[] Encode(Command command)
    {
        // 实现命令编码
    }

    public string DecodeText(byte[] raw)
    {
        // 实实现响应解码
    }

    public bool IsErrorResponse(byte[] raw, out string errorMessage)
    {
        // 实现错误检测
    }
}
```

### 自定义设备

```csharp
public class CustomDevice : DeviceBase
{
    public CustomDevice(ISession session, IProtocolCodec codec, ILogger? logger = null)
        : base(session, codec, logger)
    {
    }

    public async Task<double> ReadValueAsync(CancellationToken ct = default)
    {
        var command = Command.Read("VALUE");
        return await SendForResultAsync(command, response =>
        {
            // 解析响应
            var text = Codec.DecodeText(response);
            return double.Parse(text);
        }, ct);
    }
}
```

## 最佳实践

1. **选择合适的层组合**：根据通信场景选择需要的层，不要过度设计
2. **使用管道构建器**：简化复杂对象的创建
3. **正确处理异常**：捕获特定层级的异常，提供有意义的错误信息
4. **使用 CancellationToken**：支持操作取消
5. **释放资源**：使用 `using` 语句或实现 `IDisposable`
6. **单元测试**：使用 `LoopbackTransport` 进行无硬件测试

## 目标框架

- **netstandard2.0**: 广泛兼容性
- **net6.0**: 现代 .NET 特性

## 依赖包

- Microsoft.Extensions.Logging.Abstractions (6.0.0)
- System.IO.Ports (6.0.0) - 串口通信
- Microsoft.Extensions.DependencyInjection (6.0.0) - 依赖注入（可选）

## 许可证

MIT License
