using System.Text.Json.Nodes;

namespace CombatFramework.Core.Executor.JsonResolver
{
    public class JsonResolver
    {
        protected bool TryGetFieldValue<T>(JsonObject jsonObject, string fieldName, out T value)
        {
            if (jsonObject[fieldName] != null)
            {
                value = jsonObject[fieldName].GetValue<T>();
                return true;
            }
            value = default;
            return false;
        }
        protected T GetFieldValueOrThrow<T>(JsonObject jsonObject, string fieldName)
        {
            if (TryGetFieldValue(jsonObject, fieldName, out T value))
            {
                return value;
            }
            throw new Exception($"Missing required field: {fieldName}");
        }
    }
}