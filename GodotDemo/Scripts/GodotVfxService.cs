using System;
using System.Collections.Generic;
using CombatFramework.Core;
using CombatFramework.Unit;

/// <summary>
/// Godot 端 VFX 服务。PlayOnUnit 设置 UnitNode 的 vfx 标记，StopOnUnit 清除。
/// UnitNode._Draw 根据标记绘制对应特效。
/// </summary>
public class GodotVfxService : IVfxEffectService
{
    private readonly Dictionary<UnitEntity, UnitNode> _nodeMap = new();

    public void Register(UnitEntity entity, UnitNode node) => _nodeMap[entity] = node;

    public void PlayOnUnit(string effectName, UnitEntity target)
    {
        if (_nodeMap.TryGetValue(target, out var node))
            node.AddVfx(effectName);
    }

    public void StopOnUnit(string effectName, UnitEntity target)
    {
        if (_nodeMap.TryGetValue(target, out var node))
            node.RemoveVfx(effectName);
    }
}
