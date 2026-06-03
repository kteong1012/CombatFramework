using CombatFramework.Core.Ability;

namespace CombatFramework.Core.Model;

/// <summary>
/// 单条技能转换配置。
/// 按 <see cref="AbilityData.Transforms"/> 声明顺序检查，首个 <see cref="Condition"/> 为 true 的条目生效。
/// </summary>
public class AbilityTransformData
{
    /// <summary>
    /// 转换目标：引用 <see cref="AbilityData.SubAbilities"/> 中的 key。
    /// </summary>
    public string To { get; set; }

    /// <summary>
    /// 触发转换的条件。为 null 时视为无条件（可用作兜底回退）。
    /// </summary>
    public AbilityCondition Condition { get; set; }
}
