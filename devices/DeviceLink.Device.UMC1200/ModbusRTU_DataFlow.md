# UMC1200 Modbus RTU 数据运转流程文档

## 1. 概述

本文档详细描述UMC1200温湿度控制器通过Modbus RTU协议与DeviceLink框架进行数据通信的完整流程。

## 2. 协议参数

| 参数 | 值 |
|------|-----|
| 波特率 | 9600 |
| 数据位 | 8位 |
| 校验 | NONE |
| 停止位 | 1 |
| 从站地址 | 1（可配置） |
| 功能码 | 0x03（读）、0x10（写） |

## 3. 通信架构

```
┌─────────────────────────────────────────────────────────────────┐
│                    应用层 (UMC1200.cs)                           │
│  GetTemperaturePVAsync() → Command.Read("3.0.1")                │
├─────────────────────────────────────────────────────────────────┤
│                    协议层 (ModbusRtuCodec)                       │
│  Encode: [01][03][00][00][00][01][84][0A]                       │
├─────────────────────────────────────────────────────────────────┤
│                    数据链路层 (ModbusRtuFrameStrategy)           │
│  帧边界检测、CRC校验                                             │
├─────────────────────────────────────────────────────────────────┤
│                    物理层 (SerialPortTransport)                  │
│  RS-485串口通信                                                  │
└─────────────────────────────────────────────────────────────────┘
```

## 4. 数据流转详细过程

### 4.1 读取温度PV值的完整流程

**步骤1：应用层调用**

```csharp
var umc1200 = new UMC1200("COM3");
await umc1200.OpenAsync();
short tempPV = await umc1200.GetTemperaturePVAsync();
```

**步骤2：生成Command对象**

```csharp
// UMC1200.cs 中的 GetTemperaturePVAsync 方法
public async Task<short> GetTemperaturePVAsync(CancellationToken ct = default)
{
    ushort raw = await ReadRegisterAsync(UMC1200Registers.TemperaturePV, ct);
    return (short)raw;
}

// ReadRegisterAsync 方法
public async Task<ushort> ReadRegisterAsync(ushort registerAddress, CancellationToken ct = default)
{
    return await SendForResultAsync(
        Command.Read($"3.{registerAddress}.1"),  // 生成命令: "3.0.1"
        raw =>
        {
            var registers = _codec.ExtractRegisters(raw);
            return registers.Length > 0 ? registers[0] : (ushort)0;
        },
        ct);
}
```

**步骤3：协议编码（ModbusRtuCodec.Encode）**

```csharp
// ModbusRtuCodec.cs
public byte[] Encode(Command command)
{
    // 解析命令ID "3.0.1"
    var parts = command.Id.Split('.');
    byte functionCode = byte.Parse(parts[0]);      // 3 (0x03)
    ushort registerAddress = ushort.Parse(parts[1]); // 0
    ushort registerCount = ushort.Parse(command.Parameters[0]); // 1
    
    // 构建数据部分
    var data = new byte[6];
    data[0] = functionCode;                        // 0x03
    data[1] = (byte)(registerAddress >> 8);        // 0x00 (地址高字节)
    data[2] = (byte)(registerAddress & 0xFF);      // 0x00 (地址低字节)
    data[3] = (byte)(registerCount >> 8);          // 0x00 (数量高字节)
    data[4] = (byte)(registerCount & 0xFF);        // 0x01 (数量低字节)
    
    // 组装原始数据（不含CRC，CRC由数据链路层负责）
    var frame = new byte[1 + data.Length];
    frame[0] = _slaveAddress;                      // 0x01 (从站地址)
    Array.Copy(data, 0, frame, 1, data.Length);
    
    return frame;
}
```

**编码结果：**

```
原始字节: [01] [03] [00] [00] [00] [01]
          │    │    │    │    │    │
          │    │    │    │    │    └── 数量低字节 (1)
          │    │    │    │    └─────── 数量高字节 (0)
          │    │    │    └──────────── 地址低字节 (0)
          │    │    └───────────────── 地址高字节 (0)
          │    └────────────────────── 功能码 (0x03 = 读保持寄存器)
          └─────────────────────────── 从站地址 (1)
```

**步骤4：数据链路层帧封装（ModbusRtuFrameStrategy.BuildFrame）**

```csharp
// ModbusRtuFrameStrategy.cs
public byte[] BuildFrame(byte[] data)
{
    // 计算CRC16校验
    ushort crc = CalculateCrc16(data, data.Length);
    
    // 组装帧：数据 + CRC低字节 + CRC高字节
    var frame = new byte[data.Length + 2];
    Array.Copy(data, 0, frame, 0, data.Length);
    frame[data.Length] = (byte)(crc & 0xFF);        // CRC低字节
    frame[data.Length + 1] = (byte)((crc >> 8) & 0xFF); // CRC高字节
    return frame;
}
```

**帧封装结果：**

```
原始数据: [01] [03] [00] [00] [00] [01]
          ↓
完整帧:   [01] [03] [00] [00] [00] [01] [84] [0A]
          │    │    │    │    │    │    │    │
          │    │    │    │    │    │    │    └── CRC高字节
          │    │    │    │    │    │    └─────── CRC低字节
          │    │    │    │    │    └──────────── 数量低字节 (1)
          │    │    │    │    └───────────────── 数量高字节 (0)
          │    │    │    └────────────────────── 地址低字节 (0)
          │    │    └─────────────────────────── 地址高字节 (0)
          │    └──────────────────────────────── 功能码 (0x03)
          └───────────────────────────────────── 从站地址 (1)
```

**步骤5：物理层发送**

```csharp
// SerialPortTransport.cs
public Task WriteAsync(byte[] data, int offset, int count, CancellationToken ct = default)
{
    _serialPort.Write(data, offset, count);
    return Task.CompletedTask;
}
```

**步骤5：设备响应**

设备返回响应帧：

```
响应字节: [01] [03] [02] [00] [C8] [B9] [F6]
          │    │    │    │    │    │    │
          │    │    │    │    │    │    └── CRC高字节
          │    │    │    │    │    └─────── CRC低字节
          │    │    │    │    └──────────── 数据高字节 (0x00)
          │    │    │    └───────────────── 数据低字节 (0xC8 = 200)
          │    │    └────────────────────── 字节数 (2)
          │    └─────────────────────────── 功能码 (0x03)
          └──────────────────────────────── 从站地址 (1)
```

**步骤6：协议解码（ModbusRtuCodec.ExtractRegisters）**

```csharp
// ModbusRtuCodec.cs
public ushort[] ExtractRegisters(byte[] raw)
{
    if (raw[1] == 0x03) // 读保持寄存器响应
    {
        int byteCount = raw[2];  // 2
        int registerCount = byteCount / 2;  // 1
        var registers = new ushort[registerCount];
        
        for (int i = 0; i < registerCount; i++)
        {
            registers[i] = (ushort)((raw[3 + i * 2] << 8) | raw[4 + i * 2]);
            // registers[0] = (0x00 << 8) | 0xC8 = 200
        }
        
        return registers;
    }
    return Array.Empty<ushort>();
}
```

**步骤7：应用层获取结果**

```csharp
// UMC1200.cs
public async Task<short> GetTemperaturePVAsync(CancellationToken ct = default)
{
    ushort raw = await ReadRegisterAsync(UMC1200Registers.TemperaturePV, ct);
    // raw = 200
    return (short)raw;  // 返回 200 (表示 20.0℃)
}
```

## 5. Modbus RTU帧格式详解

### 5.1 请求帧格式

```
┌──────────────┬──────────────┬──────────────────┬──────────────────┬──────────────┐
│  从站地址    │   功能码     │   起始地址       │   寄存器数量     │   CRC校验    │
│  (1字节)     │  (1字节)     │   (2字节)        │   (2字节)        │   (2字节)    │
└──────────────┴──────────────┴──────────────────┴──────────────────┴──────────────┘
```

**示例：读取地址0开始的1个寄存器**

```
[01] [03] [00 00] [00 01] [84 0A]
 │    │     │       │       │
 │    │     │       │       └── CRC16校验
 │    │     │       └────────── 读取1个寄存器
 │    │     └────────────────── 起始地址0
 │    └──────────────────────── 功能码03（读保持寄存器）
 └───────────────────────────── 从站地址1
```

### 5.2 响应帧格式

```
┌──────────────┬──────────────┬──────────────┬──────────────────┬──────────────┐
│  从站地址    │   功能码     │   字节数     │   数据           │   CRC校验    │
│  (1字节)     │  (1字节)     │   (1字节)    │   (N字节)        │   (2字节)    │
└──────────────┴──────────────┴──────────────┴──────────────────┴──────────────┘
```

**示例：返回温度值200（20.0℃）**

```
[01] [03] [02] [00 C8] [B9 F6]
 │    │    │      │       │
 │    │    │      │       └── CRC16校验
 │    │    │      └────────── 数据：0x00C8 = 200
 │    │    └───────────────── 字节数：2
 │    └────────────────────── 功能码03
 └─────────────────────────── 从站地址1
```

### 5.3 错误响应帧格式

```
┌──────────────┬──────────────┬──────────────┬──────────────┐
│  从站地址    │   功能码     │   错误码     │   CRC校验    │
│  (1字节)     │  (1字节)     │   (1字节)    │   (2字节)    │
└──────────────┴──────────────┴──────────────┴──────────────┘
```

**错误码含义：**

| 错误码 | 含义 |
|--------|------|
| 0x01 | 非法功能码 |
| 0x02 | 非法数据地址 |
| 0x03 | 非法数据值 |
| 0x04 | 从站设备故障 |
| 0x05 | 确认 |
| 0x06 | 从站设备忙 |
| 0x08 | 存储奇偶性差错 |
| 0x0A | 不可用网关路径 |
| 0x0B | 网关目标设备响应失败 |

## 6. 写入操作示例

### 6.1 写入单个寄存器（功能码0x06）

**设置温度定值SV为250（25.0℃）：**

```csharp
await umc1200.SetTemperatureSVAsync(250);
```

**请求帧：**

```
[01] [06] [00 2B] [00 FA] [F9 C6]
 │    │     │       │       │
 │    │     │       │       └── CRC16校验
 │    │     │       └────────── 写入值：0x00FA = 250
 │    │     └────────────────── 寄存器地址：0x002B = 43
 │    └──────────────────────── 功能码06（写单个寄存器）
 └───────────────────────────── 从站地址1
```

**响应帧（回显）：**

```
[01] [06] [00 2B] [00 FA] [F9 C6]
```

### 6.2 写入多个寄存器（功能码0x10）

**同时设置温度和湿度定值：**

```csharp
await umc1200.WriteRegistersAsync(43, new ushort[] { 250, 600 });
```

**请求帧：**

```
[01] [10] [00 2B] [00 02] [04] [00 FA] [02 58] [XX XX]
 │    │     │       │      │      │       │       │
 │    │     │       │      │      │       │       └── CRC16校验
 │    │     │       │      │      │       └────────── 湿度值：0x0258 = 600
 │    │     │       │      │      └────────────────── 温度值：0x00FA = 250
 │    │     │       │      └───────────────────────── 字节数：4
 │    │     │       └──────────────────────────────── 写入2个寄存器
 │    │     └──────────────────────────────────────── 起始地址：0x002B = 43
 │    └────────────────────────────────────────────── 功能码10（写多个寄存器）
 └─────────────────────────────────────────────────── 从站地址1
```

## 7. CRC16校验计算

### 7.1 算法说明

Modbus RTU使用CRC16校验，多项式为0xA001。

### 7.2 计算示例

**计算帧 `[01] [03] [00] [00] [00] [01]` 的CRC：**

```csharp
private static ushort CalculateCrc16(byte[] data, int length)
{
    ushort crc = 0xFFFF;
    
    for (int i = 0; i < length; i++)
    {
        crc ^= data[i];
        
        for (int j = 0; j < 8; j++)
        {
            if ((crc & 0x0001) != 0)
            {
                crc >>= 1;
                crc ^= 0xA001;
            }
            else
            {
                crc >>= 1;
            }
        }
    }
    
    return crc;
}
```

**计算过程：**

```
初始值: 0xFFFF
异或0x01: 0xFFFE
处理位: ...
异或0x03: ...
...
最终结果: 0x0A84 (低字节0x84, 高字节0x0A)
```

## 8. DeviceLink分层架构中的数据流

```
┌─────────────────────────────────────────────────────────────────┐
│ 应用层: UMC1200.GetTemperaturePVAsync()                         │
│   → Command.Read("3.0.1")                                       │
├─────────────────────────────────────────────────────────────────┤
│ 协议层: ModbusRtuCodec.Encode(command)                          │
│   → [01][03][00][00][00][01] (原始数据，不含CRC)                 │
├─────────────────────────────────────────────────────────────────┤
│ 会话层: DirectSession.SendAndReceiveAsync(data)                 │
│   → 超时重试、线程安全                                           │
├─────────────────────────────────────────────────────────────────┤
│ 数据链路层: ModbusRtuFrameStrategy.BuildFrame(data)             │
│   → [01][03][00][00][00][01][84][0A] (添加CRC16校验)             │
│   → 帧边界检测、CRC校验                                         │
├─────────────────────────────────────────────────────────────────┤
│ 物理层: SerialPortTransport.WriteAsync(data)                    │
│   → RS-485串口发送                                              │
├─────────────────────────────────────────────────────────────────┤
│ 设备: UMC1200温湿度控制器                                        │
│   → 处理请求，返回响应                                           │
└─────────────────────────────────────────────────────────────────┘
```

## 9. 寄存器地址映射表

| 地址 | 属性 | 描述 | 数据类型 | 单位 |
|------|------|------|----------|------|
| 0 | ReadOnly | 温度PV值 | short | 0.1℃ |
| 1 | ReadOnly | 湿度PV值 | short | 0.1%RH |
| 2 | ReadOnly | 温度MV值 | short | - |
| 3 | ReadOnly | 湿度MV值 | short | - |
| 6 | ReadOnly | 当前工艺步数 | ushort | - |
| 7 | ReadOnly | 当前工艺步号 | ushort | - |
| 16 | ReadOnly | 当前工艺已循环数 | ushort | - |
| 17 | ReadOnly | 当前工艺总循环数 | ushort | - |
| 18 | ReadOnly | 当前工艺总时间（小时） | ushort | 小时 |
| 19 | ReadOnly | 当前工艺总时间（分钟） | ushort | 分钟 |
| 20 | ReadOnly | 当前工艺已运行时间（小时） | ushort | 小时 |
| 21 | ReadOnly | 当前工艺已运行时间（分钟） | ushort | 分钟 |
| 22 | ReadOnly | 当前步总时间（小时） | ushort | 小时 |
| 23 | ReadOnly | 当前步总时间（分钟） | ushort | 分钟 |
| 24 | ReadOnly | 当前步已运行时间（小时） | ushort | 小时 |
| 25 | ReadOnly | 当前步已运行时间（分钟） | ushort | 分钟 |
| 26 | ReadOnly | 当前连接总时间（小时） | ushort | 小时 |
| 27 | ReadOnly | 当前连接总时间（分钟） | ushort | 分钟 |
| 28 | ReadOnly | 当前连接已运行时间（小时） | ushort | 小时 |
| 29 | ReadOnly | 当前连接已运行时间（分钟） | ushort | 分钟 |
| 31 | ReadOnly | 系统时间（年） | ushort | 年 |
| 32 | ReadOnly | 系统时间（月） | ushort | 月 |
| 33 | ReadOnly | 系统时间（日） | ushort | 日 |
| 34 | ReadOnly | 系统时间（时） | ushort | 时 |
| 35 | ReadOnly | 系统时间（分） | ushort | 分 |
| 36 | ReadOnly | 系统时间（秒） | ushort | 秒 |
| 39 | ReadOnly | 定值已运行时间（小时） | ushort | 小时 |
| 40 | ReadOnly | 定值已运行时间（分钟） | ushort | 分钟 |
| 41 | ReadOnly | 当前PID区域 | ushort | - |
| 43 | ReadWrite | 温度定值SV | short | 0.1℃ |
| 44 | ReadWrite | 湿度定值SV | short | 0.1%RH |
| 45 | ReadWrite | 定值定时运行时间 | ushort | 分钟 |
| 47 | ReadWrite | 定值控制 | ushort | - |
| 48 | ReadWrite | 程序控制 | ushort | - |
| 51 | ReadOnly | 当前温度SV | short | 0.1℃ |
| 52 | ReadOnly | 当前湿度SV | short | 0.1%RH |
| 53 | ReadOnly | 当前湿球PV | short | 0.1℃ |
| 54 | ReadOnly | 当前湿球SV | short | 0.1℃ |
| 55 | ReadWrite | 定值温度斜率值 | ushort | - |
| 56 | ReadWrite | 定值湿度斜率值 | ushort | - |
| 57 | ReadOnly | 温度冷MV | short | - |
| 58 | ReadOnly | 湿度冷MV | short | - |

## 10. 使用示例

### 10.1 串口通信示例

```csharp
// 创建UMC1200实例
var umc1200 = new UMC1200("COM3", 9600, 8, StopBits.One, Parity.None, 1);

// 打开连接
await umc1200.OpenAsync();

// 读取测量值
short tempPV = await umc1200.GetTemperaturePVAsync();
short humiPV = await umc1200.GetHumidityPVAsync();

// 批量读取
var (temp, humi, tempMV, humiMV) = await umc1200.GetAllMeasurementsAsync();

// 设置温度定值
await umc1200.SetTemperatureSVAsync(250); // 25.0℃

// 启动定值控制
await umc1200.StartSetValueAsync();

// 获取工艺状态
var status = await umc1200.GetProcessStatusAsync();

// 关闭连接
await umc1200.CloseAsync();
```

### 10.2 MQTT通信示例

```csharp
// 通过MQTT连接UMC1200
var umc1200 = new UMC1200(
    "192.168.1.100",           // MQTT Broker地址
    1883,                      // MQTT Broker端口
    "device/umc1200/request",  // 请求主题
    "device/umc1200/response", // 响应主题
    1,                         // 从站地址
    5000                       // 超时时间
);

await umc1200.OpenAsync();
var temp = await umc1200.GetTemperaturePVAsync();
```

## 11. 故障排查

### 11.1 常见问题

| 问题 | 可能原因 | 解决方案 |
|------|----------|----------|
| 超时无响应 | 串口参数错误 | 检查波特率、数据位、停止位、校验位 |
| CRC校验失败 | 线路干扰或参数错误 | 检查接线，确认从站地址正确 |
| 非法数据地址 | 寄存器地址超出范围 | 检查寄存器地址表 |
| 非法功能码 | 设备不支持该功能码 | 确认设备支持的功能码 |

### 11.2 调试建议

1. 使用串口监视工具查看原始字节
2. 验证CRC计算是否正确
3. 确认从站地址与设备设置一致
4. 检查设备是否在线并正常工作

## 12. 总结

UMC1200 Modbus RTU通信流程：

1. **应用层**：调用设备方法，生成Command对象
2. **协议层**：将Command编码为原始数据（从站地址+功能码+数据，不含CRC）
3. **会话层**：管理请求-响应会话，处理超时重试
4. **数据链路层**：添加CRC16校验，帧边界检测
5. **物理层**：RS-485串口收发

### 职责划分原则

| 层级 | 职责 | 具体工作 |
|------|------|----------|
| **协议层** | 数据编解码 | 将业务命令编码为协议格式（不含校验） |
| **数据链路层** | 帧封装与校验 | 添加CRC校验，帧边界检测，错误检测 |
| **物理层** | 比特流传输 | 串口收发，电气信号 |

这种分层设计确保了：
- **职责单一**：每层只负责自己的职责
- **可替换性**：可以更换不同的帧策略（如DelimiterFrameStrategy、ModbusRtuFrameStrategy）
- **可测试性**：各层可以独立测试
