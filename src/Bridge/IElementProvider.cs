using System.Reflection;

namespace CombatFramework.Bridge
{
    public interface IElementProvider
    {
        // 获取增幅属性
        string GetAmplifyStat(string elementId);
        // 获取抗性属性
        string GetResistanceStat(string elementId);
    }

    public interface IMethodProvider
    {
        MethodInfo GetMethodInfo(string className, string methodName);
        MethodInfo GetMethodInfo(string methodName);
    }

    public interface ITypeProvider
    {
        Type[] GetTypes();
    }
}
