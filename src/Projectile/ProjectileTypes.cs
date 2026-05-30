using System.Numerics;
using CombatFramework.Core;
using CombatFramework.Unit;

namespace CombatFramework.Projectile;

/// <summary>
/// 碰撞检测接口——由 Unity 游戏项目实现。
/// </summary>
public interface ICollisionService
{
    HitResult[] CheckHits(Vector3 position, float radius, TeamFlag targetTeam);
}

public struct HitResult
{
    public UnitEntity Unit { get; set; }
    public Vector3 Point { get; set; }
    public float Distance { get; set; }
}

/// <summary>
/// 投射物实例基类。
/// </summary>
public abstract class ProjectileInstance
{
    public Vector3 Position { get; set; }
    public UnitEntity Source { get; }
    public AbilityInstance? SourceAbility { get; }
    public ProjectileConfig Config { get; }
    public bool IsActive { get; protected set; } = true;

    protected ProjectileInstance(UnitEntity source, AbilityInstance? ability, ProjectileConfig config)
    {
        Source = source;
        SourceAbility = ability;
        Config = config;
    }

    public abstract void Update(float dt, ICollisionService collision);

    // 回调——由 Lua 能力脚本注册
    public Action<UnitEntity?, Vector3>? OnProjectileHit { get; set; }
    public Action<Vector3>? OnProjectileThink { get; set; }
}

/// <summary>
/// 追踪弹——锁定目标自动追踪。
/// </summary>
public class TrackingProjectile : ProjectileInstance
{
    public UnitEntity Target { get; }

    public TrackingProjectile(UnitEntity source, AbilityInstance? ability, ProjectileConfig config, UnitEntity target)
        : base(source, ability, config)
    {
        Target = target;
    }

    public override void Update(float dt, ICollisionService collision)
    {
        if (!IsActive || Target == null) return;

        // 飞向目标
        var dir = Vector3.Normalize(Target.Resources.GetCurrent("HP") > 0
            ? Target.Position - Position : Vector3.Zero);

        // 如果目标已死亡，命中当前位置
        if (dir == Vector3.Zero) { Hit(null, Position); return; }

        var move = dir * Config.Speed * dt;
        Position += move;

        OnProjectileThink?.Invoke(Position);

        // 碰撞检测
        var hits = collision.CheckHits(Position, Config.Radius, TeamFlag.Enemy);
        foreach (var hit in hits)
        {
            if (hit.Unit == Target)
            {
                Hit(Target, hit.Point);
                return;
            }
        }
    }

    private void Hit(UnitEntity? target, Vector3 point)
    {
        IsActive = false;
        OnProjectileHit?.Invoke(target, point);
    }
}

/// <summary>
/// 线性弹——沿方向直线飞行。
/// </summary>
public class LinearProjectile : ProjectileInstance
{
    public Vector3 Direction { get; }
    public float MaxDistance { get; }
    public float TraveledDistance { get; private set; }

    private readonly HashSet<UnitEntity> _hitTargets = new();

    public LinearProjectile(UnitEntity source, AbilityInstance? ability, ProjectileConfig config,
        Vector3 direction, float maxDistance, Vector3 startPosition)
        : base(source, ability, config)
    {
        Direction = direction;
        MaxDistance = maxDistance;
        Position = startPosition;
    }

    public override void Update(float dt, ICollisionService collision)
    {
        if (!IsActive) return;

        var move = Direction * Config.Speed * dt;
        Position += move;
        TraveledDistance += move.Length();

        OnProjectileThink?.Invoke(Position);

        // 碰撞检测
        var hits = collision.CheckHits(Position, Config.Radius, TeamFlag.Enemy);
        foreach (var hit in hits)
        {
            if (!_hitTargets.Contains(hit.Unit))
            {
                _hitTargets.Add(hit.Unit);
                OnProjectileHit?.Invoke(hit.Unit, hit.Point);
            }
        }

        // 到达最大距离
        if (TraveledDistance >= MaxDistance)
        {
            IsActive = false;
            OnProjectileHit?.Invoke(null, Position);
        }
    }
}
