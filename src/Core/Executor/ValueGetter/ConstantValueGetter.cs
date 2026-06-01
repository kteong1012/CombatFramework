using CombatFramework.Bridge;
using CombatFramework.Core.Ability;
using System.Reflection;

namespace CombatFramework.Core.Executor.ValueGetter
{
    public class ConstantValueGetter<T> : IValueGetter<T>
    {
        private readonly float _value;
        public ConstantValueGetter(float value)
        {
            _value = value;
        }
        public float Value => _value;

        public float GetValue(T context)
        {
            return _value;
        }
    }

    public class CustomValueGetter<T> : IValueGetter<T>
    {
        private readonly Func<T, float> del;

        public CustomValueGetter(Func<T, float> del)
        {
            this.del = del;
        }

        public float GetValue(T context)
        {
            return del(context);
        }
    }
}
