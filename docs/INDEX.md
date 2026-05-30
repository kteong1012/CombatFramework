# CombatFramework 知识索引

所有 agent 的入口。主题 → 文档路径。

## 入门

| 主题 | 文件 | 说明 |
|------|------|------|
| 架构总览 | `architecture.md` | 全栈分层说明 |
| 编写 Lua 能力 | `guides/lua-ability.md` | 第一个能力教程 |
| 扩展框架 | `guides/extending.md` | 注册新类型/系统 |

## 核心概念

| 主题 | 文件 | 说明 |
|------|------|------|
| Modifier 系统 | `architecture.md` | 钩子轮询模式 |
| 复合属性公式 | `architecture.md` | Base + (Base×% + Extra) + Modifier |
| 伤害管线 | `reference/damage-pipeline.md` | 伤害/治疗流程 |
| 事件系统 | `reference/event-bus.md` | EventBus 全局广播 |
| ModifierHookType | `reference/modifier-hooks.md` | 全部钩子参考 |
| Effects 声明式数据 | `guides/lua-ability.md` | Effects 表 + ops |
| TagSystem | `architecture.md` | 扁平标签系统 |
| IVfxEffectService | `architecture.md` | VFX 桥接接口 |

## API 参考

| 主题 | 文件 | 关键词搜索 |
|------|------|-----------|
| 所有钩子枚举 | `reference/modifier-hooks.md` | `ModifierHookType` |
| 伤害事件数据 | `reference/damage-pipeline.md` | `DamageEventData` |
| 事件总线 | `reference/event-bus.md` | `EventBus` |
| 复合属性 | `architecture.md` | `CompoundStat` |
| 资源系统 | `architecture.md` | `ResourceSystem` |
| 状态系统 | `architecture.md` | `CheckState` |
| StatId 常量 | `architecture.md` | `StatId` |
| 单元测试索引 | `reference/unit-tests.md` | `tests/` 目录每个用例的守护行为 |

## 源码结构

| 路径 | 层 | 说明 |
|------|----|------|
| `src/Core/` | 核心 | AbilityData/AbilityEffectData/AbilityInstance/ModifierData/ModifierInstance/CFConstants/CFLog/IVfxEffectService/PreCastEventData/TagSystem |
| `src/Stat/` | 数值引擎 | CompoundStat/UnitStats/ResourceSystem/StatDefinition |
| `src/Unit/` | 实体 | UnitEntity/UnitAbilitySlot |
| `src/Modifier/` | Modifier | ModifierManager |
| `src/Event/` | 事件 | EventBus/DamageEvent |
| `src/Damage/` | 伤害 | DamagePipeline/IDamageFormula/DamageTypes |
| `src/Projectile/` | 投射物 | ProjectileManager/TrackingProjectile/LinearProjectile |
| `src/Lua/` | Lua 桥接 | LuaEngine/LuaAbilityLoader/LuaModifierBridge |

## 示例

| 文件 | 说明 |
|------|------|
| `examples/abilities/fireball.lua` | 完整火球能力（文档示例，非可运行用例） |
| `examples/abilities/test_frostbolt.lua` | 测试用冰弹（含 modifier，匹配实际桥接签名） |
| `tests/` | 全部 90+ 个测试用例 |

## 检索

```bash
# 关键词检索全部文档
python scripts/search_docs.py <关键词>
```

## 待办清单

| 主题 | 文件 |
|------|------|
| 已知问题 & 待办 | `docs/todo/remaining-issues.md` |
