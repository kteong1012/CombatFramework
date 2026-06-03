using CombatFramework.Core.Enums;
using CombatFramework.Unit;
using System.Numerics;

namespace CombatFramework.Core;

/// <summary>
/// 空间单位查询接口，由 Unity 层实现并注入 CFServices.UnitQuery。
/// </summary>
public interface IUnitQueryService
{
    IEnumerable<UnitEntity> QueryUnitsInRadius(
        Vector3 center, float radius, TeamFilter teams, UnitEntity self);
}

/// <summary>
/// 框架全局服务注入点。游戏层启动时赋值，框架内部通过此类访问。
/// </summary>
public static class CFServices
{
    public static IUnitQueryService UnitQuery { get; set; }
}
