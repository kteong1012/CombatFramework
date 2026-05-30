namespace CombatFramework.Core;

/// <summary>
/// 扁平的标签系统。支持两种来源：
/// 1. Modifier 声明的标签（通过 ModifierData.DeclareTags），由 ModifierManager 周期同步
/// 2. 外部直接添加（游戏层标签如队伍、星级等）
/// </summary>
public class TagSystem
{
    private readonly HashSet<string> _tags = new();

    /// <summary>标签变化事件：(tag, added=true/removed=false)</summary>
    public event Action<string, bool>? OnTagChanged;

    public bool HasTag(string tag) => _tags.Contains(tag);
    public bool HasAnyTag(params string[] tags) => tags.Any(_tags.Contains);
    public bool HasAllTags(params string[] tags) => tags.All(_tags.Contains);

    public void AddTag(string tag)
    {
        if (_tags.Add(tag))
            OnTagChanged?.Invoke(tag, true);
    }

    public void RemoveTag(string tag)
    {
        if (_tags.Remove(tag))
            OnTagChanged?.Invoke(tag, false);
    }

    public void Clear()
    {
        foreach (var tag in _tags.ToList())
            RemoveTag(tag);
    }

    /// <summary>批量添加（ModifierManager 同步用，不触发事件，由 caller 统一触发）</summary>
    internal void AddRange(IEnumerable<string> tags)
    {
        foreach (var t in tags) _tags.Add(t);
    }

    /// <summary>批量移除（ModifierManager 同步用）</summary>
    internal void RemoveRange(IEnumerable<string> tags)
    {
        foreach (var t in tags) _tags.Remove(t);
    }

    /// <summary>从一组标签集合快照同步，只触发 net change</summary>
    public void SyncFrom(HashSet<string> newTags)
    {
        var added = newTags.Except(_tags).ToList();
        var removed = _tags.Except(newTags).ToList();

        foreach (var t in added) _tags.Add(t);
        foreach (var t in removed) _tags.Remove(t);

        // 批量触发事件
        foreach (var t in added) OnTagChanged?.Invoke(t, true);
        foreach (var t in removed) OnTagChanged?.Invoke(t, false);
    }

    /// <summary>返回当前所有标签的快照（用于 ModifierManager 计算新并集）</summary>
    public HashSet<string> Snapshot() => new(_tags);
}
