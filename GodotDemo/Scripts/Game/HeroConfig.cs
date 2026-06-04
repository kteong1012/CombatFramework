using System.Linq;
using CombatFramework.Unit;

/// <summary>
/// 角色配置门面 — 从 ConfigManager 读取数据，对外提供便捷 API。
/// </summary>
public static class HeroConfig
{
    public const int PlayerKey = 1001;
    public const int EnemyKey  = 2001;

    /// <summary>初始化时调用一次，加载所有配置。</summary>
    public static void Init()
    {
        ConfigManager.LoadAll<UnitConfig>();
        ConfigManager.LoadAll<ConstellationConfig>();
    }

    /// <summary>获取 UnitConfig。</summary>
    public static UnitConfig GetUnit(int key) => ConfigManager.Get<UnitConfig>(key);

    /// <summary>初始化单位属性（从配置写入 StatsManager）。</summary>
    public static void InitStats(UnitEntity unit, int configKey)
    {
        var cfg = GetUnit(configKey);
        if (cfg == null) return;

        unit.Stats.Set("Atk_Base", cfg.AtkBase);
        unit.Stats.Set("Energy", cfg.MaxEnergy);
        unit.Stats.Set("DefFinal", cfg.DefFinal);
        unit.Stats.Set("HP", cfg.Hp);

        if (cfg.ToughnessMax > 0f)
        {
            unit.Stats.Set("ToughnessMax", cfg.ToughnessMax);
            unit.Stats.Set("Toughness", cfg.ToughnessMax);
        }
    }

    /// <summary>获取角色的技能文件列表。</summary>
    public static string[] GetAbilities(int configKey)
        => GetUnit(configKey)?.Abilities ?? System.Array.Empty<string>();

    /// <summary>获取命座配置的 abilityFile 列表（按 config key 顺序）。</summary>
    public static string[] GetConstellationFiles(int heroKey)
    {
        var hero = GetUnit(heroKey);
        if (hero?.Constellations == null) return System.Array.Empty<string>();

        return hero.Constellations
            .Select(cKey => ConfigManager.Get<ConstellationConfig>(cKey)?.AbilityFile)
            .Where(f => f != null)
            .ToArray();
    }
}
