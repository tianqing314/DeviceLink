---
name: mqtt-integration
overview: 将参考项目 Xmas11 中的 MQTT 通信实现（基于 MQTTnet 3.1.1）适配并集成到 DeviceLink 项目的现有分层架构中，补全 MqttSession 骨架代码，新增 MqttSettings 配置类，并添加 Pipeline 预设。
todos:
  - id: add-mqttnet-package
    content: 在 DeviceLink.Session.csproj 中添加 MQTTnet NuGet 包引用
    status: completed
  - id: enhance-mqtt-options
    content: 增强 MqttSessionOptions，添加 Username/Password/UseTls/CleanSession/KeepAliveSeconds 属性
    status: completed
    dependencies:
      - add-mqttnet-package
  - id: implement-mqtt-session
    content: 补全 MqttSession 的 OpenAsync/CloseAsync，实现 MQTT 连接/断开逻辑
    status: completed
    dependencies:
      - enhance-mqtt-options
  - id: implement-send-receive
    content: 补全 MqttSession 的 SendAndReceiveAsync/SendOnlyAsync/ReceiveOnlyAsync，使用 TaskCompletionSource 实现请求-响应模式
    status: completed
    dependencies:
      - implement-mqtt-session
  - id: add-mqtt-settings
    content: 在 DeviceCommSettings.cs 中新增 MqttSettings 配置类
    status: completed
  - id: add-mqtt-preset
    content: 在 PipelinePresets 中新增 CreateMqttPipeline 预设方法
    status: completed
    dependencies:
      - add-mqtt-settings
  - id: add-mqtt-constructor
    content: 在 DPSEX 设备中添加基于 MqttSettings 的构造函数重载
    status: completed
    dependencies:
      - implement-send-receive
      - add-mqtt-settings
---

## 产品概述

将参考项目 Xmas11 中的 MQTT 通信实现按照 DeviceLink 现有分层架构集成，补全 MqttSession 的 TODO 骨架代码，使设备能够通过 MQTT Broker 进行通信。

## 核心功能

- 补全 MqttSession 的 OpenAsync/CloseAsync（MQTT 连接/断开逻辑）
- 补全 MqttSession 的 SendAndReceiveAsync（发布请求到 RequestTopic，等待 ResponseTopic 响应）
- 补全 MqttSession 的 SendOnlyAsync 和 ReceiveOnlyAsync
- 在 DeviceCommSettings 中新增 MqttSettings 配置类
- 在 PipelinePresets 中新增 CreateMqttPipeline 预设
- 在 DeviceLink.Session.csproj 中添加 MQTTnet NuGet 包引用

## 技术栈

- MQTT 库：MQTTnet 4.3.x（NuGet 包，兼容 netstandard2.0 和 net6.0）
- 目标框架：netstandard2.0;net6.0（与现有项目一致）
- 依赖：Microsoft.Extensions.Logging.Abstractions 6.0.0

## 技术架构

### 分层设计

MQTT 在 DeviceLink 中位于 **Session 层**，不经过 Transport/DataLink 层，因为 MQTT 本身就是应用层协议，内置了连接管理、消息路由和可靠性保证。

```
参考项目 Xmas11 的分层：
  BaseDevice → CommBuilder → iMqttClient → MQTTnet

DeviceLink 的分层（集成后）：
  DeviceBase → CommunicationPipeline → MqttSession → MQTTnet
```

### MqttSession 核心实现策略

- **连接管理**：使用 MQTTnet 的 `IMqttClient`，通过 `MqttClientOptionsBuilder` 配置连接参数
- **请求-响应模式**：发布到 RequestTopic，使用 `TaskCompletionSource<byte[]>` 等待 ResponseTopic 的响应消息（比参考项目的轮询方式更高效）
- **超时处理**：通过 `CancellationToken` + `Task.Delay` 实现请求超时
- **事件驱动**：使用 `ApplicationMessageReceivedAsync` 事件接收响应消息
- **线程安全**：使用 `SemaphoreSlim` 保护并发请求

### 关键文件修改

1. `core/DeviceLink.Session/DeviceLink.Session.csproj` — 添加 MQTTnet 包引用
2. `core/DeviceLink.Session/Implementations/MqttSession.cs` — 补全所有 TODO 方法
3. `core/DeviceLink.DeviceBase/Models/DeviceCommSettings.cs` — 新增 MqttSettings 类
4. `core/DeviceLink.Pipeline/CommunicationPipelineBuilder.cs` — 新增 CreateMqttPipeline 预设

### MqttSessionOptions 增强

在现有基础上增加：

- `Username` / `Password` — MQTT 认证凭据
- `UseTls` — 是否使用 TLS 加密
- `CleanSession` — 清理会话标志
- `KeepAliveSeconds` — 心跳间隔

## 实现细节

### MqttSession.SendAndReceiveAsync 核心逻辑

```
1. 创建 TaskCompletionSource<byte[]>
2. 注册一次性事件处理器匹配 ResponseTopic 消息
3. PublishAsync 到 RequestTopic
4. await tcs.Task with timeout CancellationToken
5. 超时则抛出 SessionTimeoutException
```

### MqttSession 消息接收机制

- 在 OpenAsync 中订阅 ResponseTopic
- 在 ApplicationMessageReceivedAsync 事件中，检查 topic 是否匹配 ResponseTopic
- 匹配则设置 TaskCompletionSource 的结果，唤醒等待的 SendAndReceiveAsync

### 错误处理

- 连接失败：抛出 SessionConnectionException
- 请求超时：抛出 SessionTimeoutException
- 发布失败：抛出 SessionException
- 断线重连：MQTTnet 内置自动重连机制

## Agent Extensions

无需使用扩展，本次任务为纯代码实现。