using CombatFramework.Core.Ability;
using CombatFramework.Core.Executor.ValueGetter;

namespace CombatFramework.Core.Modifier;

/// <summary>
/// Modifier 运行时实例——附加在 unit 上的状态实体。
/// 采用内部组合（Option C）：钩子存储为列表而非继承。
/// </summary>
public class ModifierSpec
{
    public ModifierData data;
    public AbilitySpec Ability { get; set; }
}
