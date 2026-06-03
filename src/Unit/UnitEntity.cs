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

    /// <summary>
    /// 尝试施放指定槽位的技能。
    /// 流程：检查 cost → 扣除资源 → 委托 Bridge 执行技能流程（如 Timeline）。
    /// Bridge 负责在适当时机（前摇结束）触发 OnAbilityPhaseStart / OnSpellStart。
    /// 测试环境下 Bridge 可直接依次触发这两个事件。
    /// </summary>
    /// <param name="slotIndex">槽位下标（0-based）。预留枚举重载，当前以 int 为主。</param>
    /// <param name="target">技能目标，可为 null。</param>
    /// <returns>true = 已交由 Bridge 执行；false = 槽位无效或 cost 不足。</returns>
    public bool TryCast(int slotIndex, UnitEntity target = null)
    {
        var ability = AbilitySlots.GetByIndex(slotIndex);
        if (ability == null) return false;
        if (!ability.CanCast(out _)) return false;

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
