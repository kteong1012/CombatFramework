using System.Numerics;
using CombatFramework.Core;
using CombatFramework.Unit;

namespace CombatFramework.Event;

/// <summary>
/// 伤害事件参数——在全局事件总线上传递。
/// </summary>
public class DamageEventData
{
    public UnitEntity Victim { get; }
    public UnitEntity Attacker { get; }
    public float RawDamage { get; set; }
    public float FinalDamage { get; set; }
    public string DamageType { get; set; }
    public bool IsCritical { get; set; }
    public bool IsCancelled { get; set; }

    /// <summary>游戏层实体 ID（UI 跳字定位用，CF 只透传）</summary>
    public int HitEntityId { get; set; }
    /// <summary>碰撞点世界坐标（UI 跳字定位用，CF 只透传）</summary>
    public Vector3 HitPoint { get; set; }

    public DamageEventData(UnitEntity victim, UnitEntity attacker, float rawDamage, string damageType)
    {
        Victim = victim;
        Attacker = attacker;
        RawDamage = rawDamage;
        FinalDamage = rawDamage;
        DamageType = damageType;
    }
}

public class HealEventData
{
    public UnitEntity Target { get; }
    public UnitEntity Source { get; }
    public float RawHeal { get; set; }
    public float FinalHeal { get; set; }
    public bool IsCancelled { get; set; }

    public HealEventData(UnitEntity target, UnitEntity source, float rawHeal)
    {
        Target = target;
        Source = source;
        RawHeal = rawHeal;
        FinalHeal = rawHeal;
    }
}

/// <summary>韧性变化事件数据</summary>
public class ToughnessEventData
{
    public UnitEntity Target { get; }
    public UnitEntity Source { get; }
    public float OldValue { get; }
    public float NewValue { get; }
    public float MaxValue { get; }
    public bool IsBroken => NewValue <= 0 && OldValue > 0;

    public ToughnessEventData(UnitEntity target, UnitEntity source, float oldValue, float newValue, float maxValue)
    {
        Target = target;
        Source = source;
        OldValue = oldValue;
        NewValue = newValue;
        MaxValue = maxValue;
    }
}
