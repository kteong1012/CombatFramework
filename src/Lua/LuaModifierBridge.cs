//using MoonSharp.Interpreter;
//using CombatFramework.Core;

//namespace CombatFramework.Lua;

///// <summary>
///// Lua Modifier 桥接——在 C# ModifierInstance 和 Lua 回调之间提供映射。
///// </summary>
//public static class LuaModifierBridge
//{
//    /// <summary>从 Lua 调用 ApplyDamage</summary>
//    public static void ApplyDamageLua(Unit.UnitEntity victim, Unit.UnitEntity attacker,
//        float damage, string damageType, Core.AbilityInstance? ability)
//    {
//        Damage.DamagePipeline.ApplyDamage(victim, attacker, damage, damageType, ability);
//    }

//    /// <summary>注册到 Lua 引擎的全局命名空间</summary>
//    public static void RegisterGlobals(Script script)
//    {
//        script.Globals["ApplyDamage"] = (Action<Unit.UnitEntity, Unit.UnitEntity, float, string, Core.AbilityInstance?>)ApplyDamageLua;
//        script.Globals["DAMAGE_TYPE_NONE"] = Damage.DamageTypes.None;
//        script.Globals["DAMAGE_TYPE_FIRE"] = Damage.DamageTypes.Fire;
//        script.Globals["DAMAGE_TYPE_WATER"] = Damage.DamageTypes.Water;
//        script.Globals["DAMAGE_TYPE_LIGHTNING"] = Damage.DamageTypes.Lightning;
//        script.Globals["DAMAGE_TYPE_WIND"] = Damage.DamageTypes.Wind;
//        script.Globals["DAMAGE_TYPE_EARTH"] = Damage.DamageTypes.Earth;
//        script.Globals["DAMAGE_TYPE_LIGHT"] = Damage.DamageTypes.Light;
//        script.Globals["DAMAGE_TYPE_DARK"] = Damage.DamageTypes.Dark;
//    }
//}
