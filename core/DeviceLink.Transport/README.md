# DeviceLink.Transport

## 概述

`DeviceLink.Transport` 是 DeviceLink 框架的**物理传输层**实现，对应 OSI 模型的**物理层**。负责底层字节传输，提供统一的传输接口，屏蔽不同物理介质的差异。

## OSI 层级

**物理层 (Physical Layer)** - 负责在物理介质上传输原始字节流。

## 主要职责

1. **统一传输接口** - 定义 `IPhysicalTransport` 接口，提供标准化的连接、读取、写入操作
2. **多介质支持** - 支持串口、TCP、UDP、USB、回环等多种物理传输方式
3. **连接管理** - 管理物理连接的建立、维护和关闭
4. **缓冲区管理** - 处理数据缓冲区的读写和清空
5. **异常处理** - 提供传输层特定的异常类型

## 关键接口/类

### 核心接口

#### `IPhysicalTransport`
物理传输层的核心接口，定义了所有传输实现必须提供的方法：

```csharp
public interface IPhysicalTransport : IDisposable
{
    string Name { get; }           // 传输名称（用于日志）
    bool IsOpen { get; }           // 是否已连接
    Task ConnectAsync(CancellationToken ct = default);  // 建立连接
    Task CloseAsync();             // 关闭连接
    Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct = default);  // 读取数据
    Task WriteAsync(byte[] data, int offset, int count, CancellationToken ct = default);  // 写入数据
    Task ClearReceiveBufferAsync(CancellationToken ct = default);  // 清空接收缓冲区
}
```

### 传输实现

#### `SerialPortTransport`
串口传输实现，封装 `System.IO.Ports.SerialPort`：

- **配置选项**: `SerialPortOptions` - 包含端口名、波特率、数据位、停止位、校验位等
- **特点**: 支持异步读写，自动处理缓冲区管理
- **使用场景**: 串口设备通信（如 RS232、RS485）

#### `TcpTransport`
TCP 传输实现，封装 `System.Net.Sockets.TcpClient`：

- **配置选项**: `TcpOptions` - 包含主机地址、端口、连接超时、缓冲区大小
- **特点**: 支持连接超时控制，异步操作
- **使用场景**: 网络设备通信（如 TCP 服务器、仪器控制）

#### `UdpTransport`
UDP 传输实现，封装 `System.Net.Sockets.UdpClient`：

- **特点**: 无连接通信，适合广播或多播场景
- **使用场景**: UDP 设备通信

#### `UsbTransport`
USB 传输实现（通过虚拟串口或 HID）：

- **特点**: 支持 USB 设备通信
- **使用场景**: USB 接口设备

#### `LoopbackTransport`
回环传输实现，用于测试：

- **特点**: 数据发送后立即返回，无需实际硬件
- **使用场景**: 单元测试、调试

### 异常类

#### `TransportException`
传输层通用异常，表示传输过程中发生的错误。

#### `ConnectionException`
连接异常，表示连接建立或维护失败。

#### `TransportTimeoutException`
传输超时异常，表示操作超时。

## 依赖关系

- **无直接项目依赖** - 作为最底层，不依赖其他 DeviceLink 项目
- **NuGet 依赖**:
  - `Microsoft.Extensions.Logging.Abstractions` - 日志抽象
  - `System.IO.Ports` - 串口支持

## 使用示例

### 串口传输

```csharp
// 创建串口传输
var options = new SerialPortOptions
{
    PortName = "COM3",
    BaudRate = 9600,
    DataBits = 8,
    StopBits = StopBits.One,
    Parity = Parity.None
};

using var transport = new SerialPortTransport(options);
await transport.ConnectAsync();

// 发送数据
var data = Encoding.ASCII.GetBytes("Hello");
await transport.WriteAsync(data, 0, data.Length);

// 接收数据
var buffer = new byte[1024];
int bytesRead = await transport.ReadAsync(buffer, 0, buffer.Length);
```

### TCP 传输

```csharp
// 创建 TCP 传输
var options = new TcpOptions
{
    Host = "192.168.1.100",
    Port = 502,
    ConnectTimeoutMs = 5000
};

using var transport = new TcpTransport(options);
await transport.ConnectAsync();

// 使用传输进行通信...
```

## 配置选项

### SerialPortOptions

| 属性 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| PortName | string | "COM1" | 串口名称 |
| BaudRate | int | 9600 | 波特率 |
| DataBits | int | 8 | 数据位 |
| StopBits | StopBits | StopBits.One | 停止位 |
| Parity | Parity | Parity.None | 校验位 |
| ReadBufferSize | int | 4096 | 读取缓冲区大小 |
| WriteBufferSize | int | 2048 | 写入缓冲区大小 |

### TcpOptions

| 属性 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| Host | string | "127.0.0.1" | 主机地址 |
| Port | int | 10001 | 端口号 |
| ConnectTimeoutMs | int | 5000 | 连接超时时间（毫秒） |
| ReadBufferSize | int | 8192 | 读取缓冲区大小 |
| WriteBufferSize | int | 4096 | 写入缓冲区大小 |

## 异常处理

传输层定义了以下异常类型：

1. **`TransportException`** - 传输通用异常
2. **`ConnectionException`** - 连接失败异常
3. **`TransportTimeoutException`** - 传输超时异常

建议在调用传输层方法时捕获这些异常并进行适当处理。

## 设计原则

1. **单一职责** - 每个传输类只负责一种物理介质的传输
2. **接口抽象** - 通过 `IPhysicalTransport` 接口实现多态
3. **异步优先** - 所有操作都支持异步执行
4. **可扩展性** - 易于添加新的传输介质实现
5. **日志支持** - 内置日志记录，便于调试和监控

## 注意事项

1. 所有传输实现都实现了 `IDisposable`，使用后应及时释放资源
2. 读写操作支持 `CancellationToken`，可用于实现超时控制
3. 缓冲区管理由各传输实现自行处理，调用者无需关心底层细节
4. 连接状态通过 `IsOpen` 属性实时反映