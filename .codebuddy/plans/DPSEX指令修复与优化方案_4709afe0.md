---
name: DPSEX指令修复与优化方案
overview: 参考 CDP-V05-rs232_scan多点拟合.c 固件文档，修复 DPSEX 中所有错误的指令名称，并基于固件支持的完整指令集扩展 DPSEX 的功能方法，同时优化指令逻辑和修复相关 BUG。
todos:
  - id: fix-const-codec-bug
    content: 修复 ConSTCodec.IsErrorResponse 越界访问 BUG
    status: completed
  - id: fix-dpsex-commands
    content: 修复 DPSEX 3处错误指令（PRES→MRMD, PRZ→OZERO, SN→OTYPE）并移除冗余 GetMeasurementAsync
    status: completed
    dependencies:
      - fix-const-codec-bug
  - id: update-tests
    content: 更新测试用例匹配新指令名称，删除连接真实硬件的测试
    status: completed
    dependencies:
      - fix-dpsex-commands
---

## 产品概述

DPSEX 是 ConST 品牌智能数字压力模块的 .NET 驱动库，基于 DeviceLink 设备通讯框架实现。用户要求参考固件源码 `CDP-V05-rs232_scan多点拟合.c` 修复 DPSEX 中的错误指令，并优化整个指令逻辑。

## 核心问题

对比固件指令表（90条指令），发现 DPSEX.cs 中存在以下问题：

### 错误指令（3处）

1. **`GetPressureAsync`** 使用 `Command.Read("PRES")`，固件中无 "PRES" 指令。正确应为 `"MRMD"`（读测量数据，case 52）
2. **`PressureZeroAsync`** 使用 `Command.Write("PRZ")`，固件中无 "PRZ" 指令。正确应为 `"OZERO"`（清零，case 61）
3. **`GetSerialNumberAsync`** 使用 `Command.Read("SN")`，固件中无 "SN" 指令。正确应为 `"OTYPE"`（读仪器编号，case 50）

### 冗余方法

- `GetMeasurementAsync`（读 MRMD）与修复后的 `GetPressureAsync` 功能完全重复，应移除

### ConSTCodec BUG

- `IsErrorResponse` 第103行：当响应为 `address:E:errorcode\0`（仅3段）时，`parts.Length >= 4 ? parts[3] : parts[2]` 安全；但当响应为 `address:E\0`（仅2段）时，访问 `parts[2]` 会抛出 `IndexOutOfRangeException`

### 测试用例过时

- 测试中匹配的指令名（PRES、PRZ、VER、SN）均与修复后的指令不一致，需同步更新
- `GetSerialNumber` 测试直接连接真实串口 COM3，违反单元测试隔离原则

## 需求总结

1. 修复 3 处错误指令名称，使其与固件一致
2. 移除与 `GetPressureAsync` 重复的 `GetMeasurementAsync` 方法
3. 修复 ConSTCodec.IsErrorResponse 越界访问 BUG
4. 更新所有测试用例，确保与修复后的指令一致
5. 删除连接真实硬件的测试用例

## 技术栈

- 语言：C# 10，Nullable enabled
- 目标框架：netstandard2.0 + net6.0
- 测试框架：xUnit 2.4.2

## 实施方案

### 1. 修复 DPSEX.cs 错误指令

**`GetPressureAsync`**：`Command.Read("PRES")` → `Command.Read("MRMD")`

- 固件 case 52：`Send_String('F', Serialorder.data0, UNIT_CONV[Pressure.unit].str, "", "", "")`
- 响应格式：`address:F:MRMD:value:unit\0`，字段3=压力值，字段4=单位
- 当前解码器 `_codec.ExtractField(raw, 3)` 只取字段3（数值），正确
- 测试响应需从 `"1:F:PRES:1.23456\0"` 改为 `"1:F:MRMD:1.23456:kPa\0"`

**`PressureZeroAsync`**：`Command.Write("PRZ")` → `Command.Write("OZERO")`

- 固件 case 61：清零操作，返回 `Send_String('F',"OK","","","","")`
- 测试响应需从 `"1:F:PRZ:\0"` 改为 `"1:F:OZERO:\0"`

**`GetSerialNumberAsync`**：`Command.Read("SN")` → `Command.Read("OTYPE")`

- 固件 case 50：`Send_String('F', (char *)Instrument_code, "", "", "", "")`
- 响应格式：`address:F:OTYPE:code\0`
- 测试响应需从 `"1:F:SN:21803001\0"` 改为 `"1:F:OTYPE:21803001\0"`

### 2. 移除冗余方法

删除 `GetMeasurementAsync` 方法（第138-145行），因为修复后的 `GetPressureAsync` 已使用相同指令 `MRMD`。

### 3. 修复 ConSTCodec.IsErrorResponse

文件：`g:/DeviceLink/src/DeviceLink.Core/Protocol/ConSTCodec.cs` 第94-108行

当前代码：

```
errorMessage = parts.Length >= 4 ? parts[3] : parts[2];
```

修复为：

```
errorMessage = parts.Length >= 4 ? parts[3] 
             : parts.Length >= 3 ? parts[2] 
             : "未知错误";
```

### 4. 更新测试用例

**DPSEXTests.cs** 修改清单：

- `GetPressureAsync_ReturnsCorrectValue`：匹配 `R:MRMD`，响应改为 `"1:F:MRMD:1.23456:kPa\0"`
- `PressureZeroAsync_SendsCorrectCommand`：匹配 `W:OZERO`，响应改为 `"1:F:OZERO:\0"`
- `GetVersionAsync_ReturnsVersionString`：匹配 `R:OVER`，响应改为 `"1:F:OVER:DPS-EX-2.1.0\0"`（当前用 `VER` 也是错的，应为 `OVER`）
- `GetSerialNumberAsync_ReturnsSN`：匹配 `R:OTYPE`，响应改为 `"1:F:OTYPE:21803001\0"`
- `GetPressureAsync_DeviceError_ReturnsFailure`：匹配 `R:MRMD`
- `GetPressureAsync_Timeout_ReturnsFailure`：匹配 `R:MRMD`
- `DPSEX_WithLoopbackTransport_WorksCorrectly`：匹配 `R:MRMD` 和 `R:OTYPE`
- `DPSEX_CanSwitchBetweenTransportModes`：匹配 `R:MRMD`
- 删除 `GetSerialNumber` 测试（连接真实 COM3）

### 5. 实现注意事项

- 所有修改保持公共 API 签名不变（方法名、参数、返回类型）
- `GetPressureAsync` 的解码逻辑不变，仅改指令名
- `GetTemperatureAsync` 中的 `OTEMP` 指令已经是正确的，无需修改
- `GetVersionAsync` 的指令 `OVER` 已经正确
- `GetHardwareVersionAsync` 的指令 `OHVER` 已经正确
- `GetInstrumentCodeAsync` 的指令 `OTYPE` 已经正确