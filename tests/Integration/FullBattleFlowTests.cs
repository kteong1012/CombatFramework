using CombatFramework.Bridge;
using CombatFramework.Core;
using CombatFramework.Core.Ability;
using CombatFramework.Core.Ability.AbilityEvent;
using CombatFramework.Core.Model;
using CombatFramework.Unit;
using Newtonsoft.Json;
using Xunit;

namespace CombatFramework.Tests.Integration;

/// <summary>
/// 完整战斗流程集成测试：
///   1. 两个不同阵营的 unit，初始化战斗属性。
///   2. caster 装配被动技能 → 攻击力 +20%；装配主动技能。
///   3. TryCast：cost 检查 → 扣除资源 → Bridge.StartAbility → OnSpellStart。
///   4. OnSpellStart → ForEachHitAction → Box 查询 → TestShapeQueryService 返回目标。
///   5. OnHitTarget → DamageAction(Teams=Enemy) → 过滤阵营 → 打伤害 → HP 减少。
/// </summary>
public class FullBattleFlowTests
{
    // ─── 测试初始化 ──────────────────────────────────────────
    static FullBattleFlowTests()
    {
        CFBridge.Initialize(new TestBridge());
    }

    // ─── 辅助 ─────────────────────────────────────────────────

    private static AbilityData LoadAbility(string fileName)
    {
        var path = Path.Combine("Fixtures", "abilities", fileName);
        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<AbilityData>(json, AbilityJsonSettings.Instance);
    }

    /// <summary>创建初始属性已配置好的 caster (Friendly)。</summary>
    private static UnitEntity MakeCaster()
    {
        var unit = new UnitEntity { Team = TeamFlag.Friendly, Level = 1 };
        // 攻击复合属性分量：Atk = Atk_Base * (1 + Atk_Mult) + Atk_Add
        unit.Stats.Set("Atk_Base", 200f);
        // 资源
        unit.Stats.Set("Energy", 100f);
        // 防御（自伤不需要，但保持对称）
        unit.Stats.Set("DefFinal", 0f);
        unit.Stats.Set("HP", 1000f);
        return unit;
    }

    /// <summary>创建初始属性已配置好的 target (Enemy)。</summary>
    private static UnitEntity MakeTarget()
    {
        var unit = new UnitEntity { Team = TeamFlag.Enemy, Level = 1 };
        unit.Stats.Set("HP", 1000f);
        unit.Stats.Set("DefFinal", 0f);
        return unit;
    }

    // ─── 测试 ─────────────────────────────────────────────────

    [Fact]
    public void Equip_PassiveSkill_AppliesAtkMultToOwner()
    {
        // Arrange
        var caster = MakeCaster();
        var passiveData = LoadAbility("test_passive_atk_bonus.json");
        var passive = AbilitySpec.Create(passiveData);

        // Act — 装备时触发 OnEquipped → ApplyModifier → mod_atk_bonus.OnCreated → Stats.Add("Atk_Mult", 0.2)
        caster.EquipAbility(passive);

        // Assert
        Assert.Equal(0.2f, caster.GetStat("Atk_Mult"), precision: 4);
        // 复合 Atk = 200 * (1 + 0.2) = 240
        Assert.Equal(240f, caster.GetStat("Atk"), precision: 2);
    }

    [Fact]
    public void Unequip_PassiveSkill_RevertsAtkMult()
    {
        // Arrange
        var caster = MakeCaster();
        var passive = AbilitySpec.Create(LoadAbility("test_passive_atk_bonus.json"));
        caster.EquipAbility(passive);
        Assert.Equal(0.2f, caster.GetStat("Atk_Mult"), precision: 4); // precondition

        // Act — 卸下 → OnUnequipped → RemoveModifier → mod_atk_bonus.OnDestroy → Stats.Add("Atk_Mult", -0.2)
        caster.UnequipAbility("passive_atk_bonus");

        // Assert
        Assert.Equal(0f, caster.GetStat("Atk_Mult"), precision: 4);
        Assert.Equal(200f, caster.GetStat("Atk"), precision: 2);
    }

    [Fact]
    public void TryCast_DeductsCostOnSuccess()
    {
        // Arrange
        var caster = MakeCaster();
        var target = MakeTarget();
        ((TestBridge)CFBridge.Bridge).ShapeQuery = new TestShapeQueryService(new[] { caster, target });

        caster.EquipAbility(AbilitySpec.Create(LoadAbility("test_passive_atk_bonus.json")));
        caster.EquipAbility(AbilitySpec.Create(LoadAbility("test_active_attack.json")));

        // Act
        var result = caster.TryCast("active_attack", target);

        // Assert
        Assert.True(result);
        Assert.Equal(70f, caster.GetStat("Energy"), precision: 2); // 100 - 30
    }

    [Fact]
    public void TryCast_FailsWhenInsufficientCost()
    {
        // Arrange
        var caster = MakeCaster();
        caster.Stats.Set("Energy", 10f); // 不够 30
        var target = MakeTarget();
        ((TestBridge)CFBridge.Bridge).ShapeQuery = new TestShapeQueryService(new[] { caster, target });

        caster.EquipAbility(AbilitySpec.Create(LoadAbility("test_passive_atk_bonus.json")));
        caster.EquipAbility(AbilitySpec.Create(LoadAbility("test_active_attack.json")));

        // Act
        var result = caster.TryCast("active_attack", target);

        // Assert
        Assert.False(result);
        Assert.Equal(10f, caster.GetStat("Energy"), precision: 2); // 未扣除
    }

    [Fact]
    public void TryCast_DamagesEnemyAndSkipsFriendly()
    {
        // Arrange
        var caster = MakeCaster();
        var enemy = MakeTarget(); // TeamFlag.Enemy
        var ally = new UnitEntity { Team = TeamFlag.Friendly };
        ally.Stats.Set("HP", 500f);
        ally.Stats.Set("DefFinal", 0f);

        // 所有 unit 注册到 ShapeQuery
        CFBridge.Bridge.ShapeQuery = new TestShapeQueryService(new[] { caster, enemy, ally });

        caster.EquipAbility(AbilitySpec.Create(LoadAbility("test_passive_atk_bonus.json")));
        caster.EquipAbility(AbilitySpec.Create(LoadAbility("test_active_attack.json")));

        // Act
        caster.TryCast("active_attack", enemy);

        // Assert — 敌方 HP 扣减，友方 HP 不变
        Assert.True(enemy.GetStat("HP") < 1000f, "enemy should take damage");
        Assert.Equal(500f, ally.GetStat("HP"), precision: 2); // 友方不受影响
    }
}
