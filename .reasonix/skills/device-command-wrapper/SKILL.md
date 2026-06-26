---
name: device-command-wrapper
description: 按 DeviceLink 框架规范封装设备通讯库指令：强类型模型、枚举参数、region 组织、表达式体方法、零硬件测试
---

# DeviceLink 通讯库指令封装技能

你是一个 DeviceLink 框架的通讯库封装专家。当用户要求封装一个新的设备通讯库时，严格按照以下规范执行。

## 步骤 1：阅读指令文档

先读取用户提供的通讯指令文档（SCPI / Modbus / 自定义协议），理解所有指令的功能、参数、返回值。

## 步骤 2：分析指令分类

将指令按以下类别分组：
- **通用指令**：IEEE488.2 共同指令（*IDN? / *CLS / *RST）、系统指令（SYSTem:*）
- **业务控制指令**：核心业务指令（如压力控制、温度控制等文档中的主要指令）
- **私有指令**：校准指令、诊断指令、测试指令、OTA 升级指令等内部指令
- **压力配置指令**（如有）：指令文档中标注为"内部"的补充指令

## 步骤 3：创建目录结构

```
DeviceLink.Device.XXX/
├── XXXBase.cs
├── Datas/
│   ├── ModelA.cs
│   ├── ModelB.cs
│   └── EnumConstants.cs （可选）
└── DeviceLink.Device.XXX.csproj
```

## 步骤 4：数据模型设计（Datas/ 文件夹）

对每个有结构化返回值的查询指令，创建强类型模型：

### 模型设计要求
- 每个属性有 XML 注释说明含义和单位
- 提供 `IsValid` 属性
- 重写 `ToString()` 
- 数值类型用 `double.NaN` 作为无效默认值
- 字符串类型用 `string.Empty` 作为默认值

### 枚举替代魔法字符串
对文档中明确列出固定可选值的参数（如 `Pressure`/`Vacuum`），定义为枚举：

```csharp
public enum SourceModule
{
    Pressure,
    Vacuum,
    Pre
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

对非互斥的固定值（如阀门编号、版本模块名），使用常量类。

## 步骤 5：设备主类 region 规范（XXXBase.cs）

严格按以下 **5 个 region** 顺序排列：

```csharp
public class XXXBase : DeviceBase
{
    #region 属性字段
    // 只放 private 字段
    private readonly ScpiCodec _codec;
    private static readonly byte[] Delimiter = ...;
    #endregion

    #region 构造函数
    // 提供：IPAddress构造、string IP构造、DeviceCommSettings构造、串口构造
    // 不提供 ISession 注入构造器
    #endregion

    #region 通用指令
    // IEEE488.2 + SYSTem 系统指令
    #endregion

    #region 业务指令（按文档实际命名）
    // 核心业务指令
    #endregion

    #region 私有指令
    // 校准/诊断/测试/OTA等
    // 所有 private 解析方法放在本 region 最末尾
    #endregion
}
```

### 构造函数规范
```csharp
public XXXBase(IPAddress ipAddress, int port) : base(ipAddress, port, new ScpiCodec("\r\n")) { _codec = (ScpiCodec)Codec; }
public XXXBase(string ipAddress, int port) : base(IPAddress.Parse(ipAddress), port, new ScpiCodec("\r\n")) { _codec = (ScpiCodec)Codec; }
public XXXBase(DeviceCommSettings settings) : base(settings, new ScpiCodec("\r\n")) { _codec = (ScpiCodec)Codec; }
public XXXBase(string portName, int baudRate = 9600, ...) : base(portName, baudRate, ..., new ScpiCodec("\r\n"), delimiter) { _codec = (ScpiCodec)Codec; }
protected override void ConstructDefaultInfo() { base.ConstructDefaultInfo(); Name = "XXX"; }
```

### 方法实现规范
- 一行表达式体风格
- XML 注释：`/// <summary>中文描述 —— 指令原文</summary>`
- 查询用 `SendForResultAsync<T>(Command.Read(...), parser, ct)`
- 设置用 `SendNonQueryAsync(Command.Write(...), ct)`

### 私有解析方法
- 全部放在「私有指令」region 末尾
- 基础辅助方法（ParseDouble/ParseInt/IsOne）在最前，声明 static
- 模型解析方法按使用顺序排列

## 步骤 6：指令编码

| 命令类型 | Command.Read | Command.Write |
|----------|---------------|---------------|
| 格式 | `CMD? param1,param2` | `CMD param1,param2` |

- 查询用 `Command.Read` → 自动加 `?`
- 设置用 `Command.Write` → 不加 `?`
- 枚举参数用 `.ToScpiString()` 转换

## 步骤 7：测试规范

创建完整的集成测试 `XXXComprehensiveTests.cs`：
- 使用真实构造器创建设备
- 查询指令：断言强类型属性
- 安全设置指令：Set → Get 读回验证 → 恢复原值
- 危险指令（重启/校准/OTA）：用 `[Fact(Skip = "⚠ ...")]` 保护
- 使用枚举常量传参，不用魔法字符串

## 步骤 8：构建验证

```powershell
dotnet build devices\DeviceLink.Device.XXX\DeviceLink.Device.XXX.csproj
dotnet build tests\DeviceLink.Tests.XXX\DeviceLink.Tests.XXX.csproj
```

确保 0 错误、0 警告（CS1573 doc warning 除外）。

完成后用以下格式汇报：

```
## 封装完成摘要

### 目录结构
...

### Models（X 个）
...

### 指令分组
| Region | 指令 | 方法数 |
|--------|------|--------|
...

### 测试覆盖
- 查询验证：X 条
- Set/Get Sweep：X 条
- 危险操作(Skip)：X 条
```
