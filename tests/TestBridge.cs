using System.Reflection;
using CombatFramework.Bridge;

namespace CombatFramework.Tests;

/// <summary>
/// 测试用 Bridge 实现：TypeProvider 从主程序集 + 测试程序集收集所有类型，
/// 用于 AbilityEventAction 工厂的反射初始化。
/// </summary>
public sealed class TestBridge : AbstractCombatFrameBridge
{
    protected override IElementProvider CreateElementProvider() => new TestElementProvider();
    protected override IMethodProvider CreateMethodProvider() => new TestMethodProvider();
    protected override ITypeProvider CreateTypeProvider() => new TestTypeProvider();
}

file sealed class TestElementProvider : IElementProvider
{
    public string GetAmplifyStat(string elementId) => $"{elementId}_AMP";
    public string GetResistanceStat(string elementId) => $"{elementId}_RES";
}

file sealed class TestMethodProvider : IMethodProvider
{
    public MethodInfo GetMethodInfo(string className, string methodName) => null;
    public MethodInfo GetMethodInfo(string methodName) => null;
}

file sealed class TestTypeProvider : ITypeProvider
{
    private static readonly Type[] _types =
        new[] { typeof(TestTypeProvider).Assembly, typeof(CombatFramework.Core.CFLog).Assembly }
        .SelectMany(a => a.GetTypes())
        .ToArray();

    public Type[] GetTypes() => _types;
}
