using CombatFramework.Core.Ability;
using CombatFramework.Core.Model;
using CombatFramework.Unit;

namespace CombatFramework.Core.Modifier;

/// <summary>
/// 每个 unit 的 Modifier 管理器。负责 ModifierSpec 的运行时管理：
/// 添加（延迟刷入）、移除、查询、逐帧 Tick。
/// </summary>
public class ModifierManager
{
    private readonly UnitEntity _owner;
    private readonly List<ModifierSpec> _modifiers = new();
    private readonly List<ModifierSpec> _pendingAdd = new();

    public ModifierManager(UnitEntity owner) => _owner = owner;

    // ─── 添加 ─────────────────────────────────────────────────

    public void Add(ModifierData data, UnitEntity caster, AbilitySpec sourceAbility)
    {
        if (data.StackMode == ModifierStackMode.Multiple)
        {
            var spec = ModifierSpec.Create(data, _owner, caster, sourceAbility);
            _pendingAdd.Add(spec);
            spec.OnCreated();
            return;
        }

        var existing = Find(data.Name);
        if (existing != null)
        {
            if (data.StackMode == ModifierStackMode.StackCount)
                existing.IncrementStack();
            else
            {
                existing.SetDuration(data.DurationGetter);
                existing.RefreshCaster(caster);
            }
            return;
        }

        var newSpec = ModifierSpec.Create(data, _owner, caster, sourceAbility);
        _pendingAdd.Add(newSpec);
        newSpec.OnCreated();
    }

    // ─── 移除 ─────────────────────────────────────────────────

    /// <summary>
    /// 按名移除一个实例。StackCount 模式下递减；降到 0 或其他模式则完全移除。
    /// </summary>
    public int RemoveByName(string name)
    {
        var target = Find(name);
        if (target == null) return 0;

        if (target.Data.StackMode == ModifierStackMode.StackCount && target.StackCount > 1)
        {
            target.DecrementStack();
            return 1;
        }

        target.OnDestroy();
        _pendingAdd.Remove(target);
        _modifiers.Remove(target);
        return 1;
    }

    /// <summary>移除所有来源标签匹配的 modifier。</summary>
    public void RemoveBySourceTag(string tag)
    {
        _modifiers.RemoveAll(m => m.SourceTag == tag);
        _pendingAdd.RemoveAll(m => m.SourceTag == tag);
    }

    /// <summary>死亡驱散：移除所有 modifier。</summary>
    public void PurgeAll()
    {
        for (int i = _modifiers.Count - 1; i >= 0; i--)
        {
            _modifiers[i].OnDeath();
            _modifiers[i].OnDestroy();
            _modifiers.RemoveAt(i);
        }
        _pendingAdd.Clear();
    }

    /// <summary>失活全部 modifier：停止特效和计时。死亡时调用。</summary>
    public void DeactivateAll()
    {
        foreach (var m in _modifiers) m.Deactivate();
        foreach (var m in _pendingAdd) m.Deactivate();
    }

    /// <summary>激活全部 modifier：恢复特效和计时。复活时调用。</summary>
    public void ActivateAll()
    {
        foreach (var m in _modifiers) m.Activate();
        foreach (var m in _pendingAdd) m.Activate();
    }

    // ─── 查询 ─────────────────────────────────────────────────

    public bool Has(string name) => Find(name) != null;

    /// <summary>查找第一个同名实例（含 pending）。</summary>
    public ModifierSpec Find(string name)
        => _modifiers.FirstOrDefault(m => m.Name == name)
        ?? _pendingAdd.FirstOrDefault(m => m.Name == name);

    // ─── 逐帧更新 ─────────────────────────────────────────────

    public void Update(float dt)
    {
        // 将 pending 刷入活跃列表
        foreach (var spec in _pendingAdd)
            _modifiers.Add(spec);
        _pendingAdd.Clear();

        // Tick，反向遍历安全删除
        for (int i = _modifiers.Count - 1; i >= 0; i--)
        {
            if (_modifiers[i].Update(dt))
                _modifiers.RemoveAt(i);
        }
    }
}
