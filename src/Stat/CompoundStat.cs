namespace CombatFramework.Stat;

/// <summary>
/// 复合属性：Base + (Base × PctBonus + Extra) + Modifier
/// </summary>
public class CompoundStat
{
    public float Base { get; set; }
    public float PercentBonus { get; set; }
    public float Extra { get; set; }
    public float Modifier { get; set; }

    /// <summary>面板绿字 = Base × PctBonus + Extra</summary>
    public float GreenText => Base * PercentBonus + Extra;
    public float Final => Base + GreenText + Modifier;

    public void SetBase(float value) => Base = value;
    public void AddPercent(float value) => PercentBonus += value;
    public void AddExtra(float value) => Extra += value;
    public void AddModifier(float value) => Modifier += value;
}
