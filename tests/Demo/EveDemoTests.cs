namespace CombatFramework.Tests.Demo;

using System.Numerics;
using CombatFramework.Core;
using CombatFramework.Damage;
using CombatFramework.Lua;
using CombatFramework.Modifier;
using CombatFramework.Unit;
using CStat = CombatFramework.Stat;

/// <summary>
/// Eve 角色 Demo 测试。
/// 能力配置全部来自 Lua 文件，测试只负责编排和验证。
/// </summary>
public class EveDemoTests
{
    private static readonly string AbilitiesDir = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "tests", "Demo", "abilities"));

    private UnitEntity _eve = null!;
    private CStat.ResourceSystem _res => _eve.Resources;
    private ModifierManager _mods => _eve.ModifierManager;
    private CStat.UnitStats _stats => _eve.Stats;
    private Dictionary<string, ModifierData> _cMods = null!;
    private AbilityInstance? _cInst;

    public EveDemoTests()
    {
        DamageTypes.RegisterDefaults();
        CStat.StatDefinition.RegisterDefaults();
    }

    private AbilityData? LoadAbility(string fn) =>
        LuaAbilityLoader.ParseAbilityScript(File.ReadAllText(Path.Combine(AbilitiesDir, fn)));

    private AbilityInstance Equip(string fn, int lv = 1)
    {
        var d = LoadAbility(fn);
        Assert.NotNull(d);
        var i = new AbilityInstance(d!, _eve);
        i.SetLevel(lv);
        if (d!.Cooldown > 0) i.StartCooldown(d.Cooldown);
        _eve.AbilitySlots.Equip(i);
        return i;
    }

    private void P(string label) =>
        Console.WriteLine($"[{label}] ATK={_eve.GetStat("Attack"):F1} DEF={_eve.GetStat("Defense"):F1} HP={_eve.GetStat("HP"):F1} CritRate={_eve.GetStat(StatId.CritRate):F3} CritDMG={_eve.GetStat(StatId.CritDMG):F3}");

    private void PR() =>
        Console.WriteLine($"  资源: HP={_res.GetCurrent(StatId.HP):F0} Energy={_res.GetCurrent(StatId.Energy):F0} 时空尘={_res.GetCurrent("TimeDust"):F0}");


    private void C(uint id)
    {
        var key = $"eve_c{id}";
        if (_cMods.TryGetValue(key, out var mod))
            _mods.Add(mod, _eve, _cInst, sourceTag: "constellation");
        _mods.Update(0);
    }

    [Fact]
    public void Eve_FullDemo()
    {
        // ── 1. 角色初始化 ──
        _eve = new UnitEntity { Id = "Eve", Name = "Eve" };
        _res.Register(StatId.HP, 5000, 5000);
        _res.Register(StatId.Energy, 100, 100);
        _res.Register("TimeDust", 0, 10);
        _stats.Attack.SetBase(800);
        _stats.Defense.SetBase(500);
        _stats.HP.SetBase(5000);
        _stats.SetFlat(StatId.CritRate, 0.05f);
        _stats.SetFlat(StatId.CritDMG, 50f);

        var cd = LoadAbility("eve_constellation.lua");
        Assert.NotNull(cd);
        _cMods = cd!.ModifierTemplates; _cInst = new AbilityInstance(cd!, _eve);

        Console.WriteLine("═══ 初始 ═══"); P("初始"); PR(); Console.WriteLine();

        // ── 2. 装备技能（Lua） ──
        var l1 = Equip("eve_atk_light01.lua", 1);
        var l2 = Equip("eve_atk_light02.lua", 1);
        var l3 = Equip("eve_atk_light03.lua", 1);
        var hv = Equip("eve_atk_heavy.lua", 1);
        var ul = Equip("eve_ultimate.lua", 1);
        Equip("eve_atk_light_ex01.lua", 1);
        Equip("eve_atk_light_ex02.lua", 1);
        Equip("eve_atk_light_ex03.lua", 1);
        Equip("eve_atk_heavy_ex.lua", 1);

        Console.WriteLine("═══ 技能装备（Lv1 Lua） ═══");
        Console.WriteLine($"  light01 coeff={l1.GetParameter("coeff"):F2} (预期 0.85)");
        Console.WriteLine($"  ultimate coeff={ul.GetParameter("coeff"):F2} (预期 3.80)");
        Console.WriteLine();

        // ── 3. 技能升级 ──
        l1.SetLevel(5); l2.SetLevel(5); l3.SetLevel(5); hv.SetLevel(10); ul.SetLevel(10);
        Console.WriteLine("═══ 技能升级 ═══");
        Console.WriteLine($"  light01 Lv5 coeff={l1.GetParameter("coeff"):F2} (预期 1.05)");
        Console.WriteLine($"  heavy Lv10 coeff={hv.GetParameter("coeff"):F2} (越界 max=2.60)");
        Console.WriteLine();

        // ── 4. 武器装卸 ──
        Console.WriteLine("═══ 武器装卸 ═══");
        var wd = LoadAbility("eve_weapon.lua");
        Assert.NotNull(wd);
        var wp = wd!.ModifierTemplates["weapon_crit_dmg"];
        var wi = new AbilityInstance(wd, _eve);
        wi.SetLevel(1);
        _eve.AbilitySlots.Equip(wi);

        _mods.Add(wp, _eve, wi, sourceTag: "weapon");
        _stats.Attack.AddExtra(200); _stats.Defense.AddExtra(100); _stats.HP.AddExtra(1000);
        _mods.Update(0); P("装备武器后");

        _mods.RemoveBySourceTag("weapon");
        _stats.Attack.AddExtra(-200); _stats.Defense.AddExtra(-100); _stats.HP.AddExtra(-1000);
        P("卸下武器后"); Console.WriteLine("");

        _mods.Add(wp, _eve, wi, sourceTag: "weapon");
        _stats.Attack.AddExtra(200); _stats.Defense.AddExtra(100); _stats.HP.AddExtra(1000);
        P("再次装备武器"); Console.WriteLine();

        // ── 5. 真实伤害测试（SkillSimulator 执行 Lua effects） ──
        Console.WriteLine("═══ 真实伤害测试 ═══");
        var enemy = new UnitEntity { Id = "Enemy", Name = "训练假人", Team = TeamFlag.Enemy };
        enemy.Resources.Register(StatId.HP, 50000, 50000);
        enemy.Stats.Defense.SetBase(300);
        var enemyHpBefore = enemy.Resources.GetCurrent(StatId.HP);

        var l3Data = LoadAbility("eve_atk_light03.lua");
        var ultData = LoadAbility("eve_ultimate.lua");
        Assert.NotNull(l3Data); Assert.NotNull(ultData);

        Console.WriteLine($"  轻攻击三段 Lv{l3.Level} coeff={l3.GetParameter("coeff"):F2} (from Lua Effects)");
        SkillSimulator.Cast(_eve, l3Data, enemy);
        var hpAfterLight03 = enemy.Resources.GetCurrent(StatId.HP);
        Console.WriteLine($"  假人 HP={hpAfterLight03:F0} (damage={50000 - hpAfterLight03:F0})");

        _res.Restore("TimeDust", 10);
        Console.WriteLine($"  终结技 Lv{ul.Level} coeff={ul.GetParameter("coeff"):F2} (from Lua effects)");
        SkillSimulator.Cast(_eve, ultData!, enemy);
        Console.WriteLine($"  假人 HP={enemy.Resources.GetCurrent(StatId.HP):F0}");
        Assert.True(enemy.Resources.GetCurrent(StatId.HP) < enemyHpBefore, "假人应受到伤害");
        Console.WriteLine();

        // ── 6. 命座 ──
        Console.WriteLine("═══ 命座 ═══");
        C(1); Console.WriteLine("  1命：DamageInc+15% (Lua eve_c1)"); P("1命");
        C(2); Console.WriteLine($"  2命：+2Lv → light01 EffectLv={l1.EffectiveLevel}");
        C(3); C(4); Console.WriteLine($"  4命：终极技+2 → ult BonusLv={ul.BonusLevel}");
        C(5); Console.WriteLine($"  5命：+2Lv → light01 EffectLv={l1.EffectiveLevel} coeff={l1.GetParameter("coeff"):F2}");
        C(6); Console.WriteLine("  6命：DamageInc+6% (Lua eve_c6)"); P("6命");
        Console.WriteLine();

        // ── 6. 技能转换 ──
        Console.WriteLine("═══ 技能转换 ═══");
        void DoL3()
        {
            if (_res.GetCurrent("TimeDust") >= 2)
            {
                _res.TryConsume("TimeDust", 2);
                var ex = _eve.GetAbility("eve_atk_light_ex03") ?? l3;
                Console.WriteLine($"  强化轻攻击三段！伤害≈{_eve.GetStat("Attack") * ex.GetParameter("coeff"):F0}");
            }
            else Console.WriteLine($"  轻攻击三段 伤害≈{_eve.GetStat("Attack") * l3.GetParameter("coeff"):F0}");
        }
        void DoUlt() { _res.Restore("TimeDust", 10); Console.WriteLine($"  终结技！时空尘→{_res.GetCurrent("TimeDust"):F0}"); }
        DoL3(); DoUlt(); DoL3();
        Console.WriteLine($"  剩余时空尘：{_res.GetCurrent("TimeDust"):F0}");
        Console.WriteLine();

        // ── 7. 最终 ──
        Console.WriteLine("═══ 最终 ═══"); P("Eve"); PR();
        Console.WriteLine($"  light01 Lv{l1.Level}+{l1.BonusLevel}={l1.EffectiveLevel} coeff={l1.GetParameter("coeff"):F2}");

        Assert.InRange(_eve.GetStat("Attack"), 950, 1050);
        Assert.Equal(0.05f, _eve.GetStat(StatId.CritRate));
        Assert.InRange(_eve.GetStat(StatId.CritDMG), 85, 95); // 50 + 70(武器%crit_dmg_bonus, 被C2/C5提升到精炼5)
        Assert.Equal(4, l1.BonusLevel);
        Assert.Equal(9, l1.EffectiveLevel);
        Assert.Equal(1.25f, l1.GetParameter("coeff"));
        Assert.InRange(_eve.GetStat(StatId.DamageInc), 20, 22);
        Assert.Equal(8, _res.GetCurrent("TimeDust"));
    }
}
