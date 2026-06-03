namespace CombatFramework.Core.Model;

/// <summary>
/// 标记一个类型在 JSON 序列化中使用的短别名。
/// ValueGetterAliasBinder 会在启动时反射扫描所有程序集，自动注册带有此特性的类型。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class JsonAliasAttribute : Attribute
{
    public string Alias { get; }

    public JsonAliasAttribute(string alias) => Alias = alias;
}
