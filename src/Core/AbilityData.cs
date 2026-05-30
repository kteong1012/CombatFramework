using MoonSharp.Interpreter;

namespace CombatFramework.Core;

/// <summary>
/// 能力模板——从 Lua 文件加载的只读数据，全局共享。
/// 每个 ability 类型只有一个 AbilityData，多个 AbilityInstance 共享引用。
/// </summary>
public class AbilityData
{
    public string Id { get; internal set; } = string.Empty;
    public string Name { get; internal set; } = string.Empty;

    // 冷却
    public float Cooldown { get; internal set; }

    // 充能：最大层数（默认 1 = 无充能）；每层恢复时间（默认 = Cooldown）
    public int MaxCharge { get; internal set; } = 1;
    public float RechargeTime { get; internal set; }

    // 资源消耗: { "Energy" = 30, "Rage" = 10 }
    public Dictionary<string, float> Costs { get; internal set; } = new();

    // 施法参数
    public float CastRange { get; internal set; }
    public float CastPoint { get; internal set; }
    public string CastAnimation { get; internal set; } = string.Empty;

    // Lua 函数引用（运行时调用）
    public Dictionary<string, Closure> EventHandlers { get; internal set; } = new();

    // Modifier 模板定义（来自 Lua Modifiers 表）
    public Dictionary<string, ModifierData> ModifierTemplates { get; internal set; } = new();

    // 能力参数（每级一个值，Lua 中可写单值或数组）
    public Dictionary<string, float[]> Parameters { get; internal set; } = new();

    // 投射物配置
    public ProjectileConfig? Projectile { get; internal set; }

    // 命名 Effect 表（来自 Lua Effects = { foo = { picker=..., ops={...} } }），
    // 由 Timeline clip 通过 (abilityId, effectKey) 调度。值是声明式数据，
    // 读取与执行解耦，便于 Editor 仅靠数据预览形状。
    public Dictionary<string, AbilityEffectData> Effects { get; internal set; } = new();
}

public class ProjectileConfig
{
    public string Model { get; set; } = string.Empty;
    public float Speed { get; set; }
    public float Radius { get; set; }
    public bool IsTracking { get; set; }
    public bool Dodgeable { get; set; }
    public bool VisibleToEnemies { get; set; }
}
