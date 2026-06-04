# CombatFramework Roadmap

## Phase 1 — 核心引擎 ✅
- [x] AbilityData + AbilitySpec 模板/实例系统
- [x] Modifier 系统（生命周期、叠加、标签、特效自动管理）
- [x] 伤害管线（防御/抗性/暴击/破韧/部位吸收）
- [x] IBattleFormula 可替换公式接口
- [x] EventBus 全局事件广播
- [x] StatsManager 属性系统（复合公式 Base×(1+Mult)+Add + Max/Block 上限）
- [x] TagSystem 扁平标签系统
- [x] CFLog 可插拔日志
- [x] IVfxEffectService VFX 桥接接口
- [x] SkillBonus 命座/被动等级加成

## Phase 2 — JSON Action 能力系统 ✅
- [x] JSON 反序列化 AbilityData + ModifierData（Newtonsoft.Json 多态）
- [x] 7 种内置 AbilityEventAction（Damage/Heal/Modifier/Remove/ModifyStat/ReplaceAbility/ForEachHit）
- [x] TargetSelector（SingleTarget + Area）+ TeamFilter 阵营过滤
- [x] ValueGetter（const / ability_special / kxb）灵活数值计算
- [x] AbilityCondition 条件系统（And/Or/Not/HasModifier/CheckTag）
- [x] AbilityTransform 技能转换链
- [x] CFBridge 抽象桥接（Vfx/ShapeQuery/UnitQuery/Element/Formula/Method/Type）
- [x] 韧性/破韧管线（ToughnessPipeline + BreakModifierSpec）

## Phase 3 — 开发者体验 ✅
- [x] 完整文档（架构/指南/钩子参考/管线参考/事件参考）
- [x] 关键词检索脚本 `scripts/search_docs.py`
- [x] JSON 测试能力 6 个（damage/area/modifier/stack/passive/remove）
- [x] Godot Demo 可运行原型（含战斗设计文档）
- [x] 集成测试（序列化往返 + 完整战斗流程）

## Phase 4 — 策划工具（规划中）
- [ ] 可视化能力编辑器（Unity Editor Window）
  - 模板选择（投射物/范围/自身/光环）
  - 参数面板（数值、持续时间、范围、冷却）
  - Modifier 可视化配置（事件勾选 + 填值）
  - 导出 → `.json` 文件
- [ ] 预设库：常用能力模板一键生成
- [ ] 运行时调试面板（当前 buff 列表、伤害统计）
- [ ] 异常/击破多管线路由

## Phase 5 — 生态（远期）
- [ ] 能力热更新 / 热加载
- [ ] 网络同步抽象层
- [ ] 性能 Profiling 工具
- [ ] Mod 能力包格式
