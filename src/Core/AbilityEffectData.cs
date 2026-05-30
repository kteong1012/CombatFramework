using System.Collections.Generic;

namespace CombatFramework.Core;

public class AbilityEffectData
{
    public string Key { get; set; } = string.Empty;
    public TargetPickerData? Picker { get; set; }
    public List<AbilityEffectOp> Operations { get; } = new();
}

public class TargetPickerData
{
    public string Type { get; set; } = string.Empty;
    public string Filter { get; set; } = string.Empty;
    public ShapeData? Shape { get; set; }
    public string OriginAnchor { get; set; } = string.Empty;
    public float OffsetX { get; set; }
    public float OffsetY { get; set; }
    public float OffsetZ { get; set; }
}

public class ShapeData
{
    public string Type { get; set; } = string.Empty;
    public float Radius { get; set; }
    public float Height { get; set; }
    public float Angle { get; set; }
    public float OffsetX { get; set; }
    public float OffsetY { get; set; }
    public float OffsetZ { get; set; }
    public float RotateX { get; set; }
    public float RotateY { get; set; }
    public float RotateZ { get; set; }
    public float ScaleX { get; set; }
    public float ScaleY { get; set; }
    public float ScaleZ { get; set; }
}

public enum AbilityEffectOpType
{
    Damage,
    Heal,
    Modifier,
    Projectile,
    Thinker,
}

/// <summary>Damage/Heal 值——数字、%引用、或 Lua 自定义函数。</summary>
public class DamageValue
{
    /// <summary>Damage = 10</summary>
    public float? Constant { get; set; }
    /// <summary>Damage = "%dmg"</summary>
    public string? ParamRef { get; set; }
    /// <summary>Damage = { RunFunctionInAbility = "MyFunc" }</summary>
    public string? RunFunctionInAbility { get; set; }

}

public class AbilityEffectOp
{
    public AbilityEffectOpType Type { get; set; }
    public string Target { get; set; } = "TARGET";

    /// <summary>Damage/Heal 值（数字 / %引用 / RunFunctionInAbility）</summary>
    public DamageValue? Damage { get; set; }

    public string DamageType { get; set; } = string.Empty;
    public string ModifierName { get; set; } = string.Empty;
    public float Duration { get; set; }
    public int HitNum { get; set; } = 1;
    public AbilityProjectileConfig? Projectile { get; set; }
    public ThinkerConfig? Thinker { get; set; }
}

public class ThinkerConfig
{
    public float Delay { get; set; }
    public ShapeData? Shape { get; set; }
    public List<AbilityEffectOp> ChildOps { get; set; } = new();
}

public class AbilityProjectileConfig
{
    public string Model { get; set; } = string.Empty;
    public float Speed { get; set; }
    public float Radius { get; set; }
    public float Distance { get; set; }
    public bool DeleteOnHit { get; set; } = true;
    public bool ProvidesVision { get; set; }
    public float VisionRadius { get; set; }
}
