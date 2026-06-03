using CombatFramework.Core.Ability;
using CombatFramework.Core.Model;

namespace CombatFramework.Core.Executor.ValueGetter
{
    [JsonAlias("kxb")]
    public class KXBValueGetter : IAbilityValueGetter
    {
        public string BaseStat;
        public IAbilityValueGetter K;
        public IAbilityValueGetter B;
        public float GetValue(AbilitySpec context)
        {
            // kx + b
            var unit = context.Owner;
            var statValue = unit.GetStat(BaseStat);
            var k = K.GetValue(context);
            var b = B.GetValue(context);
            return k * statValue + b;
        }
    }
}
