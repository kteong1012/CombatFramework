using System;
using System.Collections.Generic;
using CombatFramework.Core.Ability;
using CombatFramework.Unit;

/// <summary>
/// 命座管理器：解锁、查询、自动装备命座技能。
/// 解锁后标记 dirty，由外部在帧末调用 PostProcess 做技能决议。
/// </summary>
public class ConstellationManager
{
    private readonly UnitEntity _owner;
    private readonly string[] _fileNames;
    private readonly bool[] _unlocked;
    private readonly List<AbilitySpec> _specs = new();

    public bool[] Unlocked => _unlocked;
    public bool Dirty { get; private set; }

    /// <summary>角色级技能决议器。参数: (abilityKey, unlocked[]) → 最终 abilityKey。</summary>
    public Func<string, bool[], string> Resolver { get; set; }

    public ConstellationManager(UnitEntity owner, string[] fileNames)
    {
        _owner = owner;
        _fileNames = fileNames;
        _unlocked = new bool[fileNames.Length];

        foreach (var file in fileNames)
        {
            var data = AbilityLoader.Load(file);
            _specs.Add(AbilitySpec.Create(data));
        }
    }

    /// <summary>解锁指定命座（1-based index）。已解锁则忽略，标记 dirty。</summary>
    public bool Unlock(int index)
    {
        int i = index - 1;
        if (i < 0 || i >= _unlocked.Length || _unlocked[i])
            return false;

        _unlocked[i] = true;
        _owner.EquipAbility(_specs[i]);
        Dirty = true;
        return true;
    }

    /// <summary>检查命座是否已解锁。</summary>
    public bool IsUnlocked(int index) =>
        index >= 1 && index <= _unlocked.Length && _unlocked[index - 1];

    /// <summary>
    /// 后处理：对每个技能调用决议器，如有变化则 swap AbilitySpec。
    /// 调用时机：帧末或任意安全时机。
    /// </summary>
    public void PostProcess()
    {
        if (!Dirty || Resolver == null) return;
        Dirty = false;

        var keys = new List<string>(_owner.Abilities.Keys);
        foreach (var key in keys)
        {
            var resolved = Resolver(key, _unlocked);
            if (resolved == key) continue;

            // swap: 保持 key 不变，换 value
            _owner.UnequipAbility(key);
            var newSpec = AbilityLoader.Create(resolved + ".json");
            if (newSpec == null) continue;

            newSpec.Owner = _owner;
            _owner.Abilities[key] = newSpec;
            newSpec.OnEquipped(_owner);
        }
    }
}
