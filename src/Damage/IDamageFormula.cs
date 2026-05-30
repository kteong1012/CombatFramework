using CombatFramework.Unit;

namespace CombatFramework.Damage;

/// <summary>
/// 伤害公式接口。框架提供默认实现，游戏方可以替换。
/// </summary>
public interface IDamageFormula
{
    /// <summary>计算抗性减免系数 (0~1)，1 = 全免</summary>
    float CalculateResistanceReduction(float resistanceValue);

    /// <summary>计算暴击倍率</summary>
    float CalculateCriticalMultiplier(float critRate, float critDamage, out bool isCrit);
}

/// <summary>
/// 默认伤害公式实现。
/// </summary>
public class DefaultDamageFormula : IDamageFormula
{
    private static readonly Random _rng = new();

    public float CalculateResistanceReduction(float resistanceValue)
    {
        // 抗性 = 百分比值 / 100，线性
        var ratio = resistanceValue / 100f;
        return Math.Max(0f, Math.Min(0.9f, ratio));
    }

    public float CalculateCriticalMultiplier(float critRate, float critDamage, out bool isCrit)
    {
        isCrit = _rng.NextDouble() < critRate;
        return isCrit ? 1f + critDamage / 100f : 1f;
    }
}
