using CombatFramework.Bridge;
using CombatFramework.Core.Model;
using CombatFramework.Unit;
using System.Reflection;

namespace CombatFramework.Core.Ability.AbilityEvent
{
    public abstract class AbilityEventAction
    {
        public readonly AbilityEventActionData data;

        protected AbilityEventAction(AbilityEventActionData data)
        {
            this.data = data;
        }
        
        public abstract void Execute(AbilityEventContext context);

        #region Factory
        private static Dictionary<Type, Type> _typeMap = new();
        private readonly static Type thisType = typeof(AbilityEventAction);

        static AbilityEventAction()
        {
            // 反射初始化
            var types = CFBridge.Bridge.TypeProvider.GetTypes();

            foreach (var type in types)
            {
                // 非抽象类， 继承自 AbilityEventAction， 有 AbilityEventActionAttribute
                if (type.IsAbstract)
                {
                    continue;
                }
                if (!thisType.IsAssignableFrom(type))
                {
                    continue;
                }
                var attr = type.GetCustomAttribute<AbilityEventActionAttribute>();
                if (attr == null)
                {
                    continue;
                }

                _typeMap[attr.dataType] = type;
            }
        }

        public static AbilityEventAction Create(AbilityEventActionData data)
        {
            if (!_typeMap.TryGetValue(data.GetType(), out var actionType))
            {
                return null;
            }
            return (AbilityEventAction)Activator.CreateInstance(actionType, data);
        }
        #endregion
    }
}
