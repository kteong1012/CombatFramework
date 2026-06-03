using CombatFramework.Core.Ability;
using CombatFramework.Core.Executor.ValueGetter;
using CombatFramework.Core.Model;
using CombatFramework.Unit;

namespace CombatFramework.Core.Modifier;

/// <summary>
/// Modifier 运行时实例——附加在 unit 上的状态实体。
/// </summary>
public class ModifierSpec
{
    public ModifierData Data { get; }
    public string Name => Data.Name;

    public UnitEntity Parent { get; }
    public UnitEntity Caster { get; }
    public AbilitySpec SourceAbility { get; }

    public string SourceTag { get; set; }
    public int StackCount { get; private set; } = 1;
    public float RemainingTime { get; private set; }
    public bool IsExpired { get; private set; }

    public ModifierSpec(ModifierData data, UnitEntity parent, UnitEntity caster, AbilitySpec sourceAbility)
    {
        Data = data;
        Parent = parent;
        Caster = caster;
        SourceAbility = sourceAbility;

        if (data.DurationGetter != null)
            RemainingTime = data.DurationGetter.GetValue(null);
    }

    public void SetDuration(float duration) => RemainingTime = duration;

    public void SetDuration(IAbilityValueGetter getter)
    {
        if (getter != null)
            RemainingTime = getter.GetValue(null);
    }

    public void IncrementStack() => StackCount++;
    public void DecrementStack() => StackCount = Math.Max(0, StackCount - 1);

    public bool TryPurge()
    {
        if (!Data.IsPurgable) return false;
        Expire();
        return true;
    }

    /// <summary>逐帧推进，返回 true 表示应被销毁。</summary>
    public bool Update(float dt)
    {
        if (IsExpired) return true;

        // 只有显式配置了 DurationGetter 且非永久模式，才做时间衰减
        if (Data.StackMode != ModifierStackMode.Permanent && Data.DurationGetter != null)
        {
            RemainingTime -= dt;
            if (RemainingTime <= 0)
            {
                Expire();
                return true;
            }
        }

        return false;
    }

    private void Expire()
    {
        IsExpired = true;
    }
}
