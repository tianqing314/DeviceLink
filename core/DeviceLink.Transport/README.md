# DeviceLink.Transport

## 概述

`DeviceLink.Transport` 是 DeviceLink 框架的**物理传输层**实现，对应 OSI 模型的**物理层**。负责底层字节传输，提供统一的传输接口，屏蔽不同物理介质（串口、TCP、UDP、USB、蓝牙、Zigbee、回环）的差异。

## OSI 层级

**物理层 (Physical Layer)** - 负责在物理介质上传输原始字节流。

## 主要职责

1. **统一传输接口** - 定义 `IPhysicalTransport` 接口，提供标准化的连接、读取、写入操作
2. **多介质支持** - 支持串口、TCP、UDP、USB、蓝牙、Zigbee、回环等多种物理传输方式
3. **连接管理** - 管理物理连接的建立、维护和关闭
4. **缓冲区管理** - 处理数据缓冲区的读写和清空
5. **异常处理** - 提供传输层特定的异常类型
6. **通信日志** - 提供 `CommunicationLogger` 静态类，记录完整通信链路日志到 HTML 文件

## 关键接口/类

### 核心接口

#### `IPhysicalTransport`

物理传输层的核心接口，定义了所有传输实现必须提供的方法：

```csharp
public interface IPhysicalTransport : IDisposable
{
    string Name { get; }           // 传输名称（用于日志），如 "COM3@9600"、"192.168.1.100:10001"
    bool IsOpen { get; }           // 是否已连接
    Task ConnectAsync(CancellationToken ct = default);  // 建立连接
    Task CloseAsync();             // 关闭连接
    Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct = default);  // 读取数据（无数据时返回 0，不阻塞）
    Task WriteAsync(byte[] data, int offset, int count, CancellationToken ct = default);  // 写入数据
    Task ClearReceiveBufferAsync(CancellationToken ct = default);  // 清空接收缓冲区
}
```

#### `IZigbeeModule`

Zigbee 模块抽象接口，定义不同厂商 Zigbee 模块（XBee / CC2530 / ZM32）的通用操作：

```csharp
public interface IZigbeeModule
{
    string Name { get; }  // 模块名称，如 "XBee" / "CC2530" / "ZM32"
    Task EnterCommandModeAsync(IPhysicalTransport transport, CancellationToken ct = default);  // 进入 AT 命令模式
    Task ExitCommandModeAsync(IPhysicalTransport transport, CancellationToken ct = default);   // 退出 AT 命令模式
    Task ConfigurePanIdAsync(IPhysicalTransport transport, ushort panId, CancellationToken ct = default);  // 配置 PAN ID (0x0000-0xFFFF)
    Task ConfigureChannelAsync(IPhysicalTransport transport, byte channel, CancellationToken ct = default);  // 配置通讯信道 (11-26)
    Task ConfigureDestinationAsync(IPhysicalTransport transport, ulong destAddress, CancellationToken ct = default);  // 配置目标地址
    byte[] BuildDataFrame(byte[] data, string? destination = null);  // 构建数据帧
    bool TryParseDataFrame(byte[] frame, out byte[] data, out string? source);  // 解析数据帧
}
```

### 传输实现

#### `SerialPortTransport`

串口传输实现，封装 `System.IO.Ports.SerialPort`：

- **构造方式**: `SerialPortTransport(SerialPortOptions, ILogger?)` 或便捷重载 `SerialPortTransport(portName, baudRate = 9600, dataBits = 8, stopBits = StopBits.One, parity = Parity.None, dtrEnable = false, rtsEnable = false, ILogger?)`
- **特点**: 支持异步读写，DTR/RTS 信号控制，自动处理缓冲区管理
- **使用场景**: 串口设备通信（如 RS232、RS485）
- **异常**: 连接失败抛 `ConnectionException`，读写失败抛 `TransportException`

#### `TcpTransport`

TCP 传输实现，封装 `System.Net.Sockets.TcpClient`：

- **构造方式**: `TcpTransport(TcpOptions, ILogger?)` 或便捷重载 `TcpTransport(host, port, connectTimeoutMs = 5000, ILogger?)`
- **特点**: 支持连接超时控制，禁用 Nagle 算法（`NoDelay = true`），异步操作
- **使用场景**: 网络设备通信（如 TCP 服务器、仪器控制）
- **异常**: 连接超时抛 `TransportTimeoutException`，连接失败抛 `ConnectionException`

#### `UdpTransport`

UDP 传输实现，封装 `System.Net.Sockets.UdpClient`：

- **构造方式**: `UdpTransport(UdpOptions, ILogger?)` 或便捷重载 `UdpTransport(host, port, localPort = 0, ILogger?)`
- **特点**: 无连接通信，适合广播或多播场景
- **使用场景**: UDP 设备通信

#### `UsbTransport`

USB 传输实现：

- **构造方式**: `UsbTransport(UsbOptions, ILogger?)` 或便捷重载 `UsbTransport(vendorId, productId, ILogger?)`
- **特点**: 通过 VID/PID 标识 USB 设备
- **注意**: 当前为 TODO 占位实现，需要根据具体 USB 库进行适配

#### `LoopbackTransport`

回环传输实现，用于测试：

- **构造方式**: `LoopbackTransport(ILogger?)`
- **特点**: 数据发送后立即返回，无需实际硬件
- **公共事件**: `event Action<byte[]>? OnSend` - 发送数据时触发
- **额外方法**: `void EnqueueReceive(byte[] data)` - 将数据推入接收队列，模拟设备主动发送
- **使用场景**: 单元测试、调试

#### `BluetoothTransport`

蓝牙传输实现，封装 `InTheHand.Net.Bluetooth` 库，支持经典蓝牙 RFCOMM/SPP 协议：

- **构造方式**: `BluetoothTransport(BluetoothOptions, ILogger?)`
- **特点**: 支持 MAC 地址和设备名称两种连接方式，设备发现、自动配对、TLS 认证配置
- **使用场景**: 蓝牙串口设备（如蓝牙转串口模块、蓝牙仪器）
- **依赖**: NuGet 包 `InTheHand.Net.Bluetooth`

#### `ZigbeeTransport`

Zigbee 传输层实现，封装串口传输和 Zigbee 模块配置：

- **构造方式**: `ZigbeeTransport(ZigbeeOptions, ILogger?)` 或 `ZigbeeTransport(ZigbeeOptions, IZigbeeModule, ILogger?)`（使用自定义模块实例）
- **特点**: 连接时自动进行模块配置（PAN ID / Channel / Destination），发送时自动通过模块的 `BuildDataFrame` 包装数据
- **注意**: 目前 `CreateModule` 仅支持 `ZM32` 模块，`XBee` / `CC2530` 会抛 `NotSupportedException`
- **使用场景**: Zigbee 无线设备组网通信

#### `ZM32Module`

周立功 ZM32 模块实现（`IZigbeeModule`）：

- **构造方式**: `ZM32Module(ZigbeeOptions, ILogger?)`
- **特点**: 支持 ZM32 系列 Zigbee 模块的 AT 指令配置和透明传输
- **AT 指令**: `+++` 进入命令模式（等待 GuardTimeMs 并验证 OK）、`AT+PANID=XXXX`、`AT+CHANNEL=XX`、`AT+DESTADDR=XXXXXXXXXXXXXXXX`、`AT+EXIT` 退出
- **传输模式**: 数据透传，`BuildDataFrame` / `TryParseDataFrame` 直接返回原始数据

### 通信日志

#### `CommunicationLogger`（静态类）

记录完整的通信链路日志到 HTML 文件，供调试与问题排查：

| 静态属性 | 类型 | 默认值 | 描述 |
|---------|------|--------|------|
| `LogDirectory` | string | `AppContext.BaseDirectory/logs` | 日志目录 |
| `LogFileName` | string | `"communication.html"` | 日志文件名 |
| `LogFilePath` | string（只读） | — | 日志文件完整路径 |
| `Enabled` | bool | `true` | 是否启用日志 |

| 静态方法 | 签名 | 描述 |
|---------|------|------|
| `LogSend` | `void LogSend(string deviceName, string commandId, string commandKind, string commandString, byte[] bytes)` | 记录发送命令 |
| `LogReceive` | `void LogReceive(string deviceName, long elapsedMs, byte[] responseBytes, string responseText)` | 记录接收响应 |
| `LogError` | `void LogError(string deviceName, string message, Exception? exception = null)` | 记录错误 |
| `LogInfo` | `void LogInfo(string deviceName, string message)` | 记录信息 |
| `LogRaw` | `void LogRaw(string deviceName, string direction, byte[] data)` | 记录原始数据 |
| `ClearLog` | `void ClearLog()` | 清空日志文件 |
| `GetLogFileSize` | `long GetLogFileSize()` | 获取日志文件大小（字节） |

### 配置选项

#### `SerialPortOptions`

| 属性 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| PortName | string | "COM1" | 串口名称 |
| BaudRate | int | 9600 | 波特率 |
| DataBits | int | 8 | 数据位 |
| StopBits | StopBits | StopBits.One | 停止位 |
| Parity | Parity | Parity.None | 校验位 |
| ReadBufferSize | int | 4096 | 读取缓冲区大小 |
| WriteBufferSize | int | 2048 | 写入缓冲区大小 |
| ReadTimeoutMs | int | 500 | 读取超时时间（毫秒） |
| DtrEnable | bool | false | 启用 DTR 信号 |
| RtsEnable | bool | false | 启用 RTS 信号 |

#### `TcpOptions`

| 属性 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| Host | string | "127.0.0.1" | 主机地址 |
| Port | int | 10001 | 端口号 |
| ConnectTimeoutMs | int | 5000 | 连接超时时间（毫秒） |
| ReadBufferSize | int | 8192 | 读取缓冲区大小 |
| WriteBufferSize | int | 4096 | 写入缓冲区大小 |

#### `UdpOptions`

| 属性 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| RemoteHost | string | "127.0.0.1" | 远程主机地址 |
| RemotePort | int | 10001 | 远程端口号 |
| LocalPort | int | 0 | 本地端口号（0=自动分配） |
| ReadBufferSize | int | 8192 | 读取缓冲区大小 |
| WriteBufferSize | int | 4096 | 写入缓冲区大小 |

#### `UsbOptions`

| 属性 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| VendorId | int | 0 | 厂商 ID |
| ProductId | int | 0 | 产品 ID |
| ReadBufferSize | int | 4096 | 读取缓冲区大小 |
| WriteBufferSize | int | 4096 | 写入缓冲区大小 |

#### `BluetoothOptions`

| 属性 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| DeviceAddress | string | "" | 蓝牙设备地址（MAC 地址或蓝牙名称） |
| ServiceUuid | Guid | `00001101-...-00805F9B34FB` | 蓝牙服务 UUID（SPP 默认） |
| ConnectTimeoutMs | int | 10000 | 连接超时时间（毫秒） |
| ReadBufferSize | int | 4096 | 读取缓冲区大小 |
| WriteBufferSize | int | 2048 | 写入缓冲区大小 |
| AutoPair | bool | true | 是否在连接前自动配对 |
| PinCode | string? | null | 配对 PIN 码（部分设备需要） |
| UseClassicBluetooth | bool | true | 是否使用蓝牙经典模式（RFCOMM/SPP） |
| DiscoveryTimeoutMs | int | 5000 | 设备发现超时时间（毫秒） |
| AutoDiscover | bool | false | 是否在连接前自动发现设备 |
| DeviceClassFilter | string? | null | 蓝牙设备类过滤（可选） |
| EnableAuthentication | bool | true | 是否启用蓝牙安全认证 |
| EnableEncryption | bool | false | 是否启用蓝牙加密 |

#### `ZigbeeOptions`（继承 `SerialPortOptions`）

| 属性 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| ModuleType | ZigbeeModuleType | ZM32 | Zigbee 模块类型 |
| PanId | ushort | 0x1234 | PAN ID (0x0000-0xFFFF) |
| Channel | byte | 0x0B (11) | 通讯信道 (11-26) |
| DestinationAddress | ulong | 0 | 目标地址（64 位长地址） |
| UseApiMode | bool | false | 是否使用 API 模式（仅 XBee 支持） |
| GuardTimeMs | int | 1000 | 进入命令模式保护时间（毫秒） |
| CommandTimeoutMs | int | 2000 | 命令模式超时时间（毫秒） |
| ZM32_TargetNetworkAddress | ushort | 0x0000 | ZM32 目标网络地址（0xFFFF=广播所有，0xFFFD=广播非睡眠，0xFFFC=广播协调器/路由） |
| ZM32_SendMode | byte | 0x01 | ZM32 发送模式（单播/广播/组播等） |
| ZM32_DeviceType | byte | 0x00 | ZM32 设备类型（0=协调器，1=路由器，2=终端） |
| ZM32_EnableAutoNetwork | bool | false | ZM32 是否启用自组网 |
| ZM32_TargetGroupNumber | ushort | 0x0001 | ZM32 目标分组号（组播模式） |

**辅助方法**: `ZigbeeOptions.FromSerialPortOptions(SerialPortOptions, ZigbeeModuleType = ZM32)` - 从串口配置创建 Zigbee 配置。

#### `ZigbeeModuleType`（枚举）

```csharp
public enum ZigbeeModuleType
{
    XBee,     // Digi XBee 模块
    CC2530,   // TI CC2530 模块
    ZM32      // 周立功 ZM32 模块
}
```

### 异常类

传输层定义了以下异常类型（均定义在 `Exceptions/TransportException.cs`）：

1. **`TransportException`** - 传输通用异常，表示传输过程中发生的错误
2. **`ConnectionException`** - 连接异常，表示连接建立或维护失败
3. **`TransportTimeoutException`** - 传输超时异常，表示操作超时

## 依赖关系

- **无直接项目依赖** - 作为最底层库，不依赖其他 DeviceLink 项目
- **NuGet 依赖**:
  - `Microsoft.Extensions.Logging.Abstractions` 6.0.0 - 日志抽象
  - `System.IO.Ports` 6.0.0 - 串口支持
  - `InTheHand.Net.Bluetooth` 4.2.4 - 蓝牙支持

## 使用示例

### 串口传输

```csharp
// 方式一：使用配置对象
var options = new SerialPortOptions
{
    PortName = "COM3",
    BaudRate = 9600,
    DataBits = 8,
    StopBits = StopBits.One,
    Parity = Parity.None,
    DtrEnable = true,   // 某些设备需要 DTR 信号
    RtsEnable = true
};
using var transport = new SerialPortTransport(options);

// 方式二：便捷重载
using var transport2 = new SerialPortTransport("COM3", 9600);

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

### 蓝牙传输

```csharp
var options = new BluetoothOptions
{
    DeviceAddress = "00:11:22:33:44:55",  // MAC 地址或设备名称
    ConnectTimeoutMs = 10000
};
using var transport = new BluetoothTransport(options);
await transport.ConnectAsync();
```

### 回环传输（测试）

```csharp
using var transport = new LoopbackTransport();
transport.OnSend += bytes => Console.WriteLine($"发送: {BitConverter.ToString(bytes)}");
await transport.ConnectAsync();

// 模拟设备主动推送数据
transport.EnqueueReceive(Encoding.ASCII.GetBytes("device data"));
var buffer = new byte[1024];
int n = await transport.ReadAsync(buffer, 0, buffer.Length);
```

### Zigbee 传输

```csharp
var options = new ZigbeeOptions
{
    PortName = "COM4",
    BaudRate = 115200,
    ModuleType = ZigbeeModuleType.ZM32,
    PanId = 0x1234,
    Channel = 11,
    DestinationAddress = 0x1234567890ABCDEF
};
using var transport = new ZigbeeTransport(options);
await transport.ConnectAsync();  // 自动完成 PAN ID / 信道 / 目标地址配置
```

## 设计原则

1. **单一职责** - 每个传输类只负责一种物理介质的传输
2. **接口抽象** - 通过 `IPhysicalTransport` 接口实现多态
3. **异步优先** - 所有操作都支持异步执行
4. **可扩展性** - 易于添加新的传输介质实现（如自定义 Zigbee 模块实现 `IZigbeeModule`）
5. **日志支持** - 内置日志记录与 `CommunicationLogger` 通信链路日志，便于调试和监控

## 注意事项

1. 所有传输实现都实现了 `IDisposable`，使用后应及时释放资源
2. 读写操作支持 `CancellationToken`，可用于实现超时控制
3. 缓冲区管理由各传输实现自行处理，调用者无需关心底层细节
4. 连接状态通过 `IsOpen` 属性实时反映
5. `UsbTransport` 目前为 TODO 占位实现，实际使用前需要根据具体 USB 库适配
6. `ZigbeeTransport` 目前仅支持 `ZM32` 模块，`XBee` / `CC2530` 模块需自行实现 `IZigbeeModule` 后通过构造函数注入
7. `ReadAsync` 在无数据时返回 0 而非阻塞，调用方需要自行轮询或使用上层（数据链路层）的接收循环
