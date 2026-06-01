using CombatFramework.Bridge;
using CombatFramework.Core.Modifier;
using System.Reflection;

namespace CombatFramework.Core.Executor.ValueGetter
{
    public class ModifierCallMethodGetter : IValueGetter<ModifierSpec>
    {
        private readonly MethodInfo method;
        private readonly List<IValueGetter<ModifierSpec>> _params;
        public ModifierCallMethodGetter(string className, string methodName, List<IValueGetter<ModifierSpec>> parameters)
        {
            method = CFBridge.Bridge.MethodProvider.GetMethodInfo(className, methodName);
            _params = parameters;
        }
        public ModifierCallMethodGetter(string methodName, List<IValueGetter<ModifierSpec>> parameters)
        {
            method = CFBridge.Bridge.MethodProvider.GetMethodInfo(methodName);
            _params = parameters;
        }
        public float GetValue(ModifierSpec context)
        {
            var paramValues = _params.Select(p => (object)p.GetValue(context)).ToArray();
            var result = method.Invoke(null, paramValues);
            return Convert.ToSingle(result);
        }
    }
}
