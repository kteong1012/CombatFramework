# CombatFramework Roadmap

## Phase 1 — 核心引擎 ✅
- [x] AbilityData + AbilityInstance 模板系统
- [x] Modifier 系统（生命周期、堆叠、状态、标签）
- [x] 伤害/治疗管线（抗性、暴击、HpMin 夹底）
- [x] 7 元素 + 可扩展 DamageTypes（含默认 VFX 路径）
- [x] ProjectileManager 投射物（追踪 + 线性）
- [x] IDamageFormula 可替换公式接口
- [x] EventBus 全局事件广播
- [x] ResourceSystem 资源槽（含 ConsumeSafe 安全自伤）
- [x] UnitStats 属性系统（CompoundStat 三分量聚合）
- [x] StatDefinition 注册表
- [x] TagSystem 扁平标签系统（DeclareTags + SyncFrom diff）
- [x] CFLog 可插拔日志
- [x] IVfxEffectService VFX 桥接接口

## Phase 2 — Lua 能力系统 ✅
- [x] LuaAbilityLoader（AbilityData + Modifiers + Effects 解析）
- [x] LuaModifierBridge（钩子双向绑定，伤害类型常量）
- [x] MoonSharp 沙箱隔离
- [x] Lua 端全局 API（ApplyDamage、ApplyHeal、PlayVfxOnUnit）
- [x] Effects 声明式效果数据（picker + ops：Damage/Heal/Modifier/Projectile）
- [x] 19 个集成测试覆盖全生命周期

## Phase 3 — 开发者体验 ✅
- [x] 完整文档（架构/指南/钩子参考/管线参考/事件参考）
- [x] 关键词检索脚本 `scripts/search_docs.py`
- [x] 示例能力 `test_frostbolt.lua`
- [x] Skill: `/combat-ability` — AI 辅助生成能力文件
- [x] 90+ 单元测试 + 集成测试

## Phase 4 — 策划工具（规划中）
- [ ] 可视化能力编辑器（Unity Editor Window）
  - 模板选择（投射物/范围/自身/光环）
  - 参数面板（数值、持续时间、范围、冷却）
  - Modifier 可视化配置（钩子勾选 + 填值）
  - 自定义 Lua 片段入口（高级模式）
  - 导出 → `.lua` 文件
- [ ] 预设库：常用能力模板一键生成
- [ ] 运行时调试面板（当前 buff 列表、伤害统计）
- [ ] 异常/击破多管线路由

## Phase 5 — 生态（远期）
- [ ] 能力热更新 / 热加载
- [ ] 网络同步抽象层
- [ ] 性能 Profiling 工具
- [ ] Mod 能力包格式
