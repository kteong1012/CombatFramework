using MoonSharp.Interpreter;
using CombatFramework.Core;

namespace CombatFramework.Lua;

/// <summary>
/// Lua 能力加载器。两阶段生命周期：
///   1) 加载时（启动/编辑器）→ ScanDirectory → 缓存脚本文本 + 解析出静态 AbilityData（不含 Closure）
///   2) 战斗时 → BindOnEngine(共享 LuaEngine) → DoString 提取 Closure，重新绑定到缓存的 AbilityData
///   战斗结束 → Engine.Dispose()，AbilityData 的 Closure 字段变 null。
/// </summary>
public class LuaAbilityLoader
{
    private static readonly HashSet<string> _lifecycleFuncs = new()
    {
        "OnCreated", "OnRefresh", "OnDestroy",
        "OnIntervalThink", "OnStackCountChanged",
    };

    private readonly Dictionary<string, AbilityData> _cache = new();
    private readonly Dictionary<string, string> _scriptTexts = new();
    private readonly string _scriptDirectory;

    public IReadOnlyDictionary<string, AbilityData> Cache => _cache;

    public LuaAbilityLoader(string scriptDirectory)
    {
        _scriptDirectory = scriptDirectory;
    }

    // ── 阶段一：加载缓存 ──

    /// <summary>扫描目录，缓存脚本文本并解析出静态参数。不创建 LuaEngine。</summary>
    public void ScanDirectory()
    {
        if (!Directory.Exists(_scriptDirectory)) return;

        foreach (var file in Directory.GetFiles(_scriptDirectory, "*.lua", SearchOption.AllDirectories))
        {
            LoadFile(file);
        }
    }

    /// <summary>加载单个能力文件。缓存文本 + 解析静态数据（不含 Closure）。</summary>
    public AbilityData? LoadFile(string filePath)
    {
        try
        {
            var text = File.ReadAllText(filePath);
            var fallbackId = Path.GetFileNameWithoutExtension(filePath);

            AbilityData? data;
            using (var temp = new LuaEngine())
            {
                temp.DoString(text);
                data = ParseAbilityFromEngine(temp, fallbackId, extractClosures: false);
                if (data == null) return null;
                _cache[data.Id] = data;
                _scriptTexts[data.Id] = text;
            }

            return data;
        }
        catch (Exception ex)
        {
            CFLog.Error($"[LuaAbilityLoader] 加载失败: {filePath} - {ex.Message}");
            return null;
        }
    }

    // ── 阶段二：战斗期绑定 ──

    /// <summary>
    /// 在共享 LuaEngine 上重新执行所有缓存的脚本，提取 Closure 绑定到缓存的 AbilityData。
    /// 调用前 EnsureEngine 须已准备好；调用后 ClearAbilityGlobals 自动清理。
    /// </summary>
    public void BindOnEngine(LuaEngine engine)
    {
        foreach (var kv in _scriptTexts)
        {
            try
            {
                engine.DoString(kv.Value);
                var data = ParseAbilityFromEngine(engine, kv.Key, extractClosures: true);
                if (data != null)
                {
                    // 合并 Closure 到已有缓存的 AbilityData
                    if (_cache.TryGetValue(kv.Key, out var existing))
                    {
                        existing.EventHandlers = data.EventHandlers;
                        existing.ModifierTemplates = data.ModifierTemplates;
                        // Effect ops 不会包含 Closure，但重新解析一遍确保数据新鲜
                        existing.Effects = data.Effects;
                    }
                }
                engine.ClearAbilityGlobals();
            }
            catch (Exception ex)
            {
                CFLog.Error($"[LuaAbilityLoader] BindOnEngine 失败: {kv.Key} - {ex.Message}");
            }
        }
    }

    /// <summary>从 Lua 脚本文本解析 AbilityData（用于 Unity Raw 资源加载）。</summary>
    public static AbilityData? ParseAbilityScript(string scriptContent, string fallbackId = "")
    {
        using var engine = new LuaEngine();
        try
        {
            engine.DoString(scriptContent);
            return ParseAbilityFromEngine(engine, fallbackId, extractClosures: true);
        }
        catch (Exception ex)
        {
            CFLog.Error($"[LuaAbilityLoader] ParseAbilityScript 失败: {ex.Message}");
            return null;
        }
    }

    private static AbilityData? ParseAbilityFromEngine(LuaEngine engine, string fallbackId, bool extractClosures)
    {
        var globals = engine.GetGlobalTable();

        // 读取 AbilityData 表
        var abilityTable = globals.Get("AbilityData").Table;
        if (abilityTable == null) return null;

        var data = new AbilityData
        {
            Id = GetTableString(abilityTable, "id") ?? fallbackId,
            Name = GetTableString(abilityTable, "name") ?? "",
            Cooldown = (float)(GetTableNumber(abilityTable, "cooldown") ?? 0),
            CastRange = (float)(GetTableNumber(abilityTable, "castRange") ?? 0),
            CastPoint = (float)(GetTableNumber(abilityTable, "castPoint") ?? 0),
            CastAnimation = GetTableString(abilityTable, "castAnimation") ?? "",
            MaxCharge = (int)(GetTableNumber(abilityTable, "charge") ?? 1),
            RechargeTime = (float)(GetTableNumber(abilityTable, "rechargeTime") ?? 0),
        };

        // 读取资源消耗
        var costsTable = abilityTable.Get("costs").Table;
        if (costsTable != null)
        {
            foreach (var kv in costsTable.Pairs)
                data.Costs[kv.Key.String] = (float)kv.Value.Number;
        }

        // 读取参数（支持单值或数组）
        var paramsTable = abilityTable.Get("parameters").Table;
        if (paramsTable != null)
        {
            foreach (var kv in paramsTable.Pairs)
            {
                var key = kv.Key.String;
                if (key == null) continue;

                var val = kv.Value;
                if (val.Type == DataType.Number)
                {
                    data.Parameters[key] = new[] { (float)val.Number };
                }
                else if (val.Type == DataType.Table)
                {
                    var arr = new List<float>();
                    foreach (var item in val.Table.Values)
                    {
                        if (item.Type == DataType.Number)
                            arr.Add((float)item.Number);
                    }
                    data.Parameters[key] = arr.ToArray();
                }
            }
        }

        // 读取投射物配置
        var projTable = abilityTable.Get("projectile").Table;
        if (projTable != null)
        {
            data.Projectile = new ProjectileConfig
            {
                Model = GetTableString(projTable, "model") ?? "",
                Speed = (float)(GetTableNumber(projTable, "speed") ?? 0),
                Radius = (float)(GetTableNumber(projTable, "radius") ?? 0),
                IsTracking = GetTableBool(projTable, "isTracking"),
                Dodgeable = GetTableBool(projTable, "dodgeable"),
                VisibleToEnemies = GetTableBool(projTable, "visibleToEnemies", true),
            };
        }

        // 读取 Modifiers 表
        var modsTable = abilityTable.Get("Modifiers").Table;
        if (modsTable != null)
        {
            foreach (var kv in modsTable.Pairs)
            {
                var modName = kv.Key.String;
                var modTable = kv.Value.Table;
                if (modTable != null)
                {
                    data.ModifierTemplates[modName] = ParseModifierData(modName, modTable);
                }
            }
        }

        // 读取 Effects 表
        var effectsTable = abilityTable.Get("Effects").Table;
        if (effectsTable != null)
        {
            foreach (var kv in effectsTable.Pairs)
            {
                var effectKey = kv.Key.String;
                var effectTable = kv.Value.Table;
                if (!string.IsNullOrEmpty(effectKey) && effectTable != null)
                {
                    data.Effects[effectKey] = ParseEffectData(effectKey, effectTable);
                }
            }
        }

        // 只有在 extractClosures=true 时才提取 Closure
        // 从 AbilityData 表中扫描函数值
        if (extractClosures && abilityTable != null)
        {
            foreach (var kv in abilityTable.Pairs)
            {
                var name = kv.Key.String;
                if (string.IsNullOrEmpty(name)) continue;
                if (name == "id" || name == "name" || name == "cooldown" || name == "castRange"
                    || name == "castPoint" || name == "castAnimation" || name == "costs"
                    || name == "parameters" || name == "projectile" || name == "Modifiers"
                    || name == "Effects" || name == "charge" || name == "rechargeTime") continue;
                if (kv.Value.Type == DataType.Function)
                    data.EventHandlers[name] = kv.Value.Function;
            }
        }

        return data;
    }

    // ── Modifier 脚本解析（不变）──

    public static Dictionary<string, ModifierData> ParseModifierScript(string scriptContent)
    {
        using var engine = new LuaEngine();
        return ParseModifierScript(scriptContent, engine);
    }

    public static Dictionary<string, ModifierData> ParseModifierScript(string scriptContent, LuaEngine engine)
    {
        var result = new Dictionary<string, ModifierData>();
        if (string.IsNullOrEmpty(scriptContent)) return result;

        try
        {
            engine.DoString(scriptContent);
            var globals = engine.GetGlobalTable();
            var modsTable = globals.Get("Modifiers").Table;
            if (modsTable != null)
            {
                foreach (var kv in modsTable.Pairs)
                {
                    var modName = kv.Key.String;
                    var modTable = kv.Value.Table;
                    if (!string.IsNullOrEmpty(modName) && modTable != null)
                        result[modName] = ParseModifierData(modName, modTable);
                }
            }

            globals.Set("Modifiers", DynValue.Nil);
        }
        catch (Exception ex)
        {
            CFLog.Error($"[LuaAbilityLoader] ParseModifierScript 失败: {ex.Message}");
        }

        return result;
    }

    private static ModifierData ParseModifierData(string name, Table t)
    {
        var data = new ModifierData
        {
            Name = name,
            IsBuff = GetTableBool(t, "isBuff"),
            IsDebuff = GetTableBool(t, "isDebuff"),
            IsHidden = GetTableBool(t, "isHidden"),
            IsPurgable = GetTableBool(t, "isPurgable", true),
        };

        // ── duration（支持 %引用） ──
        var durationStr = GetTableString(t, "duration");
        if (durationStr != null && durationStr.StartsWith("%"))
            data.DurationRef = durationStr.Substring(1);
        else
            data.Duration = (float)(GetTableNumber(t, "duration") ?? 0);

        var attrStr = GetTableString(t, "attribute");
        data.Attribute = attrStr switch
        {
            "MULTIPLE" => ModifierAttribute.Multiple,
            "STACK_COUNT" or "StackCount" => ModifierAttribute.StackCount,
            "PERMANENT" or "Permanent" => ModifierAttribute.Permanent,
            _ => ModifierAttribute.None,
        };

        var declareTagsTable = t.Get("DeclareTags").Table;
        if (declareTagsTable != null)
        {
            foreach (var kv in declareTagsTable.Pairs)
            {
                var tagStr = kv.Value.String ?? kv.Value.ToObject<string>() ?? "";
                if (!string.IsNullOrEmpty(tagStr))
                    data.DeclareTags.Add(tagStr);
            }
        }

        var statesTable = t.Get("CheckState").Table;
        if (statesTable != null)
        {
            foreach (var kv in statesTable.Pairs)
                data.States[kv.Key.String] = kv.Value.Boolean;
        }

        data.OnCreatedFn = t.Get("OnCreated").Function;
        data.OnRefreshFn = t.Get("OnRefresh").Function;
        data.OnDestroyFn = t.Get("OnDestroy").Function;
        data.OnIntervalThinkFn = t.Get("OnIntervalThink").Function;
        data.OnStackCountChangedFn = t.Get("OnStackCountChanged").Function;

        var declaredTable = t.Get("DeclareFunctions").Table;
        if (declaredTable != null)
        {
            foreach (var kv in declaredTable.Pairs)
            {
                var hookName = kv.Value.String ?? kv.Value.ToObject<string>() ?? "";
                if (Enum.TryParse<ModifierHookType>(hookName, out var hookType))
                    data.DeclaredHooks.Add(hookType);
                else
                    data.DeclaredStats.Add(hookName);
            }
        }

        foreach (var kv in t.Pairs)
        {
            var key = kv.Key.String;
            if (string.IsNullOrEmpty(key)) continue;
            if (_lifecycleFuncs.Contains(key)) continue;

            var fn = kv.Value.Function;
            if (fn == null) continue;

            data.PropertyHooks[key] = fn;

            if (Enum.TryParse<ModifierHookType>(key, out var autoHook))
            {
                if (!data.DeclaredHooks.Contains(autoHook))
                    data.DeclaredHooks.Add(autoHook);
            }
            else
            {
                if (!data.DeclaredStats.Contains(key))
                    data.DeclaredStats.Add(key);
            }
        }

        var propsTable = t.Get("Properties").Table;
        if (propsTable != null)
        {
            foreach (var kv in propsTable.Pairs)
            {
                var valStr = kv.Value.String;
                if (valStr != null && valStr.StartsWith("%"))
                {
                    data.PropertyRefs[kv.Key.String] = valStr.Substring(1);
                }
                else
                {
                    data.Properties[kv.Key.String] = (float)kv.Value.Number;
                }
            }
        }

        data.EffectName = GetTableString(t, "EffectName");
        data.EffectAttachType = GetTableString(t, "EffectAttachType");

        var statModifiersTable = t.Get("StatModifiers").Table;
        if (statModifiersTable != null)
        {
            foreach (var kv in statModifiersTable.Pairs)
            {
                var entryTable = kv.Value.Table;
                if (entryTable == null) continue;

                var stat = GetTableString(entryTable, "stat") ?? string.Empty;
                if (string.IsNullOrEmpty(stat)) continue;

                var opText = GetTableString(entryTable, "op") ?? "Add";
                if (!Enum.TryParse<StatOp>(opText, ignoreCase: true, out var op))
                    op = StatOp.Add;

                data.StatModifiers.Add(new StatModifierEntry
                {
                    Stat = stat,
                    Op = op,
                    Value = (float)(GetTableNumber(entryTable, "value") ?? 0),
                });
            }
        }

        var auraTable = t.Get("Aura").Table;
        if (auraTable != null)
        {
            data.Aura = new AuraConfig
            {
                Radius = (float)(GetTableNumber(auraTable, "radius") ?? 0),
                TargetModifier = GetTableString(auraTable, "targetModifier") ?? "",
            };
        }

        return data;
    }

    private static string? GetTableString(Table t, string key) =>
        t.Get(key).String;
    private static double? GetTableNumber(Table t, string key) =>
        t.Get(key).Number;
    private static bool GetTableBool(Table t, string key, bool defaultValue = false)
    {
        var val = t.Get(key);
        return val.IsNil() ? defaultValue : val.Boolean;
    }

    private static AbilityEffectData ParseEffectData(string key, Table t)
    {
        var data = new AbilityEffectData { Key = key };

        var targetsTable = t.Get("ActOnTargets").Table ?? t.Get("picker").Table;
        if (targetsTable != null) data.Picker = ParsePickerData(targetsTable);

        var isActionFormat = t.Get("Action").Table != null;
        var actionTable = isActionFormat ? t.Get("Action").Table : t.Get("ops").Table;
        if (actionTable != null)
        {
            foreach (var kv in actionTable.Pairs)
            {
                var opTable = kv.Value.Table;
                if (opTable == null) continue;

                AbilityEffectOpType opType;
                if (isActionFormat)
                {
                    // Action 字典：key = "Damage", "Modifier"...
                    var typeStr = kv.Key.String;
                    if (!Enum.TryParse<AbilityEffectOpType>(typeStr, ignoreCase: true, out opType))
                        continue;
                }
                else
                {
                    // ops 数组：内部 type 字段
                    var typeStr = GetTableString(opTable, "type") ?? "";
                    if (!Enum.TryParse<AbilityEffectOpType>(typeStr, ignoreCase: true, out opType))
                        continue;
                }

                var dmgVal = (opType == AbilityEffectOpType.Damage || opType == AbilityEffectOpType.Heal)
                    ? ParseDamageValue(opTable) : null;

                data.Operations.Add(new AbilityEffectOp
                {
                    Target = GetTableString(opTable, "Target") ?? GetTableString(opTable, "target") ?? "TARGET",
                    Type = opType,
                    Damage = dmgVal,
                    DamageType = GetTableString(opTable, "Element") ?? GetTableString(opTable, "damageType") ?? string.Empty,
                    ModifierName = GetTableString(opTable, "Modifier") ?? GetTableString(opTable, "modifier") ?? string.Empty,
                    Duration = (float)(GetTableNumber(opTable, "Duration") ?? GetTableNumber(opTable, "duration") ?? 0),
                    HitNum = (int)(GetTableNumber(opTable, "HitNum") ?? GetTableNumber(opTable, "hitNum") ?? 1),
                    Projectile = ParseProjectileConfig(opTable.Get("projectile").Table),
                    Thinker = ParseThinkerConfig(opTable.Get("thinker").Table),
                });
            }
        }

        return data;
    }

    private static DamageValue? ParseDamageValue(Table opTable)
    {
        var dmgField = opTable.Get("Damage");
        if (dmgField.IsNil()) dmgField = opTable.Get("damage");
        if (dmgField.IsNil()) return null;

        if (dmgField.Type == DataType.Number)
            return new DamageValue { Constant = (float)dmgField.Number };

        if (dmgField.Type == DataType.String)
        {
            var s = dmgField.String;
            if (s.StartsWith("%")) return new DamageValue { ParamRef = s.Substring(1) };
            if (float.TryParse(s, out var f)) return new DamageValue { Constant = f };
            return null;
        }

        if (dmgField.Type == DataType.Table)
        {
            var fn = dmgField.Table.Get("RunFunctionInAbility").String;
            if (fn == null) fn = dmgField.Table.Get("runFunctionInAbility").String;
            if (fn != null) return new DamageValue { RunFunctionInAbility = fn };
        }

        return null;
    }

    private static TargetPickerData ParsePickerData(Table t)
    {
        var picker = new TargetPickerData
        {
            Type = GetTableString(t, "type") ?? string.Empty,
            Filter = GetTableString(t, "filter") ?? string.Empty,
            OriginAnchor = GetTableString(t, "originAnchor") ?? string.Empty,
        };
        var offsetTable = t.Get("offset").Table;
        if (offsetTable != null)
        {
            picker.OffsetX = (float)(GetTableNumber(offsetTable, "x") ?? 0);
            picker.OffsetY = (float)(GetTableNumber(offsetTable, "y") ?? 0);
            picker.OffsetZ = (float)(GetTableNumber(offsetTable, "z") ?? 0);
        }
        var shapeTable = t.Get("shape").Table;
        if (shapeTable != null) picker.Shape = ParseShapeData(shapeTable);
        return picker;
    }

    private static AbilityProjectileConfig? ParseProjectileConfig(Table? t)
    {
        if (t == null) { return null; }
        return new AbilityProjectileConfig
        {
            Model = GetTableString(t, "model") ?? "",
            Speed = (float)(GetTableNumber(t, "speed") ?? 0),
            Radius = (float)(GetTableNumber(t, "radius") ?? 0),
            Distance = (float)(GetTableNumber(t, "distance") ?? 0),
            DeleteOnHit = GetTableBool(t, "deleteOnHit", true),
            ProvidesVision = GetTableBool(t, "providesVision"),
            VisionRadius = (float)(GetTableNumber(t, "visionRadius") ?? 0),
        };
    }

    private static ThinkerConfig? ParseThinkerConfig(Table? t)
    {
        if (t == null) return null;

        var cfg = new ThinkerConfig
        {
            Delay = (float)(GetTableNumber(t, "delay") ?? 0),
        };
        var shapeTable = t.Get("shape").Table;
        if (shapeTable != null) cfg.Shape = ParseShapeData(shapeTable);

        var opsTable = t.Get("ops").Table;
        if (opsTable != null)
        {
            foreach (var kv in opsTable.Pairs)
            {
                var opTable = kv.Value.Table;
                if (opTable == null) continue;

                var typeStr = GetTableString(opTable, "type") ?? string.Empty;
                if (!Enum.TryParse<AbilityEffectOpType>(typeStr, ignoreCase: true, out var opType))
                    continue;
                if (opType != AbilityEffectOpType.Damage && opType != AbilityEffectOpType.Modifier)
                    continue; // Thinker 子 ops 只支持 Damage / Modifier

                var co = new AbilityEffectOp
                {
                    Target = GetTableString(opTable, "target") ?? "TARGET",
                    Type = opType,
                    DamageType = GetTableString(opTable, "damageType") ?? string.Empty,
                    ModifierName = GetTableString(opTable, "modifier") ?? string.Empty,
                    Duration = (float)(GetTableNumber(opTable, "duration") ?? 0),
                };
                // Thinker child Damage 暂不支持 % 引用，只支持常量
                if (opType == AbilityEffectOpType.Damage)
                {
                    var raw = (float)(GetTableNumber(opTable, "raw") ?? (GetTableNumber(opTable, "damage") ?? 0));
                    if (raw > 0) co.Damage = new DamageValue { Constant = raw };
                }
                cfg.ChildOps.Add(co);
            }
        }

        return cfg;
    }

    private static ShapeData ParseShapeData(Table t)
    {
        var offsetTable = t.Get("offset").Table;
        var rotateTable = t.Get("rotate").Table;
        var scaleTable = t.Get("scale").Table;
        return new ShapeData
        {
            Type = GetTableString(t, "type") ?? string.Empty,
            Radius = (float)(GetTableNumber(t, "radius") ?? 0),
            Height = (float)(GetTableNumber(t, "height") ?? 0),
            Angle = (float)(GetTableNumber(t, "angle") ?? 0),
            OffsetX = offsetTable != null ? (float)(GetTableNumber(offsetTable, "x") ?? 0) : 0,
            OffsetY = offsetTable != null ? (float)(GetTableNumber(offsetTable, "y") ?? 0) : 0,
            OffsetZ = offsetTable != null ? (float)(GetTableNumber(offsetTable, "z") ?? 0) : 0,
            RotateX = rotateTable != null ? (float)(GetTableNumber(rotateTable, "x") ?? 0) : 0,
            RotateY = rotateTable != null ? (float)(GetTableNumber(rotateTable, "y") ?? 0) : 0,
            RotateZ = rotateTable != null ? (float)(GetTableNumber(rotateTable, "z") ?? 0) : 0,
            ScaleX = scaleTable != null ? (float)(GetTableNumber(scaleTable, "x") ?? 0) : 0,
            ScaleY = scaleTable != null ? (float)(GetTableNumber(scaleTable, "y") ?? 0) : 0,
            ScaleZ = scaleTable != null ? (float)(GetTableNumber(scaleTable, "z") ?? 0) : 0,
        };
    }
}
