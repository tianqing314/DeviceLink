---
name: 移除GetMigrationRangeAsync重试逻辑
overview: 移除 PS02Base.GetMigrationRangeAsync 中冗余的重试逻辑，因为策略层（DirectDataLink）已有重试+清空缓冲区机制。保留 EnableOwiViaConverterAsync 和 DisableOwiViaConverterAsync 中的 200ms 延迟。
todos:
  - id: remove-retry-logic
    content: 移除 GetMigrationRangeAsync 中的重试逻辑，恢复为单次调用 SendForResultAsync
    status: completed
---

## 问题分析

`GetMigrationRangeAsync` 方法中存在冗余的重试逻辑，因为：

1. **策略层已有重试机制**：`DirectDataLink.SendAndReceiveFrameAsync` 在每次重试时会清空接收缓冲区并重新发送
2. **应用层加重试违反职责分离原则**：重试应该由底层通信策略处理，而非业务层
3. **NaN 检查无法正确区分残留数据**：残留数据可能解析出非 NaN 的垃圾值

## 修复目标

移除 `GetMigrationRangeAsync` 中的 for 循环重试逻辑，恢复为简单的单次调用 `SendForResultAsync`。

## 保留的修改

`EnableOwiViaConverterAsync` 和 `DisableOwiViaConverterAsync` 中的 200ms 延迟保留，这是合理的：等待设备完成模式切换。

## 技术方案

### 修改文件

- `G:/PS02Item/src/libs/DeviceLink/devices/DeviceLink.Device.PS02/PS02Base.cs`

### 修改内容

**当前代码（第 478-525 行）**：

```
public async Task<PressureRange> GetMigrationRangeAsync(CancellationToken ct = default)
{
    // 尝试读取，如果返回空对象则重试一次（清除缓冲区后）
    for (int attempt = 0; attempt < 2; attempt++)
    {
        var result = await SendForResultAsync(...);
        
        // 如果读取成功（Lower和Upper都不是NaN），直接返回
        if (!double.IsNaN(result.Lower) || !double.IsNaN(result.Upper))
            return result;
        
        // 第一次读取失败，清除缓冲区后重试
        if (attempt == 0)
        {
            CommunicationLogger.LogInfo(Name, "读取迁移量程失败，清除缓冲区后重试");
            if (Pipeline.Transport != null)
                await Pipeline.Transport.ClearReceiveBufferAsync(ct);
            await Task.Delay(50, ct);
        }
    }
    
    // 两次尝试都失败，返回空对象
    return new PressureRange();
}
```

**目标代码**：

```
public async Task<PressureRange> GetMigrationRangeAsync(CancellationToken ct = default)
{
    return await SendForResultAsync(
        Command.Read("40.20798.4"),
        raw =>
        {
            // 移除转接板添加的 0x00 前缀（CPPI 错误码）
            var normalized = NormalizeF40Response(raw);

            // 最小长度：地址(1) + 功能码(1) + 字节数(1) + 数据(8) = 11字节
            if (normalized == null || normalized.Length < 11)
            {
                return new PressureRange();
            }

            // 偏移量：地址(0) + 功能码(1) + 字节数(2) = 数据从偏移3开始
            return new PressureRange
            {
                Lower = ParseFloat32LittleEndian(normalized, 3),
                Upper = ParseFloat32LittleEndian(normalized, 7)
            };
        },
        ct);
}
```

### 设计考量

1. **职责分离**：重试逻辑由 `DirectDataLink` 层处理，业务层只关注数据解析
2. **简化代码**：移除冗余的 for 循环和 NaN 检查
3. **保留延迟**：`EnableOwiViaConverterAsync` 和 `DisableOwiViaConverterAsync` 中的 200ms 延迟保留，解决模式切换时序问题