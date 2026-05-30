using System.Numerics;
using CombatFramework.Core;
using CombatFramework.Modifier;
using CombatFramework.Stat;

namespace CombatFramework.Unit;

/// <summary>
/// 单位实体——战斗框架的核心实体。
/// 持有槽位、ModifierManager、属性容器、资源系统、标签系统。
/// </summary>
public class UnitEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public TeamFlag Team { get; set; } = TeamFlag.Neutral;

    /// <summary>世界坐标（由 Unity 驱动，框架只读/写）</summary>
    public Vector3 Position { get; set; }

    /// <summary>游戏层锁定的目标 entityId（0 = 无目标；由 Unity 侧设置）</summary>
    public int TargetEntityId { get; set; }

    public UnitAbilitySlot AbilitySlots { get; }
    public ModifierManager ModifierManager { get; }
    public UnitStats Stats { get; }
    public ResourceSystem Resources { get; }
    public TagSystem Tags { get; }
    public float Level { get; set; } = 1;

    public UnitEntity()
    {
        Tags = new TagSystem();
        AbilitySlots = new UnitAbilitySlot(this);
        ModifierManager = new ModifierManager(this);
        Stats = new UnitStats();
        Resources = new ResourceSystem();
    }

    public void Update(float dt)
    {
        ModifierManager.Update(dt);
        // 遍历能力：推进冷却+充能恢复
        foreach (var ability in AbilitySlots.All)
        {
            ability.UpdateCooldown(dt);
        }
    }

    /// <summary>从所有活跃 modifier 的 CheckState 聚合状态检查</summary>
    public bool CheckState(string stateName)
    {
        foreach (var mod in ModifierManager.All)
        {
            if (mod.Data.States.TryGetValue(stateName, out var active) && active)
                return true;
        }
        return false;
    }

    /// <summary>Lua 友好：直接在 unit 上查 tag，等价 unit.Tags.HasTag(tag)</summary>
    public bool HasTag(string tag) => Tags.HasTag(tag);

    /// <summary>按能力名查找已装备的能力实例</summary>
    public AbilityInstance? GetAbility(string name) => AbilitySlots.Get(name);

    /// <summary>Lua 友好：直接在 unit 上查当前资源，等价 unit.Resources.GetCurrent(id)</summary>
    public float GetResource(string id) => Resources.GetCurrent(id);

    /// <summary>
    /// 读取某 stat 的最终值。
    /// 复合属性（HP/Attack/Defense）：以 CompoundStat 三分量为 base，分别叠加
    ///   AggregateStat("HpBase"|"HpMult"|"HpAdd") 等 modifier 贡献，再按
    ///   Final = base + (base × pct + extra) + Modifier 计算。
    /// 其它 stat：从 Stats.GetFlat / ElementalResistance 取 base 后走 ModifierManager 聚合。
    /// 关于 "Attack" / "HP" / "Defense" 这种 final 名字本身不再接受 modifier 直加，
    /// 设计上一切成长都走分量，避免双路径心智负担。
    /// </summary>
    public float GetStat(string statId)
    {
        return statId switch
        {
            "HP" => CompoundFinal(Stats.HP, "HpBase", "HpMult", "HpAdd"),
            "Attack" => CompoundFinal(Stats.Attack, "AtkBase", "AtkMult", "AtkAdd"),
            "Defense" => CompoundFinal(Stats.Defense, "DefBase", "DefMult", "DefAdd"),
            _ => Stats.ElementalResistance.TryGetValue(statId, out var res)
                ? res
                : ModifierManager.AggregateStat(statId, Stats.GetFlat(statId)),
        };
    }

    private float CompoundFinal(Stat.CompoundStat stat, string baseId, string pctId, string extraId)
    {
        var b = stat.Base + ModifierManager.AggregateStat(baseId);
        var p = stat.PercentBonus + ModifierManager.AggregateStat(pctId);
        var e = stat.Extra + ModifierManager.AggregateStat(extraId);
        return b + (b * p + e) + stat.Modifier;
    }
}

public enum TeamFlag
{
    Friendly,
    Enemy,
    Neutral,
    Both,
}
