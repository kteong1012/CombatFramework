namespace CombatFramework.Stat;

/// <summary>
/// 泛化资源系统。取代 Dota 2 的硬编码 Mana。
/// 游戏启动时注册所需资源槽（HP / Energy / Rage 等）。
/// </summary>
public class ResourceSystem
{
    public class ResourceSlot
    {
        public string Id { get; }
        public float Current { get; private set; }
        public float Max { get; private set; }
        public float Min { get; }

        public ResourceSlot(string id, float current, float max, float min = 0)
        {
            Id = id; Current = current; Max = max; Min = min;
        }

        public void Set(float value) => Current = Math.Max(Min, Math.Min(Max, value));
        public void Modify(float delta) => Set(Current + delta);
        public void SetMax(float max) => Max = max;
        public float Ratio => Max > 0 ? Current / Max : 0;
    }

    private readonly Dictionary<string, ResourceSlot> _slots = new();

    /// <summary>事件：资源变化时触发（用于 UI 更新）</summary>
    public event Action<string, float, float>? OnResourceChanged; // slotId, oldValue, newValue

    public void Register(string id, float initial, float max, float min = 0)
    {
        _slots[id] = new ResourceSlot(id, initial, max, min);
    }

    public ResourceSlot? Get(string id) =>
        _slots.TryGetValue(id, out var slot) ? slot : null;

    public float GetCurrent(string id) => Get(id)?.Current ?? 0;
    public float GetMax(string id) => Get(id)?.Max ?? 0;

    public void Set(string id, float value)
    {
        var slot = Get(id);
        if (slot == null) return;
        var old = slot.Current;
        slot.Set(value);
        OnResourceChanged?.Invoke(id, old, slot.Current);
    }

    public bool TryConsume(string id, float amount)
    {
        var slot = Get(id);
        if (slot == null || slot.Current < amount) return false;
        var old = slot.Current;
        slot.Modify(-amount);
        OnResourceChanged?.Invoke(id, old, slot.Current);
        return true;
    }

    public void Restore(string id, float amount)
    {
        var slot = Get(id);
        if (slot == null) return;
        var old = slot.Current;
        slot.Modify(amount);
        OnResourceChanged?.Invoke(id, old, slot.Current);
    }

    /// <summary>
    /// 安全自伤：从某资源扣除 amount，但保留至少 minLeft 不被扣穿。
    /// 对应 Dota 的 ModifyHealth(lethal=false)，用于哈斯卡烧血等不希望致死的自我消耗。
    /// 不走伤害管线，不触发 OnTakeDamage、暴击、抗性、击杀事件。
    /// 返回实际扣除值。
    /// </summary>
    public float ConsumeSafe(string id, float amount, float minLeft = 1f)
    {
        var slot = Get(id);
        if (slot == null || amount <= 0) return 0f;
        var actual = Math.Max(0f, Math.Min(amount, slot.Current - minLeft));
        if (actual <= 0f) return 0f;
        var old = slot.Current;
        slot.Modify(-actual);
        OnResourceChanged?.Invoke(id, old, slot.Current);
        return actual;
    }
}
