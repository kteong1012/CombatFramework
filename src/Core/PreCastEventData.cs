namespace CombatFramework.Core;

/// <summary>
/// OnAbilityPhaseStart 事件数据。Modifier 可通过设置 IsCancelled=true 阻止施法。
/// </summary>
public class PreCastEventData
{
    public AbilityInstance Ability { get; }
    public bool IsCancelled { get; private set; }
    public string? CancelReason { get; private set; }

    public PreCastEventData(AbilityInstance ability)
    {
        Ability = ability;
    }

    public void Cancel(string? reason = null)
    {
        IsCancelled = true;
        CancelReason = reason;
    }
}
