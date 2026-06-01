using CombatFramework.Core.Enums;
using CombatFramework.Core.Executor.ValueGetter;
using CombatFramework.Core.Modifier;
using System.Runtime.Serialization;
using System.Text.Json.Nodes;

namespace CombatFramework.Core.Executor.JsonResolver
{
    public class ModifierJsonResolver : JsonResolver
    {
        public ModifierData CreateModifierDataFromJson(JsonObject json)
        {
            var data = new ModifierData();
            var name = GetFieldValueOrThrow<string>(json, "Name");
            data.Name = name;
            if (TryGetFieldValue<JsonNode>(json, "Duration", out var durationNode))
            {
                data.DurationGetter = ResolveValueGetter(durationNode, 0);

            }
            if (TryGetFieldValue<string>(json, "Attribute", out var attributeStr))
            {
                if (!Enum.TryParse<ModifierAttribute>(attributeStr, true, out var attribute))
                {
                    throw new Exception($"Invalid modifier attribute: {attributeStr}");
                }
            }
            if (TryGetFieldValue<bool>(json, "IsBuff", out var isBuff))
            {
                data.IsBuff = isBuff;
            }
            if (TryGetFieldValue<bool>(json, "IsDebuff", out var isDebuff))
            {
                data.IsDebuff = isDebuff;
            }
            if (TryGetFieldValue<bool>(json, "IsHidden", out var isHidden))
            {
                data.IsHidden = isHidden;
            }
            if (TryGetFieldValue<bool>(json, "IsPurgable", out var isPurgable))
            {
                data.IsPurgable = isPurgable;
            }

            // 属性就
            return data;
        }

        private IValueGetter<ModifierSpec> ResolveValueGetter(JsonNode node, float defaultValue = 0f)
        {
            if (node is JsonValue valueNode)
            {
                // 如果是数字，直接返回常量值获取器
                if (valueNode.TryGetValue<float>(out var value))
                {
                    return new ConstantValueGetter<ModifierSpec>(value);
                }
                // 如果是字符串，看是否以%开头，表示这是一个引用，要从context中获取
                else if (valueNode.TryGetValue<string>(out var strValue) && strValue.StartsWith("%"))
                {
                    var fieldName = strValue.Substring(1);
                    return new ModifierSpecialGetter(fieldName);
                }
            }
            else if (node is JsonObject obj)
            {
                var behaviorName = GetFieldValueOrThrow<string>(obj, "Behavior");
                if (!Enum.TryParse<GlobalConstants>(behaviorName, out var behaviorEnum))
                {
                    throw new Exception($"Invalid behavior value: {behaviorName}");

                }
                // 暂时就这一种， kx + b
                if (behaviorEnum == GlobalConstants.ABILITY_STAT_BEHAVIOUR_STATBASED)
                {
                    // 先获取BaseStat
                    var baseStatName = GetFieldValueOrThrow<string>(obj, "BaseStat");
                    // 获取K和B，允许空
                    var kValue = TryGetFieldValue<JsonNode>(obj, "K", out var k) ? k : null;
                    var bValue = TryGetFieldValue<JsonNode>(obj, "B", out var b) ? b : null;
                    var kGetter = ResolveValueGetter(kValue, 1);
                    var bGetter = ResolveValueGetter(bValue, 0);
                    var customValueGetter = new CustomValueGetter<ModifierSpec>((c) =>
                    {
                        if (!c.Ability.TryGetLevelValue(baseStatName, out var baseStatValue))
                        {
                            throw new Exception($"Base stat '{baseStatName}' not found in context");
                        }
                        var kVal = kGetter.GetValue(c);
                        var bVal = bGetter.GetValue(c);
                        return kVal * baseStatValue + bVal;
                    });
                }
                else
                {
                    throw new SerializationException($"Unsupported behavior: {behaviorEnum}");
                }
            }

            return new ConstantValueGetter<ModifierSpec>(defaultValue);
        }

    }
}
