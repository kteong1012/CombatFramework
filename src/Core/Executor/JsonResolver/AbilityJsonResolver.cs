using CombatFramework.Core.Ability;
using CombatFramework.Core.Enums;
using CombatFramework.Core.Executor.Ability.ActionExecutor;
using CombatFramework.Core.Executor.AbilityExecutor;
using CombatFramework.Core.Executor.ValueGetter;
using System.Runtime.Serialization;
using System.Text.Json.Nodes;

namespace CombatFramework.Core.Executor.JsonResolver
{

    public class AbilityJsonResolver : JsonResolver
    {
        #region Readonly Data
        const string String_Name = "Name";
        const string Object_AbilitySpecial = "AbilitySpecial";
        const string String_AbilityUnitDamageElement = "AbilityUnitDamageElement";
        const string Array_AbilityCost = "AbilityCost";
        const string Object_AbilityEvent = "AbilityEvent";
        #endregion


        public AbilityData CreateAbilityDataFromJson(JsonObject jsonObject)
        {
            var abilityData = new AbilityData();
            // Name
            abilityData.Name = GetFieldValueOrThrow<string>(jsonObject, String_Name);

            // Behaviour 待扩展

            // 数值参数
            {
                // AbilitySpecial
                var abilitySpecialObject = jsonObject[Object_AbilitySpecial] as JsonObject;
                // 遍历字段，value一定是float数组
                abilityData.dynamicValues.Clear();
                foreach (var kvp in abilitySpecialObject)
                {
                    var levelValue = new AbilityData.LevelValue();
                    levelValue.values = kvp.Value.AsArray().Select(x => x.GetValue<float>()).ToArray();
                    abilityData.dynamicValues[kvp.Key] = levelValue;
                }
            }

            // 元素信息
            if (TryGetFieldValue(jsonObject, String_AbilityUnitDamageElement, out string element))
            {
                abilityData.Element = element;
            }

            // 消耗信息
            abilityData.costMaps.Clear();
            if (TryGetFieldValue(jsonObject, Array_AbilityCost, out JsonArray costArray))
            {
                foreach (var costItem in costArray)
                {
                    if (costItem is JsonObject costObject)
                    {
                        if (ResolveCost(costObject, out var getter))
                        {
                            var statName = GetFieldValueOrThrow<string>(costObject, "Stat");
                            abilityData.costMaps[statName] = new AbilitySpecialGetter(statName);
                        }
                    }
                }
            }

            // 事件信息
            abilityData.eventExecutors.Clear();
            if (TryGetFieldValue(jsonObject, Object_AbilityEvent, out JsonObject eventObject))
            {
                foreach (var kvp in eventObject)
                {
                    var eventName = kvp.Key;
                    var executorConfig = kvp.Value as JsonObject;
                    if (ResolveEventExecutor(executorConfig, out var executor))
                    {
                        abilityData.eventExecutors[eventName] = executor;
                    }
                }
            }

            return abilityData;
        }

        private IValueGetter<AbilitySpec> ResolveValueGetter(JsonNode node, float defaultValue = 0f)
        {
            if (node is JsonValue valueNode)
            {
                // 如果是数字，直接返回常量值获取器
                if (valueNode.TryGetValue<float>(out var value))
                {
                    return new ConstantValueGetter<AbilitySpec>(value);
                }
                // 如果是字符串，看是否以%开头，表示这是一个引用，要从context中获取
                else if (valueNode.TryGetValue<string>(out var strValue) && strValue.StartsWith("%"))
                {
                    var fieldName = strValue.Substring(1);
                    return new AbilitySpecialGetter(fieldName);
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
                    var customValueGetter = new CustomValueGetter<AbilitySpec>((c) =>
                    {
                        if (!c.TryGetLevelValue(baseStatName, out var baseStatValue))
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

            return new ConstantValueGetter<AbilitySpec>(defaultValue);
        }

        private bool ResolveCost(JsonObject costObject, out IValueGetter<AbilitySpec> func)
        {
            var valueNode = GetFieldValueOrThrow<JsonNode>(costObject, "Value");
            var getter = ResolveValueGetter(valueNode, 0);
            func = getter;
            return true;
        }

        private bool ResolveEventExecutor(JsonObject root, out AbilityEventExecutor executor)
        {
            var targetTeams = GetFieldValueOrThrow<string>(root, "TargetTeams");
            if (!Enum.TryParse<TeamType>(targetTeams, out var teamType))
            {
                throw new Exception($"Invalid TargetTeams value: {targetTeams}");
            }
            executor = new AbilityEventExecutor();
            executor.TeamType = teamType;
            executor.actionExecutors = new List<Ability.ActionExecutor.AbilityActionExecutor>();

            if (TryGetFieldValue<JsonArray>(root, "Action", out var actions))
            {
                foreach (var action in actions)
                {
                    if (action is JsonObject actionObject)
                    {
                        var type = GetFieldValueOrThrow<string>(actionObject, "Type");
                        var actionAction = GetFieldValueOrThrow<JsonObject>(actionObject, "Action");
                        if (type == "Damage")
                        {
                            var damageObj = actionAction;
                            var target = GetFieldValueOrThrow<string>(damageObj, "Target");
                            if (!Enum.TryParse<TargetType>(target, out var targetType))
                            {
                                throw new Exception($"Invalid Target value: {target}");
                            }
                            var element = GetFieldValueOrThrow<string>(damageObj, "Element");
                            var damageValueNode = GetFieldValueOrThrow<JsonNode>(damageObj, "Value");
                            var damageGetter = ResolveValueGetter(damageValueNode, 0);
                            var damageAction = new AbilityActionExecutor_Damage(targetType, element, damageGetter);
                            executor.actionExecutors.Add(damageAction);
                        }
                        if (type == "ApplyModifier")
                        {
                            var modifierObj = actionAction;
                            var target = GetFieldValueOrThrow<string>(modifierObj, "Target");
                            if (!Enum.TryParse<TargetType>(target, out var targetType))
                            {
                                throw new Exception($"Invalid Target value: {target}");
                            }
                            var modifierName = GetFieldValueOrThrow<string>(modifierObj, "ModifierName");
                            var durationNode = GetFieldValueOrThrow<JsonNode>(modifierObj, "Duration");
                            var durationGetter = ResolveValueGetter(durationNode, 0);
                            var applyModifierAction = new AbilityActionExecutor_ApplyModifier(targetType, modifierName, durationGetter);
                            executor.actionExecutors.Add(applyModifierAction);
                        }
                    }
                }
            }
            return true;
        }
    }
}
