using Godot;
using CombatFramework.Bridge;
using CombatFramework.Core;
using CombatFramework.Core.Ability;
using CombatFramework.Core.Enums;
using CombatFramework.Core.Model;
using CombatFramework.Unit;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// 战斗演示主场景。
///   - WASD 移动玩家
///   - Z：普攻（单体，ATK×0.5，耗能 30）
///   - X：AOE  （全场，ATK×0.8，耗能 40）
///   - C：吸血  （全场，ATK×0.3 伤害 + ATK×0.15 回血，耗能 50）
///   - 3 个敌人，全部死亡后自动重置
/// </summary>
public partial class BattleScene : Node2D
{
    private UnitEntity _player;
    private readonly List<UnitEntity> _enemies = new();
    private UnitNode _playerNode;
    private readonly List<UnitNode> _enemyNodes = new();
    private Label _logLabel;

    private const float MaxHp    = 1000f;
    private const float EnemyHp  = 600f;
    private const float MaxEnergy = 100f;

    // HUD 动态节点
    private ColorRect   _hpFill;
    private ColorRect   _energyFill;
    private Label       _hpText;
    private Label       _energyText;
    private readonly ColorRect[] _slotDims = new ColorRect[3];
    private readonly float[]     _slotCosts = { 30f, 40f, 50f };

    public override void _Ready()
    {
        CFBridge.Bridge = new GodotCFBridge();

        // ── 玩家 ──────────────────────────────────────────
        _player = new UnitEntity { Team = TeamFlag.Friendly };
        _player.Stats.Set("Atk_Base", 200f);
        _player.Stats.Set("Energy", 100f);
        _player.Stats.Set("DefFinal", 0f);
        _player.Stats.Set("HP", MaxHp);

        // ── 3 个敌人 ──────────────────────────────────────
        for (int i = 0; i < 3; i++)
        {
            var e = new UnitEntity { Team = TeamFlag.Enemy };
            e.Stats.Set("HP", EnemyHp);
            e.Stats.Set("DefFinal", 0f);
            _enemies.Add(e);
        }

        // ── ShapeQuery 注入（所有 unit）──────────────────
        var allUnits = new List<UnitEntity> { _player };
        allUnits.AddRange(_enemies);
        CFServices.ShapeQuery = new GodotShapeQueryService(allUnits);

        // ── 技能装备 ──────────────────────────────────────
        _player.AbilitySlots.Equip(SlotType.Passive0,  AbilitySpec.Create(LoadAbility("test_passive_atk_bonus.json")));
        _player.AbilitySlots.Equip(SlotType.NormalAtk, AbilitySpec.Create(LoadAbility("test_active_attack.json")));
        _player.AbilitySlots.Equip(SlotType.Skill,     AbilitySpec.Create(LoadAbility("skill_aoe.json")));
        _player.AbilitySlots.Equip(SlotType.Burst,     AbilitySpec.Create(LoadAbility("skill_drain.json")));

        // ── 绑定 Node ──────────────────────────────────────
        _playerNode = GetNode<UnitNode>("PlayerUnit");
        _playerNode.Init(_player, Colors.CornflowerBlue, MaxHp, isPlayer: true);
        // 普攻盒预览：offset(120,0)，size(240×160)，与 test_active_attack.json 一致
        _playerNode.AttackBox = (new Godot.Vector2(120f, 0f), new Godot.Vector2(240f, 160f));
        // 同步初始世界坐标到 Entity
        _player.Position = new System.Numerics.Vector3(_playerNode.Position.X, _playerNode.Position.Y, 0f);

        for (int i = 0; i < 3; i++)
        {
            var node = GetNode<UnitNode>($"Enemy{i}");
            node.Init(_enemies[i], Colors.Tomato, EnemyHp);
            _enemies[i].Position = new System.Numerics.Vector3(node.Position.X, node.Position.Y, 0f);
            _enemyNodes.Add(node);
        }

        _logLabel    = GetNode<Label>("UI/Log");

        BuildHud();

        Log($"战斗开始！ATK={_player.GetStat("Atk"):F0}  [Z]普攻  [X]AOE  [C]吸血  [WASD]移动  [R]重置");
    }

    public override void _Input(InputEvent ev)
    {
        if (ev is not InputEventKey key || !key.Pressed || key.Echo) return;

        // 找最近存活的敌人作为单体目标
        var target = _enemies.FirstOrDefault(e => e.GetStat("HP") > 0f);

        switch (key.Keycode)
        {
            case Key.Z: CastSkill(SlotType.NormalAtk, target, "普攻"); break;
            case Key.X: CastSkill(SlotType.Skill,     target, "AOE");  break;
            case Key.C: CastSkill(SlotType.Burst,     target, "吸血"); break;
            case Key.R: ResetBattle(); break;
        }
    }

    private void CastSkill(SlotType slot, UnitEntity target, string skillName)
    {
        float energyBefore = _player.GetStat("Energy");
        float playerHpBefore = _player.GetStat("HP");
        var snapBefore = _enemies.Select(e => e.GetStat("HP")).ToArray();

        bool ok = _player.TryCast(slot, target);
        if (!ok)
        {
            Log($"[{skillName}] 失败（能量 {energyBefore:F0} 不足）");
            return;
        }

        // 统计总伤害
        float totalDmg = 0f;
        for (int i = 0; i < _enemies.Count; i++)
            totalDmg += snapBefore[i] - _enemies[i].GetStat("HP");

        float heal = _player.GetStat("HP") - playerHpBefore;
        float energyAfter = _player.GetStat("Energy");

        string healStr = heal > 0 ? $"  回血 +{heal:F0}" : "";
        Log($"[{skillName}] 总伤 {totalDmg:F0}{healStr}  能量 {energyBefore:F0}→{energyAfter:F0}");

        CheckReset();
    }

    private void CheckReset()
    {
        if (_enemies.All(e => e.GetStat("HP") <= 0f))
        {
            Log("── 全灭！3秒后重置 ──");
            GetTree().CreateTimer(3.0).Timeout += ResetBattle;
        }
    }

    private void ResetBattle()
    {
        foreach (var e in _enemies) e.Stats.Set("HP", EnemyHp);
        _player.Stats.Set("HP", MaxHp);
        _player.Stats.Set("Energy", 100f);
        Log("── 战场已重置 [R 可手动重置] ──");
    }

    public override void _Process(double delta)
    {
        float hp     = _player.GetStat("HP");
        float energy = _player.GetStat("Energy");

        // 更新 HP 条
        if (_hpFill != null)
            _hpFill.Size = new Vector2(200f * Mathf.Clamp(hp / MaxHp, 0f, 1f), _hpFill.Size.Y);
        if (_hpText != null)
            _hpText.Text = $"{hp:F0}/{MaxHp:F0}";

        // 更新能量条
        if (_energyFill != null)
            _energyFill.Size = new Vector2(200f * Mathf.Clamp(energy / MaxEnergy, 0f, 1f), _energyFill.Size.Y);
        if (_energyText != null)
            _energyText.Text = $"{energy:F0}/{MaxEnergy:F0}";

        // 技能槽变暗（能量不足）
        for (int i = 0; i < _slotDims.Length; i++)
            if (_slotDims[i] != null)
                _slotDims[i].Visible = energy < _slotCosts[i];
    }

    private void Log(string msg)
    {
        _logLabel.Text = msg + "\n" + _logLabel.Text;
        var lines = _logLabel.Text.Split('\n');
        if (lines.Length > 14)
            _logLabel.Text = string.Join('\n', lines[..14]);
    }

    // ── HUD 构建 ──────────────────────────────────────────────────────────────
    // 布局（800×600）：
    //   左下  x=20,  y=476 — HP 条 + 能量条
    //   底部中央 — 3 个技能槽（80×80），带图标 / 快捷键 / 耗能 / 变暗遮罩

    private void BuildHud()
    {
        var hud = GetNode<Control>("UI/HUD");

        BuildStatusBars(hud, 20f, 476f);
        BuildSkillBar(hud);
    }

    private void BuildStatusBars(Control parent, float x, float y)
    {
        const float barW = 200f;
        const float barH = 18f;
        const float rowGap = 40f;

        // ── HP 行 ────────────────────────────────────────
        var hpLabel = MakeLabel("HP", x, y, 12, Colors.LightGreen);
        parent.AddChild(hpLabel);

        var hpBg = MakeRect(x, y + 16f, barW, barH, new Color(0.15f, 0.15f, 0.15f, 0.85f));
        parent.AddChild(hpBg);

        _hpFill = MakeRect(x, y + 16f, barW, barH, new Color(0.18f, 0.75f, 0.25f));
        parent.AddChild(_hpFill);

        _hpText = MakeLabel("", x + barW + 6f, y + 16f, 12, Colors.White);
        parent.AddChild(_hpText);

        // ── 能量行 ──────────────────────────────────────
        var enLabel = MakeLabel("能量", x, y + rowGap, 12, new Color(0.4f, 0.7f, 1f));
        parent.AddChild(enLabel);

        var enBg = MakeRect(x, y + rowGap + 16f, barW, barH, new Color(0.15f, 0.15f, 0.15f, 0.85f));
        parent.AddChild(enBg);

        _energyFill = MakeRect(x, y + rowGap + 16f, barW, barH, new Color(0.2f, 0.5f, 1f));
        parent.AddChild(_energyFill);

        _energyText = MakeLabel("", x + barW + 6f, y + rowGap + 16f, 12, Colors.White);
        parent.AddChild(_energyText);
    }

    private void BuildSkillBar(Control parent)
    {
        // 技能定义：图标路径 / 快捷键 / 耗能
        var defs = new[]
        {
            ("res://Textures/Icon01.jpg", "Z", "30 EN"),
            ("res://Textures/Icon02.jpg", "X", "40 EN"),
            ("res://Textures/Icon03.jpg", "C", "50 EN"),
        };

        const float slotW   = 80f;
        const float slotH   = 80f;
        const float slotGap = 12f;
        float totalW   = defs.Length * slotW + (defs.Length - 1) * slotGap;
        float startX   = (800f - totalW) * 0.5f;
        float startY   = 600f - slotH - 16f;   // y = 504

        for (int i = 0; i < defs.Length; i++)
        {
            var (iconPath, key, cost) = defs[i];
            float x = startX + i * (slotW + slotGap);

            // 背景
            var bg = MakeRect(x, startY, slotW, slotH, new Color(0.10f, 0.10f, 0.13f, 0.92f));
            parent.AddChild(bg);

            // 图标
            var tex = GD.Load<Texture2D>(iconPath);
            if (tex != null)
            {
                var icon = new TextureRect
                {
                    Texture      = tex,
                    ExpandMode   = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode  = TextureRect.StretchModeEnum.KeepAspectCentered,
                    Position     = new Vector2(x + 8f, startY + 8f),
                    Size         = new Vector2(slotW - 16f, slotH - 30f),
                };
                parent.AddChild(icon);
            }

            // 快捷键标签（左上角，黄色）
            var keyLbl = MakeLabel(key, x + 4f, startY + 3f, 14, Colors.Yellow);
            parent.AddChild(keyLbl);

            // 耗能标签（底部居中，灰色）
            var costLbl = MakeLabel(cost, x, startY + slotH - 17f, 11, new Color(0.75f, 0.75f, 0.75f));
            costLbl.HorizontalAlignment = HorizontalAlignment.Center;
            costLbl.Size = new Vector2(slotW, 14f);
            parent.AddChild(costLbl);

            // 变暗遮罩（能量不足时显示）
            var dim = MakeRect(x, startY, slotW, slotH, new Color(0f, 0f, 0f, 0.58f));
            dim.Visible = false;
            parent.AddChild(dim);
            _slotDims[i] = dim;

            // 外框（亮边）
            var border = MakeRect(x, startY, slotW, slotH, new Color(0.5f, 0.5f, 0.6f, 0.5f));
            border.MouseFilter = Control.MouseFilterEnum.Ignore;
            // 用内部空心方式：在 bg 上层叠一个 1px 细框，这里用简单的半透明色框作视觉分隔
            parent.AddChild(border);
            border.ZIndex = -1;  // 让它在背景之后（实际用作底层边框颜色衬托）
        }
    }

    private static ColorRect MakeRect(float x, float y, float w, float h, Color color)
    {
        return new ColorRect
        {
            Position = new Vector2(x, y),
            Size     = new Vector2(w, h),
            Color    = color,
        };
    }

    private static Label MakeLabel(string text, float x, float y, int fontSize, Color color)
    {
        var lbl = new Label { Text = text, Position = new Vector2(x, y) };
        lbl.AddThemeFontSizeOverride("font_size", fontSize);
        lbl.AddThemeColorOverride("font_color", color);
        return lbl;
    }

    private static AbilityData LoadAbility(string fileName)
    {
        var path = Path.Combine(ProjectSettings.GlobalizePath("res://Abilities"), fileName);
        return JsonConvert.DeserializeObject<AbilityData>(
            File.ReadAllText(path), AbilityJsonSettings.Instance);
    }
}

