---
name: zigbee-module-implementation
overview: 实现Zigbee模块支持，包括IZigbeeModule接口和XBee/CC2530/ZM32三个模块的实现
todos:
  - id: zigbee-interface
    content: 在 Interfaces 文件夹创建 IZigbeeModule 接口
    status: completed
  - id: zigbee-options
    content: 在 Models 文件夹创建 ZigbeeOptions 配置类
    status: completed
  - id: zigbee-xbee
    content: 实现 XBeeModule 类
    status: completed
    dependencies:
      - zigbee-interface
  - id: zigbee-cc2530
    content: 实现 CC2530Module 类
    status: completed
    dependencies:
      - zigbee-interface
  - id: zigbee-zm32
    content: 实现 ZM32Module 类
    status: completed
    dependencies:
      - zigbee-interface
  - id: zigbee-transport
    content: 实现 ZigbeeTransport 类
    status: completed
    dependencies:
      - zigbee-options
      - zigbee-xbee
      - zigbee-cc2530
      - zigbee-zm32
  - id: zigbee-docs
    content: 更新协议分层分析文档
    status: completed
    dependencies:
      - zigbee-transport
---

## 需求分析

用户希望在 DeviceLink 框架中增加对 Zigbee 通讯的支持。针对 Zigbee 模块配置指令和数据格式的差异，需要设计一个模块化的抽象层，以便支持 XBee、CC2530 和 ZM32 等不同厂商的模块。

## 核心功能

1. **模块抽象接口**：定义 Zigbee 模块的通用操作接口。
2. **具体模块实现**：分别为 XBee、CC2530、ZM32 实现具体的 AT 指令交互和数据帧处理逻辑。
3. **传输层封装**：创建 ZigbeeTransport 类，复用串口传输能力，并集成模块配置逻辑。
4. **配置管理**：提供 ZigbeeOptions 配置类，支持不同模块的特有参数。

## 技术栈

- 语言：C# 10
- 框架：.NET Standard 2.0 + .NET 6.0
- 架构模式：分层架构、工厂模式、策略模式

## 实现方案

1. **接口设计**：在 `DeviceLink.Transport.Interfaces` 命名空间下新增 `IZigbeeModule` 接口。
2. **模块实现**：在 `DeviceLink.Transport.Implementations` 命名空间下新增三个类 `XBeeModule`、`CC2530Module`、`ZM32Module`。
3. **传输层实现**：创建 `ZigbeeTransport` 类实现 `IPhysicalTransport` 接口，内部持有 `SerialPortTransport` 实例，通过 `IZigbeeModule` 处理具体的指令交互。
4. **配置模型**：定义 `ZigbeeOptions` 类继承或包含 `SerialPortOptions`，并增加 Zigbee 特有配置项（如 PanId、Channel 等）。

## 关键技术点

- **异步操作**：所有与硬件交互的方法均需使用 `async/await`。
- **日志记录**：使用 `Microsoft.Extensions.Logging` 记录关键操作和错误。
- **异常处理**：定义或复用 `TransportException` 处理通讯错误。
- **资源释放**：确保 `ZigbeeTransport` 正确释放底层串口资源。

# Agent Extensions

<!-- 不需要额外的Agent扩展，省略此部分 -->