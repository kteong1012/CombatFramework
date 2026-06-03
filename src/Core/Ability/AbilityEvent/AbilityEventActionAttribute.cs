namespace CombatFramework.Core.Ability.AbilityEvent
{
    public class AbilityEventActionAttribute : Attribute
    {
        public readonly Type dataType;
        public AbilityEventActionAttribute(Type dataType)
        {
            this.dataType = dataType;
        }
    }
}