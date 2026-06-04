using Godot;
using CombatFramework.Bridge;
using CombatFramework.Unit;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ARPG 训练场 — 组装所有系统，驱动帧循环。
///   玩家 WASD 移动 | Z 普攻 | X AOE | C 充能 | R 重置 | T 清日志
///   3 个敌人带韧性条，全部死亡后自动重置。
///   右上角命座按钮 1~6：点击/键盘解锁。
///   左上角训练面板：能量锁定、敌人无敌、重置。
/// </summary>
public partial class BattleScene : Node2D
{
    private UnitEntity _player;
    private readonly List<UnitEntity> _enemies = new();
    private UnitNode _playerNode;
    private readonly List<UnitNode> _enemyNodes = new();
    private HUD _hud;
    private TrainingPanel _trainingPanel;
    private ConstellationManager _constellation;
    private DamageTracker _damageTracker = new();

    private const float MaxHp = 1000f;
    private const float EnemyHp = 600f;
    private const float MaxEnergy = 100f;
    private const float EnemyToughness = 300f;

    public override void _Ready()
    {
        CFBridge.Initialize(new GodotCFBridge());
        InitPlayer();
        InitEnemies();
        InitServices();
        InitAbilities();
        InitNodes();
        InitHud();

        _hud.Log($"训练场就绪  ATK={_player.GetStat("Atk"):F0}  [Z]普攻 [X]AOE [C]充能 [1~6]命座 [WASD]移动 [R]重置 [T]清Log");
    }

    // ════════════════════════════════════════════════════════
    // 初始化
    // ════════════════════════════════════════════════════════

    private void InitPlayer()
    {
        _player = new UnitEntity { Team = TeamFlag.Friendly };
        HeroConfig.InitStats(_player);
    }

    private void InitEnemies()
    {
        for (int i = 0; i < 3; i++)
        {
            var e = new UnitEntity { Team = TeamFlag.Enemy };
            e.Stats.Set("HP", EnemyHp);
            e.Stats.Set("DefFinal", 0f);
            e.Stats.Set("ToughnessMax", EnemyToughness);
            e.Stats.Set("Toughness", EnemyToughness);
            _enemies.Add(e);
        }
    }

    private void InitServices()
    {
        var allUnits = new List<UnitEntity> { _player };
        allUnits.AddRange(_enemies);
        var shapeSvc = new GodotShapeQueryService(allUnits);
        var vfxSvc = new GodotVfxService();
        var bridge = (GodotCFBridge)CFBridge.Bridge;
        bridge.ShapeQuery = shapeSvc;
        bridge.UnitQuery = shapeSvc;
        bridge.Vfx = vfxSvc;
        shapeSvc.OnShowBoxPreview = HandleBoxPreview;
        shapeSvc.OnShowCirclePreview = HandleCirclePreview;
    }

    private void InitAbilities()
    {
        foreach (var file in HeroConfig.AbilityFiles)
            _player.EquipAbility(AbilityLoader.Create(file));

        _constellation = new ConstellationManager(_player, HeroConfig.ConstellationFiles);
    }

    private void InitNodes()
    {
        var vfxSvc = ((GodotCFBridge)CFBridge.Bridge).Vfx as GodotVfxService;

        _playerNode = GetNode<UnitNode>("PlayerUnit");
        _playerNode.Init(_player, Colors.CornflowerBlue, MaxHp, isPlayer: true);
        vfxSvc?.Register(_player, _playerNode);
        _player.Position = new System.Numerics.Vector3(_playerNode.Position.X, _playerNode.Position.Y, 0f);

        for (int i = 0; i < 3; i++)
        {
            var node = GetNode<UnitNode>($"Enemy{i}");
            node.Init(_enemies[i], Colors.Tomato, EnemyHp);
            vfxSvc?.Register(_enemies[i], node);
            _enemies[i].Position = new System.Numerics.Vector3(node.Position.X, node.Position.Y, 0f);
            _enemyNodes.Add(node);

            if (i < 2)
                _enemies[i].EquipAbility(AbilityLoader.Create("radiance_aura.json", level: i + 1));
        }
    }

    private void InitHud()
    {
        _hud = GetNode<HUD>("UI/HUD");
        _hud.Battle = this;
        _hud.Init(_player);
        _hud.OnUnlockConstellation += UnlockConstellation;

        _trainingPanel = GetNode<TrainingPanel>("UI/TrainingPanel");
        _trainingPanel.OnResetEnemy += ResetEnemies;
        _trainingPanel.OnResetStats += () => _damageTracker.Reset();
    }

    // ════════════════════════════════════════════════════════
    // 命座
    // ════════════════════════════════════════════════════════

    private void UnlockConstellation(int index)
    {
        if (_constellation.Unlock(index))
            _hud.Log($"── 命座 {index} 解锁！──");
    }

    // ════════════════════════════════════════════════════════
    // 输入
    // ════════════════════════════════════════════════════════

    public override void _Input(InputEvent ev)
    {
        if (ev is not InputEventKey key || !key.Pressed || key.Echo) return;

        var target = _enemies.FirstOrDefault(e => e.GetStat("HP") > 0f);

        switch (key.Keycode)
        {
            case Key.Z:      Cast("normal_attack_01", target, "普攻");  break;
            case Key.X:      Cast("skill_aoe",        target, "AOE");   break;
            case Key.C:      Cast("skill_charge",     target, "充能");  break;
            case Key.Key1:   UnlockConstellation(1);                    break;
            case Key.Key2:   UnlockConstellation(2);                    break;
            case Key.Key3:   UnlockConstellation(3);                    break;
            case Key.Key4:   UnlockConstellation(4);                    break;
            case Key.Key5:   UnlockConstellation(5);                    break;
            case Key.Key6:   UnlockConstellation(6);                    break;
            case Key.R:      ResetBattle();                              break;
            case Key.T:      _hud.ClearLog();                            break;
        }
    }

    // ════════════════════════════════════════════════════════
    // 战斗逻辑
    // ════════════════════════════════════════════════════════

    private void Cast(string abilityName, UnitEntity target, string skillName)
    {
        var hpSnap = _enemies.Select(e => e.GetStat("HP")).ToArray();

        // 无敌模式：先锁定敌人 HP
        if (TrainingConfig.EnemyInvincible)
            foreach (var e in _enemies) e.Stats.Set("HP", EnemyHp);

        bool ok = _player.TryCast(abilityName, target);
        if (!ok)
        {
            _hud.Log($"[{skillName}] 失败");
            return;
        }

        // 统计伤害
        float totalDmg = 0f;
        for (int i = 0; i < _enemies.Count; i++)
        {
            float dmg = hpSnap[i] - _enemies[i].GetStat("HP");
            if (dmg > 0f) totalDmg += dmg;
        }
        if (totalDmg > 0f)
            _damageTracker.Record(totalDmg, isCrit: false); // isCrit 可从 DamageContext 获取，简化处理

        float atk = _player.GetStat("Atk");
        var parts = new List<string>();
        if (totalDmg > 0f) parts.Add($"伤害 {totalDmg:F0}");
        if (totalDmg > 0f) parts.Add($"DPS {_damageTracker.Dps:F0}");

        _hud.Log($"[{skillName}] ATK={atk:F0}  {string.Join("  ", parts)}");

        CheckReset();
    }

    private void CheckReset()
    {
        if (_enemies.All(e => e.GetStat("HP") <= 0f))
        {
            _hud.Log("── 全灭！3秒后自动重置 ──");
            GetTree().CreateTimer(3.0).Timeout += ResetBattle;
        }
    }

    private void ResetBattle()
    {
        ResetEnemies();
        _player.Stats.Set("HP", MaxHp);
        _player.Stats.Set("Energy", MaxEnergy);
        _player.ModifierManager.ActivateAll();
        _damageTracker.Reset();
        _hud.Log("── 战场已重置 ──");
    }

    private void ResetEnemies()
    {
        foreach (var e in _enemies)
        {
            e.Stats.Set("HP", EnemyHp);
            e.Stats.Set("Toughness", EnemyToughness);
            e.ModifierManager.ActivateAll();
        }
    }

    // ════════════════════════════════════════════════════════
    // 帧循环
    // ════════════════════════════════════════════════════════

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        _player.Update(dt);
        foreach (var e in _enemies) e.Update(dt);

        // ── 训练模式 ──
        if (TrainingConfig.EnergyLock)
            _player.Stats.Set("Energy", MaxEnergy);

        _damageTracker.Update(dt);

        _hud.Update(
            hp: _player.GetStat("HP"), maxHp: MaxHp,
            energy: _player.GetStat("Energy"), maxEnergy: MaxEnergy,
            constellationUnlocked: _constellation.Unlocked);
        _hud.UpdateStatsPanel(_damageTracker);
    }

    // ════════════════════════════════════════════════════════
    // 范围预览回调
    // ════════════════════════════════════════════════════════

    private void HandleBoxPreview(System.Numerics.Vector3 center, System.Numerics.Vector3 offset, System.Numerics.Vector3 size)
        => _playerNode?.ShowSkillBoxPreview(new Vector2(offset.X, offset.Y), new Vector2(size.X, size.Y));

    private void HandleCirclePreview(System.Numerics.Vector3 center, float radius)
        => _playerNode?.ShowSkillCirclePreview(radius);
}

