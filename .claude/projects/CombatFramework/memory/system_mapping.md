---
name: system-mapping
description: Mapping between Dota 2 engine concepts and CombatFramework C# implementation
metadata:
  type: reference
---

# Dota 2 to CombatFramework System Mapping

## Core Entities

| Dota 2 Concept | CombatFramework Equivalent | Notes |
|---|---|---|
| `CDOTABaseAbility` (C++ entity) | `AbilityInstance` + `AbilityData` | Instance = runtime state; Data = loaded Lua template (shared) |
| `CDOTA_Buff` / modifier | `ModifierInstance` + per-unit `ModifierManager` | Hook registration via DeclareFunctions; closures instead of interfaces |
| Thinker entity (point-placed modifier) | `ModifierInstance` with no parent unit | Area-of-effect modifiers |
| ProjectileManager | `ProjectileManager` | Same concept: CreateLinear + CreateTracking |
| Unit entity | `UnitEntity` | Holds AbilitySlot[], ModifierManager, TagSystem, Stats, Resources |
| `ApplyDamage` | `DamagePipeline.ApplyDamage()` | **Positional args** (5 params), NOT table-based |
| `FindUnitsInRadius` | Not provided in CF | Must be implemented by game layer (Unity) |

## Dota 2 API Patterns -> C# equivalents

| Dota 2 Lua API | MoonSharp C# Binding |
|---|---|
| `ability:GetLevelSpecialValueFor("key", level)` | `ability:GetParameter("key")` — simplified, no level-based scaling |
| `ability:StartCooldown(duration)` | `ability:StartCooldown(seconds)` |
| `ability:EndCooldown()` | `ability:EndCooldown()` |
| `caster:AddNewModifier(caster, ability, name, {})` | `ModifierManager.Add(ModifierData, caster, ability, ...)` via C# only |
| `self:StartIntervalThink(interval)` | `modifier:StartIntervalThink(seconds)` |
| `DeclareFunctions()` returning table of constants | Same pattern in Lua; auto-discovered or explicit via DeclareFunctions |
| `ListenToGameEvent(name, callback, self)` | `EventBus.Subscribe(eventType, handler)` |
| `Timers:CreateTimer(delay, callback)` | Coroutine-based timer in MoonSharp or C# TimerManager |
| `ApplyDamage({victim=..., attacker=..., damage=...})` (table) | `ApplyDamage(victim, attacker, damage, damageType, ability?)` (positional) |
| `caster:PlayEffect(path)` | `PlayVfxOnUnit(path, unit, lifeTime?)` |
| `caster:GetPosition()` | `unit.Position` (Vector3 property, framework-managed) |

## MiHoYo ARPG Systems -> Modifier Mapping

| ARPG System | Dota 2 Analogue | Implementation |
|---|---|---|
| Relic/Artifact substats | Permanent stat bonus modifier | Apply permanent modifier with Properties / StatModifiers |
| Relic set bonus (2pc/4pc) | Conditional permanent modifier | Check equip count, SetStackCount, conditional hooks |
| Constellation (命座) | Permanent modifier + event hooks | Modifier registered via C# that checks rank on ability events |
| Elemental damage bonus | Custom damage type + stat | Register `FIRE_DMG = "fire"` in StatDefinition |
| Elemental resistance | Per-type resistance stat | Each damage type has a paired resistance stat |
| Personal energy bar | ResourceSystem named slot | `ResourceSystem.Register("Energy")`, no Mana slot used |
