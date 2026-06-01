using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CombatFramework.Bridge
{

    public abstract class AbstractCombatFrameBridge
    {
        public IElementProvider ElementProvider { get; }
        public IMethodProvider MethodProvider { get; }

        protected AbstractCombatFrameBridge()
        {
            ElementProvider = CreateElementProvider();
            MethodProvider = CreateMethodProvider();
        }

        protected abstract IElementProvider CreateElementProvider();
        protected abstract IMethodProvider CreateMethodProvider();
    }
}
