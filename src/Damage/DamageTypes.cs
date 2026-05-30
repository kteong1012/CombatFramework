namespace CombatFramework.Damage;

/// <summary>
/// 伤害类型注册表。框架内置元素在启动时注册，游戏可扩展。
/// </summary>
public static class DamageTypes
{
    public const string None = "NONE";
    public const string Fire = "FIRE";
    public const string Water = "WATER";
    public const string Lightning = "LIGHTNING";
    public const string Wind = "WIND";
    public const string Earth = "EARTH";
    public const string Light = "LIGHT";
    public const string Dark = "DARK";

    private static readonly HashSet<string> _registered = new();
    private static readonly Dictionary<string, string> _resistanceMap = new();
    private static readonly Dictionary<string, string> _vfxMap = new();
    private static bool _defaultsRegistered;

    public static void Register(string typeId, string resistanceStatId, string defaultVfx = "")
    {
        _registered.Add(typeId);
        _resistanceMap[typeId] = resistanceStatId;
        if (!string.IsNullOrEmpty(defaultVfx)) _vfxMap[typeId] = defaultVfx;
    }

    public static bool IsValid(string typeId) => _registered.Contains(typeId);
    public static string? GetResistanceStat(string typeId) =>
        _resistanceMap.TryGetValue(typeId, out var stat) ? stat : null;
    public static string? GetDefaultVfx(string typeId) =>
        _vfxMap.TryGetValue(typeId, out var vfx) ? vfx : null;

    public static void RegisterDefaults()
    {
        if (_defaultsRegistered) return;
        _defaultsRegistered = true;

        Register(None, "NoneRES");
        Register(Fire, "FireRES", "vfx/hit_fire");
        Register(Water, "WaterRES", "vfx/hit_water");
        Register(Lightning, "LightningRES", "vfx/hit_lightning");
        Register(Wind, "WindRES", "vfx/hit_wind");
        Register(Earth, "EarthRES", "vfx/hit_earth");
        Register(Light, "LightRES", "vfx/hit_light");
        Register(Dark, "DarkRES", "vfx/hit_dark");
    }
}
