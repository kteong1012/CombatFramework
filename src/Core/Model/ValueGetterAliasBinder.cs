using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CombatFramework.Core.Model;

/// <summary>
/// 反射驱动的 JSON 类型绑定器。
/// 启动时扫描所有已加载程序集，自动注册带有 <see cref="JsonAliasAttribute"/> 的类型。
/// 无别名的类型序列化时写短类名，反序列化时在所有程序集中按短名搜索。
/// </summary>
public sealed class ValueGetterAliasBinder : ISerializationBinder
{
    private static readonly Lazy<(Dictionary<string, Type> aliasToType, Dictionary<Type, string> typeToAlias)> _maps
        = new(BuildMaps, isThreadSafe: true);

    private static (Dictionary<string, Type>, Dictionary<Type, string>) BuildMaps()
    {
        var aliasToType = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        var typeToAlias = new Dictionary<Type, string>();

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try { types = asm.GetExportedTypes(); }
            catch { continue; }

            foreach (var type in types)
            {
                var attr = (JsonAliasAttribute)Attribute.GetCustomAttribute(type, typeof(JsonAliasAttribute));
                if (attr == null) continue;
                aliasToType[attr.Alias] = type;
                typeToAlias[type] = attr.Alias;
            }
        }

        return (aliasToType, typeToAlias);
    }

    /// <summary>
    /// 反序列化：有别名用别名；否则在所有已加载程序集里按短类名或全限定名搜索。
    /// </summary>
    public Type BindToType(string assemblyName, string typeName)
    {
        var (aliasToType, _) = _maps.Value;
        if (aliasToType.TryGetValue(typeName, out var aliased))
            return aliased;

        // 尝试完整限定名
        if (!string.IsNullOrEmpty(assemblyName))
        {
            var direct = Type.GetType($"{typeName}, {assemblyName}");
            if (direct != null) return direct;
        }

        // 回退：按短类名或全限定名遍历
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try { types = asm.GetExportedTypes(); }
            catch { continue; }

            var t = Array.Find(types, x => x.Name == typeName || x.FullName == typeName);
            if (t != null) return t;
        }

        throw new JsonSerializationException($"Cannot resolve type '{typeName}' (assembly: '{assemblyName}').");
    }

    /// <summary>
    /// 序列化：有别名写别名；否则写短类名。
    /// </summary>
    public void BindToName(Type serializedType, out string assemblyName, out string typeName)
    {
        assemblyName = null;
        var (_, typeToAlias) = _maps.Value;
        typeName = typeToAlias.TryGetValue(serializedType, out var alias) ? alias : serializedType.Name;
    }
}

