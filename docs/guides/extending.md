# 扩展 CombatFramework

## 注册新的伤害类型

```csharp
// 在游戏启动时调用
DamageTypes.Register("PHYSICAL", "PhysicalRES", "vfx/hit_physical");
DamageTypes.IsValid("PHYSICAL");          // true
DamageTypes.GetResistanceStat("PHYSICAL"); // "PhysicalRES"
DamageTypes.GetDefaultVfx("PHYSICAL");    // "vfx/hit_physical"
```

框架内置 7 元素 + NONE，默认注册在 `DamageTypes.RegisterDefaults()`。

## 注册新的属性（StatDefinition）

```csharp
StatDefinition.Register("ATK_Base", "基础攻击力");
StatDefinition.Register("ATK_Pct", "攻击力百分比");
StatDefinition.Register("ATK", "攻击力", isCompound: true);

// 查询
var def = StatDefinition.Get("ATK");
def.Name;        // "攻击力"
def.IsCompound;  // true
```

默认属性通过 `StatDefinition.RegisterDefaults()` 注册（ATK/DEF/HP 分量及其复合类型、元素精通、暴击率、暴击伤害、治疗加成、充能效率）。

## 注册新的资源槽

```csharp
unit.Resources.Register("Energy", 100f, 100f);
unit.Resources.Register("Rage", 0f, 100f, min: -50);  // 支持负值

unit.Resources.TryConsume("Energy", 30);
unit.Resources.Restore("Rage", 10);

// 安全自伤（不扣到 minLeft 以下）
unit.Resources.ConsumeSafe("HP", 50, minLeft: 1);
```

## 切换伤害公式

```csharp
// 实现 IDamageFormula 接口
public class MyFormula : IDamageFormula
{
    public float CalculateResistanceReduction(float resistanceValue) { ... }
    public float CalculateCriticalMultiplier(float critRate, float critDamage, out bool isCrit) { ... }
}

// 全局替换
DamagePipeline.Formula = new MyFormula();
```

## 替换碰撞检测

```csharp
// 实现 ICollisionService 接口（Unity 侧实现）
public class UnityCollisionService : ICollisionService
{
    HitResult[] CheckHits(Vector3 position, float radius, TeamFlag team) { ... }
}

projectileManager.CollisionService = new UnityCollisionService();
```

## 实现 VFX 桥接

```csharp
// 实现 IVfxEffectService 接口（Unity 侧实现）
public class UnityVfxService : IVfxEffectService
{
    public int PlayAtPoint(string assetPath, Vector3 position, float? lifeTime) { ... }
    public int PlayOnUnit(string assetPath, UnitEntity target, float? lifeTime) { ... }
    public void Stop(int vfxId) { ... }
}

DamagePipeline.VfxService = new UnityVfxService();
```

## 监听全局事件

```csharp
DamagePipeline.GlobalEventBus.Subscribe(EventBus.Events.EntityKilled, data => {
    // 处理击杀事件
});
```

预定义事件：`EntitySpawned`、`EntityKilled`、`EntityHurt`、`AbilityUsed`、`AbilityEquipped`、`AbilityUnequipped`、`ConstellationUpgrade`、`HealApplied`。

## 添加新元素

```csharp
// 1. 注册伤害类型及抗性
DamageTypes.Register("ICE", "IceRES", "vfx/hit_ice");

// 2. UnitStats 已用 Dictionary<string, float> 存储元素伤害加成和抗性
// 框架自动支持新类型，只需在 Lua 能力中引用即可

// 3. 可选：在 StatDefinition 注册对应属性
StatDefinition.Register("IceRES", "冰抗");
```

## 自定义 Modifier Stat

Stat 使用字符串 ID，无需修改 C# 枚举。在 Lua modifier 中直接声明：

```lua
DeclareFunctions = {
    "MyCustomStat",
},
MyCustomStat = function(self)
    return 42
end,
```

在 C# 中收集：

```csharp
var value = unit.ModifierManager.AggregateStat("MyCustomStat", baseValue);
// 或带上下文的旧签名（兼容旧代码）：
var value = unit.ModifierManager.CollectStatValue("MyCustomStat", context);
```

引擎自动对所有活跃 modifier 中声明了 `"MyCustomStat"` 的函数或 Properties 值求和。

## 通过 DeclareTags 管理标签

Modifier 可以通过 `DeclareTags` 在激活时自动添加标签，过期时自动移除：

```lua
my_stun_modifier = {
    DeclareTags = { "stunned" },
}
```

`ModifierManager.Update()` 自动计算所有活跃 modifier 的标签并集，通过 `TagSystem.SyncFrom()` 同步到 `UnitEntity.TagSystem`。标签变化时触发 `OnTagChanged` 事件。
