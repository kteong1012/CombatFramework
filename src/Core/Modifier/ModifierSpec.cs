using CombatFramework.Bridge;
using CombatFramework.Core.Ability;
using CombatFramework.Core.Ability.AbilityEvent;
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

    #region Factory
    /// <summary>
    /// 创建 ModifierSpec 实例。若 data.Class 不为空，用反射实例化对应子类。
    /// </summary>
    public static ModifierSpec Create(ModifierData data, UnitEntity parent, UnitEntity caster, AbilitySpec sourceAbility)
    {
        ModifierSpec spec = null;

        if (!string.IsNullOrEmpty(data?.Class))
        {
            var types = CFBridge.Bridge.TypeProvider.GetTypes();
            foreach (var t in types)
            {
                if (t.Name == data.Class && typeof(ModifierSpec).IsAssignableFrom(t))
                {
                    spec = (ModifierSpec)Activator.CreateInstance(t, data, parent, caster, sourceAbility);
                    break;
                }
            }
        }

        return spec ?? new ModifierSpec(data, parent, caster, sourceAbility);
    }
    #endregion

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

    #region Events — 虚方法，子类可 override；默认跑 ModifierData.Events 里配的 Action 列表
    public virtual void OnCreated()
    {
        ApplyProperties();
        DispatchEvent(ModifierEvents.OnCreated);
    }

    /// <summary>modifier 过期或被主动移除时触发：撤销 Properties 并派发 OnDestroy 事件。</summary>
    public virtual void OnDestroy()
    {
        RemoveProperties();
        DispatchEvent(ModifierEvents.OnDestroy);
    }
    public virtual void OnIntervalThink() => DispatchEvent(ModifierEvents.OnIntervalThink);
    public virtual void OnAttackStart()   => DispatchEvent(ModifierEvents.OnAttackStart);
    public virtual void OnAttack()        => DispatchEvent(ModifierEvents.OnAttack);
    public virtual void OnAttackLanded()  => DispatchEvent(ModifierEvents.OnAttackLanded);
    public virtual void OnAttackFailed()  => DispatchEvent(ModifierEvents.OnAttackFailed);
    public virtual void OnAttacked()      => DispatchEvent(ModifierEvents.OnAttacked);
    public virtual void OnTakeDamage()    => DispatchEvent(ModifierEvents.OnTakeDamage);
    public virtual void OnDeath()         => DispatchEvent(ModifierEvents.OnDeath);
    public virtual void OnOrder()         => DispatchEvent(ModifierEvents.OnOrder);
    public virtual void OnUnitMoved()     => DispatchEvent(ModifierEvents.OnUnitMoved);

    /// <summary>按事件名执行 ModifierData.Events 中配置的 Action 列表。</summary>
    protected void DispatchEvent(string eventName)
    {
        if (Data?.Events == null) return;
        if (!Data.Events.TryGetValue(eventName, out var actions)) return;

        var ctx = new AbilityEventContext
        {
            Ability = SourceAbility,
            Caster  = Caster,
            Target  = Parent,
        };
        foreach (var actionData in actions)
        {
            var action = AbilityEventAction.Create(actionData);
            action?.Execute(ctx);
        }
    }
    #endregion

    private void Expire()
    {
        IsExpired = true;
        OnDestroy();
    }

    // ─── 属性应用/撤销 ────────────────────────────────────────

    private void ApplyProperties()
    {
        if (Data.Properties == null || Data.Properties.Count == 0) return;
        foreach (var entry in Data.Properties)
        {
            var value = entry.Value?.GetValue(SourceAbility) ?? 0f;
            if (entry.Op == StatOp.Add)
                Parent.Stats.Add(entry.Stat, value);
            // StatOp.Override 不可逆，跳过自动反向；子类可 override OnDestroy 处理
        }
    }

    private void RemoveProperties()
    {
        if (Data.Properties == null || Data.Properties.Count == 0) return;
        foreach (var entry in Data.Properties)
        {
            var value = entry.Value?.GetValue(SourceAbility) ?? 0f;
            if (entry.Op == StatOp.Add)
                Parent.Stats.Add(entry.Stat, -value);
        }
    }
}

