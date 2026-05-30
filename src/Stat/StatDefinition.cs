namespace CombatFramework.Stat;

/// <summary>
/// 属性定义注册表。游戏启动时注册所有属性类型和元数据。
/// </summary>
public class StatDefinition
{
    public string Id { get; }
    public string Name { get; }
    public bool IsCompound { get; }  // 是否复合属性（需要 Base/Pct/Extra/Modifier 四分量）

    private StatDefinition(string id, string name, bool isCompound)
    {
        Id = id;
        Name = name;
        IsCompound = isCompound;
    }

    private static readonly Dictionary<string, StatDefinition> _registry = new();

    public static void Register(string id, string name, bool isCompound = false)
    {
        _registry[id] = new StatDefinition(id, name, isCompound);
    }

    public static StatDefinition? Get(string id) =>
        _registry.TryGetValue(id, out var def) ? def : null;

    public static IEnumerable<StatDefinition> All => _registry.Values;

    /// <summary>启动时注册默认属性</summary>
    public static void RegisterDefaults()
    {
        Register("ATK_Base", "基础攻击力");
        Register("ATK_Pct", "攻击力百分比");
        Register("ATK_Extra", "额外攻击力");
        Register("ATK", "攻击力", isCompound: true);

        Register("DEF_Base", "基础防御力");
        Register("DEF_Pct", "防御力百分比");
        Register("DEF_Extra", "额外防御力");
        Register("DEF", "防御力", isCompound: true);

        Register("HP_Base", "基础生命值");
        Register("HP_Pct", "生命值百分比");
        Register("HP_Extra", "额外生命值");
        Register("HP", "生命值", isCompound: true);

        Register("ElementalMastery", "属性精通");

        Register("CritRate", "暴击率");
        Register("CritDMG", "暴击伤害");
        Register("HealBonus", "治疗加成");
        Register("EnergyRecovery", "充能效率");
    }
}
