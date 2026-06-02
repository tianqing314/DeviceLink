# DeviceLink.Session

## 概述

`DeviceLink.Session` 是 DeviceLink 框架的**会话层**实现，对应 OSI 模型的**会话层**。负责管理请求-响应会话，提供可靠的会话管理服务。

## OSI 层级

**会话层 (Session Layer)** - 负责建立、管理和终止会话连接。

## 主要职责

1. **会话管理** - 建立、维护和终止会话连接
2. **请求-响应模式** - 提供标准的请求-响应通信模式
3. **超时重试** - 会话级别的超时和重试机制
4. **线程安全** - 保证会话操作的串行化
5. **错误处理** - 会话级别的异常处理

## 关键接口/类

### 核心接口

#### `ISession`
会话层的核心接口：

```csharp
public interface ISession : IDisposable
{
    string Name { get; }  // 会话名称
    bool IsOpen { get; }  // 会话是否已打开
    Task OpenAsync(CancellationToken ct = default);  // 打开会话
    Task CloseAsync();  // 关闭会话
    Task<byte[]> SendAndReceiveAsync(byte[] request, CancellationToken ct = default);  // 发送请求并接收响应
    Task SendOnlyAsync(byte[] request, CancellationToken ct = default);  // 单向发送
    Task<byte[]> ReceiveOnlyAsync(CancellationToken ct = default);  // 仅接收
}
```

### 会话实现

#### `DirectSession`
直连会话实现，基于数据链路层：

- **特点**: 适用于点对点连接场景（串口、TCP、USB 等）
- **依赖**: `IDataLink` 数据链路层接口
- **功能**: 内建重试机制，支持会话级别的超时控制

#### `MqttSession`
MQTT 会话实现，基于 MQTT 协议：

- **特点**: 适用于物联网场景，支持发布/订阅模式
- **依赖**: MQTT 客户端库
- **功能**: 支持主题订阅、消息发布

### 配置选项

#### `SessionOptions`
会话配置选项：

```csharp
public class SessionOptions
{
    public int RequestTimeoutMs { get; set; } = 1000;  // 请求超时时间
    public int MaxRetryCount { get; set; } = 0;        // 最大重试次数
    public int RetryDelayMs { get; set; } = 300;       // 重试延迟时间
}
```

### 异常类

#### `SessionException`
会话层通用异常。

#### `SessionTimeoutException`
会话超时异常。

## 依赖关系

- **项目依赖**:
  - `DeviceLink.Transport` - 物理传输层接口（通过数据链路层）
  - `DeviceLink.DataLink` - 数据链路层接口
- **NuGet 依赖**:
  - `Microsoft.Extensions.Logging.Abstractions` - 日志抽象

## 使用示例

### 基本使用

```csharp
// 创建物理传输层
var transport = new SerialPortTransport("COM3", 9600);

// 创建帧策略
var frameStrategy = new DelimiterFrameStrategy(new byte[] { 0 });

// 创建数据链路层
var dataLink = new DirectDataLink(transport, frameStrategy);

// 创建会话层
var sessionOptions = new SessionOptions
{
    RequestTimeoutMs = 1000,
    MaxRetryCount = 3
};

using var session = new DirectSession(dataLink, sessionOptions);
await session.OpenAsync();

// 发送请求并接收响应
var request = Encoding.ASCII.GetBytes("1:R:PRES:");
var response = await session.SendAndReceiveAsync(request);
```

### 单向发送

```csharp
// 发送命令，不等待响应
var command = Encoding.ASCII.GetBytes("1:W:PUNIT:bar");
await session.SendOnlyAsync(command);
```

### 仅接收

```csharp
// 等待接收数据（不发送命令）
var data = await session.ReceiveOnlyAsync();
```

## 配置选项详解

### SessionOptions

| 属性 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| RequestTimeoutMs | int | 1000 | 请求超时时间（毫秒） |
| MaxRetryCount | int | 0 | 最大重试次数 |
| RetryDelayMs | int | 300 | 重试延迟时间（毫秒） |

## 异常处理

会话层定义了以下异常类型：

1. **`SessionException`** - 会话通用异常
2. **`SessionTimeoutException`** - 会话超时异常

## 设计原则

1. **会话抽象** - 通过 `ISession` 接口统一不同会话实现
2. **依赖倒置** - 会话层依赖数据链路层接口，不依赖具体实现
3. **重试机制** - 支持可配置的重试策略
4. **线程安全** - 保证会话操作的串行化
5. **资源管理** - 实现 `IDisposable`，确保资源正确释放

## 注意事项

1. 会话层建立在数据链路层之上，必须先设置数据链路层
2. 重试机制在会话层实现，数据链路层可能也有自己的重试逻辑
3. 超时参数需要根据实际通信环境调整
4. 会话层保证线程安全，可以安全地在多线程环境中使用
5. 使用完毕后应及时释放会话资源

## 与其他层的关系

会话层在架构中起到承上启下的作用：

1. **向下** - 依赖数据链路层提供的帧传输服务
2. **向上** - 为协议层提供可靠的会话服务
3. **横向** - 可以基于不同传输机制实现（直连、MQTT 等）

## 扩展性

会话层设计具有良好的扩展性：

1. **新的会话实现** - 可以轻松添加基于其他协议的会话实现（如 WebSocket、gRPC 等）
2. **自定义重试策略** - 可以扩展重试机制，支持更复杂的重试策略
3. **会话池管理** - 可以在此层实现会话池，提高资源利用率
4. **会话监控** - 可以添加会话状态监控和统计功能