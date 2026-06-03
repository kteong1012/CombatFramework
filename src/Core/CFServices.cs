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
/// 盒形范围查询接口，由 Unity 层实现并注入 CFServices.ShapeQuery。
/// offset / eulerRotation / size 为相对于 center 的局部空间描述；
/// 测试端可忽略几何细节，直接返回所有已知单位。
/// </summary>
public interface IShapeQueryService
{
    IEnumerable<UnitEntity> QueryBox(
        Vector3 center, Vector3 offset, Vector3 eulerRotation, Vector3 size,
        TeamFilter teams, UnitEntity self);
}

/// <summary>
/// 框架全局服务注入点。游戏层启动时赋值，框架内部通过此类访问。
/// </summary>
public static class CFServices
{
    public static IUnitQueryService UnitQuery { get; set; }
    public static IShapeQueryService ShapeQuery { get; set; }
}
