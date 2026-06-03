using CombatFramework.Bridge;
using CombatFramework.Core.Ability.AbilityEvent;
using CombatFramework.Core.Model;
using CombatFramework.Unit;

namespace CombatFramework.Core.Ability;

public class AbilitySpec
{
    #region Variables
    public AbilityData data;
    public string Name => data?.Name;
    public UnitEntity Owner { get; set; }
    public int Level { get; set; } = 0;
    /// <summary>当前所在槽位下标（0-based）。由 UnitAbilitySlot.Equip/Replace 维护。</summary>
    public int SlotIndex { get; internal set; } = -1;
    #endregion

    #region Factory
    /// <summary>
    /// 创建 AbilitySpec 实例。若 data.Class 不为空，用反射实例化对应子类。
    /// </summary>
    public static AbilitySpec Create(AbilityData data)
    {
        AbilitySpec spec = null;

        if (!string.IsNullOrEmpty(data?.Class))
        {
            var types = CFBridge.Bridge.TypeProvider.GetTypes();
            foreach (var t in types)
            {
                if (t.Name == data.Class && typeof(AbilitySpec).IsAssignableFrom(t))
                {
                    spec = (AbilitySpec)Activator.CreateInstance(t);
                    break;
                }
            }
        }

        spec ??= new AbilitySpec();
        spec.data = data;
        return spec;
    }
    #endregion

    #region Public Methods
    public bool TryGetLevelValue(string name, out float value)
    {
        if (Level == 0)
        {
            value = 0;
            return false;
        }
        if (!data.AbilitySpecialFields.TryGetValue(name, out var levelValues))
        {
            value = 0;
            return false;
        }
        var index = Level - 1;
        if (index >= levelValues.Length)
        {
            value = levelValues[levelValues.Length - 1];
            return true;
        }
        else
        {
            value = levelValues[index];
            return true;
        }
    }
    #endregion

    #region Cast — 施放检查与资源扣除

    /// <summary>
    /// 检查是否可以施放：逐项对比 AbilityCosts 与 Owner 当前属性值。
    /// </summary>
    public bool CanCast(out string reason)
    {
        if (data?.AbilityCosts == null) { reason = null; return true; }
        foreach (var cost in data.AbilityCosts)
        {
            var required = cost.Value?.GetValue(this) ?? 0f;
            var have     = Owner?.GetStat(cost.Stat) ?? 0f;
            if (have < required)
            {
                reason = $"{cost.Stat}: need {required:F0}, have {have:F0}";
                return false;
            }
        }
        reason = null;
        return true;
    }

    /// <summary>
    /// 按 AbilityCosts 扣除 Owner 的属性值。应在 CanCast 通过后调用。
    /// </summary>
    public void DeductCosts()
    {
        if (data?.AbilityCosts == null) return;
        foreach (var cost in data.AbilityCosts)
        {
            var amount = cost.Value?.GetValue(this) ?? 0f;
            Owner?.Stats.Add(cost.Stat, -amount);
        }
    }

    #endregion

    #region Events — 虚方法，子类可 override；默认跑 AbilityData.AbilityEvents 里配的 Action 列表
    public virtual void OnAbilityPhaseStart(AbilityEventContext ctx) => DispatchEvent(AbilityEvents.OnAbilityPhaseStart, ctx);
    public virtual void OnSpellStart(AbilityEventContext ctx)        => DispatchEvent(AbilityEvents.OnSpellStart, ctx);
    public virtual void OnProjectileHitUnit(AbilityEventContext ctx) => DispatchEvent(AbilityEvents.OnProjectileHitUnit, ctx);
    public virtual void OnProjectileFinish(AbilityEventContext ctx)  => DispatchEvent(AbilityEvents.OnProjectileFinish, ctx);
    public virtual void OnChannelFinish(AbilityEventContext ctx)     => DispatchEvent(AbilityEvents.OnChannelFinish, ctx);
    public virtual void OnChannelInterrupted(AbilityEventContext ctx)=> DispatchEvent(AbilityEvents.OnChannelInterrupted, ctx);
    public virtual void OnToggleOn(AbilityEventContext ctx)          => DispatchEvent(AbilityEvents.OnToggleOn, ctx);
    public virtual void OnToggleOff(AbilityEventContext ctx)         => DispatchEvent(AbilityEvents.OnToggleOff, ctx);
    public virtual void OnUpgrade(AbilityEventContext ctx)           => DispatchEvent(AbilityEvents.OnUpgrade, ctx);

    /// <summary>技能被装备到 unit 上时触发（含被动技能）。</summary>
    public virtual void OnEquipped(UnitEntity owner)   => DispatchEvent(AbilityEvents.OnEquipped,   new AbilityEventContext { Ability = this, Caster = owner });
    /// <summary>技能从 unit 上卸下时触发。</summary>
    public virtual void OnUnequipped(UnitEntity owner) => DispatchEvent(AbilityEvents.OnUnequipped, new AbilityEventContext { Ability = this, Caster = owner });
    /// <summary>命中每个目标时由 ForEachHitAction 触发。</summary>
    public virtual void OnHitTarget(AbilityEventContext ctx)         => DispatchEvent(AbilityEvents.OnHitTarget, ctx);

    /// <summary>按事件名执行 AbilityData.AbilityEvents 中配置的 Action 列表。</summary>
    protected void DispatchEvent(string eventName, AbilityEventContext ctx)
    {
        if (data?.AbilityEvents == null) return;
        if (!data.AbilityEvents.TryGetValue(eventName, out var actions)) return;
        foreach (var actionData in actions)
        {
            var action = AbilityEventAction.Create(actionData);
            action?.Execute(ctx);
        }
    }
    #endregion
}
