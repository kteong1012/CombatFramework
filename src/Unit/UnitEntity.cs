using System.Numerics;
using CombatFramework.Bridge;
using CombatFramework.Core;
using CombatFramework.Core.Ability;
using CombatFramework.Core.Ability.AbilityEvent;
using CombatFramework.Core.Enums;
using CombatFramework.Core.Modifier;
using CombatFramework.Core.Stat;

namespace CombatFramework.Unit;

public class UnitEntity
{
    public uint Id { get; set; }
    public TeamFlag Team { get; set; } = TeamFlag.Neutral;

    /// <summary>世界坐标（由 Unity 驱动，框架只读/写）</summary>
    public Vector3 Position { get; set; }

    /// <summary>游戏层锁定的目标 entityId（0 = 无目标；由 Unity 侧设置）</summary>
    public int TargetEntityId { get; set; }

    public UnitAbilitySlot AbilitySlots { get; }
    public ModifierManager ModifierManager { get; }
    public StatsManager Stats { get; }
    public TagSystem Tags { get; }
    public float Level { get; set; } = 1;

    public Dictionary<string,object> Blackboard { get; set; }

    public UnitEntity()
    {
        Tags = new TagSystem();
        AbilitySlots = new UnitAbilitySlot(this);
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

    /// <summary>TryCast 的 SlotType 枚举重载，等价于 TryCast((int)slotType, target)。</summary>
    public bool TryCast(SlotType slotType, UnitEntity target = null)
        => TryCast((int)slotType, target);

    /// <summary>按技能名称查找已装备的 AbilitySpec。</summary>
    public AbilitySpec GetAbilitySpecByName(string name) => AbilitySlots.Get(name);

    /// <summary>
    /// 按名称查找技能并尝试施放。找不到技能时返回 false。
    /// </summary>
    public bool TryCast(string abilityName, UnitEntity target = null)
    {
        var spec = GetAbilitySpecByName(abilityName);
        return spec != null && TryCast(spec, target);
    }

    /// <summary>
    /// 尝试施放指定槽位的技能。先评估 Transforms，再对最终技能执行 CanCast + DeductCosts。
    /// </summary>
    public bool TryCast(int slotIndex, UnitEntity target = null)
    {
        var ability = AbilitySlots.GetByIndex(slotIndex);
        return ability != null && TryCast(ability, target);
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
