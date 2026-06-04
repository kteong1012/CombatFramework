using System.Collections.Generic;
using CombatFramework.Core.Ability;
using CombatFramework.Unit;

/// <summary>
/// 命座管理器：解锁、查询、自动装备命座技能。
/// </summary>
public class ConstellationManager
{
    private readonly UnitEntity _owner;
    private readonly string[] _fileNames;
    private readonly bool[] _unlocked;
    private readonly List<AbilitySpec> _specs = new();

    public bool[] Unlocked => _unlocked;

    public ConstellationManager(UnitEntity owner, string[] fileNames)
    {
        _owner = owner;
        _fileNames = fileNames;
        _unlocked = new bool[fileNames.Length];

        // 预加载所有命座 AbilitySpec
        foreach (var file in fileNames)
        {
            var data = AbilityLoader.Load(file);
            _specs.Add(AbilitySpec.Create(data));
        }
    }

    /// <summary>解锁指定命座（1-based index）。已解锁则忽略。</summary>
    public bool Unlock(int index /* 1~6 */)
    {
        int i = index - 1;
        if (i < 0 || i >= _unlocked.Length || _unlocked[i])
            return false;

        _unlocked[i] = true;
        _owner.EquipAbility(_specs[i]);
        return true;
    }

    /// <summary>检查命座是否已解锁。</summary>
    public bool IsUnlocked(int index) =>
        index >= 1 && index <= _unlocked.Length && _unlocked[index - 1];
}
