using CombatFramework.Core.Ability;
using CombatFramework.Core.Model;
using Newtonsoft.Json;

namespace CombatFramework.Core.Executor.ValueGetter
{
    [JsonAlias("const")]
    public class ConstantValueGetter : IAbilityValueGetter
    {
        [JsonProperty("Value")]
        public float Value { get; private set; }

        public ConstantValueGetter() { }

        public ConstantValueGetter(float value)
        {
            Value = value;
        }

        public float GetValue(AbilitySpec context) => Value;
    }
}
