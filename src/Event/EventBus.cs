namespace CombatFramework.Event;

/// <summary>
/// 全局事件总线——供外部系统（圣遗物、套装、命座）监听战斗事件。
/// </summary>
public class EventBus
{
    private readonly Dictionary<string, List<Delegate>> _handlers = new();

    public static EventBus Global { get; } = new();

    public void Subscribe(string eventType, Action<object?> handler)
    {
        if (!_handlers.ContainsKey(eventType))
            _handlers[eventType] = new List<Delegate>();
        _handlers[eventType].Add(handler);
    }

    public void Unsubscribe(string eventType, Action<object?> handler)
    {
        if (_handlers.TryGetValue(eventType, out var list))
            list.Remove(handler);
    }

    public void Publish(string eventType, object data = null)
    {
        if (!_handlers.TryGetValue(eventType, out var list)) return;
        // 复制一份以防 handler 修改列表
        foreach (var handler in list.ToArray())
        {
            try { handler.DynamicInvoke(data); }
            catch { /* log */ }
        }
    }

    public void Clear() => _handlers.Clear();

    // 预定义事件类型常量
    public static class Events
    {
        // ── 基础事件 ──
        public const string StatChanged = "StatChanged";
        public const string EntitySpawned = "EntitySpawned";
        public const string EntityKilled = "EntityKilled";
        public const string EntityHurt = "EntityHurt";
        public const string AbilityUsed = "AbilityUsed";
        public const string AbilityEquipped = "AbilityEquipped";
        public const string AbilityUnequipped = "AbilityUnequipped";
        public const string ConstellationUpgrade = "ConstellationUpgrade";
        public const string HealApplied = "HealApplied";
        public const string OnDealDamage = "OnDealDamage";
        public const string OnTakeDamage = "OnTakeDamage";

        // ── 韧性系统 ──
        public const string ToughnessChanged = "ToughnessChanged";
        public const string ToughnessBreak = "ToughnessBreak";
    }
}
