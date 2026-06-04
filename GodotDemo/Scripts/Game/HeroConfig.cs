using CombatFramework.Unit;

/// <summary>
/// 角色配置：初始属性、技能列表、命座列表。
/// 纯数据，不依赖 Godot 节点。
/// </summary>
public static class HeroConfig
{
    public const float MaxHp = 1000f;
    public const float MaxEnergy = 100f;
    public const float AtkBase = 200f;
    public const float MoveSpeed = 200f;

    /// <summary>基础属性初始化。</summary>
    public static void InitStats(UnitEntity unit)
    {
        unit.Stats.Set("Atk_Base", AtkBase);
        unit.Stats.Set("Energy", MaxEnergy);
        unit.Stats.Set("DefFinal", 0f);
        unit.Stats.Set("HP", MaxHp);
    }

    /// <summary>主动/被动技能文件名列表（装备顺序即优先级）。</summary>
    public static readonly string[] AbilityFiles =
    {
        "test_passive_atk_bonus.json",  // 被动：+20% ATK
        "normal_attack_01.json",        // 普攻一段 ⇒ Z
        "skill_aoe.json",               // AOE 战技 ⇒ X
        "skill_charge.json",            // 充能 ⇒ C
        "normal_attack_02.json",        // 连击二段（Transform 按名查找）
        "normal_attack_03.json",        // 连击三段
        "normal_attack_01_ex.json",     // EX 一段
        "normal_attack_02_ex.json",     // EX 二段
        "normal_attack_03_ex.json",     // EX 三段
    };

    /// <summary>6 个命座的 JSON 文件名。</summary>
    public static readonly string[] ConstellationFiles =
    {
        "const_01_skill_enhance.json",   // C1：战技等级 +1
        "const_02_normal_enhance.json",  // C2：普攻等级 +2
        "const_03_break_master.json",    // C3：破韧增伤 +30%
        "const_04_energy_surge.json",    // C4：击杀回能
        "const_05_all_enhance.json",     // C5：全技能等级 +1
        "const_06_crit_master.json",     // C6：暴击暴伤提升
    };
}
