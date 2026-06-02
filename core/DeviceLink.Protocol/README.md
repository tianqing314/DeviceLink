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
    string ProtocolName { get; }  // 协议名称
    byte[] Encode(Command command);  // 编码命令
    string DecodeText(byte[] raw);  // 解码为文本
    bool IsErrorResponse(byte[] raw, out string errorMessage);  // 检查错误响应
}
```

### 协议实现

#### `ConSTCodec`
ConST 私有协议编解码器：

- **协议格式**: `address:mark:command:param1:param2:...\0`
- **命令类型**: R=读 / W=写
- **响应格式**: F=成功 / E=错误
- **特点**: 简单文本协议，适合仪器控制
- **使用场景**: ConST 系列仪器通信

#### `ModbusRtuCodec`
Modbus RTU 协议编解码器：

- **协议格式**: 符合 Modbus RTU 标准
- **功能码**: 支持读保持寄存器、写单个/多个寄存器等
- **特点**: 二进制协议，CRC 校验
- **使用场景**: Modbus RTU 设备通信

#### `ScpiCodec`
SCPI 协议编解码器：

- **协议格式**: 符合 SCPI (Standard Commands for Programmable Instruments) 标准
- **特点**: 文本协议，命令结构化
- **使用场景**: 可编程仪器控制

### 命令类

#### `Command`
逻辑命令类：

```csharp
public class Command
{
    public CommandKind Kind { get; set; }  // 命令类型
    public string Id { get; set; }          // 命令ID
    public string[] Parameters { get; set; }  // 命令参数
    public byte[]? Data { get; set; }       // 命令数据
    
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
  - `Microsoft.Extensions.Logging.Abstractions` - 日志抽象

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

// 读取保持寄存器
var readCommand = new Command
{
    Kind = CommandKind.Read,
    Id = "40001",  // 寄存器地址
    Parameters = new[] { "10" }  // 读取数量
};

var requestData = codec.Encode(readCommand);
// requestData: [0x01, 0x03, 0x9C, 0x41, 0x00, 0x0A, CRC_L, CRC_H]

// 解码响应
var responseData = new byte[] { 0x01, 0x03, 0x14, ... };
if (!codec.IsErrorResponse(responseData, out var error))
{
    // 处理正常响应
}
```

### SCPI 协议

```csharp
// 创建 SCPI 协议编解码器
var codec = new ScpiCodec();

// 创建 SCPI 命令
var command = Command.Read("*IDN?");
var requestData = codec.Encode(command);
// requestData: "*IDN?\n"

// 解码响应
var responseData = Encoding.ASCII.GetBytes("Manufacturer,Model,SerialNumber,Version\n");
var responseText = codec.DecodeText(responseData);
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
- `\0`: 帧结束符

**响应格式**:
```
address:F:command:value1:value2:...\0  // 成功
address:E:errorcode\0                  // 错误
```

### Modbus RTU 协议

**请求格式**:
```
[设备地址][功能码][起始地址高][起始地址低][数量高][数量低][CRC低][CRC高]
```

**响应格式**:
```
[设备地址][功能码][字节数][数据...][CRC低][CRC高]
```

## 异常处理

协议层定义了以下异常类型：

1. **`ProtocolException`** - 协议通用异常

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

## 扩展性

协议层设计具有良好的扩展性：

1. **新协议支持** - 可以轻松添加新的协议实现（如自定义二进制协议）
2. **协议转换** - 可以实现协议转换器，在不同协议间转换
3. **协议验证** - 可以添加协议验证逻辑，确保数据符合协议规范
4. **协议版本管理** - 可以支持同一协议的不同版本