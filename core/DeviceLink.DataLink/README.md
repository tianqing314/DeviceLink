# DeviceLink.DataLink

## 概述

`DeviceLink.DataLink` 是 DeviceLink 框架的**数据链路层**实现，对应 OSI 模型的**数据链路层**。负责帧的组装、解析和边界检测，提供可靠的数据帧传输服务。

## OSI 层级

**数据链路层 (Data Link Layer)** - 负责在物理层基础上提供可靠的数据帧传输。

## 主要职责

1. **帧管理** - 数据帧的组装、解析和边界检测
2. **帧策略** - 支持多种帧格式（分隔符帧、定长帧、Modbus RTU 帧等）
3. **传输控制** - 超时重试、接收循环、自动连接
4. **错误检测** - 帧完整性检查和错误处理
5. **线程安全** - 保证串行化访问，避免并发问题

## 关键接口/类

### 核心接口

#### `IDataLink`
数据链路层的核心接口：

```csharp
public interface IDataLink : IDisposable
{
    string Name { get; }                    // 数据链路名称
    IPhysicalTransport Transport { get; }   // 底层物理传输
    bool IsOpen { get; }                    // 是否已连接
    Task OpenAsync(CancellationToken ct = default);  // 打开数据链路
    Task CloseAsync();                      // 关闭数据链路
    Task<byte[]> SendAndReceiveFrameAsync(byte[] frameData, CancellationToken ct = default);  // 发送帧并接收响应
    Task SendFrameAsync(byte[] frameData, CancellationToken ct = default);  // 单向发送帧
    Task<byte[]> ReceiveFrameAsync(CancellationToken ct = default);  // 接收帧
}
```

#### `IFrameStrategy`
帧策略接口，定义帧的组装和解析规则：

```csharp
public interface IFrameStrategy
{
    string Name { get; }  // 策略名称
    byte[] BuildFrame(byte[] data);  // 组装帧
    bool TryParseFrame(byte[] accumulated, out int frameLength, out byte[] frameData);  // 解析帧
}
```

### 数据链路实现

#### `DirectDataLink`
直连数据链路实现，适用于点对点连接场景：

- **特点**: 内建接收循环、超时重试、自动连接、线程安全
- **使用场景**: 串口、TCP、USB 等直连通信
- **依赖**: `IPhysicalTransport` 和 `IFrameStrategy`

### 帧策略实现

#### `DelimiterFrameStrategy`
分隔符帧策略，使用特定字节作为帧结束符：

- **配置**: 可指定分隔符（如 `\0`、`\n`、自定义字节序列）
- **特点**: 简单通用，适合文本协议
- **使用场景**: ConST 协议、SCPI 协议等

#### `FixedLengthFrameStrategy`
固定长度帧策略，帧长度固定：

- **配置**: 指定固定帧长度
- **特点**: 解析简单，效率高
- **使用场景**: 固定长度二进制协议

#### `ModbusRtuFrameStrategy`
Modbus RTU 帧策略，实现 Modbus RTU 协议的帧处理：

- **特点**: 支持 CRC 校验，符合 Modbus RTU 标准
- **使用场景**: Modbus RTU 设备通信

### 配置选项

#### `DataLinkOptions`
数据链路配置选项：

```csharp
public class DataLinkOptions
{
    public int ReceiveTimeoutMs { get; set; } = 1000;      // 接收超时时间
    public int ReceiveIdleTimeoutMs { get; set; } = 50;    // 接收空闲超时
    public int MaxRetryCount { get; set; } = 0;            // 最大重试次数
    public int RetryDelayMs { get; set; } = 300;           // 重试延迟时间
    public int ReceivePollIntervalMs { get; set; } = 10;   // 接收轮询间隔
}
```

### 异常类

#### `DataLinkException`
数据链路层通用异常。

#### `FrameTimeoutException`
帧接收超时异常。

## 依赖关系

- **项目依赖**:
  - `DeviceLink.Transport` - 物理传输层接口
- **NuGet 依赖**:
  - `Microsoft.Extensions.Logging.Abstractions` - 日志抽象

## 使用示例

### 基本使用

```csharp
// 创建物理传输层
var transport = new SerialPortTransport("COM3", 9600);

// 创建帧策略（使用分隔符 \0）
var frameStrategy = new DelimiterFrameStrategy(new byte[] { 0 });

// 创建数据链路
var options = new DataLinkOptions
{
    ReceiveTimeoutMs = 1000,
    MaxRetryCount = 3
};

using var dataLink = new DirectDataLink(transport, frameStrategy, options);
await dataLink.OpenAsync();

// 发送帧并接收响应
var requestData = Encoding.ASCII.GetBytes("1:R:PRES:");
var responseData = await dataLink.SendAndReceiveFrameAsync(requestData);
```

### 自定义帧策略

```csharp
// 实现自定义帧策略
public class CustomFrameStrategy : IFrameStrategy
{
    public string Name => "Custom";
    
    public byte[] BuildFrame(byte[] data)
    {
        // 实现帧组装逻辑
        var frame = new byte[data.Length + 2]; // 添加帧头帧尾
        frame[0] = 0xAA; // 帧头
        Buffer.BlockCopy(data, 0, frame, 1, data.Length);
        frame[^1] = 0x55; // 帧尾
        return frame;
    }
    
    public bool TryParseFrame(byte[] accumulated, out int frameLength, out byte[] frameData)
    {
        // 实现帧解析逻辑
        frameLength = 0;
        frameData = Array.Empty<byte>();
        
        if (accumulated.Length < 2) return false;
        if (accumulated[0] != 0xAA) return false;
        
        // 查找帧尾
        for (int i = 1; i < accumulated.Length; i++)
        {
            if (accumulated[i] == 0x55)
            {
                frameLength = i + 1;
                frameData = new byte[i - 1];
                Buffer.BlockCopy(accumulated, 1, frameData, 0, i - 1);
                return true;
            }
        }
        
        return false;
    }
}
```

## 配置选项详解

### DataLinkOptions

| 属性 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| ReceiveTimeoutMs | int | 1000 | 接收超时时间（毫秒） |
| ReceiveIdleTimeoutMs | int | 50 | 接收空闲超时（毫秒），连续无数据到达视为帧结束 |
| MaxRetryCount | int | 0 | 最大重试次数 |
| RetryDelayMs | int | 300 | 重试延迟时间（毫秒） |
| ReceivePollIntervalMs | int | 10 | 接收轮询间隔（毫秒） |

## 异常处理

数据链路层定义了以下异常类型：

1. **`DataLinkException`** - 数据链路通用异常
2. **`FrameTimeoutException`** - 帧接收超时异常

## 设计原则

1. **策略模式** - 通过 `IFrameStrategy` 接口实现帧策略的可插拔
2. **单一职责** - 数据链路层只负责帧管理，不关心物理传输细节
3. **线程安全** - 使用 `SemaphoreSlim` 保证串行化访问
4. **自动重试** - 支持配置重试次数和延迟
5. **日志记录** - 内置 HEX 和文本格式的日志记录

## 注意事项

1. 数据链路层依赖物理传输层，必须先设置传输层
2. 帧策略的选择取决于具体协议要求
3. 超时和重试参数需要根据实际通信环境调整
4. 线程安全由数据链路层内部保证，调用者无需额外同步
5. 接收缓冲区管理由数据链路层自动处理