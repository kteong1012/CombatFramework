using System.IO;
using CombatFramework.Core.Ability;
using CombatFramework.Core.Model;
using Godot;
using Newtonsoft.Json;

/// <summary>
/// JSON 能力文件加载器。
/// </summary>
public static class AbilityLoader
{
    private static readonly string _abilitiesDir =
        ProjectSettings.GlobalizePath("res://Abilities");

    public static AbilityData Load(string fileName)
    {
        var path = Path.Combine(_abilitiesDir, fileName);
        return JsonConvert.DeserializeObject<AbilityData>(
            File.ReadAllText(path), AbilityJsonSettings.Instance);
    }

    /// <summary>加载并创建指定等级的 AbilitySpec。</summary>
    public static AbilitySpec Create(string fileName, int level = 1)
    {
        var data = Load(fileName);
        var spec = AbilitySpec.Create(data);
        for (int i = 0; i < level; i++)
            spec.LevelUp();
        return spec;
    }
}
