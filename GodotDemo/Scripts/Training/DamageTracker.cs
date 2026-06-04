using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 伤害统计追踪器 — DPS、总伤、最高单段、暴击次数。
/// 通过 BattleScene 在 EntityHurt 事件后调用 Record()。
/// </summary>
public class DamageTracker
{
    private readonly Queue<(float time, float dmg)> _recent = new();
    private const float DpsWindow = 5f;    // DPS 统计窗口（秒）
    private float _sessionTime;

    public float TotalDamage { get; private set; }
    public float HighestHit  { get; private set; }
    public int   CritCount   { get; private set; }
    public int   HitCount    { get; private set; }

    /// <summary>当前 DPS（最近 5 秒均伤）。</summary>
    public float Dps
    {
        get
        {
            PruneOld(_sessionTime);
            if (_recent.Count == 0) return 0f;
            return _recent.Sum(x => x.dmg) / DpsWindow;
        }
    }

    /// <summary>记录一次伤害。</summary>
    public void Record(float damage, bool isCrit)
    {
        TotalDamage += damage;
        HitCount++;
        if (damage > HighestHit) HighestHit = damage;
        if (isCrit) CritCount++;
        _recent.Enqueue((_sessionTime, damage));
        PruneOld(_sessionTime);
    }

    /// <summary>每帧调用以推进时间和清理过期数据。</summary>
    public void Update(float dt)
    {
        _sessionTime += dt;
        PruneOld(_sessionTime);
    }

    public void Reset()
    {
        TotalDamage = 0;
        HighestHit = 0;
        CritCount = 0;
        HitCount = 0;
        _sessionTime = 0;
        _recent.Clear();
    }

    private void PruneOld(float now)
    {
        while (_recent.Count > 0 && _recent.Peek().time < now - DpsWindow)
            _recent.Dequeue();
    }
}
