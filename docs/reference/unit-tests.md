# 单元测试用例索引

CF 单元测试在 `tests/` 下，按模块分目录。这里只列**测试用例 → 守护行为**的映射，不写实现细节。

新增或修改测试用例时，必须同时更新本文件对应章节，保持文档与代码一致。

## 索引

| 模块 | 测试文件 | 用例数 | 章节 |
|---|---|---|---|
| Core / AbilityInstance | `tests/Core/AbilityInstanceTests.cs` | 多 | [§1](#1-abilityinstance) |
| Core / TagSystem | `tests/Core/TagSystemTests.cs` | 多 | [§2](#2-tagsystem) |
| Core / CompoundStat | `tests/Core/CompoundStatTests.cs` | 多 | [§3](#3-compoundstat) |
| Stat / ResourceSystem | `tests/Stat/ResourceSystemTests.cs` | 多+5 | [§4](#4-resourcesystem) |
| Stat / StatDefinition | `tests/Stat/StatDefinitionTests.cs` | 多 | [§5](#5-statdefinition) |
| Unit / UnitEntity | `tests/Unit/UnitEntityTests.cs` | 16 | [§6](#6-unitentity) |
| Modifier / ModifierManager | `tests/Modifier/ModifierManagerTests.cs` | 17 | [§7](#7-modifiermanager) |
| Event / EventBus | `tests/Event/EventBusTests.cs` | 多 | [§8](#8-eventbus) |
| Damage / DamagePipeline | `tests/Damage/DamagePipelineTests.cs` | 多+2 | [§9](#9-damagepipeline) |
| Damage / DamageTypes | `tests/Damage/DamageTypesTests.cs` | 多 | [§10](#10-damagetypes) |
| Projectile / ProjectileManager | `tests/Projectile/ProjectileManagerTests.cs` | 多 | [§11](#11-projectilemanager) |
| Lua / LuaAbilityIntegration | `tests/Lua/LuaAbilityIntegrationTests.cs` | 19 | [§12](#12-lua-ability-integration) |
| Integration / EveAttack1 | `tests/Integration/EveAttack1IntegrationTests.cs` | 1 | [§13](#13-eve-attack1-integration) |

> 标记"多"的章节尚未逐条登记，后续按需补全。新增/修改的章节按下面的格式写。

---

## 4. ResourceSystem

本章节只记录本轮新增/变更用例；历史用例后续按需补全。

| 用例 | 守护行为 |
|---|---|
| `ConsumeSafe_BelowMinimum_ClampsLeftover` | 安全自伤不会把资源扣到 `minLeft` 以下，返回实际扣除值 |
| `ConsumeSafe_AboveMinimum_DeductsFully` | 当前值足够时按 amount 全额扣除 |
| `ConsumeSafe_AlreadyAtMinimum_DeductsZero` | 已在下限时不扣除 |
| `ConsumeSafe_FiresOnResourceChanged` | 有实际扣除时触发资源变化事件 |
| `ConsumeSafe_NoChange_DoesNotFireEvent` | 没有实际变化时不触发事件 |

## 6. UnitEntity

| 用例 | 守护行为 |
|---|---|
| `Constructor_InitializesAllSystems` | 构造时 Abilities / ModifierManager / Stats / Tags 都被实例化，Level=1 |
| `Update_ReducesCooldowns` | `Update(dt)` 推进所有装备能力的冷却 |
| `Update_CallsModifierManagerUpdate` | `Update` 会驱动 ModifierManager，过期 modifier 被移除 |
| `AbilitySlot_EquipAndUnequip` | 装备/卸下能力的基本路径 |
| `AbilitySlot_DuplicateEquip_Ignored` | 重复装备同一能力被忽略 |
| `AbilitySlot_GetByIndex_OutOfRange_ReturnsNull` | 越界索引返回 null |
| `GetStat_CompoundAttack_NoModifier_UsesCompoundFinal` | `GetStat("Attack")` 无 modifier 时等于 CompoundStat.Final |
| `GetStat_CompoundAttack_ModifierAddsToBaseComponent` | `AtkBase` modifier 经 Final 公式参与计算 |
| `GetStat_CompoundAttack_ModifierAddsToMultComponent` | `AtkMult` modifier 作为 PercentBonus 分量参与 |
| `GetStat_CompoundAttack_ModifierAddsToExtraComponent` | `AtkAdd` modifier 作为 Extra 分量参与 |
| `GetStat_CompoundAttack_DirectAttackModifierIsIgnored` | 直接对 "Attack" 名加 modifier 不生效，强制走分量 |
| `GetStat_CompoundHp_GoesThroughHpComponents` | HpMult/HpAdd 分量同样参与 HP Final 公式 |
| `GetStat_FlatStat_UsesFlatBase` | `GetStat("CritRate")` 走 `Stats.GetFlat` 作为 base |
| `GetStat_FlatStat_PlusProperties` | `GetStat("CritRate")` 叠加 modifier `Properties` 的 Add 贡献 |
| `GetStat_ElementalResistance_UsesElementTable` | `GetStat("FIRE")` 走 `Stats.ElementalResistance` 表 |
| `GetStat_Unknown_ReturnsZero` | 未知 stat 名字返回 0 |

## 7. ModifierManager

按场景分组列出。

### Add / 生命周期

| 用例 | 守护行为 |
|---|---|
| `Add_NewModifier_CreatesAndPending` | Add 不立即落入 All，需要 Update flush |
| `Add_NoneAttribute_SameNameRefreshes` | 同名 None 属性 modifier 重复 Add 会刷新 duration，不增加数量 |
| `Add_MultipleAttribute_AllowsDuplicates` | Multiple 属性允许同名多实例 |
| `Add_StackCount_IncrementsStack` | StackCount 属性重复 Add 增加 StackCount |
| `Add_Permanent_NeverExpires` | Permanent 属性不随时间过期 |
| `Update_ExpiredModifier_IsRemoved` | duration 到期后从 All 移除 |

### 移除 / 驱散

| 用例 | 守护行为 |
|---|---|
| `RemoveBySourceTag_RemovesMatching` | 按 SourceTag 批量清除（如重刷命座） |
| `Purge_RemovesPurgableDebuffs` | Purge(negative) 清除 IsDebuff && IsPurgable |
| `Purge_NonPurgable_NotRemoved` | IsPurgable=false 的 modifier 无法被驱散 |

### 属性聚合（C2 加入）

| 用例 | 守护行为 |
|---|---|
| `CollectStatValue_SumFromProperties` | 旧 `CollectStatValue` API 仍能从 Properties 求和 |
| `CollectStatValue_MultipleModifiers_Summed` | 多个 modifier 的同名 Properties 累加 |
| `CollectStatValue_NoDeclaredStats_ReturnsZero` | 无声明的 stat 返回 0 |
| `AggregateStat_BaseOnly_ReturnsBase` | 没有 modifier 时 `AggregateStat(base)` 返回 base |
| `AggregateStat_StatModifiers_AddAccumulates` | 同 stat 多条 Add 条目线性累加 |
| `AggregateStat_OrderInvariant_AddCommutative` | 多个 modifier Add 的添加顺序不影响结果 |
| `AggregateStat_Override_WinsOverAdd` | Override 出现时直接返回 override 值，覆盖 add 累计 |
| `AggregateStat_PropertiesAndStatModifiers_CombineAsAdd` | 同 modifier 上 Properties 与 StatModifiers Add 共同累加 |

### 事件分发

| 用例 | 守护行为 |
|---|---|
| `DispatchEvent_NoCrashWithNoHandlers` | 没有订阅时 DispatchEvent 不抛异常 |

## 9. DamagePipeline

本章节只记录本轮新增/变更用例；历史用例后续按需补全。

| 用例 | 守护行为 |
|---|---|
| `ApplyDamage_HpMin_ClampsDamageToMinimumHealth` | 受伤管线读取 modifier 的 HpMin Property，扣血夹底（薄葬式） |
| `ApplyDamage_HpMin_MultipleSourcesUseHighestMinimum` | 多个来源同时挂 HpMin 时取最大值，最强守护生效 |

## 12. Lua Ability Integration

按主题分组：

### test_frostbolt.lua 加载

| 用例 | 守护行为 |
|---|---|
| `LoadFile_ParsesFrostboltCorrectly` | id/name/Cooldown/CastRange/CastPoint 字段正确解析 |
| `LoadFile_ParsesParameters` | parameters 表读入 AbilityData.Parameters |
| `LoadFile_ParsesCosts` | costs 表读入 AbilityData.Costs |
| `LoadFile_ParsesProjectileConfig` | projectile 表解析为 ProjectileConfig |
| `LoadFile_ParsesEventHandlers` | OnSpellStart / OnProjectileHit 闭包被缓存 |
| `LoadFile_ParsesModifierTemplates` | Modifiers 表解析为 ModifierTemplates 字典 |
| `LoadFile_MissingFile_ReturnsNull` | 不存在的文件返回 null |

### Modifier 解析

| 用例 | 守护行为 |
|---|---|
| `FrostboltSlow_HasCorrectProperties` | Duration / IsDebuff / IsPurgable / IsHidden / Attribute 正确 |
| `FrostboltSlow_HasDeclaredHooks` | DeclareFunctions 中的 stat 进入 DeclaredStats |
| `FrostboltSlow_HasPropertyHookFunctions` | 同名函数登记到 PropertyHooks |
| `FrostboltSlow_HasLifecycleClosures` | OnCreated/OnDestroy/OnIntervalThink 闭包被缓存 |
| `FrostboltShield_IsPermanent` | Permanent 属性 + IsBuff/IsHidden/不可驱散 |
| `FrostboltShield_HasIntervalThink` | OnIntervalThink 与 ConstantHealthRegen DeclaredStats 一致 |

### ModifierInstance 行为

| 用例 | 守护行为 |
|---|---|
| `ModifierInstance_OnCreatedClosure_Runs` | OnCreated 闭包不抛异常 |
| `ModifierInstance_DestroyClosure_Runs` | OnDestroy 闭包不抛异常 |
| `ModifierInstance_PropertyHookClosures_ReturnValue` | PropertyHook 闭包返回 Lua 返回值 |
| `ModifierInstance_IntervalThink_Ticks` | StartIntervalThink 后按间隔触发 OnIntervalThink |

### StatModifiers 结构化解析（C2 加入）

| 用例 | 守护行为 |
|---|---|
| `ParseModifierScript_StatModifiers_ParsesAddAndOverride` | StatModifiers 列表解析 Add 与 Override |
| `ParseModifierScript_StatModifiers_DefaultOpIsAdd` | 未指定 op 时默认 Add |
| `ParseModifierScript_StatModifiers_UnknownOpFallsBackToAdd` | 未识别的 op 名退化为 Add，避免崩溃 |

### AbilityData.Effects 解析（C3.5 加入）

| 用例 | 守护行为 |
|---|---|
| `ParseAbilityScript_Effects_ParsesPickerAndOps` | Effects 表的 picker（type/filter/shape）和 ops（Damage/Modifier 等）正确解析 |
| `ParseAbilityScript_Effects_BoxShape_ParsesScale` | Box 形状的 offset / rotate / scale 三个 Vector3 字段正确读取 |
| `ParseAbilityScript_Effects_ParsesOriginAnchorAndOffset` | originAnchor/offset 字段正确解析，缺省时默认空字符串和 0 |
| `ParseAbilityScript_Effects_UnknownOpType_IsSkipped` | ops 中未识别的 type 字段被跳过，不影响其它 op |

## 13. Eve Attack1 Integration

| 用例 | 守护行为 |
|---|---|
| `EveAttack1_FullFlow` | 完整流程：挂 buff（AtkMult StatModifiers）→ 伤害（ATK*0.9+FIRE）→ 吸血 → 减速 debuff → buff 过期自动清除 |

---

## 维护约定

- 新增测试 → 先在对应章节加一行表格记录守护行为
- 删除/重命名测试 → 同步删除/重命名表格行
- 表格"守护行为"一栏写**意图**，不写实现；让人扫一眼能看懂这个测试在守护什么
- 不要把测试代码贴进来，doc 与代码各司其职
