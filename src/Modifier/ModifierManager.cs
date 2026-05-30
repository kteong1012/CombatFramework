using System.Numerics;
using CombatFramework.Core;
using CombatFramework.Damage;
using CombatFramework.Unit;

namespace CombatFramework.Modifier;

/// <summary>
/// 每个 unit 的 Modifier 管理器。
/// 持有所有活跃 modifier、处理生命周期、提供轮询/事件分发接口。
/// </summary>
public class ModifierManager
{
    private readonly List<ModifierInstance> _modifiers = new();
    private readonly List<ModifierInstance> _pendingAdd = new();
    private readonly Unit.UnitEntity _owner;

    public IReadOnlyList<ModifierInstance> All => _modifiers;

    public ModifierManager(Unit.UnitEntity owner) => _owner = owner;

    public ModifierInstance? Add(ModifierData data, Unit.UnitEntity caster,
        AbilityInstance? sourceAbility, float? durationOverride = null,
        string? sourceTag = null)
    {
        // 非 Multiple 模式：移出现有同名 modifier
        if (data.Attribute != ModifierAttribute.Multiple)
        {
            var existing = _modifiers.FirstOrDefault(m => m.Name == data.Name);
            if (existing != null)
            {
                if (data.Attribute == ModifierAttribute.StackCount)
                {
                    existing.IncrementStack();
                    data.OnStackCountChangedFn?.Call(existing);
                    return existing;
                }

                // 刷新
                if (durationOverride.HasValue)
                    existing.SetDuration(durationOverride.Value);
                else
                    existing.SetDuration(data.Duration);
                data.OnRefreshFn?.Call(existing);
                return existing;
            }
        }

        var instance = new ModifierInstance(data, _owner, caster, sourceAbility);
        if (durationOverride.HasValue) instance.SetDuration(durationOverride.Value);
        if (sourceTag != null) instance.SourceTag = sourceTag;

        // 延迟添加，避免在迭代时修改集合
        _pendingAdd.Add(instance);

        // 调用 OnCreated
        data.OnCreatedFn?.Call(instance);

        return instance;
    }

    /// <summary>按来源标签移除所有 modifier（用于重刷命座等）</summary>
    public int RemoveBySourceTag(string tag)
    {
        var count = _modifiers.RemoveAll(m => m.SourceTag == tag);
        if (count > 0) SyncTags();
        return count;
    }

    /// <summary>驱散所有可驱散的 modifier</summary>
    public int Purge(bool purgePositive, bool purgeNegative)
    {
        var count = _modifiers.RemoveAll(m =>
        {
            if (purgePositive && m.Data.IsBuff) return m.TryPurge();
            if (purgeNegative && m.Data.IsDebuff) return m.TryPurge();
            return false;
        });
        if (count > 0) SyncTags();
        return count;
    }

    public void Update(float dt)
    {
        // 处理待添加（含 VFX 启动）
        if (_pendingAdd.Count > 0)
        {
            foreach (var inst in _pendingAdd)
            {
                PlayVfx(inst);
            }
            _modifiers.AddRange(_pendingAdd);
            _pendingAdd.Clear();
            _dirty = true;
        }

        // 更新并移除过期的（含 VFX 停止）
        var removed = _modifiers.RemoveAll(m =>
        {
            var expired = m.Update(dt);
            if (expired) StopVfx(m);
            return expired;
        });
        if (removed > 0)
            _dirty = true;

        // 标签同步
        if (_dirty)
        {
            SyncTags();
            _dirty = false;
        }

        // 光环扫描
        TickAuras(dt);
    }

    // ── 光环系统 ──
    private float _auraTickTimer;
    private const float AuraTickInterval = 0.5f;

    private readonly Dictionary<string, HashSet<UnitEntity>> _auraTargetCache = new();

    /// <summary>设置光环的子 modifier 数据（由游戏方在 Lua 加载后调用）</summary>
    public static void BindAuraChild(ModifierData auraMod, ModifierData childMod)
    {
        if (auraMod?.Aura == null) return;
        auraMod.Aura._childData = childMod;
    }

    /// <summary>光环单位收集委托（由游戏方注入，返回指定阵营、XZ 平面距离内的目标单位）</summary>
    public static Func<Vector3, float, TeamFlag, IEnumerable<UnitEntity>>? AuraTargetCollector { get; set; }

    private void TickAuras(float dt)
    {
        if (AuraTargetCollector == null) return;

        _auraTickTimer -= dt;
        if (_auraTickTimer > 0) return;
        _auraTickTimer = AuraTickInterval;

        foreach (var mod in _modifiers)
        {
            var aura = mod.Data.Aura;
            if (aura == null || aura._childData == null) continue;
            if (mod.IsExpired) continue;

            var inRange = new HashSet<UnitEntity>();
            foreach (var unit in AuraTargetCollector(_owner.Position, aura.Radius, (TeamFlag)aura.SearchTeam))
            {
                inRange.Add(unit);

                var hasAura = false;
                foreach (var m in unit.ModifierManager.All)
                {
                    if (m.Name == aura.TargetModifier && m.SourceTag == mod.Name)
                    {
                        hasAura = true;
                        break;
                    }
                }

                if (!hasAura)
                {
                    unit.ModifierManager.Add(aura._childData, _owner, mod.SourceAbility, sourceTag: mod.Name);
                    unit.ModifierManager.Update(0); // 立即 flush 目标上的 pending
                }
            }

            var cacheKey = $"{_owner.Id}:{mod.Name}";
            if (_auraTargetCache.TryGetValue(cacheKey, out var prevSet))
            {
                foreach (var prev in prevSet)
                {
                    if (!inRange.Contains(prev) && prev != null)
                        prev.ModifierManager.RemoveBySourceTag(mod.Name);
                }
            }
            _auraTargetCache[cacheKey] = inRange;
        }
    }

    private bool _dirty;

    private void PlayVfx(ModifierInstance inst)
    {
        var vfx = Damage.DamagePipeline.VfxService;
        if (vfx == null || string.IsNullOrEmpty(inst.Data.EffectName)) return;
        inst.VfxHandle = vfx.PlayOnUnit(inst.Data.EffectName, _owner, lifeTime: inst.Data.Duration > 0 ? inst.Data.Duration + 1f : 5f);
    }

    private void StopVfx(ModifierInstance inst)
    {
        if (inst.VfxHandle == 0) return;
        var vfx = Damage.DamagePipeline.VfxService;
        if (vfx != null) vfx.Stop(inst.VfxHandle);
        inst.VfxHandle = 0;
    }

    /// <summary>计算当前所有活跃 modifier 的 DeclareTags 并集</summary>
    private HashSet<string> RecalculateTags()
    {
        var union = new HashSet<string>();
        foreach (var mod in _modifiers)
        {
            foreach (var tag in mod.Data.DeclareTags)
                union.Add(tag);
        }
        return union;
    }

    /// <summary>将并集同步到 UnitEntity 的 TagSystem</summary>
    private void SyncTags()
    {
        var union = RecalculateTags();
        _owner.Tags.SyncFrom(union);
    }

    /// <summary>收集所有 modifier 对某 stat 的 Add 型贡献值（兼容旧调用）。</summary>
    public float CollectStatValue(string statId, object? context = null) => AggregateStat(statId);

    /// <summary>
    /// 聚合某 stat 在所有活跃 modifier 上的修饰，并以指定 base 为起点计算。
    /// 顺序：base → Σ(Add: Properties / PropertyHooks / StatModifiers) → Override（若有则直接返回）。
    /// 乘性成长走 CompoundStat 的 PercentBonus 或专用乘性 stat；这里只保留线性叠加。
    /// </summary>
    public float AggregateStat(string statId, float baseValue = 0f)
    {
        float addSum = 0f;
        bool hasOverride = false;
        float overrideValue = 0f;

        foreach (var mod in _modifiers)
        {
            // 1. PropertyHooks（Lua 函数）：等价 Add
            if (mod.Data.PropertyHooks.TryGetValue(statId, out var fn))
            {
                try { addSum += (float)fn.Call(mod, null).Number; }
                catch { /* log */ }
            }
            // 2. Properties 快捷写法：等价 Add，与 PropertyHooks 互斥（同名以 hook 优先）
            else if (mod.Data.Properties.TryGetValue(statId, out var val))
            {
                addSum += val;
            }
            // 2b. PropertyRefs（%引用 ability 参数，运行时按等级解析）
            else if (mod.Data.PropertyRefs.TryGetValue(statId, out var refName) && mod.SourceAbility != null)
            {
                addSum += mod.SourceAbility.GetParameter(refName);
            }

            // 3. StatModifiers 结构化条目
            foreach (var entry in mod.Data.StatModifiers)
            {
                if (entry.Stat != statId) continue;
                switch (entry.Op)
                {
                    case StatOp.Add:
                        addSum += entry.Value;
                        break;
                    case StatOp.Override:
                        hasOverride = true;
                        overrideValue = entry.Value;
                        break;
                }
            }
        }

        if (hasOverride) return overrideValue;
        return baseValue + addSum;
    }

    /// <summary>
    /// 收集所有 modifier 对某 stat 的最大值（用于 HpMin 这类"取最强"语义）。
    /// 没有任何 modifier 提供该 stat 时返回 null。
    /// </summary>
    public float? CollectMax(string statId)
    {
        bool any = false;
        float best = 0f;
        foreach (var mod in _modifiers)
        {
            if (!mod.Data.Properties.TryGetValue(statId, out var val)) continue;
            if (!any || val > best) { best = val; any = true; }
        }
        return any ? best : null;
    }

    /// <summary>分发事件到所有声明了该事件钩子的 modifier</summary>
    public void DispatchEvent(ModifierHookType hookType, object eventData)
    {
        foreach (var mod in _modifiers)
        {
            if (mod.Data.DeclaredHooks.Contains(hookType))
            {
                if (mod.Data.PropertyHooks.TryGetValue(hookType.ToString(), out var fn))
                {
                    try { fn.Call(mod, eventData); }
                    catch { /* log */ }
                }
            }
        }
    }
}
