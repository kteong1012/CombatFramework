namespace CombatFramework.Damage;

/// <summary>
/// 多段命中计算结果。
/// </summary>
public readonly struct MultiHitResult
{
    public float TotalDamage { get; }
    public int CritCount { get; }
    public int NoCritCount { get; }
    public float BasePerHit { get; }
    public float CritDmgPerHit { get; }

    public MultiHitResult(float totalDamage, int critCount, int totalHits, float basePerHit, float critDmgPerHit)
    {
        TotalDamage = totalDamage;
        CritCount = critCount;
        NoCritCount = totalHits - critCount;
        BasePerHit = basePerHit;
        CritDmgPerHit = critDmgPerHit;
    }
}

/// <summary>
/// 多段命中分摊暴击辅助。
/// </summary>
public static class MultiHitHelper
{
    /// <summary>执行分段暴击计算，返回汇总结果。</summary>
    public static MultiHitResult Process(float totalDamage, int hitNum, float critRate, float critDmgStat,
        Func<double> randomProvider)
    {
        var basePerHit = totalDamage / Math.Max(1, hitNum);
        var critMult = 1f + critDmgStat / 100f;
        var critDmgPerHit = basePerHit * critMult;

        float final = 0f;
        int critCount = 0;
        for (int i = 0; i < hitNum; i++)
        {
            var dmg = basePerHit;
            if (randomProvider() < critRate)
            {
                dmg *= critMult;
                critCount++;
            }
            final += dmg;
        }

        return new MultiHitResult(final, critCount, hitNum, basePerHit, critDmgPerHit);
    }
}
