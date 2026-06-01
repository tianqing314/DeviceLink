---
name: DeviceLink项目优化方案
overview: 基于对 DeviceLink 项目的全面分析，识别出 7 类优化点：Bug修复、性能优化、资源管理改进、代码去重、项目工程化、测试补充、API设计改进。
todos:
  - id: fix-bug-const-codec
    content: 修复 ConSTCodec.IsErrorResponse 越界访问 BUG
    status: pending
  - id: fix-bug-tcp-transport
    content: 修复 TcpTransport 三个 BUG（取消令牌、同步阻塞、条件判断）
    status: pending
  - id: fix-performance-channel
    content: 优化 DirectChannel.ReceiveLoopAsync 缓冲区分配与拷贝
    status: pending
  - id: fix-thread-safety-policy
    content: 修复 ChannelPolicy.Default 全局可变线程安全问题
    status: pending
  - id: fix-dispose-pattern
    content: 修复 Transport Dispose fire-and-forget 异步问题
    status: pending
    dependencies:
      - fix-bug-tcp-transport
  - id: refactor-dpsex-logic
    content: 提取 DPSEX 重复的数值解析逻辑为共享方法
    status: pending
  - id: add-project-infra
    content: 添加 .gitignore、README.md 和 Directory.Build.props
    status: pending
  - id: add-unit-tests
    content: 为修复项添加回归测试并补充 Codec/Transport 单元测试
    status: pending
    dependencies:
      - fix-bug-const-codec
      - fix-bug-tcp-transport
      - fix-performance-channel
---

## 产品概述

DeviceLink 是一个 .NET 设备通讯框架，采用分层架构设计（Transport → Framing → Channel → Protocol → Device），为仪器设备提供统一的通讯抽象。项目从旧版 "Xmas11" 系统重构而来，目标是消除旧系统中严重的代码重复问题。

## 需求总结

用户要求对当前项目进行全面的代码审查和优化。经过深入代码探索，发现以下需要优化的问题：

### 核心优化点

1. **BUG 修复（P0）**：ConSTCodec.IsErrorResponse 越界访问 — 当错误响应只有 2 段时访问 `parts[2]` 会抛出 IndexOutOfRangeException
2. **BUG 修复（P0）**：TcpTransport.ConnectAsync 未传递取消令牌 — 超时机制完全失效
3. **BUG 修复（P1）**：TcpTransport.ClearReceiveBufferAsync 使用同步阻塞调用
4. **BUG 修复（P1）**：TcpTransport.WriteAsync 条件判断逻辑错误 — 运算符优先级问题
5. **性能优化（P2）**：DirectChannel.ReceiveLoopAsync 缓冲区重复分配与不必要的数组拷贝
6. **线程安全（P2）**：ChannelPolicy.Default 可被全局任意代码修改
7. **资源管理（P2）**：Transport 的 Dispose 方法 fire-and-forget 异步操作
8. **代码重复（P3）**：DPSEX 数值解析逻辑重复
9. **工程化缺失（P3）**：无 .gitignore、README.md、Directory.Build.props
10. **测试覆盖不足（P3）**：Codec、边界条件、Transport 实现等缺少单元测试

## 技术栈

- 语言：C# 10，Nullable enabled
- 目标框架：netstandard2.0 + net6.0 多目标
- 测试框架：xUnit 2.4.2
- 日志：Microsoft.Extensions.Logging.Abstractions 6.0.0
- 硬件接口：System.IO.Ports 6.0.0

## 实施方案

### 方案策略

按优先级分批修复：先修 P0/P1 的 4 个 BUG，再处理 P2 的性能与线程安全问题，最后处理 P3 的工程化和测试。每批修改独立可验证。

### 关键技术决策

**1. ConSTCodec.IsErrorResponse 修复**
当 `parts.Length >= 2 && parts[1] == "E"` 时，需要检查 `parts.Length >= 3` 才访问 `parts[2]`，否则返回通用错误信息。

**2. TcpTransport 修复集合**

- ConnectAsync：将 `_client.ConnectAsync` 改为传入 `linkedCts.Token`，使超时和取消令牌生效
- ClearReceiveBufferAsync：改为循环异步读取直到缓冲区清空
- WriteAsync：修复条件为 `(_stream == null || _client == null || !_client.Connected)`

**3. DirectChannel.ReceiveLoopAsync 性能优化**

- 将读取缓冲区提到循环外，避免每次迭代分配
- 使用 `ArraySegment<byte>` 或直接操作 `List<byte>` 减少 `ToArray()` 拷贝
- 帧匹配时使用 `Span<byte>` 避免中间数组创建

**4. ChannelPolicy.Default 线程安全**
将 `Default` 改为每次返回新实例的静态属性（或使用 `readonly` 字段但确保文档说明不可修改）。

**5. Transport Dispose 模式**
在 Dispose 中同步关闭底层资源（SerialPort.Close、TcpClient.Close 已是同步），确保不 fire-and-forget。

### 实现注意事项

- 所有修改必须保持向后兼容，不改变公共 API 签名
- 修复 BUG 时需同步添加对应的回归测试用例
- 性能优化需验证正确性不变（通过现有测试）
- Directory.Build.props 仅提取公共配置，不改变编译行为