---
name: devicelink-command-wrapper
description: This skill should be used when developing DeviceLink device command libraries. It provides standardized workflows for creating SCPI-based device communication classes, including directory structure, data models, method implementation patterns, and testing conventions. Use this skill when building new device drivers, adding commands to existing devices, or refactoring device communication code.
---

# DeviceLink 指令封装规范

## 概述

本技能用于指导基于 DeviceLink 框架的工业设备通讯库开发。当需要为新设备创建指令封装库、或为现有设备添加新指令时，使用此技能确保代码风格统一、类型安全、便于维护。

## 快速开始

### 触发场景

- 创建新的设备类库（如 `DeviceLink.Device.XXX`）
- 为现有设备添加 SCPI 指令
- 重构设备通讯代码以符合规范
- 编写设备测试用例

### 核心工作流

1. **分析设备协议文档** → 提取指令列表和响应格式
2. **创建目录结构** → 按规范组织文件
3. **定义数据模型** → 为每个查询指令创建强类型模型
4. **实现设备主类** → 按 region 规范组织代码
5. **编写测试用例** → 使用真实构造器和强类型断言

## 目录结构规范

每个设备类库独立一个项目，文件结构如下：

```
DeviceLink.Device.XXX/
├── XXXBase.cs                        ← 设备主类（指令封装）
├── Datas/
│   ├── ModelA.cs                     ← 响应数据模型
│   ├── ModelB.cs
│   ├── SourceModule.cs               ← 枚举常量（可选）
│   └── VersionModules.cs             ← 枚举常量（可选）
└── DeviceLink.Device.XXX.csproj
```

**规则：**
- 所有数据模型放到 `Datas/` 文件夹，每个文件一个类
- 设备主类以 `Base` 结尾（如 `ConST171Base`）
- 枚举常量类也放入 `Datas/`，用于替代魔法字符串

## 数据模型设计

### 核心原则

1. **每个 SCPI 查询指令对应一个强类型模型** - 不要返回 `string`
2. **提供 `IsValid` 属性** - 用于快速判断数据是否有效
3. **使用 `double.NaN` 作为数值类型的无效默认值** - 而非 `0` 或 `-1`
4. **重写 `ToString()`** - 便于调试日志
5. **为属性提供 XML 注释** - 说明含义和单位

### 模型模板

```csharp
public class PressureValue
{
    /// <summary>压力值（单位：kPa）</summary>
    public double Value { get; set; } = double.NaN;
    
    /// <summary>压力单位</summary>
    public string Unit { get; set; } = string.Empty;
    
    /// <summary>是否有效</summary>
    public bool IsValid => !double.IsNaN(Value);
    
    /// <inheritdoc/>
    public override string ToString() => $"{Value} {Unit}";
}
```

### 枚举替代魔法字符串

对于文档中明确给出的固定参数值，定义枚举确保编译期类型安全：

```csharp
public enum SourceModule
{
    Pressure,   // 正压气源
    Vacuum,     // 真空气源
    Pre         // 前级泵
}

public static class SourceModuleExtensions
{
    public static string ToScpiString(this SourceModule module) => module switch
    {
        SourceModule.Pressure => "Pressure",
        SourceModule.Vacuum   => "Vacuum",
        SourceModule.Pre      => "Pre",
        _ => throw new ArgumentOutOfRangeException(nameof(module), module, null)
    };
}
```

## 设备主类实现

### Region 组织规范（顺序不可变）

```csharp
public class XXXBase : DeviceBase
{
    #region 属性字段
    // 只放 private 字段 / 只读属性
    #endregion

    #region 构造函数
    // 提供多种实用构造方式
    #endregion

    #region 通用指令
    // IEEE488.2 共同指令（*IDN? / *CLS / *RST 等）
    // 系统指令（SYSTem:* 等设备无关指令）
    #endregion

    #region 业务指令
    // 核心业务指令集中放在这里
    #endregion

    #region 私有指令
    // 校准指令 / 诊断指令 / 测试指令 / OTA 等内部指令
    // 所有 private 解析方法放在此 region 的最末尾
    #endregion
}
```

### 构造函数规范

提供以下实用构造器（按需选择）：

| 构造方式 | 适用场景 |
|----------|----------|
| `XXXBase(IPAddress ip, int port)` | TCP/IP 直连 |
| `XXXBase(string ip, int port)` | TCP/IP 直连（字符串 IP） |
| `XXXBase(DeviceCommSettings settings)` | USB / MQTT / 自定义通信配置 |
| `XXXBase(string portName, int baudRate, ...)` | 串口通信 |

### 方法实现规范

每个公开方法遵循 **一行表达式体** 风格：

```csharp
/// <summary>仪器标识查询 —— *IDN?（返回 厂家,型号,序列号,固件版本）</summary>
public Task<DeviceIdentification> GetIdentificationAsync(CancellationToken ct = default) =>
    SendForResultAsync(Command.Read("*IDN"), ParseIdentification, ct);

/// <summary>设置指定气源控制状态 —— PRESsure:CONTrol source,state</summary>
public Task SetPressureControlStateAsync(SourceModule module, bool running, CancellationToken ct = default) =>
    SendNonQueryAsync(Command.Write("PRESsure:CONTrol", module.ToScpiString(), running ? "1" : "0"), ct);
```

**XML 注释规范：**
- 第一行：中文功能描述 + `—— 指令原文`
- 第二行（可选）：返回值格式说明或示例

**三种发送方式：**

| 方法 | 用途 | 说明 |
|------|------|------|
| `SendForResultAsync<T>(Command.Read(...), parser, ct)` | 查询指令 | 有返回值，需指定解析函数 |
| `SendNonQueryAsync(Command.Write(...), ct)` | 设置指令 | 无返回值，单向发送 |
| `SendAsync(command, ct)` | 自定义处理 | 返回原始 `byte[]`，外部自行解析 |

### 私有解析方法

所有 `private` 解析方法集中放在「私有指令」region 最末尾：

```csharp
#region 私有指令

// ... 公共指令方法 ...

// ---- 私有解析方法 ------------------------------------------------------

private static double ParseDouble(string text) =>
    double.TryParse(text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : double.NaN;

private static int ParseInt(string text) =>
    int.TryParse(text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : -1;

private static bool IsOne(string text) => text.Trim() == "1";

private PressureValue ParsePressureValue(byte[] raw) { ... }

#endregion
```

## 指令编码规则

### SCPI 命令格式（ScpiCodec）

| 命令类型 | `Command.Read` | `Command.Write` |
|----------|---------------|-----------------|
| 格式 | `CMD? param1,param2` | `CMD param1,param2` |
| 示例 | `Command.Read("PRESsure", "Pressure")` → `PRESsure? Pressure` | `Command.Write("PRESsure:CONTrol", "Vacuum", "1")` → `PRESsure:CONTrol Vacuum,1` |

- **查询命令**：使用 `Command.Read(id, params)` → 自动追加 `?`
- **设置命令**：使用 `Command.Write(id, params)` → 不加 `?`
- 多个参数自动以 `,` 拼接，与 SCPI 标准一致。

### 协议 / 帧策略配置

| 协议类型 | Codec | 帧策略 | 分隔符 |
|----------|-------|--------|--------|
| SCPI | `ScpiCodec("\r\n")` | `DelimiterFrameStrategy` | `\r\n` |
| ConST | `ConSTCodec(address)` | `DelimiterFrameStrategy` | `\0` |
| Modbus RTU | `ModbusRtuCodec(address)` | `ModbusRtuFrameStrategy` | CRC16 |

## 测试文件规范

### 使用真实构造器

```csharp
private XXXDevice CreateDevice()
{
    var settings = new SerialPortSettings(TestPortName, TestBaudRate, TestDataBits, TestStopBits, TestParity)
    {
        ReceiveTimeoutMs = 15000,
        ReceiveIdleTimeoutMs = 100,
        MaxRetryCount = 2,
        RetryDelayMs = 500
    };
    return new XXXDevice(settings);
}
```

### 使用枚举常量传参

```csharp
// ✅ 正确
var result = await device.GetPressureAsync(SourceModule.Pressure);
var fanSpeed = await device.GetFanSpeedAsync(SourceModule.Pressure);

// ❌ 错误 — 不要使用魔法字符串
var result = await device.GetPressureAsync("Pressure");
```

### 断言强类型属性

```csharp
// ✅ 正确 — 访问强类型属性
var id = await device.GetIdentificationAsync();
Assert.NotEmpty(id.Manufacturer);
Assert.NotEmpty(id.Model);

var range = await device.GetPressureRangeAsync(SourceModule.Pressure);
Assert.True(range.IsValid);
Assert.True(range.Max >= range.Min);

// ❌ 错误 — 仅检查非空字符串
var id = await device.GetIdentificationAsync();
Assert.NotNull(id);
```

## 参考资源

详细的规范文档请参考 `references/devicelink-spec.md`，包含：
- 完整的代码示例
- 常见问题解答
- 最佳实践指南

## 工作流示例

### 场景：为新设备创建指令库

1. **分析协议文档**
   - 提取所有 SCPI 指令列表
   - 识别查询指令和设置指令
   - 记录响应格式和参数

2. **创建项目结构**
   ```
   DeviceLink.Device.NewDevice/
   ├── NewDeviceBase.cs
   ├── Datas/
   │   ├── DeviceIdentification.cs
   │   ├── PressureValue.cs
   │   └── SourceModule.cs
   └── DeviceLink.Device.NewDevice.csproj
   ```

3. **实现数据模型**
   - 为每个查询指令创建强类型模型
   - 添加 `IsValid` 属性和 `ToString()` 方法
   - 定义枚举替代魔法字符串

4. **实现设备主类**
   - 按 region 规范组织代码
   - 提供多种构造函数
   - 实现一行表达式体方法
   - 添加详细的 XML 注释

5. **编写测试用例**
   - 使用真实构造器
   - 断言强类型属性
   - 覆盖所有指令

### 场景：为现有设备添加新指令

1. **查看现有代码结构**
   - 确认设备主类和数据模型位置
   - 了解现有的编码风格

2. **添加数据模型**（如有新响应格式）
   - 在 `Datas/` 文件夹创建新模型
   - 遵循模型设计规范

3. **添加指令方法**
   - 在相应的 region 中添加方法
   - 使用一行表达式体风格
   - 添加 XML 注释

4. **添加解析方法**
   - 在「私有指令」region 末尾添加
   - 基础辅助方法声明为 `static`

5. **更新测试用例**
   - 添加新指令的测试
   - 使用枚举常量传参
   - 断言强类型属性
