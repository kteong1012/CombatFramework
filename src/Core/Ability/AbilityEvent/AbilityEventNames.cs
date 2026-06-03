namespace CombatFramework.Core.Ability.AbilityEvent;

/// <summary>
/// Ability 顶层事件名常量。
/// 对应 AbilityData.AbilityEvents 字典的 key，也是 AbilitySpec 虚方法的 dispatch 标识。
/// </summary>
public static class AbilityEvents
{
    /// <summary>开始蓄力（CastPoint 期间）</summary>
    public const string OnAbilityPhaseStart    = nameof(OnAbilityPhaseStart);
    /// <summary>施法完成瞬间（蓄力结束后）</summary>
    public const string OnSpellStart           = nameof(OnSpellStart);
    /// <summary>追踪弹命中单位</summary>
    public const string OnProjectileHitUnit    = nameof(OnProjectileHitUnit);
    /// <summary>追踪弹飞行结束（未命中目标）</summary>
    public const string OnProjectileFinish     = nameof(OnProjectileFinish);
    /// <summary>引导技能引导完毕</summary>
    public const string OnChannelFinish        = nameof(OnChannelFinish);
    /// <summary>引导技能被打断</summary>
    public const string OnChannelInterrupted   = nameof(OnChannelInterrupted);
    /// <summary>开关技能被开启</summary>
    public const string OnToggleOn             = nameof(OnToggleOn);
    /// <summary>开关技能被关闭</summary>
    public const string OnToggleOff            = nameof(OnToggleOff);
    /// <summary>技能升级时</summary>
    public const string OnUpgrade              = nameof(OnUpgrade);
    /// <summary>技能被装备到 unit 上时（含被动技能）</summary>
    public const string OnEquipped             = nameof(OnEquipped);
    /// <summary>技能从 unit 上卸下时</summary>
    public const string OnUnequipped           = nameof(OnUnequipped);
    /// <summary>技能命中每个目标时（由 ForEachHitAction 触发）</summary>
    public const string OnHitTarget            = nameof(OnHitTarget);
}

/// <summary>
/// Modifier 内部事件名常量。
/// 对应 ModifierData.Events 字典的 key，也是 ModifierSpec 虚方法的 dispatch 标识。
/// </summary>
public static class ModifierEvents
{
    /// <summary>modifier 被施加时</summary>
    public const string OnCreated          = nameof(OnCreated);
    /// <summary>每隔 ThinkInterval 秒触发一次</summary>
    public const string OnIntervalThink    = nameof(OnIntervalThink);
    /// <summary>攻击动作开始（弹射物飞出前）</summary>
    public const string OnAttackStart      = nameof(OnAttackStart);
    /// <summary>攻击弹射物发射时</summary>
    public const string OnAttack           = nameof(OnAttack);
    /// <summary>攻击命中目标时</summary>
    public const string OnAttackLanded     = nameof(OnAttackLanded);
    /// <summary>攻击被闪避/miss</summary>
    public const string OnAttackFailed     = nameof(OnAttackFailed);
    /// <summary>持有者被攻击时</summary>
    public const string OnAttacked         = nameof(OnAttacked);
    /// <summary>持有者受到伤害时</summary>
    public const string OnTakeDamage       = nameof(OnTakeDamage);
    /// <summary>持有者死亡时</summary>
    public const string OnDeath            = nameof(OnDeath);
    /// <summary>持有者收到新命令时</summary>
    public const string OnOrder            = nameof(OnOrder);
    /// <summary>持有者移动时</summary>
    public const string OnUnitMoved        = nameof(OnUnitMoved);
    /// <summary>modifier 被移除或过期时</summary>
    public const string OnDestroy          = nameof(OnDestroy);
}
