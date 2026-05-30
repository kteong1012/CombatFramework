using System.Numerics;
using CombatFramework.Core;
using CombatFramework.Unit;

namespace CombatFramework.Projectile;

/// <summary>
/// 投射物管理器——用 C# 管理所有活跃投射物的生命周期。
/// </summary>
public class ProjectileManager
{
    private readonly List<ProjectileInstance> _active = new();
    private readonly List<ProjectileInstance> _pendingAdd = new();

    public ICollisionService? CollisionService { get; set; }

    public TrackingProjectile CreateTracking(UnitEntity source, AbilityInstance? ability,
        ProjectileConfig config, UnitEntity target)
    {
        var p = new TrackingProjectile(source, ability, config, target)
        {
            Position = source.Position,
        };
        _pendingAdd.Add(p);
        return p;
    }

    public LinearProjectile CreateLinear(UnitEntity source, AbilityInstance? ability,
        ProjectileConfig config, Vector3 direction, float distance)
    {
        var p = new LinearProjectile(source, ability, config,
            Vector3.Normalize(direction), distance, source.Position);
        _pendingAdd.Add(p);
        return p;
    }

    /// <summary>在指定位置创建线性投射物（用于 Thinker 弹片等场景）</summary>
    public LinearProjectile CreateLinearAt(UnitEntity source, AbilityInstance? ability,
        ProjectileConfig config, Vector3 direction, float distance, Vector3 startPosition)
    {
        var p = new LinearProjectile(source, ability, config,
            Vector3.Normalize(direction), distance, startPosition);
        _pendingAdd.Add(p);
        return p;
    }

    public void Update(float dt)
    {
        // 无论 CollisionService 是否 null，pending 都必须先刷入
        if (_pendingAdd.Count > 0)
        {
            _active.AddRange(_pendingAdd);
            _pendingAdd.Clear();
        }

        if (CollisionService == null) return;

        _active.RemoveAll(p =>
        {
            p.Update(dt, CollisionService);
            return !p.IsActive;
        });
    }

    public void Clear()
    {
        _active.Clear();
        _pendingAdd.Clear();
    }
}
