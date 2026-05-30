using MoonSharp.Interpreter;

namespace CombatFramework.Core;

/// <summary>
/// Modifier 运行时实例——附加在 unit 上的状态实体。
/// 采用内部组合（Option C）：钩子存储为列表而非继承。
/// </summary>
public class ModifierInstance
{
    public string Name => Data.Name;
    public ModifierData Data { get; }
    public Unit.UnitEntity Parent { get; }
    public Unit.UnitEntity Caster { get; }
    public AbilityInstance? SourceAbility { get; }

    public float RemainingTime { get; private set; }
    public int StackCount { get; private set; } = 1;
    public bool IsExpired { get; private set; }

    /// <summary>来源标签，用于外部系统分组清除（如 "constellation"）</summary>
    public string? SourceTag { get; set; }

    /// <summary>VFX 实例句柄（由 VfxService.PlayOnUnit 返回）</summary>
    public int VfxHandle { get; set; }

    // 缓存的 Lua 执行上下文
    private Closure? OnIntervalThinkFn;
    private float _intervalTickRemaining;
    private float _intervalRate;

    public ModifierInstance(ModifierData data, Unit.UnitEntity parent, Unit.UnitEntity caster,
        AbilityInstance? sourceAbility)
    {
        Data = data;
        Parent = parent;
        Caster = caster;
        SourceAbility = sourceAbility;
        OnIntervalThinkFn = data.OnIntervalThinkFn;

        // 解析 duration 引用
        if (data.DurationRef != null && sourceAbility != null)
            RemainingTime = sourceAbility.GetParameter(data.DurationRef);
        else
            RemainingTime = data.Duration;
    }

    public void SetDuration(float duration) => RemainingTime = duration;
    public void SetStackCount(int count) => StackCount = Math.Max(0, count);
    public void IncrementStack() => StackCount++;
    public void DecrementStack() => StackCount = Math.Max(0, StackCount - 1);

    public void StartIntervalThink(float interval)
    {
        _intervalRate = interval;
        _intervalTickRemaining = interval;
    }

    /// <summary>逐帧推进，返回 true 表示 modifier 应被销毁</summary>
    public bool Update(float dt)
    {
        if (IsExpired) return true;

        // 非永久 modifier 减少剩余时间
        if (Data.Attribute != ModifierAttribute.Permanent)
        {
            RemainingTime -= dt;
            if (RemainingTime <= 0)
            {
                Expire();
                return true;
            }
        }

        // 周期性回调
        if (OnIntervalThinkFn != null && _intervalRate > 0)
        {
            _intervalTickRemaining -= dt;
            if (_intervalTickRemaining <= 0)
            {
                _intervalTickRemaining = _intervalRate;
                try { OnIntervalThinkFn.Call(this); }
                catch { /* log */ }
            }
        }

        return false;
    }

    private void Expire()
    {
        IsExpired = true;
        try { Data.OnDestroyFn?.Call(this); }
        catch { /* log */ }
    }

    /// <summary>驱散判定</summary>
    public bool TryPurge()
    {
        if (!Data.IsPurgable) return false;
        Expire();
        return true;
    }
}
