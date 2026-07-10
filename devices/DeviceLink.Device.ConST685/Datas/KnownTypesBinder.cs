#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DeviceLink.Device.ConST685;

/// <summary>
/// 自定义序列化绑定器，用于映射 TAU 模块类型到本地类型
/// 解决设备返回的 $type 引用 TAU.Module.Channels 类型但本地不存在的问题
/// </summary>
public class KnownTypesBinder : ISerializationBinder
{
    /// <summary>
    /// 类型映射字典：设备返回的类型名 -> 本地类型
    /// </summary>
    private static readonly Dictionary<string, Type> TypeMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        { "TAU.Module.Channels.DI.DIReading", typeof(ScanReading) },
        { "TAU.Module.Channels.DI.TimeTick", typeof(TimeTick) }
    };

    /// <summary>
    /// 反向映射：本地类型 -> 设备返回的类型名
    /// </summary>
    private static readonly Dictionary<Type, string> ReverseMappings = new()
    {
        { typeof(ScanReading), "TAU.Module.Channels.DI.DIReading" },
        { typeof(TimeTick), "TAU.Module.Channels.DI.TimeTick" }
    };

    /// <summary>
    /// 根据类型名绑定到本地类型
    /// </summary>
    public Type BindToType(string? assemblyName, string typeName)
    {
        // 处理泛型类型，如 System.Collections.Generic.List`1[[TAU.Module.Channels.DI.DIReading, TAU.Module.Channels]]
        if (typeName.StartsWith("System.Collections.Generic.List`1[["))
        {
            // 提取泛型参数中的类型名：TAU.Module.Channels.DI.DIReading
            var innerStart = typeName.IndexOf("[[") + 2;
            var innerEnd = typeName.IndexOf(",", innerStart);
            if (innerEnd < 0) innerEnd = typeName.IndexOf("]]", innerStart);
            var innerTypeName = typeName.Substring(innerStart, innerEnd - innerStart);

            if (TypeMappings.TryGetValue(innerTypeName, out var elementType))
            {
                return typeof(List<>).MakeGenericType(elementType);
            }
        }

        // 尝试从映射中查找本地类型
        if (TypeMappings.TryGetValue(typeName, out var mappedType))
        {
            return mappedType;
        }

        // 如果没有映射，尝试从程序集加载类型
        var type = Type.GetType(typeName);
        if (type != null)
        {
            return type;
        }

        // 如果仍然找不到，抛出异常
        throw new JsonSerializationException($"无法找到类型: {typeName}");
    }

    /// <summary>
    /// 将类型绑定到名称
    /// </summary>
    public void BindToName(Type type, out string? assemblyName, out string? typeName)
    {
        assemblyName = type.Assembly.FullName;

        // 尝试从反向映射中查找设备类型名
        if (ReverseMappings.TryGetValue(type, out var mappedTypeName))
        {
            typeName = mappedTypeName;
            return;
        }

        // 使用默认类型名
        typeName = type.FullName;
    }
}
