using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CombatFramework.Core.Ability;
using CombatFramework.Core.Ability.AbilityEvent;

namespace CombatFramework.Bridge
{

    public abstract class AbstractCombatFrameworkBridge
    {
        public IElementProvider ElementProvider { get; }
        public IMethodProvider MethodProvider { get; }
        public ITypeProvider TypeProvider { get; }

        protected AbstractCombatFrameworkBridge()
        {
            ElementProvider = CreateElementProvider();
            MethodProvider = CreateMethodProvider();
            TypeProvider = CreateTypeProvider();
        }

        protected abstract IElementProvider CreateElementProvider();
        protected abstract IMethodProvider CreateMethodProvider();
        protected abstract ITypeProvider CreateTypeProvider();

        /// <summary>
        /// 启动技能执行流程。Unity 端通常触发 Timeline / 动画序列；
        /// Timeline 内部在前摇开始时调用 ability.OnAbilityPhaseStart，
        /// 在施法点调用 ability.OnSpellStart。
        /// 测试端可直接依次调用这两个事件以简化验证。
        /// </summary>
        public abstract void StartAbility(AbilitySpec ability, AbilityEventContext ctx);
    }
}
