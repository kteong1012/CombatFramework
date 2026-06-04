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
    public int Level { get; private set; } = 0;

    /// <summary>提升技能等级并触发 OnUpgrade 事件。</summary>
    public void LevelUp()
    {
        Level++;
        OnUpgrade(new AbilityEventContext { Ability = this, Caster = Owner });
    }
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
    /// <summary>
    /// 计算技能的有效等级：BaseLevel + 来自所有命座/被动槽 SkillBonusEntries 的汇总加成。
    /// 只在求值时汇总，不修改 <see cref="Level"/>，避免状态污染。
    /// </summary>
    public int GetEffectiveLevel()
    {
        int bonus = 0;
        if (Owner != null && data?.Tags != null && data.Tags.Count > 0)
        {
            foreach (var other in Owner.Abilities.Values)
            {
                if (other == this) continue;
                var entries = other.data?.SkillBonusEntries;
                if (entries == null) continue;
                foreach (var entry in entries)
                    if (data.Tags.Contains(entry.TargetFlag))
                        bonus += entry.LevelBonus;
            }
        }
        return Level + bonus;
    }

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
    /// 按顺序枚举所有条件满足的转换目标 spec。
    /// 由 <see cref="Unit.UnitEntity.TryCast(AbilitySpec,Unit.UnitEntity)"/> 逐个尝试施放。
    /// 转换目标通过 <see cref="AbilityTransformData.To"/> 指定技能名，
    /// 由 caster.GetAbilitySpecByName 在已装备技能中查找。
    /// </summary>
    public IEnumerable<AbilitySpec> GetMatchingTransforms(UnitEntity caster, UnitEntity target)
    {
        if (data?.Transforms == null) yield break;
        foreach (var t in data.Transforms)
        {
            if (t.Condition == null || t.Condition.Evaluate(this, caster, target))
            {
                var spec = caster.GetAbilitySpecByName(t.To);
                if (spec != null) yield return spec;
            }
        }
    }

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
