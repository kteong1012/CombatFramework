using CombatFramework.Bridge;
using CombatFramework.Core;
using CombatFramework.Core.Ability;
using CombatFramework.Core.Ability.AbilityEvent;
using CombatFramework.Core.Enums;
using CombatFramework.Core.Executor.ValueGetter;
using CombatFramework.Core.Model;
using CombatFramework.Core.Modifier;
using CombatFramework.Unit;
using Newtonsoft.Json;

namespace CombatFramework.Tests.Integration;

/// <summary>
/// 验证三个关注点：
///   1. 序列化往返（ActionData + TargetSelector 多态）
///   2. Data 模板无状态——多次执行不修改模板字段
///   3. 事件触发端到端——Action.Execute 产生预期副作用
/// </summary>
public class AbilityEventActionTests
{
    // ─── 测试初始化 ──────────────────────────────────────────
    static AbilityEventActionTests()
    {
        // 每个测试文件只需初始化一次 Bridge
        if (CFBridge.Bridge == null)
            CFBridge.Bridge = new TestBridge();
    }

    // ─── 1. 序列化往返 ─────────────────────────────────────────

    [Fact]
    public void Serialize_ApplyModifierActionData_RoundTrip()
    {
        var original = new ApplyModifierActionData
        {
            Target = new SingleTargetSelector { Type = TargetType.Target },
            ModifierName = "modifier_atk_bonus",
        };

        var json = JsonConvert.SerializeObject(original, AbilityJsonSettings.Instance);
        var restored = JsonConvert.DeserializeObject<ApplyModifierActionData>(json, AbilityJsonSettings.Instance);

        Assert.NotNull(restored);
        Assert.Equal("modifier_atk_bonus", restored.ModifierName);
        Assert.IsType<SingleTargetSelector>(restored.Target);
        Assert.Equal(TargetType.Target, ((SingleTargetSelector)restored.Target).Type);
    }

    [Fact]
    public void Serialize_DamageActionData_WithAreaTarget_RoundTrip()
    {
        var original = new DamageActionData
        {
            Target = new AreaTargetSelector { Center = TargetType.Caster, Radius = 300f, Teams = TeamFilter.Enemy },
            Element = "FIRE",
            Damage = new ConstantValueGetter(100f),
        };

        var json = JsonConvert.SerializeObject(original, AbilityJsonSettings.Instance);
        var restored = JsonConvert.DeserializeObject<DamageActionData>(json, AbilityJsonSettings.Instance);

        Assert.NotNull(restored);
        Assert.Equal("FIRE", restored.Element);
        var area = Assert.IsType<AreaTargetSelector>(restored.Target);
        Assert.Equal(300f, area.Radius);
        Assert.Equal(TeamFilter.Enemy, area.Teams);
        Assert.Equal(TargetType.Caster, area.Center);
    }

    [Fact]
    public void Serialize_AbilityData_WithEvents_RoundTrip()
    {
        var abilityData = new AbilityData
        {
            Name = "test_ability",
            AbilityModifiers = new Dictionary<string, ModifierData>
            {
                ["mod_a"] = new ModifierData { Name = "mod_a", IsBuff = true },
            },
            AbilityEvents = new List<AbilityEventActionData>
            {
                new ApplyModifierActionData
                {
                    Target = new SingleTargetSelector { Type = TargetType.Target },
                    ModifierName = "mod_a",
                },
            },
        };

        var json = JsonConvert.SerializeObject(abilityData, AbilityJsonSettings.Instance);
        var restored = JsonConvert.DeserializeObject<AbilityData>(json, AbilityJsonSettings.Instance);

        Assert.NotNull(restored);
        Assert.Single(restored.AbilityEvents);
        Assert.IsType<ApplyModifierActionData>(restored.AbilityEvents[0]);
        Assert.True(restored.AbilityModifiers["mod_a"].IsBuff);
    }

    // ─── 2. Data 模板无状态 ────────────────────────────────────

    [Fact]
    public void Execute_ApplyModifier_DataIsUnchanged_AfterMultipleExecutions()
    {
        var modData = new ModifierData { Name = "mod_stateless", IsBuff = true };
        var abilityData = new AbilityData
        {
            Name = "stateless_test",
            AbilityModifiers = new Dictionary<string, ModifierData> { ["mod_stateless"] = modData },
        };
        var actionData = new ApplyModifierActionData
        {
            Target = new SingleTargetSelector { Type = TargetType.Target },
            ModifierName = "mod_stateless",
        };

        var action = AbilityEventAction.Create(actionData);
        Assert.NotNull(action);

        var (caster, target) = MakeUnits();
        var ability = MakeAbility(abilityData, caster);

        // 执行三次
        for (int i = 0; i < 3; i++)
        {
            var ctx = MakeContext(ability, caster, target);
            action.Execute(ctx);
        }

        // 模板字段不变
        Assert.Equal("mod_stateless", actionData.ModifierName);
        Assert.IsType<SingleTargetSelector>(actionData.Target);
        Assert.Equal(TargetType.Target, ((SingleTargetSelector)actionData.Target).Type);
    }

    // ─── 3. 事件触发端到端 ─────────────────────────────────────

    [Fact]
    public void Execute_ApplyModifier_AddsModifierToTarget()
    {
        var modData = new ModifierData { Name = "mod_apply_test", IsBuff = true };
        var abilityData = new AbilityData
        {
            Name = "apply_test",
            AbilityModifiers = new Dictionary<string, ModifierData> { [modData.Name] = modData },
        };
        var actionData = new ApplyModifierActionData
        {
            Target = new SingleTargetSelector { Type = TargetType.Target },
            ModifierName = modData.Name,
        };

        var action = AbilityEventAction.Create(actionData);
        var (caster, target) = MakeUnits();
        var ability = MakeAbility(abilityData, caster);

        action.Execute(MakeContext(ability, caster, target));

        Assert.True(target.ModifierManager.Has(modData.Name));
    }

    [Fact]
    public void Execute_RemoveModifier_RemovesModifierFromTarget()
    {
        var modData = new ModifierData { Name = "mod_remove_test", IsBuff = true };
        var abilityData = new AbilityData
        {
            Name = "remove_test",
            AbilityModifiers = new Dictionary<string, ModifierData> { [modData.Name] = modData },
        };

        var (caster, target) = MakeUnits();
        var ability = MakeAbility(abilityData, caster);

        // 先手动加上 modifier
        target.ModifierManager.Add(modData, caster, ability);
        target.ModifierManager.Update(0f); // flush pending
        Assert.True(target.ModifierManager.Has(modData.Name));

        var actionData = new RemoveModifierActionData
        {
            Target = new SingleTargetSelector { Type = TargetType.Target },
            ModifierName = modData.Name,
        };
        var action = AbilityEventAction.Create(actionData);
        action.Execute(MakeContext(ability, caster, target));

        Assert.False(target.ModifierManager.Has(modData.Name));
    }

    [Fact]
    public void Execute_ApplyModifier_StackCount_DecrementOnRemove()
    {
        var modData = new ModifierData
        {
            Name = "mod_stack_test",
            IsBuff = true,
            StackMode = ModifierStackMode.StackCount,
        };
        var abilityData = new AbilityData
        {
            Name = "stack_test",
            AbilityModifiers = new Dictionary<string, ModifierData> { [modData.Name] = modData },
        };

        var (caster, target) = MakeUnits();
        var ability = MakeAbility(abilityData, caster);

        // 加 3 层
        var applyData = new ApplyModifierActionData
        {
            Target = new SingleTargetSelector { Type = TargetType.Target },
            ModifierName = modData.Name,
        };
        var applyAction = AbilityEventAction.Create(applyData);
        for (int i = 0; i < 3; i++)
            applyAction.Execute(MakeContext(ability, caster, target));

        target.ModifierManager.Update(0f);
        var spec = target.ModifierManager.Find(modData.Name);
        Assert.NotNull(spec);
        Assert.Equal(3, spec.StackCount);

        // 减一层
        var removeData = new RemoveModifierActionData
        {
            Target = new SingleTargetSelector { Type = TargetType.Target },
            ModifierName = modData.Name,
        };
        var removeAction = AbilityEventAction.Create(removeData);
        removeAction.Execute(MakeContext(ability, caster, target));

        Assert.Equal(2, spec.StackCount);
        Assert.True(target.ModifierManager.Has(modData.Name)); // 仍然存在
    }

    // ─── 工具方法 ─────────────────────────────────────────────

    private static (UnitEntity caster, UnitEntity target) MakeUnits()
    {
        var caster = new UnitEntity { Team = TeamFlag.Friendly };
        var target = new UnitEntity { Team = TeamFlag.Enemy };
        return (caster, target);
    }

    private static AbilitySpec MakeAbility(AbilityData data, UnitEntity owner)
        => new AbilitySpec { data = data, Owner = owner, Level = 1 };

    private static AbilityEventContext MakeContext(AbilitySpec ability, UnitEntity caster, UnitEntity target)
        => new AbilityEventContext { Ability = ability, Caster = caster, Target = target };
}
