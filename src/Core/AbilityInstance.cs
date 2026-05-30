namespace CombatFramework.Core;

/// <summary>
/// 能力运行时实例——每个 unit 槽位上的独立对象。
/// 持有共享的 AbilityData 引用 + 自身的可变状态。
/// 支持充能机制（MaxCharge > 1 时层数独立恢复）。
/// </summary>
public class AbilityInstance
{
    public string Name => Data.Name;
    public AbilityData Data { get; }
    public Unit.UnitEntity Owner { get; }

    public int Level { get; private set; } = 1;

    /// <summary>等级加成（命座/技能加成，modifier 叠加），GetParameter 使用 Level + BonusLevel 索引</summary>
    public int BonusLevel { get; private set; }
    /// <summary>有效等级 ≈ Level + BonusLevel，用于参数索引</summary>
    public int EffectiveLevel => Level + BonusLevel;

    /// <summary>叠加等级加成（可正可负，OnDestroy 时撤回需传入相反值）</summary>
    public void AddBonusLevel(int delta) => BonusLevel = Math.Max(0, BonusLevel + delta);

    // ── 冷却 ──
    public float CooldownRemaining { get; private set; }
    public bool IsOnCooldown => CooldownRemaining > 0;

    // ── 充能 ──
    public int MaxCharge => Data.MaxCharge;
    public int ChargeCount { get; private set; }
    /// <summary>距离下一层充能恢复的剩余时间（仅 ChargeCount < MaxCharge 时有效）</summary>
    public float RechargeRemaining { get; private set; }

    public AbilityInstance(AbilityData data, Unit.UnitEntity owner)
    {
        Data = data;
        Owner = owner;
        ChargeCount = data.MaxCharge;
        RechargeRemaining = data.RechargeTime > 0 ? data.RechargeTime : data.Cooldown;
    }

    /// <summary>使用一层充能。调用 StartCooldown 同步冷却计时。</summary>
    public void StartCooldown(float duration)
    {
        CooldownRemaining = duration;
        if (ChargeCount > 0) ChargeCount--;

        // 如果还有剩余充能，冷却结束后恢复的仍然是充能层数
        // 如果没有充能了（ChargeCount == 0），冷却结束后恢复一层
    }

    public void EndCooldown()
    {
        CooldownRemaining = 0;
        RechargeRemaining = Data.RechargeTime > 0 ? Data.RechargeTime : Data.Cooldown;
        if (ChargeCount < MaxCharge) ChargeCount++;
    }

    /// <summary>逐帧推进冷却与充能恢复。返回 true 表示有层数恢复（可用于触发回调）。</summary>
    public bool UpdateCooldown(float dt)
    {
        var gained = false;

        // 推进冷却时间
        if (CooldownRemaining > 0)
        {
            CooldownRemaining = Math.Max(0, CooldownRemaining - dt);
            if (CooldownRemaining <= 0)
            {
                // 冷却结束：恢复一层充能
                if (ChargeCount < MaxCharge)
                {
                    ChargeCount++;
                    gained = true;
                }
                RechargeRemaining = Data.RechargeTime > 0 ? Data.RechargeTime : Data.Cooldown;
            }
        }

        // 如果冷却结束且还有充能缺口，推进充能恢复
        if (CooldownRemaining <= 0 && ChargeCount < MaxCharge)
        {
            RechargeRemaining -= dt;
            if (RechargeRemaining <= 0)
            {
                ChargeCount++;
                gained = true;
                RechargeRemaining = Data.RechargeTime > 0 ? Data.RechargeTime : Data.Cooldown;
                // 如果还没满，继续下一层恢复
                if (ChargeCount < MaxCharge)
                    RechargeRemaining = Data.RechargeTime > 0 ? Data.RechargeTime : Data.Cooldown;
            }
        }

        return gained;
    }

    public void ReduceCooldown(float delta) => CooldownRemaining = Math.Max(0, CooldownRemaining - delta);

    public void SetLevel(int level) => Level = Math.Max(1, Math.Min(10, level));

    /// <summary>按 EffectiveLevel 索引读取能力参数。Level=1 → values[0]，越界时取最后一个值。</summary>
    public float GetParameter(string key)
    {
        if (!Data.Parameters.TryGetValue(key, out var arr) || arr == null || arr.Length == 0)
            return 0f;
        var idx = Math.Max(0, Math.Min(EffectiveLevel - 1, arr.Length - 1));
        return arr[idx];
    }

    // ── 预施法检查管线 ──

    /// <summary>检查是否可施法（只读，不消耗任何资源）。返回 false 说明被某种条件阻止。</summary>
    public bool CanCast(out string? reason)
    {
        reason = null;

        // 1. 状态检查
        if (Owner.CheckState("stunned")) { reason = "stunned"; return false; }
        if (Owner.CheckState("dead")) { reason = "dead"; return false; }
        if (Owner.CheckState("silenced")) { reason = "silenced"; return false; }

        // 2. 充能/冷却检查
        if (ChargeCount <= 0) { reason = "no_charge"; return false; }

        // 3. 资源检查（只读）
        foreach (var kv in Data.Costs)
        {
            if (Owner.Resources.GetCurrent(kv.Key) < kv.Value)
            {
                reason = $"insufficient_{kv.Key}";
                return false;
            }
        }

        // 4. Modifier 否决（OnAbilityPhaseStart 钩子）
        var phaseData = new PreCastEventData(this);
        if (Data.EventHandlers.TryGetValue("OnAbilityPhaseStart", out var onPhase))
        {
            try { onPhase.Call(this); }
            catch { /* log */ }
        }
        Owner.ModifierManager.DispatchEvent(ModifierHookType.OnAbilityPhaseStart, phaseData);
        if (phaseData.IsCancelled)
        {
            reason = phaseData.CancelReason ?? "modifier_veto";
            return false;
        }

        return true;
    }

    /// <summary>消耗施法资源。在正式施法点调用。</summary>
    public bool DeductCosts()
    {
        foreach (var kv in Data.Costs)
        {
            if (!Owner.Resources.TryConsume(kv.Key, kv.Value))
                return false;
        }
        return true;
    }
}
