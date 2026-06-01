namespace CombatFramework.Core.Executor.ValueGetter
{
    public interface IValueGetter<T>
    {
        float GetValue(T context);
    }
}
