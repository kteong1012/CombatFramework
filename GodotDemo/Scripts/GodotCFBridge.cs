using System;
using System.Linq;
using System.Reflection;
using CombatFramework.Bridge;
using CombatFramework.Core.Ability;
using CombatFramework.Core.Ability.AbilityEvent;

/// <summary>
/// Godot 端 Bridge 实现。
/// StartAbility 直接同步触发 OnAbilityPhaseStart → OnSpellStart，
/// 后续接入动画时替换此处逻辑即可。
/// </summary>
public class GodotCFBridge : AbstractCombatFrameworkBridge
{
    protected override IElementProvider CreateElementProvider() => new GodotElementProvider();
    protected override IMethodProvider CreateMethodProvider() => new GodotMethodProvider();
    protected override ITypeProvider CreateTypeProvider() => new GodotTypeProvider();

    public override void StartAbility(AbilitySpec ability, AbilityEventContext ctx)
    {
        ability.OnAbilityPhaseStart(ctx);
        ability.OnSpellStart(ctx);
    }
}

file sealed class GodotElementProvider : IElementProvider
{
    public string GetAmplifyStat(string elementId) => $"{elementId}_AMP";
    public string GetResistanceStat(string elementId) => $"{elementId}_RES";
}

file sealed class GodotMethodProvider : IMethodProvider
{
    public MethodInfo GetMethodInfo(string className, string methodName) => null;
    public MethodInfo GetMethodInfo(string methodName) => null;
}

file sealed class GodotTypeProvider : ITypeProvider
{
    private static readonly Type[] _types =
        new[] { typeof(GodotTypeProvider).Assembly, typeof(CombatFramework.Core.CFLog).Assembly }
        .SelectMany(a => a.GetTypes())
        .ToArray();

    public Type[] GetTypes() => _types;
}
