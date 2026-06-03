namespace CombatFramework.Core.Model;

/// <summary>
/// 命座/被动技能对指定 Flag 技能的等级加成条目。
/// 挂在 <see cref="AbilityData.SkillBonusEntries"/> 中，由 <see cref="CombatFramework.Core.Ability.AbilitySpec.GetEffectiveLevel"/> 汇总。
/// </summary>
public class SkillBonusEntry
{
    /// <summary>目标技能必须含有此 Tag 才获得加成。</summary>
    public string TargetFlag { get; set; } = string.Empty;

    /// <summary>等级加成量（正整数）。</summary>
    public int LevelBonus { get; set; }
}
