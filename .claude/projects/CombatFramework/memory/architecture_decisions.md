---
name: architecture-decisions
description: Key architecture decisions from the initial design session (May 2026), reverse-engineered from Pizzalol/SpellLibrary
metadata:
  type: project
---

# Architecture Decisions

These decisions were made in the initial design session (2026-05-27) while reverse-engineering the Dota 2 ability system from the SpellLibrary codebase.

## Language & Platform

- **Runtime**: C# (.NET standard class library), consumed as DLL in Unity
- **Scripting**: MoonSharp (pure C# Lua interpreter, no native deps, built-in sandbox for UGC safety)
- **Target game**: ARPG (not MOBA)
- **Why MoonSharp over XLua**: XLua tightly coupled to Unity type system/codegen. MoonSharp works in any .NET project.

## Systems to Drop (MOBA-specific)

- Three primary attributes (STR/AGI/INT) — replaced by custom stat system
- Level-up skill point allocation
- Gold / death penalty / buyback / creeps / towers / fountain / XP range

## Systems to Keep & Adapt

- Ability instance lifecycle (ability_inst -> per-unit slot)
- Modifier system (buff/debuff instances with hook registration)
- Event system (ability-level / modifier-level / global)
- Projectile system (tracking + linear)
- Damage pipeline (type/resistance/modifier hooks)

## Ability Configuration Format

Single Lua file per ability (improvement over SpellLibrary's KV + Lua split):
- `AbilityData` table for params (cooldown, cost, range, behavior flags)
- Event handler functions (`OnSpellStart`, `OnProjectileHit`, etc.)
- `Modifiers` table with embedded modifier definitions
- No separate KV files, no separate modifier files — everything in one .lua

## Modifier System

- Modifier registers hooks via `DeclareFunctions()` (like Dota 2's pattern)
- Engine POLLS modifiers for stat contributions (not modifiers pushing changes)
- Engine CALLS into modifiers for event notifications (OnTakeDamage, etc.)
- Each unit has a ModifierManager managing all its active modifier instances
- Support: None | Multiple | StackCount | Permanent stack behaviors
- Aura = modifier that auto-applies another modifier in radius

## Stat Aggregation

- All stats use: `Final = (Base + Add) * (1 + Mul)` pattern
- Stat system is isolated layer, game can swap formula implementation
- Damage types are extensible (framework: PHYSICAL/MAGICAL/PURE; game adds ELEMENTAL types)

## Resource System (Replacing Dota 2 Mana)

- Not hardcoded. Registered at startup per game project.
- `Unit.ResourceSystem` with named slots: Get/Set/Modify API
- Abilities reference resource by name: `costs = { Energy = 30 }`
- Supports multiple energy bars (ZZZ-style personal energy, HSR technique points, etc.)

## Entity Model

- OOP (not ECS)
- ModifierManager is the central orchestrator per unit
- Each ability instance is an independent entity on a unit slot
- EventBus for decoupled dispatch across layers

## UGC Safety

- MoonSharp sandbox (no os/io/package access)
- Per-ability-file Script isolation (separate Lua state per ability)
- Only whitelisted C# types exposed via UserData registration

## Event System

Three tiers:
1. Ability events (OnSpellStart, OnProjectileHit, OnOwnerDied)
2. Modifier events (OnTakeDamage, OnAttackLanded, OnKill)
3. Global events (EntitySpawned, EntityKilled, GameStateChanged)

Ordered chain dispatch with cancel support.

## Session 2 Decisions (2026-05-27)

### Entity Model → Option C (OOP + Internal Composition)

Chose composition over inheritance for ModifierInstance:
```csharp
class ModifierInstance {
    List<IModifierHook> hooks;         // From DeclareFunctions
    IStatContribution[] contributions; // Stat hooks parsed from Lua
    // shared fields: duration, stackCount, etc.
}
```
- **No resistance to Dota 2 mechanics** — Lua's `DeclareFunctions` + `GetModifierXxx` is already disguised composition. Each function is an independent hook that the engine dispatches by type. C# composition directly mirrors this.
- Lua still writes `function(self, params)` — same habit.

### External System → Combat Framework Boundary

Confirmed integration chain for relics/sets/constellations:
```
[External] Equip item → detect set → fire framework event
[Framework] EventBus → AbilityManager.EquipAbility
    → Add ModifierInstance (stat bonus) → StatAggregator auto recalc
    → Add ModifierInstance (set passive effect)
```
Framework does NOT know about "relics" or "sets" — only modifiers and equipment events.

### Event System → Hybrid (Both)

| Path | Mechanism | Used For |
|------|-----------|----------|
| Internal | Hard call chain | Ability → Projectile → Damage → ModifierManager performance-critical path |
| External | EventBus.Publish | EntityKilled, EntityHurt, AbilityUsed — for game systems (relic triggers, constellation checks) |

### Ability Loading Pipeline

```
.lua file → LuaAbilityLoader.Load() → AbilityData (shared template, cached)
    → AbilityManager.CreateInstance() → AbilityInstance (per-unit runtime state)
    → unit.EquipAbility() → slots[]
```
One .lua load = one template. N units with that ability = N instances sharing one template.

### Constellation System

- 6 levels, upgrade-only (no downgrade).
- `OnUpgrade` hook per level, can do anything (add modifier, modify ability params, unlock skills, register event hooks).
- Init by looping `SetConstellationLevel(6)` → `for i=1..6` → `OnUpgrade(hero, i)`.
- All constellation-applied modifiers tagged with `sourceTag = "constellation"` for clean re-init.

### Damage Pipeline (from Design Doc)

Four damage pipelines routed by skill config:
1. **Normal** — full flow (base dmg → DEF ratio → multiplier zones → crit → final)
2. **Abnormal** — skip crit, append `× (1 + ElementalMastery)` at end
3. **Break** — normal variant, steps 3/5/6/9 differ (for Toughness system)
4. **Heal** — independent pipeline

Crit determination is PER HIT (each `作用表id` × repeat count rolls independently).

### Element System (7 Types)

| Element | Identity | Abnormal Damage |
|---------|----------|----------------|
| Fire | Detonate buff stacks | Burn DOT |
| Water | Energy restore / cost reduction | Delayed damage |
| Lightning | Interrupt | Lightning strike |
| Wind | Combo (repeat skill) | On-hit trigger DOT |
| Earth | Shield / anti-shield | %HP damage |
| Light | Merge other elements | Additive damage |
| Dark | Devour / execute below threshold | Devour accumulation |

Each character belongs to one element. Damage always carries an element type (NONE reserved).

### Compound Attribute Formula (from Design Doc)

```
HP   = BaseHP   + (BaseHP   × HPPctBonus   + ExtraHP)   + ModifierHP
ATK  = BaseATK  + (BaseATK  × ATKPctBonus  + ExtraATK)  + ModifierATK
DEF  = BaseDEF  + (BaseDEF  × DEFPctBonus  + ExtraDEF)  + ModifierDEF
```

Parenthesized = "green text" (panel display). Modifier path for modifier-system adjustments.

Element stats:
```
ElementalDMG = Base + AllTypeBonus + Modifier
ElementalRES = Base + AllTypeRES + Modifier
```

Stat aggregation formula: `CharacterStats = LevelBase + Constellation + Skills + Weapon + Equipment + ...`

### Shield System

- Multiple shields stack total, each timed independently
- Damage deduction: shortest remaining duration first
- Individual shield expires or depletes independently

### State System (MODIFIER_STATE equivalents)

Framework built-in states, declared per modifier via `CheckState()`:
- 眩晕 (Stun) — cannot act
- 无敌 (Invincible) — immune to damage
- 隐身 (Invisible) — untargetable
- 嘲讽 (Taunt) — forced to attack caster
- 恐惧 (Fear) — uncontrollable flee
- 霸体 (SuperArmor) — immune to crowd control
- 禁锢 (Root) — cannot move, can still use skills

Composite state = engine merges CheckState() results from ALL active modifiers.

### Git Repo Initialized

- Repo initialized at `D:\Projects_D\CombatFramework`
- Documents converted to Chinese (CLAUDE.md, docs/architecture.md, example files)
- All architecture decisions recorded in memory

## Divergence Notes (2026-05-29: Actual vs Planned)

Several design decisions were changed during implementation:

### Damage Pipeline → Unified (not 4 pipelines)
The planned 4-pipeline (Normal/Abnormal/Break/Heal) was simplified to **one generic damage pipeline** + one heal pipeline. Abnormal/Break stats (`AbnormalAccrueMult`, `BreakAccrueMult` etc.) remain in `StatId` as placeholders. The `DefaultDamageFormula` only implements linear resistance + crit — there are no "multiplier zones" (damage inc/red/vuln/element).

### IModifierHook → Closure-based
Planned `List<IModifierHook>` was implemented as **`Dictionary<string, Closure> PropertyHooks`** — Lua closures are stored directly, no C# interface needed.

### StatAggregator → Inlined
The planned standalone `StatAggregator` class was merged into `ModifierManager.AggregateStat()` + `UnitEntity.GetStat()` + `UnitEntity.CompoundFinal()`. No separate file exists.

### Compound Stats → Three-component decomposition
Each of HP/ATK/DEF uses three sub-stats (Base/Mult/Add) with independent modifier aggregation via `ModifierManager.AggregateStat()`. Direct modification of "Attack" is blocked — all growth goes through components.

### CheckState Tags → Flat string tags
Planned Dota 2 style MODIFIER_STATE enums were replaced by **`TagSystem`** with flat string tags. `DeclareTags` auto-syncs to `UnitEntity.TagSystem`. Composite state checking uses `HasTag()` / `CheckState()`.

### Event Bus → Simplified
`AbilityEvent.cs` does not exist. Event data classes live in `DamageEvent.cs`. Events use `DynamicInvoke` (simpler than ordered dispatch with priority).

### IModifierHook interface → Not implemented
No `IModifierHook` interface or `ModifierHook.cs` file exists. All hook dispatch is handled by `ModifierData.PropertyHooks` (Dictionary<string, Closure>) and `ModifierManager.DispatchEvent()`.

