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
- **使用场景**: ConST 协议、SCPI 协议、AT 指令等

**使用示例**:

```csharp
// ConST 协议（以 \0 结尾）
var strategy = new DelimiterFrameStrategy(new byte[] { 0 });

// SCPI 协议（以 \n 结尾）
var strategy = new DelimiterFrameStrategy("\n");

// AT 指令（以 \r\n 结尾）
var strategy = new DelimiterFrameStrategy("\r\n");

// 自定义文本协议
var strategy = new DelimiterFrameStrategy(new byte[] { 0x0D, 0x0A });
```

**适用场景详解**:
| 协议类型 | 分隔符 | 示例 |
| --- | --- | --- |
| ConST 协议 | `\0` (0x00) | `new DelimiterFrameStrategy(new byte[]{0})` |
| SCPI 协议 | `\n` (0x0A) | `new DelimiterFrameStrategy("\n")` |
| AT 指令 | `\r\n` | `new DelimiterFrameStrategy("\r\n")` |
| 自定义文本协议 | 自定义 | 根据协议规范定义 |

#### `FixedLengthFrameStrategy`
固定长度帧策略，帧长度固定：

- **配置**: 指定固定帧长度
- **特点**: 解析简单，效率高
- **使用场景**: 固定长度二进制协议

**使用示例**:

```csharp
// 固定 10 字节长度的协议
var strategy = new FixedLengthFrameStrategy(10);

// 固定 8 字节长度的协议
var strategy = new FixedLengthFrameStrategy(8);
```

**适用场景详解**:
- 固定长度的二进制传感器数据
- 固定长度的状态查询响应
- 简单的定长命令/响应协议

#### `ModbusRtuFrameStrategy`
Modbus RTU 帧策略，实现 Modbus RTU 协议的帧处理：

- **特点**: 支持 CRC16 校验，符合 Modbus RTU 标准
- **使用场景**: Modbus RTU 设备通信
- **自动处理**: CRC16 校验、帧边界检测

**使用示例**:

```csharp
// Modbus RTU 协议（自动 CRC16 校验）
var strategy = new ModbusRtuFrameStrategy();
```

**适用场景详解**:
- 温湿度控制器（如 UMC1200）
- 电力仪表
- 工业传感器
- PLC 通信

#### `ZqwlFrameStrategy`
ZQWL（智嵌物联）二进制帧策略，用于智嵌物联网络继电器控制器：

- **帧格式**: `[48 3A] [addr] [func] [data x8] [checksum] [45 44]`
- **帧头**: 0x48 0x3A（"H:"）
- **帧尾**: 0x45 0x44（"ED"）
- **校验和**: 帧头到数据区所有字节之和，取低 8 位
- **总帧长**: 15 字节

**使用示例**:

```csharp
// ZQWL 网络继电器控制器
var strategy = new ZqwlFrameStrategy();
```

**适用场景详解**:
- 智嵌物联 BNRC8（8 路继电器控制器）
- 智嵌物联 BNRC16（16 路继电器控制器）
- 智嵌物联 BNRC32（32 路继电器控制器）
- 智嵌物联其他网络继电器设备

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

## 协议选择指南

### 如何选择合适的帧策略？

```
┌─────────────────────────────────────────────────────────────┐
│                    协议类型判断                              │
├─────────────────────────────────────────────────────────────┤
│  文本协议 ──→ DelimiterFrameStrategy                        │
│    ├── 以 \0 结尾 ──→ new byte[]{0}                        │
│    ├── 以 \n 结尾 ──→ "\n"                                  │
│    └── 以 \r\n 结尾 ──→ "\r\n"                              │
├─────────────────────────────────────────────────────────────┤
│  二进制协议                                                 │
│    ├── 固定长度 ──→ FixedLengthFrameStrategy               │
│    ├── Modbus RTU ──→ ModbusRtuFrameStrategy               │
│    └── ZQWL 继电器 ──→ ZqwlFrameStrategy                   │
└─────────────────────────────────────────────────────────────┘
```

### 快速选择表

| 协议类型 | 帧特征 | 推荐策略 | 示例 |
|---------|--------|---------|------|
| SCPI | 以 `\n` 结尾 | `DelimiterFrameStrategy("\n")` | 仪器控制 |
| ConST | 以 `\0` 结尾 | `DelimiterFrameStrategy(new byte[]{0})` | 压力控制器 |
| AT 指令 | 以 `\r\n` 结尾 | `DelimiterFrameStrategy("\r\n")` | 调制解调器 |
| Modbus RTU | CRC16 校验 | `ModbusRtuFrameStrategy()` | 温控器、仪表 |
| ZQWL 继电器 | 固定 15 字节 | `ZqwlFrameStrategy()` | 网络继电器 |
| 自定义定长 | 固定字节数 | `FixedLengthFrameStrategy(n)` | 传感器 |

### 通过 DeviceBase 自动选择

在 `DeviceBase` 中，框架会根据协议编解码器的 `ProtocolName` 自动选择推荐的帧策略：

```csharp
// DeviceBase 内部自动映射
protected static IFrameStrategy? GetDefaultFrameStrategy(IProtocolCodec codec)
{
    return codec.ProtocolName switch
    {
        "ModbusRTU" => new ModbusRtuFrameStrategy(),
        "ZQWL" => new ZqwlFrameStrategy(),
        _ => null  // 默认使用 DelimiterFrameStrategy
    };
}
```

**使用方式**:

```csharp
// 设备类只需指定协议编解码器，帧策略自动选择
public class MyDevice : DeviceBase
{
    public MyDevice(string portName)
        : base(portName, new ModbusRtuCodec(1))  // 自动使用 ModbusRtuFrameStrategy
    {
    }
}

// 或者手动指定帧策略（覆盖自动选择）
public class MyDevice : DeviceBase
{
    public MyDevice(string portName)
        : base(portName, new ModbusRtuCodec(1), new CustomFrameStrategy())
    {
    }
}
```

## 常见问题与最佳实践

### Q1: 如何处理不确定帧长度的协议？

**A**: 使用 `DelimiterFrameStrategy`，它会根据分隔符自动检测帧边界。

```csharp
// 以换行符作为帧结束
var strategy = new DelimiterFrameStrategy("\n");
```

### Q2: Modbus RTU 的 CRC 校验应该在哪里处理？

**A**: CRC 校验应该在数据链路层（`ModbusRtuFrameStrategy`）处理，而不是协议层。

```csharp
// 正确：协议层只负责编码，数据链路层负责 CRC
public class ModbusRtuCodec : IProtocolCodec
{
    public byte[] Encode(Command command)
    {
        // 只返回 [slaveAddress][functionCode][data...]
        // 不包含 CRC
        return new byte[] { slaveAddress, functionCode, ... };
    }
}

// CRC 由 ModbusRtuFrameStrategy.BuildFrame() 自动添加
```

### Q3: 如何实现自定义帧策略？

**A**: 实现 `IFrameStrategy` 接口：

```csharp
public class MyFrameStrategy : IFrameStrategy
{
    public string Name => "MyProtocol";
    
    public byte[] BuildFrame(byte[] data)
    {
        // 组装帧：添加帧头、帧尾、校验等
        var frame = new byte[data.Length + 4]; // 帧头2字节 + 数据 + 校验1字节 + 帧尾1字节
        frame[0] = 0xAA; // 帧头
        frame[1] = 0x55;
        Buffer.BlockCopy(data, 0, frame, 2, data.Length);
        // 计算校验
        frame[^2] = CalculateChecksum(data);
        frame[^1] = 0x0D; // 帧尾
        return frame;
    }
    
    public bool TryParseFrame(byte[] accumulated, out int frameLength, out byte[] frameData)
    {
        frameLength = 0;
        frameData = Array.Empty<byte>();
        
        // 查找帧头
        if (accumulated.Length < 4) return false;
        if (accumulated[0] != 0xAA || accumulated[1] != 0x55) return false;
        
        // 查找帧尾
        for (int i = 2; i < accumulated.Length; i++)
        {
            if (accumulated[i] == 0x0D)
            {
                frameLength = i + 1;
                frameData = new byte[i - 3]; // 去除帧头2字节和帧尾1字节
                Buffer.BlockCopy(accumulated, 2, frameData, 0, i - 3);
                
                // 验证校验
                if (ValidateChecksum(frameData, accumulated[i - 1]))
                    return true;
            }
        }
        return false;
    }
}
```

### Q4: 帧策略的性能考虑？

**A**: 
- `FixedLengthFrameStrategy` 性能最好，无需扫描
- `DelimiterFrameStrategy` 需要扫描分隔符，性能中等
- `ModbusRtuFrameStrategy` 需要 CRC 计算，性能较低但功能完整

### Q5: 如何调试帧解析问题？

**A**: 启用详细日志记录：

```csharp
var options = new DataLinkOptions
{
    ReceiveTimeoutMs = 1000,
    ReceiveIdleTimeoutMs = 50,
    // 启用详细日志
};
```

日志会显示：
- 发送的原始字节（HEX 格式）
- 接收的原始字节（HEX 格式）
- 帧解析结果
- 超时和错误信息

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