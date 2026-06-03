using System.Numerics;
using CombatFramework.Core;
using CombatFramework.Core.Ability;
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

}

public enum TeamFlag
{
    Friendly,
    Enemy,
    Neutral,
    Both,
}
