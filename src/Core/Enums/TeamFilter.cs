namespace CombatFramework.Core.Enums;

[Flags]
public enum TeamFilter
{
    Mate = 1,
    Enemy = 2,
    All = Mate | Enemy
}