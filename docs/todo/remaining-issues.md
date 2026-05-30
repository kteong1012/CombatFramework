# CombatFramework 待办 & 已知问题

## P0 — 编译/运行断链 ✅

- [x] `LuaModifierBridge.RegisterGlobals` 未被调用 — `LuaEngine` 构造时已调
- [x] `AbilityData.MaxCharge` / `RechargeTime` 未解析 — `ParseAbilityFromEngine` 已读取
- [x] LuaEngine 生命周期重构：两阶段加载（缓存文本 → 战斗期 `BindOnEngine` 共享 Engine 提取 Closure）

## P1 — 缺少核心机制

### Thinker / 延迟放置判定物 ✅
- `AbilityEffectOpType.Thinker` + `ThinkerConfig`（Delay, Shape, ChildOps）
- 位置复用 `ResolvePickerOrigin` 逻辑
- 延迟后在记录位置做 Overlap 拾取，对命中目标执行子 ops
- 子 ops 支持 Damage / Modifier

### 光环运行时 ✅
- `ModifierManager.TickAuras` 每 0.5s 扫描
- `AuraTargetCollector` 委托注入，XZ 平面距离 + 阵营过滤
- 自动施加/移除 `AuraConfig.TargetModifier`

## P2 — 缺失 op 类型

### `Summon` op
- **描述**: `AbilityEffectOpType` 缺少召唤单位的能力
- **当前**: 只有 Damage / Heal / Modifier / Projectile / Thinker
- **需要**: 新增 `Summon` op，在指定位置创建召唤单位

Thinker 已在 P1 实现 ✅

## P3 — 工程/质量

### 测试增量
- 缺少 MultiHit + originAnchor 的集成测试
- 缺少光环系统的单元测试 ✅（4 个 Aura 测试已通过）

## 已修复/已完成

## 已修复/已完成

- [x] 伤害管线（单管线 + HpMin 夹底）
- [x] 多段命中分摊暴击（MultiHitHelper + 可注入随机数）
- [x] INavMeshService + originAnchor（picker 中心点位置修正）
- [x] 韧性系统（StatId + ApplyBreakDamage + EventBus 事件）
- [x] 充能机制（AbilityData.MaxCharge / AbilityInstance.UpdateCooldown）
- [x] Effects 声明式效果（picker + ops: Damage/Heal/Modifier/Projectile/Thinker）
- [x] Modifier 系统（DeclareFunctions / Properties / StatModifiers）
- [x] 复合属性三分量聚合 + TagSystem
- [x] 闪避 2 层共享充能（SkillComp.Cd.cs）
- [x] SkillConfig 清理（移除 charge / coolDown / fallbackSkillConfigId）
- [x] 投射物：`CreateLinearAt` 指定起始位置；Effect op 绑定 Lua `OnProjectileHit` 回调
