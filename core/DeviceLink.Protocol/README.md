# DeviceLink.Protocol

## 概述

`DeviceLink.Protocol` 是 DeviceLink 框架的**协议层**实现，对应 OSI 模型的**表示层/应用层**的一部分。负责将逻辑命令编码为字节，将响应字节解码为业务结果。

## OSI 层级

**协议层 (Protocol Layer)** - 负责数据格式转换、编解码和协议处理。

## 主要职责

1. **协议编解码** - 将逻辑命令编码为传输字节，将响应字节解码为业务数据
2. **协议抽象** - 定义统一的协议接口，支持多种协议实现
3. **错误检测** - 检测设备错误响应
4. **命令管理** - 定义命令类型和结构
5. **响应处理** - 处理协议响应，提取业务数据

## 关键接口/类

### 核心接口

#### `IProtocolCodec`

协议编解码器接口：

```csharp
public interface IProtocolCodec
{
    string ProtocolName { get; }  // 协议名称，如 "ConST"、"SCPI"、"ModbusRTU"、"ZQWL"
    byte[] Encode(Command command);  // 将逻辑命令编码为传输字节（已包含帧分隔符）
    string DecodeText(byte[] raw);  // 将响应字节解码为文本（用于日志和简单查询）
    bool IsErrorResponse(byte[] raw, out string errorMessage);  // 检查错误响应
}
```

### 协议实现

#### `ConSTCodec`

ConST 私有协议编解码器：

- **协议格式**: `address:mark:command:param1:param2:...\0`
- **命令类型**: R=读 / W=写
- **响应格式**: F=成功 / E=错误
- **构造方式**: `ConSTCodec(byte address = 255, char separator = ':', byte[]? terminator = null)` - 可自定义分隔符和帧结束符
- **扩展方法**: `ExtractFields(raw, startIndex = 3)`、`ExtractField(raw, index = 3)` - 提取响应字段值
- **使用场景**: ConST 系列仪器通信

**示例**:
```csharp
// 发送: "255:R:PRES:\0"          → 读取压力
// 接收: "255:F:PRES:1.23456\0"   → 压力值 1.23456
// 接收: "255:E:ERR_OVER\0"       → 设备错误
```

#### `ModbusRtuCodec`

Modbus RTU 协议编解码器：

- **协议格式**: 符合 Modbus RTU 标准，帧格式 `[设备地址][功能码][数据...][CRC低][CRC高]`
- **构造方式**: `ModbusRtuCodec(byte slaveAddress = 1)`（从站地址 1~247）
- **命令 ID 格式**: `功能码.寄存器地址[.寄存器数量]`，例如 `"3.0.10"` 表示读取从地址 0 开始的 10 个保持寄存器
- **支持功能码**: 0x03 读保持寄存器、0x06 写单个寄存器、0x10 写多个寄存器、0x28 读寄存器（PS02 自定义 F40）、0x29 写寄存器（PS02 自定义 F41，支持通过 `Command.Data` 传入原始字节）
- **错误检测**: 识别 Modbus 异常响应（功能码 | 0x80），翻译为标准错误码消息（非法功能码、非法数据地址等）
- **扩展方法**: `ExtractRegisters(raw)` - 提取寄存器值数组
- **使用场景**: Modbus RTU 设备通信

#### `ScpiCodec`

SCPI 协议编解码器：

- **协议格式**: 符合 SCPI (Standard Commands for Programmable Instruments) 标准，命令以换行符结尾
- **构造方式**: `ScpiCodec(string terminator = "\n")` - 支持多字符结束符如 `"\r\n"`
- **编码规则**: `CommandKind.Read` 自动追加 `?`；`Write`/`NonQuery` 追加空格分隔的参数
- **错误检测**: 识别 `-100,"Command error"` 格式的 SCPI 错误响应
- **扩展方法**: `ExtractNumeric(raw)`（解析数值，失败返回 NaN）、`ExtractString(raw)`、`ExtractBoolean(raw)`、`ExtractField(raw)`（直接返回解码文本）、`ExtractField(raw, separator, index)`、`ExtractFields(raw, separator)`
- **使用场景**: 可编程仪器控制

#### `ZqwlCodec`

ZQWL（智嵌物联）继电器协议编解码器：

- **协议格式**: `[addr] [func] [8 bytes data]`（不含帧头帧尾，帧头帧尾由 `ZqwlFrameStrategy` 负责）
- **构造方式**: `ZqwlCodec(byte address = 1)`
- **命令 ID 格式**: `操作[.参数]`，例如 `"SetOutput.1.1"`（设置第 1 路输出为开）、`"GetOutput.1"`（读取单路输出状态）
- **支持功能码**:
  - `0x52` GetInput - 读取输入状态
  - `0x57` CloseAll/OpenAll - 设置全部输出（批量）
  - `0x70` SetOutput - 设置单路输出
  - `0x53` GetAllStatuses - 读取全部输出状态
  - `0x72` GetOutput - 读取单路输出状态
  - `0x66` GetVersion - 读取版本号
  - `0x0A` GetAnalogInput - 读取模拟量输入
- **错误检测**: 响应功能码为 0xFF 时视为设备错误
- **公共属性**: `byte Address` - 设备地址
- **扩展方法**: `ExtractInputState(raw, channel)`（提取单路输入状态）、`ExtractVersion(raw)`（提取版本号）、`ExtractAnalogValue(raw)`（提取模拟量值）
- **使用场景**: 智嵌物联 BNRC8 / BNRC16 / BNRC32 网络继电器控制器

### 命令类

#### `Command`

逻辑命令类：

```csharp
public class Command
{
    public CommandKind Kind { get; set; }  // 命令类型
    public string Id { get; set; } = string.Empty;  // 命令ID（如寄存器地址、SCPI命令等）
    public string[] Parameters { get; set; } = Array.Empty<string>();  // 命令参数
    public byte[]? Data { get; set; }  // 命令数据（用于写入操作，如 Modbus F41 原始字节）

    // 静态工厂方法
    public static Command Read(string id, params string[] parameters);
    public static Command Write(string id, params string[] parameters);
    public static Command NonQuery(string id, params string[] parameters);
}
```

#### `CommandKind`

命令类型枚举：

```csharp
public enum CommandKind
{
    Read,      // 读取命令（需要返回数据）
    Write,     // 写入命令（发送数据，需要确认）
    NonQuery   // 无返回命令（发送命令，不需要返回）
}
```

### 响应类

#### `Response`

协议响应类：

```csharp
public class Response
{
    public bool Success { get; set; }  // 是否成功
    public byte[]? Data { get; set; }  // 响应数据
    public string? Text { get; set; }  // 响应文本
    public string? ErrorMessage { get; set; }  // 错误消息

    // 静态工厂方法
    public static Response Succeed(byte[] data);
    public static Response Succeed(string text);
    public static Response Fail(string errorMessage);
}
```

### 异常类

#### `ProtocolException`

协议层异常。

## 依赖关系

- **无直接项目依赖** - 协议层不依赖其他 DeviceLink 项目
- **NuGet 依赖**:
  - `Microsoft.Extensions.Logging.Abstractions` 6.0.0 - 日志抽象

## 使用示例

### ConST 协议

```csharp
// 创建 ConST 协议编解码器
var codec = new ConSTCodec(address: 255);

// 创建读取命令
var readCommand = Command.Read("PRES");
var requestData = codec.Encode(readCommand);
// requestData: "255:R:PRES:\0"

// 解码响应
var responseData = Encoding.ASCII.GetBytes("255:F:PRES:1.23456\0");
var responseText = codec.DecodeText(responseData);
// responseText: "255:F:PRES:1.23456"

// 检查错误响应
var errorData = Encoding.ASCII.GetBytes("255:E:ERR_OVER\0");
if (codec.IsErrorResponse(errorData, out var errorMessage))
{
    Console.WriteLine($"设备错误: {errorMessage}");
}

// 提取字段值
var fields = codec.ExtractFields(responseData);
// fields: ["1.23456"]
```

### Modbus RTU 协议

```csharp
// 创建 Modbus RTU 协议编解码器
var codec = new ModbusRtuCodec(slaveAddress: 1);

// 读取保持寄存器（功能码.寄存器地址.寄存器数量）
var readCommand = Command.Read("3.0", "10");
var requestData = codec.Encode(readCommand);
// requestData: [0x01, 0x03, 0x00, 0x00, 0x00, 0x0A]（不含CRC，CRC由数据链路层添加）

// 解码响应并提取寄存器值
var responseData = new byte[] { 0x01, 0x03, 0x04, 0x00, 0x01, 0x00, 0x02, 0x00, 0x00 };
if (!codec.IsErrorResponse(responseData, out var error))
{
    var registers = codec.ExtractRegisters(responseData);
    // registers: [1, 2]
}
```

### SCPI 协议

```csharp
// 创建 SCPI 协议编解码器
var codec = new ScpiCodec();

// 创建 SCPI 命令（Read 自动追加 ?）
var command = Command.Read("*IDN");
var requestData = codec.Encode(command);
// requestData: "*IDN?\n"

// 解码响应
var responseData = Encoding.ASCII.GetBytes("Manufacturer,Model,SerialNumber,Version\n");
var responseText = codec.DecodeText(responseData);

// 提取数值
var valueData = Encoding.ASCII.GetBytes("1.2345\n");
double value = codec.ExtractNumeric(valueData);
```

### ZQWL 协议

```csharp
// 创建 ZQWL 协议编解码器
var codec = new ZqwlCodec(address: 1);

// 设置第 1 路输出为开
var command = Command.NonQuery("SetOutput.1.1");
var requestData = codec.Encode(command);
// requestData: [0x01, 0x70, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]

// 读取全部输出状态
var statusCommand = Command.Read("GetAllStatuses");
var statusData = codec.Encode(statusCommand);
// statusData: [0x01, 0x53, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA]

// 从响应中提取版本号
var version = codec.ExtractVersion(responseData);
```

## 协议格式说明

### ConST 协议

**请求格式**:
```
address:mark:command:param1:param2:...\0
```

- `address`: 设备地址 (0-255)
- `mark`: 命令类型 (R=读, W=写)
- `command`: 命令标识符
- `param1, param2, ...`: 命令参数
- `\0`: 帧结束符（可自定义）

**响应格式**:
```
address:F:command:value1:value2:...\0  // 成功
address:E:errorcode\0                  // 错误
```

### Modbus RTU 协议

**请求格式**（CRC 由数据链路层 `ModbusRtuFrameStrategy` 添加）:
```
[设备地址][功能码][数据...]
```

**响应格式**:
```
[设备地址][功能码][字节数][数据...][CRC低][CRC高]
```

**功能码**:
| 功能码 | 含义 |
|--------|------|
| 0x03 | 读保持寄存器 |
| 0x06 | 写单个寄存器 |
| 0x10 | 写多个寄存器 |
| 0x28 | 读寄存器（PS02 自定义 F40） |
| 0x29 | 写寄存器（PS02 自定义 F41，支持原始字节数据） |

### ZQWL 协议

**请求格式**（不含帧头帧尾）:
```
[addr] [func] [8 bytes data]
```

**响应格式**:
```
[addr] [func] [8 bytes data]
```

**功能码**:
| 功能码 | 含义 | 数据区 |
|--------|------|--------|
| 0x52 | 读取输入状态 | 全 0x00 |
| 0x57 | 设置全部输出（批量） | 每字节代表 1 路（BNRC8）/ 2 路（BNRC16）/ 4 路（BNRC32） |
| 0x70 | 设置单路输出 | `[channel] [state] [00]...` |
| 0x53 | 读取全部输出状态 | 全 0xAA |
| 0x72 | 读取单路输出状态 | `[channel] [00]...` |
| 0x66 | 读取版本号 | 全 0x00 |
| 0x0A | 读取模拟量输入 | `[channel index] [00]...` |

## 异常处理

协议层定义了以下异常类型：

1. **`ProtocolException`** - 协议通用异常（如命令格式错误、编码参数错误）

## 设计原则

1. **策略模式** - 通过 `IProtocolCodec` 接口实现协议的可插拔
2. **单一职责** - 协议层只负责编解码，不关心传输细节
3. **工厂方法** - 使用静态工厂方法创建命令和响应
4. **扩展性** - 易于添加新的协议实现
5. **错误处理** - 统一的错误检测和报告机制

## 注意事项

1. 协议层是无状态的，不维护连接信息
2. 编码后的数据直接交给会话层或数据链路层传输
3. 解码时需要注意字符编码（通常为 ASCII 或 UTF-8）
4. 错误检测逻辑因协议而异，需要根据具体协议实现
5. 命令参数和响应字段的格式取决于具体协议规范
6. Modbus RTU 的 CRC 校验由数据链路层的 `ModbusRtuFrameStrategy` 负责，协议层编码结果不含 CRC
7. ZQWL 的帧头（`48 3A`）帧尾（`45 44`）由数据链路层的 `ZqwlFrameStrategy` 负责，协议层只处理中间 10 字节

## 扩展性

协议层设计具有良好的扩展性：

1. **新协议支持** - 可以轻松添加新的协议实现（如自定义二进制协议）
2. **协议转换** - 可以实现协议转换器，在不同协议间转换
3. **协议验证** - 可以添加协议验证逻辑，确保数据符合协议规范
4. **协议版本管理** - 可以支持同一协议的不同版本
