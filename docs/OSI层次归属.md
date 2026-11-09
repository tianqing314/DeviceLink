# DeviceLink OSI 层次归属

> 本文是 DeviceLink 层次归属的**唯一权威文档**。代码侧的对应实现在
> `core/DeviceLink.Osi/`（模型与校验）与 `core/DeviceLink.Pipeline/OsiComponentRegistrations.cs`（组件登记）。
> 文档与代码不一致时，以代码的装配校验结果为准，并立即修正本文。

## 1. 原则

1. **协议决定层，组件服从层。** 每个协议栈的层集合由 `OsiProfiles` 声明，组件的层归属必须落在该集合内。
2. **层归属必须显式声明。** 未登记的组件不允许进入通信栈，不存在"默认按物理层"这种蒙混方式。
3. **空缺层不等于缺失。** RS-232 没有会话层是协议事实，不是实现缺陷；但"画像要求实现却没人提供"才是错误。
4. **装配即校验。** 层次问题在**构建管道时**暴露：`CommunicationPipelineBuilder.Build()` 内部会跑一次
   `OsiAssembler`，报告挂在 `CommunicationPipeline.OsiReport` 上；需要硬失败时加 `.StrictOsi()`。
   （此前校验只存在于无人调用的 `OsiStackBuilder` 里，生产路径一次都没跑过 —— 已修复。）
5. **可扩展层不等于协议层。** 画像的 `extendable` 表示"协议本身没有这一层，但框架允许补一个实现"。
   最典型的是 L5：Modbus TCP / SCPI / Modbus RTU 都没有会话层，但框架仍会挂 `DirectSession`
   统一连接生命周期与安全重连。这类补位**不产生越界告警**，也**不会**让该层从"空缺"变成"实现"。

## 2. 协议 × 层次归属矩阵

图例：`●` 框架实现　`○` 委托外部（OS/硬件/模块）　`◐` 内联于相邻层　`·` 协议天然空缺

| 协议栈 | L1 | L2 | L3 | L4 | L5 | L6 | L7 | 空缺层 |
|---|---|---|---|---|---|---|---|---|
| RS-232 串口直连 | ● | ● | · | · | · | ◐ | ● | L3-L5 |
| RS-485 多点总线 | ● | ● | · | · | · | ◐ | ● | L3-L5 |
| Modbus RTU | ● | ● | · | · | · | ◐ | ● | L3-L5 |
| SCPI over RS-232 | ● | ● | · | · | · | ◐ | ● | L3-L5 |
| SCPI over TCP | ● | ● | ● | ● | · | ◐ | ● | L5 |
| Modbus TCP | ● | ● | ● | ● | · | ◐ | ● | L5 |
| MQTT over TCP | ● | ● | ● | ● | ● | ◐ | ● | 无 |
| Modbus RTU over TCP | ● | ● | ● | ● | · | ◐ | ● | L5 |
| Zigbee 透传（ZM32） | ● | ○ | ○ | · | · | ◐ | ● | L4-L5 |
| USB HID | ● | ● | · | · | · | ◐ | ● | L3-L5 |
| Bluetooth SPP | ● | ● | ● | ● | · | ◐ | ● | L5 |

### 关键结论

- **所有协议都空缺或被内联 L6 表示层。** 字节序、类型解释散落在各编解码器里，这就是工业协议必须人工维护寄存器表的根本原因。
- **只有 MQTT 有真正的 L5。** MQTT 有 `Clean Session`、`Client ID`、`Session Expiry Interval`、`Session Present` 与 QoS 1/2 的会话状态。
- **Modbus TCP 没有会话层。** MBAP 事务标识只做单次请求-响应**配对（correlation）**，不建立、不保持、不恢复会话状态，因此是 L7 报文头里的一个关联标签，不构成 L5。
- **RS-232 / RS-485 / Modbus RTU / SCPI 同样没有 L5**，靠主从轮询或串行请求-响应替代会话管理。
- **TCP 类协议跳过 L5 时会由 `DirectSession` 补一个"框架增强"的会话层**。L5 已在各画像的 `extendable` 中声明，
  因此**不再**产生 `OutOfProfile` 警告；但 L5 仍然留在 `Gaps` 里 —— "协议没有会话层"这个结论不因框架补位而改变。
- **串口类协议（L1+L2+L7）是"层数最少"的栈**，也最容易把上层职责误写进下层，因为中间的隔离带天然不存在。

## 3. 每层的职责与红线

| 层 | 必须做 | 绝对不做 |
|---|---|---|
| **L1 物理层** | 字节读写、连接建立、电气参数（波特率/DTR/RTS） | 帧定界、校验、重试、命令语义 |
| **L2 数据链路层** | 帧定界（分隔符/定长/长度前缀/CRC）、帧同步、校验 | 超时重试、请求-响应配对、功能码语义 |
| **L3 网络层** | 逻辑寻址、路由 | 端到端可靠性、业务语义 |
| **L4 传输层** | 端到端可靠、重传、流控、分片重组 | 帧定界（已由 L2 或下层完成）、业务语义 |
| **L5 会话层** | 连接生命周期、请求-响应配对、超时、重试、并发串行化 | 字节编解码、帧格式处理 |
| **L6 表示层** | 字节序、编码转换、类型解释、序列化 | 帧定界、连接管理 |
| **L7 应用层** | 业务命令、设备语义、错误码含义 | 帧格式、重试策略 |

### 三条最常被违反的红线

1. **L1 里组帧** —— `ZigbeeTransport.WriteAsync` 调 `BuildDataFrame` 属此列（待迁移，见 M2）。
2. **L2 里重试** —— ~~`DirectDataLink` 持有 `MaxRetryCount`~~（**已修复，见 M1**）。
3. **L1 里读响应并判断状态码** —— `ZigbeeTransport.ConfigureZM32TargetAddressAsync` 里判断 `response[3] == 0xD2` 属此列。

### 关于 L2 原样透传（`IRawFrameChannel`）

有些协议（如 CPPI V3 转接板报文）需要"把已经拼好的完整帧直接发出去"，不能被链路层再加 CRC。
这类需求过去由设备层直接调 `IPhysicalTransport` 实现，顺带把"读循环 + 超时 + 累积"整段抄进驱动。

现在统一走 `IRawFrameChannel`：

| 职责 | 归属 |
|---|---|
| 串行化、连接保证、字节读取、首次超时、空闲超时、累积上限 | **L2**（`DirectDataLink`） |
| 决定"什么算一帧"（`RawFrameOptions.TryParse`） | 设备（L7） |

设备层**不再允许**直接触碰 `IPhysicalTransport`。

## 4. 组件归属清单

> 与 `OsiComponentRegistrations.cs` 一一对应。新增组件必须在此登记，否则 `OsiStackBuilder` 会拒绝装配。

### L1 物理层

| 组件 | 层 | 依据 |
|---|---|---|
| `SerialPortTransport` | L1 | 只搬运字节与控制电气参数 |
| `LoopbackTransport` | L1 | 内存回环，仅测试 |
| `UsbTransport` | L1 | USB 报文分帧由驱动完成 |
| `TcpTransport` | L1-L4 | 封装 TCP/IP，由系统协议栈覆盖 L1-L4 |
| `UdpTransport` | L1-L4 | 封装 UDP |
| `BluetoothTransport` | L1-L4 | SPP/RFCOMM 已提供到传输层 |
| `ZigbeeTransport` | L1, L2, L7 | L1 串口；L7 为 AT 配置协议；L2 为模块报文封装（越界项 M2） |

### L2 数据链路层

| 组件 | 层 | 依据 |
|---|---|---|
| `DelimiterFrameStrategy` | L2 | 结束符定界 |
| `FixedLengthFrameStrategy` | L2 | 定长定界 |
| `ModbusRtuFrameStrategy` | L2 | 按功能码定长 + CRC16 校验 |
| `LengthPrefixedFrameStrategy` | L2 | 长度字段定界，先校验上限再分配 |
| `ZqwlFrameStrategy` | L2 | ZWQL 私有帧定界 |
| `CpplV3FrameStrategy` | L2 | CPPL V3 定界 + CRC8/CRC16 |
| `DirectDataLink` | L2 | 组帧发送与接收循环；另实现 `IRawFrameChannel` 提供"已拼好帧"的收发通道。**不再持有重试**（M1 已关闭） |

### L5 会话层

| 组件 | 层 | 依据 |
|---|---|---|
| `DirectSession` | L5 | 请求-响应配对、超时、异常映射 |
| `MqttSession` | L1-L5, L7 | MQTT 自带 TCP 与 session/keepalive/QoS |

### L6 + L7 编解码

| 组件 | 层 | 依据 |
|---|---|---|
| `ConSTCodec` | L6, L7 | 文本编码 + 地址/读写命令语法 |
| `ScpiCodec` | L6, L7 | ASCII 编码 + SCPI 命令树与错误码 |
| `ModbusRtuCodec` | L6, L7 | 寄存器大端解释 + 功能码 PDU |
| `ZqwlCodec` | L6, L7 | 私有载荷类型转换 + 命令语义 |

## 5. 使用方式

```csharp
using DeviceLink.Osi;
using DeviceLink.Pipeline;

// 装配即校验：缺层或越界会在 Build() 时抛出 OsiStackViolationException
using var pipeline = new OsiStackBuilder(OsiProfiles.ModbusRtu)
    .UseTransport(new SerialPortTransport("COM3", 9600))   // L1
    .UseDataLink(new ModbusRtuFrameStrategy())             // L2
    .UseProtocol(new ModbusRtuCodec(1))                    // L6 + L7
    .Build();
```

只想看层次是否正确，不构建管道：

```csharp
var report = new OsiStackBuilder(OsiProfiles.ModbusRtu)
    .UseTransport(new SerialPortTransport("COM3", 9600))
    .UseDataLink(new ModbusRtuFrameStrategy())
    .UseProtocol(new ModbusRtuCodec(1))
    .Inspect();

Console.WriteLine(report.ToTextTable());   // 直接写日志
```

输出示例：

```
OSI 协议栈校验: Modbus RTU over RS-485
  实现层 : L1,L2,L7
  委托层 : 无
  内联层 : L6
  空缺层 : L3-L5
  结果   : 通过 (4 个组件, 0 个错误, 0 个警告)
--------------------------------------------------------------
  L7 应用层        ModbusRtuCodec
  L6 表示层        (内联于协议编解码器，无独立组件)
  L5 会话层        (空缺 - 协议本身不提供该层)
  L4 传输层        (空缺 - 协议本身不提供该层)
  L3 网络层        (空缺 - 协议本身不提供该层)
  L2 数据链路层    ModbusRtuFrameStrategy + DirectDataLink
  L1 物理层        SerialPortTransport
```

## 6. 校验规则

| # | 规则 | 违规类型 | 严重度 |
|---|---|---|---|
| 1 | 组件提供的层必须在画像覆盖范围（`Covered` **+ `Extendable`**）内 | `OutOfProfile` | 警告 |
| 2 | 画像要求实现的每一层必须有提供者 | `MissingLayer` | **错误** |
| 3 | 同一层多个提供者（未被 `allowShared` 放开） | `LayerConflict` | 警告 |
| 4 | 组件依赖的层必须被其他组件提供 | `UnmetDependency` | **错误** |
| 5 | 协议栈必须落到 L7 应用层 | `NoApplicationLayer` | **错误** |

第 1 条取警告而非错误，是因为框架允许为无会话协议补充请求-响应管理（如 `DirectSession` 挂在 RS-232 栈上），这属于能力增强而非层次错误；但它必须可见，由评审判断是"增强"还是"写错层"。

## 7. 已知越界与迁移计划

| 编号 | 位置 | 现状 | 目标态 |
|---|---|---|---|
| **M1** ✅ | `DirectDataLink` | **已完成**：`MaxRetryCount`/`RetryDelayMs` 从 `DataLinkOptions` 删除，重试全部上移到 `DirectSession`；链路层改用 `RequestNotSentException` 表达"确定未送达" | — |
| **M2** | `ZigbeeTransport` | `WriteAsync` 内调用 `BuildDataFrame`，并在 `ConnectAsync` 里执行 AT 配置与响应判断 | AT 配置抽为 `IZigbeeConfigurator`（L7 初始化面）；组帧抽为 `ZigbeeFrameStrategy`（L2） |
| **M3** | `ModbusRtuCodec` | 残留未被调用的 `CalculateCrc16`（CRC 应只在 L2） | 删除；CRC 仅由 `ModbusRtuFrameStrategy` 负责 |
| **M4** ✅ | `docs/协议分类.md` + 代码 | **已完成**：`ProtocolBindings` 把 `ModbusTcpCodec` 绑定到 `LengthPrefixedFrameStrategy(LengthFieldOffset)`；仍未登记帧策略的协议会显式抛异常，不再静默退回分隔符帧 | — |

M2、M3 未完成前，`OsiComponentRegistrations` 的 `Reason` 字段会如实标注，不做粉饰。

### 已修复的相关缺陷（不在迁移表内，但同源）

| 缺陷 | 位置 | 说明 |
|---|---|---|
| 半帧被当成成功响应 | `DirectDataLink.ReceiveLoopAsync` | 空闲超时后若仍有未定界字节，过去直接返回给编解码器，绕过了 CRC/长度校验；现在抛 `FrameFormatException` |
| 帧策略由设备基类硬编码推导 | `DeviceBase.GetDefaultFrameStrategy` | 已移除，改由 `ProtocolBindings` 统一登记；SCPI 曾长期被套上 `\0` 定帧 |
| 日志工厂是空开关 | `CommunicationPipelineBuilder.UseLoggerFactory` | 现在真正传给 `DirectDataLink` / `DirectSession` |
| 传输层被 Dispose 两次 | `CommunicationPipeline.Dispose` | 明确所有权：Session → DataLink → Transport；仅当没有 DataLink 时 Pipeline 才直接释放 Transport |

## 8. 新增协议的四个步骤

1. **选画像**：在 `OsiProfiles` 里新增或复用。用 `implemented` / `delegated` / `inlined` 三个集合把"框架实现""外部实现""被折叠"分清。
2. **实现补层**：按画像缺哪层补哪层。缺 L2 就写 `IFrameStrategy`，缺 L5 就写 `ISession`，不要把职责塞进已有组件。
3. **登记归属**：在 `OsiComponentRegistrations` 里 `Register<T>(...)`，并在 `Reason` 写明依据。
4. **写测试**：至少覆盖"完整栈通过校验"和"缺层被报错"两个用例，参考 `tests/DeviceLink.Tests/OsiLayerModelTests.cs`。

## 9. Modbus TCP 装配（无会话层）

```csharp
using DeviceLink.Osi;
using DeviceLink.Pipeline;
using DeviceLink.Protocol;

var codec = new ModbusTcpCodec(unitId: 1);

using var pipeline = new OsiStackBuilder(OsiProfiles.ModbusTcp)
    .UseTransport(new TcpTransport(host, 502))                            // L1-L4
    .UseDataLink(new LengthPrefixedFrameStrategy(
        lengthFieldOffset: ModbusTcpCodec.LengthFieldOffset))              // L2：MBAP 长度字段
    .UseProtocol(codec)                                                    // L6+L7
    .Build();
```

两个必须注意的点：

- **不要传 `DirectSession`**。Modbus TCP 没有会话层，强行挂一个会在报告里产生 L5 的 `OutOfProfile` 警告。如果确实需要断线重连，见第 10 节——那是框架能力，不是协议的一层。
- **长度字段偏移必须是 4**。MBAP 结构是 `[TID 2][PID 2][长度 2][UID 1][PDU N]`，`ModbusTcpCodec.Encode` 只输出 `[TID][PID][UID][PDU]`，长度字段由 L2 帧策略插入。

## 10. 重试与重连的归属

核心原则：**重连安全，盲重试危险。**

| 场景 | 请求是否送达 | 允许重发 | 归属 |
|---|---|---|---|
| TCP 连接建立失败 | 确定未送达 | ✅ 可以 | L5 会话层 |
| 连接已断开（IOException） | 确定未送达 | ✅ 可以 | L5 会话层 |
| 写成功但读超时 | **不确定**（设备可能已执行） | ❌ 禁止 | 抛给应用层决策 |
| 收到异常响应 | 已送达并已拒绝 | ❌ 无意义 | 业务错误 |

### 开关（`SessionOptions`）

| 选项 | 默认 | 作用 |
|---|---|---|
| `MaxConnectRetries` | 0 | 连接失败/断开时的重连次数，0 表示不重连 |
| `ReconnectDelayMs` | 300 | 重连间隔 |
| `ReconnectOnFailure` | false | 请求失败后关闭连接，使下次调用重建 |

### 判据

判据不再是"事后看一眼 `IsOpen`"，而是**由下层用异常类型明确告知**：

```csharp
// DirectDataLink：连接没建立，或写之前连接已断开 → 字节确定没出去
throw new RequestNotSentException($"连接未建立，请求未发送: {ex.Message}", ex);

// DirectSession：只认这个类型（以及 Transport 层的 ConnectionException）
private static bool IsRequestNotDelivered(Exception ex)
    => ex is RequestNotSentException || ex is ConnectionException;

if (IsRequestNotDelivered(ex) && attempt < maxAttempts)
{
    await SafeCloseAsync();
    await DelayAsync(_options.ReconnectDelayMs, ct);
    continue;
}

// 已发出但结果未知 → 关闭连接，把决策权交给应用层
throw MapException(ex);
```

为什么不再用 `IsOpen`：那个信号会**漏判**。串口写失败、TCP 半关闭等场景下连接仍报 `IsOpen == true`，
请求其实没出去，却因为判据不成立而放弃了本可以安全执行的重连。
错误类型是在**事件发生的当下**产生的，不存在这种时序错配。

写调用中途失败**不**包装成 `RequestNotSentException` —— 那时可能已经有部分字节出去了，一律按"结果未知"处理。

### 为什么 Modbus 尤其不能盲重试

| 功能码 | 幂等性 | 重发风险 |
|---|---|---|
| `0x03` / `0x04` 读寄存器 | 幂等 | 无 |
| `0x06` 写单寄存器 | 幂等（同值覆盖） | 通常无害 |
| `0x10` 写多寄存器 | **非幂等** | 从站可能二次执行 |
| `0x05` 写单线圈 | **非幂等** | 可能把状态改回原值 |

只有当满足以下之一时才允许在超时后重发：

1. 设备寄存器表明确该操作幂等；
2. 设备实现了按事务标识去重。

第 2 条在 Modbus TCP 规范里**并未强制**——规范只要求服务器把请求的事务标识回抄到响应，没有要求识别重复请求。所以绝大多数从站不提供去重能力，只能依靠第 1 条。

### 心跳与连接探活

Modbus TCP 本身没有心跳。TCP keepalive 的默认时间是 2 小时，对产线没有意义。常用做法是应用层周期性读一个固定寄存器（如设备版本或状态寄存器）作为探活，失败计数达到阈值后触发重连。这属于应用策略，不属于协议层，建议放在设备类（L7）里实现。

