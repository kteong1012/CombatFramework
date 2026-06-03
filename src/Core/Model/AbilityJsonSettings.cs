using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CombatFramework.Core.Model;

/// <summary>
/// 框架统一 JSON 序列化配置：多态类型自动写 $type，使用短别名绑定器。
/// </summary>
public static class AbilityJsonSettings
{
    public static readonly JsonSerializerSettings Instance = new()
    {
        TypeNameHandling = TypeNameHandling.Auto,
        SerializationBinder = new ValueGetterAliasBinder(),
        NullValueHandling = NullValueHandling.Ignore,
        Converters = { new StringEnumConverter() },
    };
}
