namespace CombatFramework.Bridge
{
    public static class CFBridge
    {
        public static AbstractCombatFrameworkBridge Bridge { get; private set; }

        public static void Initialize(AbstractCombatFrameworkBridge bridge)
        {
            Bridge = bridge;
        }
    }
}
