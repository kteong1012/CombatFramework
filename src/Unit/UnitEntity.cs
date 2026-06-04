using System.Numerics;
using CombatFramework.Bridge;
using CombatFramework.Core;
using CombatFramework.Core.Ability;
using CombatFramework.Core.Ability.AbilityEvent;
using CombatFramework.Core.Modifier;
using CombatFramework.Core.Stat;

namespace CombatFramework.Unit;

public class UnitEntity
{
    public uint Id { get; set; }
    public TeamFlag Team { get; set; } = TeamFlag.Neutral;

    /// <summary>世界坐标（由引擎驱动，框架只读/写）</summary>
    public Vector3 Position { get; set; }

    /// <summary>游戏层锁定的目标 entityId（0 = 无目标；由引擎侧设置）</summary>
    public int TargetEntityId { get; set; }

    /// <summary>已装备的技能，按名称索引。</summary>
    public Dictionary<string, AbilitySpec> Abilities { get; }
    public ModifierManager ModifierManager { get; }
    public StatsManager Stats { get; }
    public TagSystem Tags { get; }
    public float Level { get; set; } = 1;

    public Dictionary<string,object> Blackboard { get; set; }

    public UnitEntity()
    {
        Tags = new TagSystem();
        Abilities = new Dictionary<string, AbilitySpec>();
        ModifierManager = new ModifierManager(this);
        Stats = new StatsManager();

        Blackboard = new Dictionary<string, object>();
    }

    public void Update(float dt)
    {
        ModifierManager.Update(dt);
        // TODO 刷新位置
    }

    public bool HasTag(string tag) => Tags.HasTag(tag);
    public float GetStat(string statId) => Stats.Get(statId);

    // ─── 技能管理 ────────────────────────────────────────────────

    /// <summary>按名称查找已装备的 AbilitySpec。</summary>
    public AbilitySpec GetAbilitySpecByName(string name)
        => Abilities.TryGetValue(name, out var ability) ? ability : null;

    /// <summary>装备技能。若同名已存在，先触发 OnUnequipped 再替换。</summary>
    public void EquipAbility(AbilitySpec ability)
    {
        if (ability?.Name == null) return;

        if (Abilities.TryGetValue(ability.Name, out var old))
        {
            old.OnUnequipped(this);
            old.Owner = null;
        }

        ability.Owner = this;
        Abilities[ability.Name] = ability;
        ability.OnEquipped(this);
    }

    /// <summary>卸下指定名称的技能。</summary>
    public void UnequipAbility(string name)
    {
        if (Abilities.TryGetValue(name, out var ability))
        {
            ability.OnUnequipped(this);
            ability.Owner = null;
            Abilities.Remove(name);
        }
    }

    // ─── 施放 ────────────────────────────────────────────────────

    /// <summary>
    /// 按名称查找技能并尝试施放。找不到技能时返回 false。
    /// </summary>
    public bool TryCast(string abilityName, UnitEntity target = null)
    {
        var spec = GetAbilitySpecByName(abilityName);
        return spec != null && TryCast(spec, target);
    }

    /// <summary>
    /// 对指定 AbilitySpec 递归执行转换链，然后施放最终技能。
    /// 每一步转换结果继续尝试转换（可用于多级条件分支），直到无匹配为止。
    /// </summary>
    public bool TryCast(AbilitySpec ability, UnitEntity target = null)
    {
        // 按顺序尝试所有满足条件的转换，任一成功即返回
        foreach (var transformed in ability.GetMatchingTransforms(this, target))
        {
            if (TryCast(transformed, target))
                return true;
        }

        if (!ability.CanCast(out _))
            return false;

        ability.DeductCosts();

        var ctx = new AbilityEventContext { Ability = ability, Caster = this, Target = target };
        CFBridge.Bridge?.StartAbility(ability, ctx);
        return true;
    }
}

public enum TeamFlag
{
    Friendly,
    Enemy,
    Neutral,
    Both,
}
