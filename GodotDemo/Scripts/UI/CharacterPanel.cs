using Godot;
using System.Collections.Generic;
using CombatFramework.Unit;

/// <summary>
/// 米哈游风格角色面板 — 属性/技能/命座星图。
/// 按 P 键打开/关闭。
/// </summary>
public partial class CharacterPanel : Control
{
    private bool _visible;
    private UnitEntity _player;
    private UnitConfig _cfg;
    private ConstellationManager _constMgr;

    // ── 命座六芒星布局（相对坐标，归一化到 0~1 再乘半径）──
    private static readonly Vector2[] StarLayout =
    {
        new(-0.30f, -0.15f),  // 1 — 左上
        new(-0.30f,  0.15f),  // 2 — 左下
        new( 0.00f, -0.50f),  // 3 — 顶部
        new( 0.30f,  0.15f),  // 4 — 右下
        new( 0.30f, -0.15f),  // 5 — 右上
        new( 0.00f,  0.50f),  // 6 — 底部
    };

    // ── 节点之间连线 ──
    private static readonly (int a, int b)[] StarLines =
    {
        (0,1), (1,5), (5,3), (3,4), (4,2), (2,0),
        (0,3), (3,2), (2,4), (4,5),
    };

    public void Init(UnitEntity player, UnitConfig cfg, ConstellationManager constMgr)
    {
        _player = player;
        _cfg = cfg;
        _constMgr = constMgr;
    }

    public void Toggle()
    {
        _visible = !_visible;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!_visible || _player == null) return;

        float w = GetViewportRect().Size.X, h = GetViewportRect().Size.Y;

        // ── 全屏暗色遮罩 ──
        DrawRect(new Rect2(Vector2.Zero, new Vector2(w, h)), new Color(0f, 0f, 0f, 0.78f));

        float panelX = w * 0.08f, panelY = h * 0.10f;
        float panelW = w * 0.84f, panelH = h * 0.80f;

        // ── 面板背景 ──
        DrawRect(new Rect2(panelX, panelY, panelW, panelH),
            new Color(0.08f, 0.09f, 0.14f, 0.95f));
        DrawRect(new Rect2(panelX, panelY, panelW, panelH),
            new Color(0.4f, 0.55f, 0.8f, 0.3f), filled: false, width: 2f);

        // ── 标题栏 ──
        float titleY = panelY + 20f;
        float portraitCX = panelX + 60f, portraitCY = titleY + 50f;
        float portraitR = 45f;

        // 角色头像圆形
        var bodyCol = new Color(
            _cfg?.BodyColor?[0] ?? 0.4f,
            _cfg?.BodyColor?[1] ?? 0.6f,
            _cfg?.BodyColor?[2] ?? 0.9f);
        DrawCircle(new Vector2(portraitCX, portraitCY), portraitR + 3f, new Color(0.3f, 0.35f, 0.5f, 0.5f));
        DrawCircle(new Vector2(portraitCX, portraitCY), portraitR, bodyCol);
        DrawArc(new Vector2(portraitCX, portraitCY), portraitR, 0f, Mathf.Tau, 48, Colors.White, 2f);

        DrawString(ThemeDB.FallbackFont, new Vector2(panelX + 30f, titleY),
            _cfg?.Name ?? "角色", fontSize: 28);
        DrawString(ThemeDB.FallbackFont, new Vector2(panelX + 30f, titleY + 30f),
            $"Lv.{_player.Level}", fontSize: 16, modulate: new Color(0.6f, 0.7f, 0.9f));

        // ── 关闭提示 ──
        DrawString(ThemeDB.FallbackFont, new Vector2(panelX + panelW - 120f, titleY),
            "[P] 关闭", fontSize: 14, modulate: new Color(0.5f, 0.5f, 0.6f));

        // ════════════════════════════════════════════════════════
        // 左侧：属性面板
        // ════════════════════════════════════════════════════════
        float statsX = panelX + 30f, statsY = titleY + 70f;
        DrawStats(statsX, statsY, panelW * 0.38f);

        // ════════════════════════════════════════════════════════
        // 中右：技能列表
        // ════════════════════════════════════════════════════════
        float skillX = statsX + panelW * 0.32f, skillY = statsY;
        DrawSkills(skillX, skillY, panelW * 0.30f);

        // ════════════════════════════════════════════════════════
        // 右侧：命座星图
        // ════════════════════════════════════════════════════════
        float starCX = panelX + panelW - panelW * 0.22f;
        float starCY = panelY + panelH * 0.52f;
        float starR = panelH * 0.30f;
        DrawConstellationStar(starCX, starCY, starR);
    }

    // ── 属性列表 ────────────────────────────────────────────
    private void DrawStats(float x, float y, float width)
    {
        DrawString(ThemeDB.FallbackFont, new Vector2(x, y - 24f),
            "◆ 基础属性", fontSize: 18, modulate: new Color(0.7f, 0.8f, 1f));

        var stats = new (string name, string id, bool isPercent)[]
        {
            ("生命值",     "HP",             false),
            ("攻击力",     "Atk",            false),
            ("防御力",     "DefFinal",       false),
            ("暴击率",     "CritRate",       true),
            ("暴击伤害",   "CritDMG",        true),
            ("破韧增伤",   "BreakDmgBonus",  true),
        };

        float rowH = 36f;
        for (int i = 0; i < stats.Length; i++)
        {
            var (name, id, isPct) = stats[i];
            float rowY = y + i * rowH;
            float val = _player.GetStat(id);

            // 标签
            DrawString(ThemeDB.FallbackFont, new Vector2(x, rowY), name, fontSize: 15);

            // 数值背景条
            float barX = x + 80f, barW = width - 80f, barH = 14f;
            DrawRect(new Rect2(barX, rowY + 2f, barW, barH), new Color(0.12f, 0.13f, 0.18f));

            float ratio = 0f;
            if (isPct) ratio = Mathf.Clamp(val / 100f, 0f, 1f);
            else       ratio = Mathf.Clamp(val / 2000f, 0f, 1f);

            var barColor = i switch
            {
                0 => new Color(0.2f, 0.7f, 0.3f),
                1 => new Color(0.9f, 0.55f, 0.15f),
                2 => new Color(0.3f, 0.5f, 0.9f),
                _ => new Color(0.75f, 0.6f, 0.2f),
            };
            DrawRect(new Rect2(barX, rowY + 2f, barW * ratio, barH), barColor);

            // 数值
            string valStr = isPct ? $"{val:F1}%" : $"{val:F0}";
            DrawString(ThemeDB.FallbackFont, new Vector2(barX + barW + 8f, rowY),
                valStr, fontSize: 14, modulate: Colors.White);
        }
    }

    // ── 技能列表 ──────────────────────────────────────────────
    private void DrawSkills(float x, float y, float width)
    {
        DrawString(ThemeDB.FallbackFont, new Vector2(x, y - 24f),
            "◆ 技能", fontSize: 18, modulate: new Color(0.7f, 0.8f, 1f));

        var skills = new (string name, string file)[]
        {
            ("普攻 · 剑击",    "normal_attack_01"),
            ("战技 · 爆裂",    "skill_aoe"),
            ("充能 · 汇聚",    "skill_charge"),
        };

        float rowH = 54f;
        for (int i = 0; i < skills.Length; i++)
        {
            var (name, file) = skills[i];
            float rowY = y + i * rowH;

            var spec = _player.GetAbilitySpecByName(file);
            int level = spec?.GetEffectiveLevel() ?? 0;
            string key = i == 0 ? "Z" : i == 1 ? "X" : "C";

            // 背景
            DrawRect(new Rect2(x, rowY, width, rowH - 6f),
                new Color(0.10f, 0.11f, 0.18f, 0.6f));
            DrawRect(new Rect2(x, rowY, width, rowH - 6f),
                new Color(0.3f, 0.4f, 0.6f, 0.3f), filled: false);

            // 快捷键
            DrawString(ThemeDB.FallbackFont, new Vector2(x + 12f, rowY + 6f),
                $"[{key}]", fontSize: 16, modulate: Colors.Yellow);

            // 技能名
            DrawString(ThemeDB.FallbackFont, new Vector2(x + 52f, rowY + 6f),
                name, fontSize: 15, modulate: Colors.White);

            // 等级
            DrawString(ThemeDB.FallbackFont, new Vector2(x + width - 64f, rowY + 6f),
                $"Lv.{level}", fontSize: 14, modulate: new Color(0.4f, 0.7f, 1f));
        }
    }

    // ── 命座星图（六芒星 + 连线 + 节点）────────────────────
    private void DrawConstellationStar(float cx, float cy, float r)
    {
        DrawString(ThemeDB.FallbackFont, new Vector2(cx - r * 0.3f, cy - r - 44f),
            "◆ 命座", fontSize: 18, modulate: new Color(0.7f, 0.8f, 1f));

        var unlocked = _constMgr?.Unlocked;
        float nodeR = r * 0.12f;

        // 连线
        foreach (var (ai, bi) in StarLines)
        {
            var a = StarLayout[ai] * r + new Vector2(cx, cy);
            var b = StarLayout[bi] * r + new Vector2(cx, cy);
            bool bothUnlocked = unlocked != null && unlocked[ai] && unlocked[bi];
            var lineColor = bothUnlocked
                ? new Color(1f, 0.75f, 0.2f, 0.7f)
                : new Color(0.3f, 0.3f, 0.4f, 0.4f);
            DrawLine(a, b, lineColor, bothUnlocked ? 2f : 1.2f);
        }

        // 节点
        for (int i = 0; i < 6; i++)
        {
            var pos = StarLayout[i] * r + new Vector2(cx, cy);
            bool isUnlocked = unlocked != null && unlocked[i];

            // 光辉晕
            if (isUnlocked)
                DrawCircle(pos, nodeR * 1.6f, new Color(1f, 0.7f, 0.15f, 0.15f));

            // 节点圆
            var fill = isUnlocked ? new Color(0.9f, 0.65f, 0.15f) : new Color(0.2f, 0.2f, 0.3f);
            DrawCircle(pos, nodeR, fill);
            DrawArc(pos, nodeR, 0f, Mathf.Tau, 32,
                isUnlocked ? new Color(1f, 0.8f, 0.3f) : new Color(0.4f, 0.4f, 0.5f), 2f);

            // 数字
            string label = isUnlocked ? $"{i + 1}✓" : $"{i + 1}";
            var labelColor = isUnlocked ? Colors.White : new Color(0.5f, 0.5f, 0.6f);
            DrawString(ThemeDB.FallbackFont, pos - new Vector2(6f, 8f),
                label, fontSize: 12, modulate: labelColor);
        }
    }
}
