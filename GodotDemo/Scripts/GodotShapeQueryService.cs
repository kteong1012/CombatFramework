using System;
using System.Collections.Generic;
using System.Numerics;
using CombatFramework.Core;
using CombatFramework.Core.Enums;
using CombatFramework.Unit;

/// <summary>
/// Godot 端盒形查询实现（2D XY 平面）。
/// 将目标坐标变换到盒的本地空间后做 AABB 判断。
/// <para>
/// 参数语义（与 BoxTargetSelector 一致）：
///   center        — 施法者世界坐标
///   offset        — 盒中心相对施法者的偏移（本地空间，先偏移再旋转）
///   eulerRotation — 盒的旋转，只取 Z 分量（度）
///   size          — 盒的全尺寸（非半长），X = 宽，Y = 高
/// </para>
/// </summary>
public class GodotShapeQueryService : IShapeQueryService
{
    private readonly IReadOnlyList<UnitEntity> _units;

    public GodotShapeQueryService(IEnumerable<UnitEntity> units)
    {
        _units = new List<UnitEntity>(units);
    }

    public IEnumerable<UnitEntity> QueryBox(
        Vector3 center, Vector3 offset, Vector3 eulerRotation, Vector3 size,
        TeamFilter teams, UnitEntity self)
    {
        // 盒中心世界坐标（只计算 XY）
        float angleRad = eulerRotation.Z * MathF.PI / 180f;
        float cos = MathF.Cos(angleRad);
        float sin = MathF.Sin(angleRad);

        // offset 旋转到世界空间
        float boxWorldX = center.X + cos * offset.X - sin * offset.Y;
        float boxWorldY = center.Y + sin * offset.X + cos * offset.Y;

        float halfW = size.X * 0.5f;
        float halfH = size.Y * 0.5f;

        foreach (var unit in _units)
        {
            if (unit == self) continue;
            if (!MatchTeam(unit, self, teams)) continue;

            // 目标相对盒中心的向量
            float dx = unit.Position.X - boxWorldX;
            float dy = unit.Position.Y - boxWorldY;

            // 逆旋转到盒本地空间
            float localX =  cos * dx + sin * dy;
            float localY = -sin * dx + cos * dy;

            if (MathF.Abs(localX) <= halfW && MathF.Abs(localY) <= halfH)
                yield return unit;
        }
    }

    private static bool MatchTeam(UnitEntity unit, UnitEntity self, TeamFilter filter)
    {
        if (filter == TeamFilter.All) return true;
        bool isEnemy = unit.Team != self?.Team;
        return filter == TeamFilter.Enemy ? isEnemy : !isEnemy;
    }
}

