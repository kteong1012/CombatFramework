using System.Numerics;
using System.Reflection;
using CombatFramework.Bridge;
using CombatFramework.Core;
using CombatFramework.Core.Ability;
using CombatFramework.Core.Ability.AbilityEvent;
using CombatFramework.Core.Enums;
using CombatFramework.Unit;

namespace CombatFramework.Tests;

/// <summary>
/// 测试用 Bridge 实现：TypeProvider 从主程序集 + 测试程序集收集所有类型，
/// 用于 AbilityEventAction 工厂的反射初始化。
/// StartAbility 简化为同步依次触发 OnAbilityPhaseStart → OnSpellStart，
/// 模拟"前摇时机与施法时机重合"的测试场景。
/// </summary>
public sealed class TestBridge : AbstractCombatFrameworkBridge
{
    protected override IElementProvider CreateElementProvider() => new TestElementProvider();
    protected override IMethodProvider CreateMethodProvider() => new TestMethodProvider();
    protected override ITypeProvider CreateTypeProvider() => new TestTypeProvider();

    public override void StartAbility(AbilitySpec ability, AbilityEventContext ctx)
    {
        // 测试简化：前摇时机与施法时机重合，直接依次触发
        ability.OnAbilityPhaseStart(ctx);
        ability.OnSpellStart(ctx);
    }
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

/// <summary>
/// 测试用盒形查询服务：忽略几何参数，直接返回预设的 unit 列表（排除自身）。
/// </summary>
public sealed class TestShapeQueryService : IShapeQueryService
{
    private readonly IReadOnlyList<UnitEntity> _units;

    public TestShapeQueryService(IEnumerable<UnitEntity> units)
    {
        _units = units.ToList();
    }

    public IEnumerable<UnitEntity> QueryBox(
        Vector3 center, Vector3 offset, Vector3 eulerRotation, Vector3 size,
        TeamFilter teams, UnitEntity self)
    {
        foreach (var unit in _units)
        {
            if (unit == self) continue;
            yield return unit;
        }
    }

    public void ShowBoxPreview(Vector3 center, Vector3 offset, Vector3 eulerRotation, Vector3 size) { }
    public void ShowCirclePreview(Vector3 center, float radius) { }
}

