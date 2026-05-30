using MoonSharp.Interpreter;

namespace CombatFramework.Core;

/// <summary>
/// Modifier 模板——从 Lua Modifiers 表解析的只读定义。
/// </summary>
public class ModifierData
{
    public string Name { get; internal set; } = string.Empty;
    public float Duration { get; internal set; }
    public bool IsBuff { get; internal set; }
    public bool IsDebuff { get; internal set; }
    public bool IsHidden { get; internal set; }
    public bool IsPurgable { get; internal set; } = true;
    public ModifierAttribute Attribute { get; internal set; } = ModifierAttribute.None;

    // 声明式事件钩子列表（从 DeclareFunctions 解析，仅事件钩子）
    public List<ModifierHookType> DeclaredHooks { get; internal set; } = new();

    // 声明式 stat 关注列表（从 DeclareFunctions 解析，string 通用路由）
    public List<string> DeclaredStats { get; internal set; } = new();

    // 声明式标签列表（从 DeclareTags 解析，生命周期随 modifier 自动管理）
    public List<string> DeclareTags { get; internal set; } = new();

    // 生命周期 Lua 函数引用
    public Closure? OnCreatedFn { get; internal set; }
    public Closure? OnRefreshFn { get; internal set; }
    public Closure? OnDestroyFn { get; internal set; }
    public Closure? OnIntervalThinkFn { get; internal set; }
    public Closure? OnStackCountChangedFn { get; internal set; }

    // 属性钩子 Lua 函数引用（key = statId 或 event hook 名）
    public Dictionary<string, Closure> PropertyHooks { get; internal set; } = new();

    // 状态声明（CheckState 返回的 MODIFIER_STATE 映射）
    public Dictionary<string, bool> States { get; internal set; } = new();

    // 光环配置
    public AuraConfig? Aura { get; internal set; }

    /// <summary>duration 引用 ability 参数名（如 "damage_delay"），优先级高于 Duration 字段</summary>
    public string? DurationRef { get; internal set; }

    // 属性值映射（KV 风格的 Properties 快捷方式，等价 Add 操作）
    public Dictionary<string, float> Properties { get; internal set; } = new();

    /// <summary>Properties 中引用 ability 参数的条目（key=stat名, value=参数名）</summary>
    public Dictionary<string, string> PropertyRefs { get; internal set; } = new();

    // 结构化 stat modifier 列表（支持 Add/Sub/Mul/Div/Override）
    public List<StatModifierEntry> StatModifiers { get; internal set; } = new();

    // 特效资源路径及附着方式（Dota 风格，OnCreated 自动播放，OnDestroy 自动停止）
    public string? EffectName { get; internal set; }
    public string? EffectAttachType { get; internal set; }
}

public enum StatOp
{
    Add,
    Override,
}

public class StatModifierEntry
{
    public string Stat { get; set; } = string.Empty;
    public StatOp Op { get; set; } = StatOp.Add;
    public float Value { get; set; }
}

public class AuraConfig
{
    public float Radius { get; set; }
    public string TargetModifier { get; set; } = string.Empty;
    public TeamFilter SearchTeam { get; set; } = TeamFilter.Enemy;
    public UnitFilter SearchType { get; set; } = UnitFilter.All;

    /// <summary>运行时解析的子 modifier 数据（由 CreateAuraChild 设置）</summary>
    internal ModifierData? _childData;
}

public enum ModifierAttribute
{
    None,
    Multiple,
    StackCount,
    Permanent,
}

public enum ModifierHookType
{
    // ── 事件钩子（分发模式） ──
    OnTakeDamage,
    OnDealDamage,
    OnHealReceived,
    OnHealDealt,
    OnKill,
    OnDeath,
    OnAttackStart,
    OnAttackHit,
    OnAbilityPhaseStart,
    OnSpellStartCast,
    OnSpellEndCast,
    OnStackCountChanged,
}

public enum TeamFilter
{
    Friendly,
    Enemy,
    Both,
    None,
}

public enum UnitFilter
{
    Hero,
    Basic,
    All,
}
