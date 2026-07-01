# PS02 测试项目

本项目包含 PS02 压力变送器的单元测试和集成测试。

## 测试类型

### 1. 单元测试 (PS02Tests.cs)
- 使用回环测试环境，无需实际设备
- 模拟转换板行为，验证协议解析
- 可在 CI/CD 环境中运行

### 2. 串口集成测试 (PS02SerialTests.cs)
- 需要实际 PS02 设备连接
- 测试真实串口通信
- 标记为 `Category=Serial`，默认不会在 CI/CD 中运行

## 运行测试

### 运行所有单元测试
```bash
dotnet test --filter "Category!=Serial"
```

### 运行串口集成测试
```bash
# 设置环境变量
set PS02_SERIAL_PORT=COM3
set PS02_BAUD_RATE=9600
set PS02_SLAVE_ADDRESS=1

# 运行测试
dotnet test --filter "Category=Serial"
```

### 运行所有测试（包括串口测试）
```bash
dotnet test
```

## 环境变量配置

| 变量名 | 默认值 | 说明 |
|--------|--------|------|
| `PS02_SERIAL_PORT` | `COM3` | 串口号 |
| `PS02_BAUD_RATE` | `9600` | 波特率 |
| `PS02_SLAVE_ADDRESS` | `1` | Modbus 从站地址 |

## 测试用例说明

### 连接测试
- `Serial_ConnectAndDisconnect_ShouldSucceed` - 测试基本连接和断开
- `Serial_TestConnection_ShouldReturnTrue` - 测试设备响应

### 设备信息读取
- `Serial_GetSerialNumber_ShouldReturnNonEmpty` - 读取序列号
- `Serial_GetFirmwareVersion_ShouldReturnNonEmpty` - 读取固件版本
- `Serial_GetHardwareVersion_ShouldReturnNonEmpty` - 读取硬件版本
- `Serial_GetIdentification_ShouldReturnAllInfo` - 读取完整设备标识

### 压力读取
- `Serial_GetPressure_ShouldReturnValue` - 读取实时压力值 (F03)
- `Serial_GetPressureF40_ShouldReturnValue` - 读取实时压力值 (F40)

### 设备参数读取
- `Serial_GetPrecision_ShouldReturnValue` - 读取精度
- `Serial_GetPressureType_ShouldReturnValidType` - 读取压力类型
- `Serial_GetModuleType_ShouldReturnValue` - 读取模块类型
- `Serial_GetMigrationRange_ShouldReturnRange` - 读取迁移量程

### 寄存器读写
- `Serial_ReadRegister_ShouldReturnValue` - 读取单个寄存器
- `Serial_ReadRegisters_ShouldReturnMultipleValues` - 读取多个寄存器

### 错误处理和性能
- `Serial_WithCancellation_ShouldRespectToken` - 测试取消令牌
- `Serial_MultipleReads_ShouldBeConsistent` - 测试多次读取一致性

## 故障排除

### 串口不存在
1. 检查设备管理器中的串口号
2. 确认串口驱动已安装
3. 检查环境变量 `PS02_SERIAL_PORT` 是否正确

### 连接超时
1. 检查串口参数（波特率、数据位、停止位、校验位）
2. 确认设备已上电
3. 检查串口线缆连接

### 设备无响应
1. 确认设备的 Modbus 从站地址正确
2. 检查设备是否处于正常工作状态
3. 尝试使用其他 Modbus 工具测试设备

## 添加新测试

1. 在 `PS02SerialTests.cs` 中添加新的测试方法
2. 使用 `[Fact]` 和 `[Trait("Category", "Serial")]` 标记
3. 在测试开始时检查串口可用性：`if (!IsSerialPortAvailable()) return;`
4. 使用 `try-finally` 确保设备正确关闭

## 注意事项

1. **设备安全**：写入操作可能影响设备配置，请谨慎使用
2. **测试隔离**：每个测试应该独立，不依赖其他测试的执行结果
3. **资源清理**：实现 `IDisposable` 确保串口资源正确释放
4. **超时设置**：根据实际设备响应时间调整超时参数