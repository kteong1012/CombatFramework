namespace CombatFramework.Core;
/// <summary>CF 内部使用的资源槽与 stat 字符串 ID 常量。</summary>
public static class StatId
{
    // ── 资源槽 ──
    public const string HP          = "HP";
    public const string Energy      = "Energy";
    public const string Shield      = "Shield";
    public const string BlackShield = "BlackShield";

    // ── 复合属性分量 ──
    public const string HpBase  = "HpBase";
    public const string HpMult  = "HpMult";
    public const string HpAdd   = "HpAdd";
    public const string AtkBase = "AtkBase";
    public const string AtkMult = "AtkMult";
    public const string AtkAdd  = "AtkAdd";
    public const string DefBase = "DefBase";
    public const string DefMult = "DefMult";
    public const string DefAdd  = "DefAdd";

    // ── 复合属性最终名（兼容层，GetStat 查询用字面量） ──
    public const string Attack = "Attack";
    public const string Defense = "Defense";
    public const string HP_Final = "HP";

    // ── Flat stats ──
    public const string CritRate            = "CritRate";
    public const string CritDMG             = "CritDMG";
    public const string HealBonus           = "HealBonus";
    public const string HealRecv            = "HealRecv";
    public const string DamageInc           = "DamageInc";
    public const string DamageRed           = "DamageRed";
    public const string DefIgnore           = "DefIgnore";
    public const string DefIgnoreRate       = "DefIgnoreRate";
    public const string AbnormalAccrueMult  = "AbnormalAccrueMult";
    public const string AbnormalDamageMult  = "AbnormalDamageMult";
    public const string BreakAccrueMult     = "BreakAccrueMult";
    public const string BreakForce          = "BreakForce";
    public const string PowerAutoRecover    = "PowerAutoRecover";
    public const string PowerRecvMult       = "PowerRecvMult";
    public const string WalkSpeed           = "WalkSpeed";

    // ── HpMin（薄葬式生命下限）──
    public const string HpMin = "HpMin";

    // ── 韧性系统 ──
    public const string Toughness     = "Toughness";
    public const string ToughnessMax  = "ToughnessMax";
}

/// <summary>CF TagSystem 字符串 tag 常量。</summary>
public static class TagId
{
    public const string Stunned        = "stunned";
    public const string Dead           = "dead";
    public const string Silenced       = "silenced";
    public const string Invincible     = "invincible";
}
