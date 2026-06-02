# DeviceLink.DeviceBase

## 概述

`DeviceLink.DeviceBase` 是 DeviceLink 框架的**设备基类库**，为应用层设备提供统一的基类。封装了命令发送、响应接收、错误检查等通用逻辑，设备开发者只需继承基类并实现业务方法。

## 主要职责

1. **设备抽象** - 定义设备基类 `DeviceBase`，封装通用设备操作
2. **命令发送** - 提供 `SendAsync`、`SendForResultAsync`、`SendNonQueryAsync` 方法
3. **错误检查** - 自动通过 `IProtocolCodec.IsErrorResponse` 检测设备错误
4. **日志记录** - 内置设备名 + 操作名的日志记录
5. **异常处理** - 定义 `DeviceException` 设备异常类型

## 关键接口/类

### `DeviceBase`（抽象类）
设备基类，所有设备类的父类：

| 成员 | 描述 |
|------|------|
| `Name` | 设备名称（默认为类名） |
| `Session` | 会话层实例（protected） |
| `Codec` | 协议编解码器实例（protected） |
| `Logger` | 日志记录器（protected） |
| `IsOpen` | 设备是否已连接 |
| `OpenAsync(ct)` | 打开设备连接 |
| `CloseAsync()` | 关闭设备连接 |
| `SendAsync(command, ct)` | 发送命令并返回原始响应字节 |
| `SendForResultAsync<T>(command, decoder, ct)` | 发送命令并返回解析后的业务数据 |
| `SendNonQueryAsync(command, ct)` | 单向发送命令（不等待响应） |
| `Dispose()` | 释放资源 |

### `DeviceException`
设备异常类，表示设备操作过程中发生的错误。

## 依赖关系

- **项目依赖**:
  - `DeviceLink.Session` - 会话层接口
  - `DeviceLink.Protocol` - 协议编解码器接口
- **NuGet 依赖**:
  - `Microsoft.Extensions.Logging.Abstractions` - 日志抽象

## 使用示例

### 继承 DeviceBase 实现设备类

```csharp
public class DPSEX : DeviceBase
{
    private readonly ConSTCodec _codec;

    public DPSEX(ISession session, ConSTCodec codec, ILogger? logger = null)
        : base(session, codec, logger)
    {
        _codec = codec;
    }

    // 读取压力值
    public async Task<double> ReadPressureAsync(CancellationToken ct = default)
    {
        return await SendForResultAsync(
            Command.Read("PRES"),
            raw => double.Parse(_codec.ExtractField(raw)),
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
// 创建会话层
var transport = new SerialPortTransport("COM3", 9600);
var frameStrategy = new DelimiterFrameStrategy(new byte[] { 0 });
var dataLink = new DirectDataLink(transport, frameStrategy);
var session = new DirectSession(dataLink);

// 创建设备
var codec = new ConSTCodec(255);
using var device = new DPSEX(session, codec);
await device.OpenAsync();

// 使用设备
double pressure = await device.ReadPressureAsync();
await device.SetUnitAsync("bar");
```

## 设计原则

1. **模板方法模式** - 基类提供通用流程，子类实现业务细节
2. **依赖倒置** - 依赖 `ISession` 和 `IProtocolCodec` 接口
3. **错误自动检测** - 通过协议编解码器自动检测设备错误
4. **日志内置** - 所有操作自动记录日志
5. **资源管理** - 实现 `IDisposable`，确保资源正确释放

## 注意事项

1. `DeviceBase` 是抽象类，不能直接实例化
2. 子类应通过 `SendAsync` / `SendForResultAsync` 发送命令，不要直接操作 Session
3. 基类不提供业务接口（如压力、温度等），这些由子类定义
4. 基类不提供重试逻辑，重试在会话层已处理
5. 命名空间 `DeviceLink.DeviceBase` 与类名 `DeviceBase` 相同时，子类需要使用完全限定名 `DeviceLink.DeviceBase.DeviceBase` 继承