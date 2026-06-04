using CombatFramework.Bridge;
using CombatFramework.Core;
using CombatFramework.Core.Ability.AbilityEvent;
using CombatFramework.Core.Enums;
using CombatFramework.Core.Model;
using System.Numerics;

/// <summary>
/// 对目标选择器返回的每个 unit 触发 ability.OnHitTarget。
/// OnHitTarget 的 context.Target 被替换为当前迭代的 unit，
/// 后续 Action（如 DamageAction）可在 OnHitTarget 事件中对单个目标生效。
/// </summary>
[AbilityEventAction(typeof(ForEachHitActionData))]
public class ForEachHitAction : AbilityEventAction
{
    public new ForEachHitActionData data => (ForEachHitActionData)base.data;

    public ForEachHitAction(ForEachHitActionData data) : base(data)
    {
    }

    public override void Execute(AbilityEventContext context)
    {
        // ── 范围预览：形状类选择器 → Bridge 通知游戏侧显示可视化 ──
        ShowAreaPreviewIfNeeded(data.Target, context);

        foreach (var target in data.Target.Resolve(context))
        {
            var hitCtx = new AbilityEventContext
            {
                Ability = context.Ability,
                Caster  = context.Caster,
                Target  = target,
            };
            context.Ability.OnHitTarget(hitCtx);
        }
    }

    private static void ShowAreaPreviewIfNeeded(TargetSelector selector, AbilityEventContext context)
    {
        var shapeQuery = CFBridge.Bridge.ShapeQuery;
        if (shapeQuery == null) { CFLog.Warning("[Preview] CFBridge.Bridge.ShapeQuery is null"); return; }
        if (selector == null)   { CFLog.Warning("[Preview] TargetSelector is null"); return; }

        switch (selector)
        {
            case BoxTargetSelector box:
                CFLog.Info($"[Preview] Box: offset=({box.Offset.X},{box.Offset.Y}) size=({box.Size.X},{box.Size.Y})");
                if (box.Size.X >= 5000f || box.Size.Y >= 5000f) { CFLog.Info("[Preview] skipped (fullscreen)"); return; }
                shapeQuery.ShowBoxPreview(
                    GetSelectorCenter(box.Center, context),
                    box.Offset, box.EulerRotation, box.Size);
                break;

            case AreaTargetSelector area:
                CFLog.Info($"[Preview] Circle: radius={area.Radius}");
                if (area.Radius >= 5000f) { CFLog.Info("[Preview] skipped (fullscreen)"); return; }
                shapeQuery.ShowCirclePreview(
                    GetSelectorCenter(area.Center, context), area.Radius);
                break;

            default:
                CFLog.Info($"[Preview] Unknown selector type: {selector.GetType().Name}");
                break;
        }
    }

    private static Vector3 GetSelectorCenter(TargetType centerType, AbilityEventContext context)
    {
        return centerType switch
        {
            TargetType.Target => context.Target?.Position ?? context.Caster?.Position ?? Vector3.Zero,
            TargetType.Owner  => context.Ability?.Owner?.Position ?? context.Caster?.Position ?? Vector3.Zero,
            _                 => context.Caster?.Position ?? Vector3.Zero,
        };
    }
}

public class ForEachHitActionData : AbilityEventActionData
{
    public TargetSelector Target;
}
