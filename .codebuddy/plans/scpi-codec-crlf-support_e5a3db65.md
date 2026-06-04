---
name: scpi-codec-crlf-support
overview: 修复 ScpiCodec 不支持多字符终止符（如 \r\n）的问题
todos:
  - id: update-scpi-codec
    content: 修改 ScpiCodec.cs：将 _terminator 改为 string 类型，添加新构造函数，更新 DecodeText 和 Encode 方法
    status: completed
  - id: update-const860
    content: 更新 ConST860.cs 中的 ScpiCodec 构造函数调用
    status: completed
    dependencies:
      - update-scpi-codec
  - id: update-dpcex-base
    content: 更新 DPCEXBase.cs 中的 ScpiCodec 构造函数调用
    status: completed
    dependencies:
      - update-scpi-codec
  - id: update-pipeline-presets
    content: 更新 CommunicationPipelineBuilder.cs 中的 CreateScpiPipeline 方法
    status: completed
    dependencies:
      - update-scpi-codec
  - id: add-scpi-tests
    content: 在 ProtocolTests.cs 中添加 \r\n 终止符的测试用例
    status: completed
    dependencies:
      - update-scpi-codec
---

## 需求描述

为 ScpiCodec 添加通用的字段提取方法，支持按分隔符提取字段，简化 ConST860 等设备的响应解析代码。

## 问题分析

1. **ScpiCodec 缺少字段提取方法**：只有 `ExtractNumeric`、`ExtractString`、`ExtractBoolean`，没有通用的字段提取功能
2. **ConST860 代码重复**：大量使用 `_codec.ExtractString(r).Split(',')` 模式，代码冗余
3. **SCPI 协议特点**：返回的是整体字符串，没有固定的字段索引概念，需要按分隔符分割

## 功能需求

- 为 ScpiCodec 添加 `ExtractField` 方法：按分隔符提取指定位置的字段
- 为 ScpiCodec 添加 `ExtractFields` 方法：按分隔符提取所有字段
- 更新 ConST860 中的代码，使用新方法简化响应解析
- 保持向后兼容性：保留现有的 `ExtractString`、`ExtractNumeric`、`ExtractBoolean` 方法

## 技术栈

- 语言：C#
- 框架：.NET 6.0

## 技术方案

### 1. 修改 ScpiCodec 类

**文件**：`core/DeviceLink.Protocol/Implementations/ScpiCodec.cs`

添加两个新方法：

```csharp
/// <summary>
/// 按分隔符分割响应，提取指定位置的字段
/// </summary>
/// <param name="raw">原始响应数据</param>
/// <param name="separator">分隔符（如 ','、':'、' '）</param>
/// <param name="index">字段索引（从0开始）</param>
/// <returns>字段值，如果索引越界返回空字符串</returns>
public string ExtractField(byte[] raw, char separator, int index)
{
    var text = DecodeText(raw);
    var parts = text.Split(separator);
    return index >= 0 && index < parts.Length ? parts[index].Trim() : string.Empty;
}

/// <summary>
/// 按分隔符分割响应，返回所有字段
/// </summary>
/// <param name="raw">原始响应数据</param>
/// <param name="separator">分隔符（如 ','、':'、' '）</param>
/// <returns>字段数组</returns>
public string[] ExtractFields(byte[] raw, char separator)
{
    var text = DecodeText(raw);
    var parts = text.Split(separator);
    for (int i = 0; i < parts.Length; i++)
        parts[i] = parts[i].Trim();
    return parts;
}
```

### 2. 更新 ConST860 设备代码

**文件**：`devices/DeviceLink.Device.ConST860/ConST860.cs`

更新以下方法使用新的 `ExtractField` 方法：

- `ParsePV` 方法：改为接受 `byte[]` 参数，使用 `ExtractField` 提取值和单位
- `GetModuleInfoAsync`：使用 `ExtractField` 提取各字段
- `GetModuleFilterAsync`：使用 `ExtractField` 提取过滤器参数
- `GetTargetPressureRangeAsync`：使用 `ExtractField` 提取范围参数
- `GetPressureLimitAsync`：使用 `ExtractField` 提取限制参数
- `GetPressureTypeAsync`：使用 `ExtractField` 提取类型信息
- `GetControlInfoAsync`：使用 `ExtractField` 提取控制信息
- `GetSlewRateAsync`：使用 `ExtractField` 提取斜率信息
- `GetStabilityAsync`：使用 `ExtractField` 提取稳定性信息
- `GetHeightCorrectionAsync`：使用 `ExtractField` 提取高度修正信息
- `GetTareAsync`：使用 `ExtractField` 提取去皮信息
- `GetExtendedInterfaceStateAsync`：使用 `ExtractField` 提取接口状态
- `GetFixedAtmosphericPressureAsync`：使用 `ExtractField` 提取大气压信息
- `GetRs232InfoAsync`：使用 `ExtractField` 提取 RS232 配置信息

对于使用两种分隔符的方法（如 `GetSwitchValueAsync`、`GetExtendedInterfaceModeAsync`），先使用 `ExtractFields` 按 '&' 分割，再对每个部分使用 `Split(',')` 分割。

## 实现要点

1. **向后兼容**：保留现有的 `ExtractString` 方法，新方法作为补充
2. **空值处理**：`ExtractField` 在索引越界时返回空字符串
3. **自动 Trim**：`ExtractFields` 返回的字段会自动去除首尾空格
4. **通用性**：支持任意分隔符，不限于逗号

## 代码对比

### 更新前（ConST860）：
```csharp
private PressureValue ParsePV(string text)
{
    var p = text.Split(',');
    return p.Length >= 2 && double.TryParse(p[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var v)
        ? new PressureValue { Value = v, Unit = p[1].Trim() } : new PressureValue();
}

// 调用
ParsePV(_codec.ExtractString(r))
```

### 更新后（ConST860）：
```csharp
private PressureValue ParsePV(byte[] raw)
{
    var value = _codec.ExtractField(raw, ',', 0);
    var unit = _codec.ExtractField(raw, ',', 1);
    return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)
        ? new PressureValue { Value = v, Unit = unit } : new PressureValue();
}

// 调用
ParsePV(r)
```

## 测试验证

1. 编译项目：`dotnet build`
2. 运行测试：`dotnet test`
3. 验证 ConST860 的各个方法是否正常工作