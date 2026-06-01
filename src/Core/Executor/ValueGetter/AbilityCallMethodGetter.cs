using CombatFramework.Bridge;
using CombatFramework.Core.Ability;
using CombatFramework.Core.Modifier;
using System.Reflection;

namespace CombatFramework.Core.Executor.ValueGetter
{
    public class AbilityCallMethodGetter : IValueGetter<AbilitySpec>
    {
        private readonly MethodInfo method;
        private readonly List<IValueGetter<AbilitySpec>> _params;
        public AbilityCallMethodGetter(string className, string methodName, List<IValueGetter<AbilitySpec>> parameters)
        {
            method = CFBridge.Bridge.MethodProvider.GetMethodInfo(className, methodName);
            _params = parameters;
        }

        public AbilityCallMethodGetter(string methodName, List<IValueGetter<AbilitySpec>> parameters)
        {
            method = CFBridge.Bridge.MethodProvider.GetMethodInfo(methodName);
            _params = parameters;
        }

        public float GetValue(AbilitySpec context)
        {
            var paramValues = _params.Select(p => (object)p.GetValue(context)).ToArray();
            var result = method.Invoke(null, paramValues);
            return Convert.ToSingle(result);
        }
    }
}
