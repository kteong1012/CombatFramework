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
///   3. 事件触发端到端——ability.On*() 事件方法产生预期副作用
///
/// 所有 AbilityData 均从 tests/Fixtures/abilities/ 目录的 JSON 文件读取。
/// </summary>
public class AbilityEventActionTests
{
    // ─── 测试初始化 ──────────────────────────────────────────
    static AbilityEventActionTests()
    {
        if (CFBridge.Bridge == null)
            CFBridge.Bridge = new TestBridge();
    }

    // ─── 1. 序列化往返 ─────────────────────────────────────────

    [Fact]
    public void Serialize_ApplyModifierActionData_RoundTrip()
    {
        // 从文件加载 → 再序列化 → 再反序列化，验证多态类型还原正确
        var data = LoadAbility("test_apply_modifier.json");

        var json = JsonConvert.SerializeObject(data, AbilityJsonSettings.Instance);
        var restored = JsonConvert.DeserializeObject<AbilityData>(json, AbilityJsonSettings.Instance);

        Assert.NotNull(restored);
        var action = Assert.IsType<ApplyModifierActionData>(
            restored.AbilityEvents[AbilityEvents.OnSpellStart][0]);
        Assert.Equal("mod_apply_test", action.ModifierName);
        var selector = Assert.IsType<SingleTargetSelector>(action.Target);
        Assert.Equal(TargetType.Target, selector.Type);
    }

    [Fact]
    public void Serialize_DamageActionData_WithAreaTarget_RoundTrip()
    {
        var data = LoadAbility("test_damage_area.json");

        var json = JsonConvert.SerializeObject(data, AbilityJsonSettings.Instance);
        var restored = JsonConvert.DeserializeObject<AbilityData>(json, AbilityJsonSettings.Instance);

        Assert.NotNull(restored);
        var action = Assert.IsType<DamageActionData>(
            restored.AbilityEvents[AbilityEvents.OnSpellStart][0]);
        Assert.Equal("FIRE", action.Element);
        var area = Assert.IsType<AreaTargetSelector>(action.Target);
        Assert.Equal(TargetType.Caster, area.Center);
        Assert.Equal(300f, area.Radius);
        Assert.Equal(TeamFilter.Enemy, area.Teams);
        var constGetter = Assert.IsType<ConstantValueGetter>(action.Damage);
        Assert.Equal(100f, constGetter.Value);
    }

    [Fact]
    public void Serialize_AbilityData_WithEvents_RoundTrip()
    {
        var data = LoadAbility("test_apply_modifier.json");

        var json = JsonConvert.SerializeObject(data, AbilityJsonSettings.Instance);
        var restored = JsonConvert.DeserializeObject<AbilityData>(json, AbilityJsonSettings.Instance);

        Assert.NotNull(restored);
        Assert.Equal("test_apply_modifier", restored.Name);
        Assert.True(restored.AbilityEvents.ContainsKey(AbilityEvents.OnSpellStart));
        Assert.Single(restored.AbilityEvents[AbilityEvents.OnSpellStart]);
        Assert.IsType<ApplyModifierActionData>(restored.AbilityEvents[AbilityEvents.OnSpellStart][0]);
        Assert.True(restored.AbilityModifiers["mod_apply_test"].IsBuff);
    }

    // ─── 2. Data 模板无状态 ────────────────────────────────────

    [Fact]
    public void Execute_ApplyModifier_DataIsUnchanged_AfterMultipleExecutions()
    {
        var abilityData = LoadAbility("test_apply_modifier.json");
        var actionData = (ApplyModifierActionData)abilityData.AbilityEvents[AbilityEvents.OnSpellStart][0];

        var (caster, target) = MakeUnits();
        var ability = MakeAbility(abilityData, caster);

        // 执行三次，走完整事件派发路径
        for (int i = 0; i < 3; i++)
            ability.OnSpellStart(MakeContext(ability, caster, target));

        // 模板字段不变
        Assert.Equal("mod_apply_test", actionData.ModifierName);
        var selector = Assert.IsType<SingleTargetSelector>(actionData.Target);
        Assert.Equal(TargetType.Target, selector.Type);
    }

    // ─── 3. 事件触发端到端 ─────────────────────────────────────

    [Fact]
    public void Execute_ApplyModifier_AddsModifierToTarget()
    {
        var abilityData = LoadAbility("test_apply_modifier.json");
        var (caster, target) = MakeUnits();
        var ability = MakeAbility(abilityData, caster);

        ability.OnSpellStart(MakeContext(ability, caster, target));

        Assert.True(target.ModifierManager.Has("mod_apply_test"));
    }

    [Fact]
    public void Execute_RemoveModifier_RemovesModifierFromTarget()
    {
        var abilityData = LoadAbility("test_remove_modifier.json");
        var (caster, target) = MakeUnits();
        var ability = MakeAbility(abilityData, caster);

        // 先手动加上 modifier
        var modData = abilityData.AbilityModifiers["mod_remove_test"];
        target.ModifierManager.Add(modData, caster, ability);
        target.ModifierManager.Update(0f);
        Assert.True(target.ModifierManager.Has("mod_remove_test"));

        // OnSpellStart 触发 RemoveModifierAction
        ability.OnSpellStart(MakeContext(ability, caster, target));

        Assert.False(target.ModifierManager.Has("mod_remove_test"));
    }

    [Fact]
    public void Execute_DamageAction_ReducesTargetHp()
    {
        // 目标初始 1000 HP，施放 100 固定伤害（无暴击/无元素乘区），验证 HP 下降
        var abilityData = LoadAbility("test_damage_single_target.json");
        var (caster, target) = MakeUnits();
        target.Stats.Set("HP", 1000f);

        var ability = MakeAbility(abilityData, caster);
        ability.OnSpellStart(MakeContext(ability, caster, target));

        var hpAfter = target.GetStat("HP");
        Assert.True(hpAfter < 1000f, $"HP 应该降低，实际为 {hpAfter}");
        // 无暴击（CritRate=0）、无乘区（抗性/加成均为0）：finalDamage == 100
        Assert.Equal(900f, hpAfter, precision: 1);
    }

    [Fact]
    public void Execute_ApplyModifier_StackCount_DecrementOnRemove()
    {
        var abilityData = LoadAbility("test_stack_modifier.json");
        var (caster, target) = MakeUnits();
        var ability = MakeAbility(abilityData, caster);

        // OnAbilityPhaseStart 执行 ApplyModifierAction（StackCount 模式堆叠 3 次）
        for (int i = 0; i < 3; i++)
            ability.OnAbilityPhaseStart(MakeContext(ability, caster, target));

        target.ModifierManager.Update(0f);
        var spec = target.ModifierManager.Find("mod_stack_test");
        Assert.NotNull(spec);
        Assert.Equal(3, spec.StackCount);

        // OnSpellStart 执行 RemoveModifierAction（减一层）
        ability.OnSpellStart(MakeContext(ability, caster, target));

        Assert.Equal(2, spec.StackCount);
        Assert.True(target.ModifierManager.Has("mod_stack_test"));
    }

    // ─── 工具方法 ─────────────────────────────────────────────

    private static AbilityData LoadAbility(string fileName)
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fixtures", "abilities", fileName);
        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<AbilityData>(json, AbilityJsonSettings.Instance)!;
    }

    private static (UnitEntity caster, UnitEntity target) MakeUnits()
    {
        var caster = new UnitEntity { Team = TeamFlag.Friendly };
        var target = new UnitEntity { Team = TeamFlag.Enemy };
        return (caster, target);
    }

    private static AbilitySpec MakeAbility(AbilityData data, UnitEntity owner)
    {
        var ability = AbilitySpec.Create(data);
        ability.Owner = owner;
        ability.Level = 1;
        return ability;
    }

    private static AbilityEventContext MakeContext(AbilitySpec ability, UnitEntity caster, UnitEntity target)
        => new AbilityEventContext { Ability = ability, Caster = caster, Target = target };
}
