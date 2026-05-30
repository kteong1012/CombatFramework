using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;

namespace CombatFramework.Lua;

/// <summary>
/// MoonSharp 引擎包装。每场战斗一个 Script 实例，共享于所有能力。
/// DoFile 后应调用 ClearAbilityGlobals() 防止跨文件全局污染。
/// </summary>
public class LuaEngine : IDisposable
{
    private readonly Script _script;
    private bool _disposed;

    public Script Script => _script;

    public LuaEngine()
    {
        _script = new Script(CoreModules.Preset_Default);

        // 沙箱限制：禁用危险 IO
        _script.Options.ScriptLoader = new FileSystemScriptLoader();

        // 注册 C# 类型映射（仅一次）
        RegisterDefaultTypes();

        // 注册 CF API 全局函数
        RegisterGlobalAPI();

        // 注册 ApplyDamage 和 DAMAGE_TYPE_* 常量
        LuaModifierBridge.RegisterGlobals(_script);
    }

    private void RegisterGlobalAPI()
    {
        // ApplyHeal(target, source, amount) — 吸血/治疗 modifier 用
        _script.Globals["ApplyHeal"] = DynValue.NewCallback((_, args) =>
        {
            var target = args[0].ToObject<Unit.UnitEntity>();
            var source = args[1].ToObject<Unit.UnitEntity>();
            var amount = (float)args[2].Number;
            return DynValue.NewNumber(Damage.DamagePipeline.ApplyHeal(target, source, amount));
        });

        // PlayVfxOnUnit(assetPath, unit, lifeTime) — 在单位身上播特效，到期自动消失
        _script.Globals["PlayVfxOnUnit"] = DynValue.NewCallback((_, args) =>
        {
            var path = args[0].CastToString() ?? "";
            var unit = args[1].ToObject<Unit.UnitEntity>();
            var lifeTime = (float)(args.Count > 2 ? args[2].Number : 0);
            var svc = Damage.DamagePipeline.VfxService;
            if (svc != null && !string.IsNullOrEmpty(path) && unit != null)
            {
                return DynValue.NewNumber(svc.PlayOnUnit(path, unit, lifeTime > 0 ? lifeTime : null));
            }
            return DynValue.NewNumber(0);
        });
    }

    private void RegisterDefaultTypes()
    {
        // 框架核心类型注册给 Lua
        UserData.RegisterType<Core.AbilityData>();
        UserData.RegisterType<Core.AbilityInstance>();
        UserData.RegisterType<Core.ModifierData>();
        UserData.RegisterType<Core.ModifierInstance>();
        UserData.RegisterType<Core.TagSystem>();
        UserData.RegisterType<Unit.UnitEntity>();
        UserData.RegisterType<Stat.UnitStats>();
        UserData.RegisterType<Stat.ResourceSystem>();
        UserData.RegisterType<Stat.ResourceSystem.ResourceSlot>();
    }

    /// <summary>执行 Lua 文件</summary>
    public void DoFile(string path)
    {
        _script.DoFile(path);
    }

    /// <summary>执行 Lua 字符串</summary>
    public void DoString(string code)
    {
        _script.DoString(code);
    }

    /// <summary>获取全局表</summary>
    public Table GetGlobalTable() => _script.Globals;

    /// <summary>获取全局变量</summary>
    public DynValue GetGlobal(string name) => _script.Globals.Get(name);

    /// <summary>调用全局函数</summary>
    public DynValue Call(string functionName, params object[] args)
    {
        var fn = _script.Globals.Get(functionName);
        if (fn.Type != DataType.Function) return DynValue.Nil;
        return _script.Call(fn, args);
    }

    /// <summary>获取 Closure（函数引用），用于缓存</summary>
    public Closure? GetClosure(string name)
    {
        var val = _script.Globals.Get(name);
        return val.Type == DataType.Function ? val.Function : null;
    }

    /// <summary>设置全局变量</summary>
    public void SetGlobal(string name, object value)
    {
        _script.Globals[name] = value;
    }

    /// <summary>清除上一个能力文件留下的 AbilityData / Modifiers 表，防止跨文件全局污染。</summary>
    public void ClearAbilityGlobals()
    {
        var g = _script.Globals;
        g.Set("AbilityData", DynValue.Nil);
        g.Set("Modifiers", DynValue.Nil);
        g.Set("Effects", DynValue.Nil);
        g.Set("OnSpellStart", DynValue.Nil);
        g.Set("OnAbilityPhaseStart", DynValue.Nil);
        g.Set("OnProjectileHit", DynValue.Nil);
        g.Set("OnProjectileThink", DynValue.Nil);
        g.Set("Transforms", DynValue.Nil);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            // MoonSharp Script 无 Dispose；释放引用即可让 GC 回收
        }
    }
}
