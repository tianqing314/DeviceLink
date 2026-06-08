---
name: fix-version-read-debug
overview: 在 ZqwlCodec.ExtractVersion 中添加调试日志，打印接收到的原始字节数据，以诊断版本号读取失败的原因。
todos:
  - id: add-console-output
    content: 修改 CommunicationLogger 添加控制台输出支持
    status: pending
  - id: add-version-debug-log
    content: 在 ZqwlCodec.ExtractVersion 中添加详细诊断日志
    status: pending
    dependencies:
      - add-console-output
  - id: verify-logging
    content: 构建并验证日志输出功能
    status: pending
    dependencies:
      - add-version-debug-log
---

## 用户需求

用户报告无法读取 ZQWL 继电器版本号，怀疑板子内部未写入版本。经代码分析发现 `ExtractVersion` 存在 bug：长度检查 `raw.Length > 12` 与帧解析器输出的 10 字节 frameData 不匹配，导致永远返回空字符串。用户希望先添加调试日志查看设备实际返回的原始数据，以确定版本字符串在数据区的真实偏移位置。

## 核心功能

- 在版本读取流程的关键节点添加详细的调试日志
- 记录帧解析后的原始响应数据（逐字节）
- 支持控制台实时输出，方便调试
- 保持现有文件日志功能不变

## 技术栈

- 语言：C# (.NET 6.0)
- 日志框架：自定义 `CommunicationLogger`（已有）
- 测试框架：xUnit + Moq

## 实现方案

### 数据流分析

```
设备响应 15 字节帧
    ↓
ZqwlFrameStrategy.TryParseFrame → 提取 10 字节 frameData [addr][func][data×8]
    ↓
DeviceBase.SendAsync → CommunicationLogger.LogReceive（已记录到文件）
    ↓
SendForResultAsync<T>(command, decoder) → 调用 decoder(response)
    ↓
ZqwlCodec.ExtractVersion(raw) → 检查 raw.Length > 12（BUG：永远 false）→ 返回 ""
```

### 修改策略

1. **CommunicationLogger 增加控制台输出**：添加 `ConsoleOutput` 开关，启用时同时输出到控制台
2. **ExtractVersion 添加详细日志**：记录输入数据长度、逐字节内容、提取结果
3. **保留现有文件日志**：不破坏已有日志机制

### 关键修改点

- `CommunicationLogger.cs`：添加 `ConsoleOutput` 属性和控制台输出逻辑
- `ZqwlCodec.cs`：在 `ExtractVersion` 中添加诊断日志，调用 `CommunicationLogger.LogInfo`