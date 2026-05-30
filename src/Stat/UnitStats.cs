namespace CombatFramework.Stat;

/// <summary>
/// 每个 unit 的属性容器。持有所有属性注册值和复合属性对象。
/// </summary>
public class UnitStats
{
    // 简单属性（直接用 float）
    private readonly Dictionary<string, float> _flatStats = new();

    // 复合属性（HP/ATK/DEF）
    public CompoundStat HP { get; } = new();
    public CompoundStat Attack { get; } = new();
    public CompoundStat Defense { get; } = new();

    // 元素伤害加成 / 元素抗性（key = Fire, Water, Lightning, ...）
    public Dictionary<string, float> ElementalDamageBonus { get; } = new();
    public Dictionary<string, float> ElementalResistance { get; } = new();

    public float GetFlat(string id) =>
        _flatStats.TryGetValue(id, out var val) ? val : 0f;

    public void SetFlat(string id, float value) => _flatStats[id] = value;
    public void AddFlat(string id, float value) =>
        _flatStats[id] = GetFlat(id) + value;

    public void ResetAll()
    {
        _flatStats.Clear();
        HP.SetBase(0); HP.AddExtra(-HP.Extra); HP.AddPercent(-HP.PercentBonus); HP.AddModifier(-HP.Modifier);
        Attack.SetBase(0); Attack.AddExtra(-Attack.Extra); Attack.AddPercent(-Attack.PercentBonus); Attack.AddModifier(-Attack.Modifier);
        Defense.SetBase(0); Defense.AddExtra(-Defense.Extra); Defense.AddPercent(-Defense.PercentBonus); Defense.AddModifier(-Defense.Modifier);
        ElementalDamageBonus.Clear();
        ElementalResistance.Clear();
    }
}
