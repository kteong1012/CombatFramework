using CombatFramework.Bridge;
using CombatFramework.Core.Ability.AbilityEvent;
using CombatFramework.Core.Enums;
using CombatFramework.Unit;
using System.Numerics;

namespace CombatFramework.Core;

/// <summary>
/// 目标选择器基类。ActionData 中的 Target 字段持有此类型，
/// 序列化时通过 ValueGetterAliasBinder 按短类名区分子类。
/// </summary>
public abstract class TargetSelector
{
    public abstract IEnumerable<UnitEntity> Resolve(AbilityEventContext context);
}

/// <summary>单目标：Owner / Caster / Target 之一。</summary>
public class SingleTargetSelector : TargetSelector
{
    public TargetType Type { get; set; } = TargetType.Target;

    public override IEnumerable<UnitEntity> Resolve(AbilityEventContext context)
    {
        var unit = Type switch
        {
            TargetType.Caster => context.Caster,
            TargetType.Owner  => context.Ability?.Owner,
            _                 => context.Target,
        };

        if (unit != null)
            yield return unit;
    }
}

/// <summary>范围目标：以 Center 为圆心，Radius 半径，按 Teams 过滤。</summary>
public class AreaTargetSelector : TargetSelector
{
    public TargetType Center { get; set; } = TargetType.Caster;
    public float Radius { get; set; }
    public TeamFilter Teams { get; set; } = TeamFilter.All;

    public override IEnumerable<UnitEntity> Resolve(AbilityEventContext context)
    {
        var service = CFBridge.Bridge.UnitQuery;
        if (service == null)
            return Enumerable.Empty<UnitEntity>();

        var origin = Center switch
        {
            TargetType.Target => context.Target?.Position ?? Vector3.Zero,
            TargetType.Owner  => context.Ability?.Owner?.Position ?? Vector3.Zero,
            _                 => context.Caster?.Position ?? Vector3.Zero,
        };

        var self = Center == TargetType.Target ? context.Target : context.Caster;
        return service.QueryUnitsInRadius(origin, Radius, Teams, self);
    }
}

/// <summary>盒形范围目标：以 Center 为原点，按 offset/eulerRotation/size 描述盒形，按 Teams 过滤。</summary>
public class BoxTargetSelector : TargetSelector
{
    public TargetType Center { get; set; } = TargetType.Caster;
    public Vector3 Offset { get; set; }
    public Vector3 EulerRotation { get; set; }
    public Vector3 Size { get; set; } = new Vector3(1f, 1f, 1f);
    public TeamFilter Teams { get; set; } = TeamFilter.All;

    public override IEnumerable<UnitEntity> Resolve(AbilityEventContext context)
    {
        var service = CFBridge.Bridge.ShapeQuery;
        if (service == null)
            return Enumerable.Empty<UnitEntity>();

        var origin = Center switch
        {
            TargetType.Target => context.Target?.Position ?? Vector3.Zero,
            TargetType.Owner  => context.Ability?.Owner?.Position ?? Vector3.Zero,
            _                 => context.Caster?.Position ?? Vector3.Zero,
        };

        var self = Center == TargetType.Target ? context.Target : context.Caster;
        return service.QueryBox(origin, Offset, EulerRotation, Size, Teams, self);
    }
}
