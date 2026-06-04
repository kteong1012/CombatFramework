using CombatFramework.Core.Ability;
using CombatFramework.Core.Model;
using Newtonsoft.Json;

namespace CombatFramework.Core.Executor.ValueGetter
{
    [JsonAlias("special")]
    public class AbilitySpecialGetter : IAbilityValueGetter
    {
        [JsonProperty("Name")]
        private string _name;

        public AbilitySpecialGetter()
        {
            _name = string.Empty;
        }

        public AbilitySpecialGetter(string name)
        {
            _name = name;
        }

        public float GetValue(AbilitySpec context)
        {
            if (context == null) return 0f;
            if (context.TryGetLevelValue(_name, out var value))
            {
                return value;
            }
            else
            {
                CFLog.Warning($"AbilitySpecial {_name} not found in ability {context.data?.Name}, return 0");
                return 0f;
            }
        }
    }
}
