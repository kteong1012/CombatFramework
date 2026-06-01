using CombatFramework.Core;
using CombatFramework.Event;

namespace CombatFramework.Core.Stat;

/// <summary>
/// 每个 unit 的属性容器。持有所有属性注册值和复合属性对象。
/// </summary>
public class StatsManager
{
    private const string k_BaseSuffix = "_Base";
    private const string k_MultSuffix = "_Mult";
    private const string k_AddSuffix = "_Add";
    private const string k_MaxSuffix = "_Max";
    private const string k_BlockSuffix = "_Block"; // 负面属性，活动值的最大值 = Max - Block


    // 简单属性（直接用 float）
    private readonly Dictionary<string, float> _flatStats = new();

    public float Get(string id)
    {
        if (TryGet(id, out var flatValue))
        {
            return flatValue;
        }
        if (TryCalcCompound(id, out var compoundValue))
        {
            return compoundValue;
        }
        CFLog.Warning($"Stat {id} not found, return 0");
        return 0f;
    }

    public void Set(string id, float value)
    {
        _flatStats[id] = value;
    }
    public void Add(string id, float value)
    {
        if (!_flatStats.TryGetValue(id, out var current))
        {
            _flatStats[id] = value;
        }
        else
        {
            _flatStats[id] = current + value;
        }
    }

    public void ResetAll()
    {
        _flatStats.Clear();
    }

    private bool TryGet(string id, out float value) => _flatStats.TryGetValue(id, out value);

    // 复合属性计算：y = x * (1 + k) + b
    private bool TryCalcCompound(string xKey, out float result)
    {
        // 约定：属性名若为X，则分量为 X{Base|Mult|Add}

        // 返回值取决于有没有Base，其余2个分量可以没有，默认为0
        if (TryGet(BaseKey(), out var xBase))
        {
            var mult = TryGet(MultKey(), out var m) ? m : 0f;
            var add = TryGet(AddKey(), out var a) ? a : 0f;
            result = xBase * (1 + mult) + add;

            // 如果有 Max，则结果不能超过 Max - Block（Block 也可以没有，默认为0）
            if (TryGet(MaxKey(), out var xMax))
            {
                var block = TryGet(BlockKey(), out var b) ? b : 0f;
                result = Math.Min(result, xMax - block);
            }
            return true;
        }
        result = 0f;
        return false;

        string BaseKey() => xKey + k_BaseSuffix;
        string MultKey() => xKey + k_MultSuffix;
        string AddKey() => xKey + k_AddSuffix;
        string MaxKey() => xKey + k_MaxSuffix;
        string BlockKey() => xKey + k_BlockSuffix;
    }
}
