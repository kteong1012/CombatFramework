using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using Newtonsoft.Json;

/// <summary>
/// 泛型配置加载器。从 Configs/Data/{T}Config/ 目录加载所有 JSON，
/// 文件名为数字 key（如 1001.json → key=1001）。
/// </summary>
public static class ConfigManager
{
    private static readonly Dictionary<Type, object> _stores = new();

    private static string ConfigRoot =>
        ProjectSettings.GlobalizePath("res://Configs/Data");

    /// <summary>加载指定类型的所有配置，一次调用全局缓存。</summary>
    public static void LoadAll<T>() where T : class
    {
        var t = typeof(T);
        if (_stores.ContainsKey(t)) return;

        var dir = Path.Combine(ConfigRoot, $"{t.Name}");
        var dict = new Dictionary<int, T>();

        if (Directory.Exists(dir))
        {
            foreach (var file in Directory.GetFiles(dir, "*.json"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (!int.TryParse(name, out var id)) continue;

                var json = File.ReadAllText(file);
                var obj = JsonConvert.DeserializeObject<T>(json);
                if (obj != null) dict[id] = obj;
            }
        }

        _stores[t] = dict;
    }

    /// <summary>获取指定类型的所有配置。</summary>
    public static Dictionary<int, T> GetAll<T>() where T : class
    {
        LoadAll<T>();
        return (Dictionary<int, T>)_stores[typeof(T)];
    }

    /// <summary>按 key 获取单个配置。</summary>
    public static T Get<T>(int id) where T : class
    {
        var all = GetAll<T>();
        return all.TryGetValue(id, out var v) ? v : null;
    }

    /// <summary>清除所有缓存（热重载用，demo 不需要）。</summary>
    public static void Clear() => _stores.Clear();
}
