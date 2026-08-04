# DeviceLink.DeviceBase

## 概述

`DeviceLink.DeviceBase` 是 DeviceLink 框架的**设备基类库**，为应用层设备提供统一的基类。封装了命令发送、响应接收、错误检查、通信链路组装等通用逻辑，设备开发者只需继承基类并实现业务方法。

## 主要职责

1. **设备抽象** - 定义设备基类 `DeviceBase`，封装通用设备操作
2. **通信链路组装** - 通过 `CommunicationPipelineBuilder` 组装完整 OSI 通信栈（串口 / TCP / 蓝牙 / MQTT / 自定义）
3. **命令发送** - 提供 `SendAsync`、`SendForResultAsync`、`SendNonQueryAsync` 方法
4. **错误检查** - 自动通过 `IProtocolCodec.IsErrorResponse` 检测设备错误
5. **通信日志** - 内置 `CommunicationLogger` 通信链路日志（设备名 + 操作名 + 原始字节）
6. **通信配置** - 提供 `DeviceCommSettings` 系列配置类（串口 / TCP / MQTT / 蓝牙）
7. **异常处理** - 定义 `DeviceException` 设备异常类型

## 关键接口/类

### `DeviceBase`（抽象类）

设备基类，所有设备类的父类：

| 成员 | 描述 |
|------|------|
| `Name` | 设备名称（默认为类名） |
| `IsOpen` | 设备是否已连接 |
| `Pipeline`（protected） | 通信管道实例（`CommunicationPipeline`，封装完整 OSI 链路） |
| `Session`（protected） | 会话层实例（`ISession`，从 Pipeline 提取） |
| `Codec`（protected） | 协议编解码器实例（`IProtocolCodec`） |
| `OpenAsync(ct)` | 打开设备连接 |
| `CloseAsync()` | 关闭设备连接 |
| `SendAsync(command, ct)`（protected virtual） | 发送命令并返回原始响应字节 |
| `SendForResultAsync<T>(command, decoder, ct)`（protected） | 发送命令并返回解析后的业务数据 |
| `SendNonQueryAsync(command, ct)`（protected virtual） | 单向发送命令（不等待响应） |
| `GetDefaultFrameStrategy(codec)`（protected static） | 根据协议类型自动选择推荐帧策略 |
| `ConstructDefaultInfo()`（protected virtual） | 配置构造函数默认信息（子类可重写） |
| `Dispose()` | 释放资源 |

> **注意**: `DeviceBase` 没有 `Logger` 属性，日志通过静态类 `CommunicationLogger` 记录。

#### 构造函数

`DeviceBase` 提供了 11 个 protected 构造函数，覆盖常见通信场景（子类通过 `: base(...)` 调用）：

| # | 构造函数 | 说明 |
|---|---------|------|
| 1 | `DeviceBase(ISession session, IProtocolCodec codec)` | 直接注入会话层（适用于测试、MQTT 等不需要完整 OSI 链路的场景） |
| 2 | `DeviceBase(string portName, int baudRate, int dataBits, StopBits stopBits, Parity parity, IProtocolCodec codec)` | 串口通信（完整参数） |
| 3 | `DeviceBase(string portName, int baudRate, int dataBits, StopBits stopBits, Parity parity, IProtocolCodec codec, IFrameStrategy frameStrategy, DataLinkOptions? dataLinkOptions = null)` | 串口通信 + 自定义帧策略 |
| 4 | `DeviceBase(string portName, int baudRate, int dataBits, StopBits stopBits, Parity parity, IProtocolCodec codec, byte[]? delimiter, DataLinkOptions? dataLinkOptions = null)` | 串口通信 + 自定义帧分隔符 |
| 5 | `DeviceBase(string portName, IProtocolCodec codec)` | 串口通信（默认配置 9600,8,1,None） |
| 6 | `DeviceBase(string portName, IProtocolCodec codec, IFrameStrategy frameStrategy)` | 串口通信（默认串口参数 + 自定义帧策略） |
| 7 | `DeviceBase(IPAddress ipAddress, int port, IProtocolCodec codec)` | TCP 通信 |
| 8 | `DeviceBase(IPAddress ipAddress, int port, IProtocolCodec codec, IFrameStrategy frameStrategy)` | TCP 通信 + 自定义帧策略 |
| 9 | `DeviceBase(BluetoothOptions bluetoothOptions, IProtocolCodec codec)` | 蓝牙通信 |
| 10 | `DeviceBase(DeviceCommSettings settings, IProtocolCodec codec)` | 通信配置实例（未指定帧策略时按协议自动选择） |
| 11 | `DeviceBase(DeviceCommSettings settings, IProtocolCodec codec, IFrameStrategy frameStrategy)` | 通信配置实例 + 自定义帧策略 |

> 串口/TCP 构造中，当未显式指定帧策略时，框架会通过 `GetDefaultFrameStrategy` 根据协议自动选择（`ModbusRTU` → `ModbusRtuFrameStrategy`，`ZQWL` → `ZqwlFrameStrategy`），其他协议默认使用 `DelimiterFrameStrategy(\0)`。

### 通信配置类

#### `DeviceCommSettings`（抽象类）

设备通信配置基类，内部通过 `CommunicationPipelineBuilder` 组装完整的 OSI 通信栈。

#### `SerialPortSettings : DeviceCommSettings`

串口通信配置：

| 属性 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| PortName | string | "COM1" | 串口名称 |
| BaudRate | int | 9600 | 波特率 |
| DataBits | int | 8 | 数据位 |
| StopBits | StopBits | StopBits.One | 停止位 |
| Parity | Parity | Parity.None | 校验位 |
| Delimiter | byte[] | `{0}` | 帧分隔符（FrameStrategy 为 null 时使用） |
| FrameStrategy | IFrameStrategy? | null | 自定义帧策略（null 时使用分隔符策略） |
| DtrEnable | bool | false | 启用 DTR 信号 |
| RtsEnable | bool | false | 启用 RTS 信号 |
| ReceiveTimeoutMs | int | 10000 | 接收超时时间（毫秒） |
| ReceiveIdleTimeoutMs | int | 100 | 接收空闲超时（毫秒） |
| MaxRetryCount | int | 2 | 最大重试次数 |
| RetryDelayMs | int | 300 | 重试延迟（毫秒） |

**静态方法**: `SerialPortSettings.CreateDefault(string portName)` - 创建默认串口配置（9600,8,1,None）。

#### `TcpSettings : DeviceCommSettings`

TCP 通信配置：

| 属性 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| IpAddress | IPAddress | IPAddress.Loopback | IP 地址 |
| Port | int | 10001 | 端口号 |
| ConnectTimeoutMs | int | 5000 | 连接超时（毫秒） |
| Delimiter | byte[] | `{0}` | 帧分隔符 |
| FrameStrategy | IFrameStrategy? | null | 自定义帧策略 |

#### `MqttSettings : DeviceCommSettings`

MQTT 通信配置：

| 属性 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| BrokerHost | string | "127.0.0.1" | MQTT Broker 地址 |
| BrokerPort | int | 1883 | Broker 端口 |
| ClientId | string | `DeviceLink_{Guid}` | 客户端 ID |
| RequestTopic | string | "devicelink/request" | 请求主题 |
| ResponseTopic | string | "devicelink/response" | 响应主题 |
| RequestTimeoutMs | int | 5000 | 请求超时（毫秒） |
| Username / Password | string? | null | 认证信息（可选） |
| UseTls | bool | false | 是否使用 TLS |
| CleanSession | bool | true | 是否清理会话 |
| KeepAliveSeconds | ushort | 60 | 心跳间隔（秒） |

#### `BluetoothSettings : DeviceCommSettings`

蓝牙通信配置：

| 属性 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| BluetoothOptions | BluetoothOptions | `new BluetoothOptions()` | 蓝牙配置选项 |
| Delimiter | byte[] | `{0}` | 帧分隔符 |
| FrameStrategy | IFrameStrategy? | null | 自定义帧策略 |
| ReceiveTimeoutMs | int | 5000 | 接收超时（毫秒，蓝牙建议 ≥10000） |
| ReceiveIdleTimeoutMs | int | 50 | 接收空闲超时（毫秒） |
| MaxRetryCount | int | 0 | 最大重试次数（蓝牙建议 2-3） |
| RetryDelayMs | int | 300 | 重试延迟（毫秒） |

### `DeviceException`

设备异常类，表示设备操作过程中发生的错误。

## 依赖关系

- **直接项目引用**:
  - `DeviceLink.Protocol` - 协议编解码器接口
  - `DeviceLink.Pipeline` - 通信管道构建器
- **传递依赖**（通过 Pipeline / Protocol）:
  - `DeviceLink.Session` - 会话层
  - `DeviceLink.DataLink` - 数据链路层
  - `DeviceLink.Transport` - 物理传输层
- **NuGet 依赖**: 无直接包引用（日志抽象、MQTTnet 等经项目引用传递引入）

## 使用示例

### 继承 DeviceBase 实现设备类

```csharp
public class DPSEX : DeviceBase
{
    public DPSEX(string portName)
        : base(portName, new ConSTCodec(255))  // 串口 + ConST 协议，自动使用分隔符帧策略
    {
    }

    // 读取压力值
    public async Task<double> ReadPressureAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("PRES"),
            raw => double.Parse(Codec.ExtractField(raw)),
            ct);
    }

    // 设置压力单位
    public async Task SetUnitAsync(string unit, CancellationToken ct = default)
    {
        await SendAsync(Command.Write("PUNIT", unit), ct);
    }

    // 单向发送命令
    public async Task TareAsync(CancellationToken ct = default)
    {
        await SendNonQueryAsync(Command.NonQuery("TARE"), ct);
    }
}
```

### 使用设备类

```csharp
using var device = new DPSEX("COM3");
await device.OpenAsync();

// 使用设备
double pressure = await device.ReadPressureAsync();
await device.SetUnitAsync("bar");
```

### 通过通信配置创建设备

```csharp
// 串口配置
var settings = new SerialPortSettings("COM3", 9600, 8, StopBits.One, Parity.None)
{
    MaxRetryCount = 3
};
public class MyDevice : DeviceBase
{
    public MyDevice(DeviceCommSettings settings) : base(settings, new ModbusRtuCodec(1)) { }
}

// TCP 配置
var tcpSettings = new TcpSettings(IPAddress.Parse("192.168.1.100"), 502);

// MQTT 配置（设备通过 MQTT Broker 通信）
var mqttSettings = new MqttSettings("192.168.1.50", 1883, "devicelink/request", "devicelink/response");

// 蓝牙配置
var bluetoothSettings = new BluetoothSettings("00:11:22:33:44:55");
```

### 直接注入会话层（测试 / MQTT 场景）

```csharp
// 适用于测试或不需要完整 OSI 链路的场景
var dataLink = new DirectDataLink(transport, frameStrategy);
var session = new DirectSession(dataLink);
var codec = new ConSTCodec(255);
using var device = new DPSEX(session, codec);
```

## 设计原则

1. **模板方法模式** - 基类提供通用流程，子类实现业务细节
2. **依赖倒置** - 依赖 `ISession` 和 `IProtocolCodec` 接口
3. **错误自动检测** - 通过协议编解码器自动检测设备错误
4. **日志内置** - 通过 `CommunicationLogger` 自动记录完整通信链路日志
5. **资源管理** - 实现 `IDisposable`，通过 Pipeline 统一释放资源

## 注意事项

1. `DeviceBase` 是抽象类，不能直接实例化
2. 子类应通过 `SendAsync` / `SendForResultAsync` 发送命令，不要直接操作 Session
3. 基类不提供业务接口（如压力、温度等），这些由子类定义
4. 基类不提供重试逻辑，重试在会话层已处理
5. 命名空间 `DeviceLink.DeviceBase` 与类名 `DeviceBase` 相同时，子类需要使用完全限定名 `DeviceLink.DeviceBase.DeviceBase` 继承
6. 日志记录默认输出到 `AppContext.BaseDirectory/logs/communication.html`（可通过 `CommunicationLogger` 静态属性配置）
