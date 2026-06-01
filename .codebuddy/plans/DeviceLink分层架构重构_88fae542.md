---
name: DeviceLink分层架构重构
overview: 基于OSI七层模型思想，将DeviceLink重构为4-5层的清晰分层架构，支持Modbus RTU/TCP、SCPI、自定义协议，每层可插拔、可组合，并提供详细的使用说明文档。
todos:
  - id: create-transport-layer
    content: 创建物理传输层项目 DeviceLink.Transport，定义 IPhysicalTransport 接口
    status: completed
  - id: implement-transport-implementations
    content: 实现串口、TCP、UDP、USB、回环传输
    status: completed
    dependencies:
      - create-transport-layer
  - id: create-datalink-layer
    content: 创建数据链路层项目 DeviceLink.DataLink，定义 IDataLink 接口
    status: completed
    dependencies:
      - create-transport-layer
  - id: implement-frame-strategies
    content: 实现分隔符帧、定长帧、Modbus RTU 帧策略
    status: completed
    dependencies:
      - create-datalink-layer
  - id: create-session-layer
    content: 创建会话层项目 DeviceLink.Session，定义 ISession 接口
    status: completed
    dependencies:
      - create-datalink-layer
  - id: implement-session-implementations
    content: 实现直连会话和 MQTT 会话
    status: completed
    dependencies:
      - create-session-layer
  - id: create-protocol-layer
    content: 创建协议层项目 DeviceLink.Protocol，定义 IProtocolCodec 接口
    status: completed
    dependencies:
      - create-session-layer
  - id: implement-protocol-codecs
    content: 实现 ConST、Modbus RTU/TCP、SCPI 协议编解码器
    status: completed
    dependencies:
      - create-protocol-layer
  - id: create-devices-layer
    content: 创建应用层项目 DeviceLink.Devices，定义 DeviceBase 基类
    status: completed
    dependencies:
      - create-protocol-layer
  - id: implement-dpsex-device
    content: 迁移 DPSEX 设备到新架构
    status: completed
    dependencies:
      - create-devices-layer
  - id: create-pipeline-builder
    content: 创建通信管道构建器，支持灵活的层组合
    status: completed
    dependencies:
      - implement-session-implementations
      - implement-protocol-codecs
  - id: create-testing-project
    content: 创建测试项目，迁移现有测试用例
    status: completed
    dependencies:
      - create-pipeline-builder
  - id: create-documentation
    content: 创建详细的使用说明文档，包含 API 文档和使用示例
    status: completed
    dependencies:
      - implement-dpsex-device
  - id: update-solution-file
    content: 更新解决方案文件，整合所有项目
    status: completed
    dependencies:
      - create-documentation
---

## 产品概述

基于OSI七层模型（简化为4-5层）完全重构DeviceLink设备通信框架，实现清晰的分层架构，支持Modbus RTU/TCP、SCPI、自定义协议等多种通信协议。

## 核心功能

1. **物理传输层**：统一的字节传输接口，支持串口、TCP、USB等物理介质
2. **数据链路层**：帧边界检测和帧组装/解析，支持分隔符帧、定长帧、Modbus RTU帧等
3. **会话层**：请求-响应会话管理，支持超时重试、线程安全
4. **协议层**：协议编解码器，支持ConST、Modbus RTU/TCP、SCPI、自定义协议
5. **应用层**：设备基类和具体设备实现，支持DPSEX、DPG等设备
6. **工厂/构建器**：通信管道构建器，支持灵活的层组合
7. **详细使用文档**：完整的API文档和使用示例

## 设计要求

- 每层接口保持简单，只暴露核心功能
- 所有IO操作支持async/await异步
- 支持CancellationToken取消令牌
- 分层错误处理：底层捕获底层异常，转换为上层能理解的异常类型
- 每层都是可插拔的，根据通信场景选择需要的层
- 支持依赖注入

## 技术栈

- **目标框架**：netstandard2.0 + net6.0（双目标）
- **语言版本**：C# 10
- **日志框架**：Microsoft.Extensions.Logging
- **依赖注入**：Microsoft.Extensions.DependencyInjection
- **测试框架**：xUnit
- **串口通信**：System.IO.Ports
- **网络通信**：System.Net.Sockets

## 技术架构

### 分层架构设计

```mermaid
graph TB
    subgraph "Layer 5: 应用层 (DeviceLink.Devices)"
        DeviceBase[DeviceBase 设备基类]
        DPSEX[DPSEX 压力传感器]
        DPG[DPG 压力发生器]
        CustomDevice[自定义设备]
    end
    
    subgraph "Layer 4: 协议层 (DeviceLink.Protocol)"
        IProtocolCodec[IProtocolCodec 接口]
        ConSTCodec[ConST 协议]
        ModbusRtuCodec[Modbus RTU 协议]
        ModbusTcpCodec[Modbus TCP 协议]
        ScpiCodec[SCPI 协议]
        CustomCodec[自定义协议]
    end
    
    subgraph "Layer 3: 会话层 (DeviceLink.Session)"
        ISession[ISession 接口]
        DirectSession[直连会话]
        MqttSession[MQTT 会话]
    end
    
    subgraph "Layer 2: 数据链路层 (DeviceLink.DataLink)"
        IDataLink[IDataLink 接口]
        DelimiterFrame[分隔符帧]
        FixedLengthFrame[定长帧]
        ModbusRtuFrame[Modbus RTU 帧]
        CustomFrame[自定义帧]
    end
    
    subgraph "Layer 1: 物理传输层 (DeviceLink.Transport)"
        IPhysicalTransport[IPhysicalTransport 接口]
        SerialPortTransport[串口传输]
        TcpTransport[TCP 传输]
        UdpTransport[UDP 传输]
        UsbTransport[USB 传输]
        LoopbackTransport[回环传输]
    end
    
    DeviceBase --> ISession
    DeviceBase --> IProtocolCodec
    ISession --> IDataLink
    ISession --> IPhysicalTransport
    IDataLink --> IPhysicalTransport
```

### 项目依赖关系

```mermaid
graph LR
    Transport[DeviceLink.Transport]
    DataLink[DeviceLink.DataLink]
    Session[DeviceLink.Session]
    Protocol[DeviceLink.Protocol]
    Devices[DeviceLink.Devices]
    Testing[DeviceLink.Testing]
    Docs[DeviceLink.Docs]
    
    DataLink --> Transport
    Session --> Transport
    Session --> DataLink
    Protocol --> Session
    Devices --> Protocol
    Devices --> Session
    Testing --> Transport
    Testing --> Session
    Docs --> Devices
```

## 实现细节

### 关键设计决策

1. **接口隔离原则**：每层定义独立的小接口，避免"上帝接口"
2. **依赖倒置**：上层依赖抽象接口，不依赖具体实现
3. **可插拔设计**：每层都是可选的，通过构建器模式灵活组合
4. **工厂模式**：提供通信管道构建器，简化复杂对象的创建

### 性能考虑

- 使用`SemaphoreSlim`保证线程安全
- 使用`ArrayPool<byte>`减少内存分配
- 支持异步IO操作，避免阻塞线程

### 错误处理策略

- **物理层**：捕获IOException、TimeoutException，转换为TransportException
- **数据链路层**：处理帧超时、帧格式错误，转换为FrameException
- **会话层**：处理连接断开、超时，转换为SessionException
- **协议层**：处理命令格式错误、响应解析错误，转换为ProtocolException
- **应用层**：处理业务错误（设备错误、参数错误）

## 代理扩展

### 子代理：code-explorer

- **用途**：在重构过程中探索现有代码，确保正确迁移所有功能
- **预期结果**：完整理解现有代码结构，避免遗漏关键功能