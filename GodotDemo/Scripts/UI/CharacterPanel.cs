using Godot;
using CombatFramework.Unit;

/// <summary>
/// 星铁风格角色面板 — 左侧页签（属性/技能/星魂）+ 右侧内容。
/// 按 P 键打开/关闭，Tab 或点击切换页签。
/// </summary>
public partial class CharacterPanel : Control
{
    private bool _visible;
    private int _tab; // 0=属性 1=技能 2=星魂
    private UnitEntity _player;
    private UnitConfig _cfg;
    private ConstellationManager _constMgr;

    private static readonly string[] TabLabels = { "属性", "技能", "星魂" };
    private static readonly (string name, string id, bool pct)[] StatDefs =
    {
        ("生命值",   "HP",             false),
        ("攻击力",   "Atk",            false),
        ("防御力",   "DefFinal",       false),
        ("暴击率",   "CritRate",       true),
        ("暴击伤害", "CritDMG",        true),
        ("破韧增伤", "BreakDmgBonus",  true),
    };
    private static readonly (string name, string file, string key, Color color)[] SkillDefs =
    {
        ("普攻·剑击",  "normal_attack_01", "Z", new Color(0.9f, 0.65f, 0.15f)),
        ("战技·爆裂",  "skill_aoe",        "X", new Color(0.2f, 0.7f, 0.85f)),
        ("充能·汇聚",  "skill_charge",     "C", new Color(0.6f, 0.3f, 0.9f)),
    };
    private static readonly Vector2[] StarPos =
    {
        new(-0.30f, -0.15f), new(-0.30f, 0.15f), new(0f, -0.50f),
        new( 0.30f,  0.15f), new( 0.30f, -0.15f), new(0f, 0.50f),
    };
    private static readonly (int, int)[] StarEdges =
    {
        (0,1),(1,5),(5,3),(3,4),(4,2),(2,0), (0,3),(3,2),(2,4),(4,5),
    };

    public void Init(UnitEntity player, UnitConfig cfg, ConstellationManager constMgr)
    {
        _player = player;
        _cfg = cfg;
        _constMgr = constMgr;
    }

    public void Toggle() { _visible = !_visible; QueueRedraw(); }
    public void NextTab() { _tab = (_tab + 1) % 3; QueueRedraw(); }

    public override void _Draw()
    {
        if (!_visible || _player == null) return;

        float W = GetViewportRect().Size.X, H = GetViewportRect().Size.Y;

        // 遮罩
        DrawRect(new Rect2(0, 0, W, H), new Color(0, 0, 0, 0.72f));

        // 面板
        float px = W * 0.06f, py = H * 0.08f, pw = W * 0.88f, ph = H * 0.84f;
        DrawRect(new Rect2(px, py, pw, ph), new Color(0.06f, 0.07f, 0.12f, 0.96f));
        DrawRect(new Rect2(px, py, pw, ph), new Color(0.35f, 0.5f, 0.75f, 0.25f), false, 2f);

        // === 顶部头像栏 ===
        float topH = ph * 0.22f;
        DrawRect(new Rect2(px, py, pw, topH), new Color(0.04f, 0.05f, 0.09f, 0.5f));
        DrawLine(new Vector2(px, py + topH), new Vector2(px + pw, py + topH), new Color(0.3f, 0.4f, 0.6f, 0.3f));

        float portCX = px + topH * 0.45f, portCY = py + topH * 0.50f, portR = topH * 0.35f;
        var body = new Color(_cfg?.BodyColor?[0] ?? 0.4f, _cfg?.BodyColor?[1] ?? 0.6f, _cfg?.BodyColor?[2] ?? 0.9f);
        DrawCircle(new Vector2(portCX, portCY), portR + 3, new Color(0.25f, 0.3f, 0.45f, 0.5f));
        DrawCircle(new Vector2(portCX, portCY), portR, body);
        DrawArc(new Vector2(portCX, portCY), portR, 0, Mathf.Tau, 48, Colors.White, 2f);

        float nameX = portCX + portR + 24f;
        DrawString(ThemeDB.FallbackFont, new Vector2(nameX, portCY - 22f), _cfg?.Name ?? "???", fontSize: 26);
        DrawString(ThemeDB.FallbackFont, new Vector2(nameX, portCY + 8f), $"Lv.{_player.Level}", fontSize: 15,
            modulate: new Color(0.5f, 0.65f, 0.9f));

        DrawString(ThemeDB.FallbackFont, new Vector2(px + pw - 100f, py + 14f), "[P] 关闭", fontSize: 12,
            modulate: new Color(0.45f, 0.45f, 0.55f));

        // === 左侧页签栏 ===
        float tabW = pw * 0.16f, tabX = px, tabY = py + topH;
        float contentX = px + tabW + 10f, contentY = tabY + 10f;
        float contentW = pw - tabW - 20f, contentH = ph - topH - 20f;

        // 页签背景
        DrawRect(new Rect2(tabX, tabY, tabW, ph - topH), new Color(0.04f, 0.05f, 0.09f, 0.4f));

        for (int i = 0; i < 3; i++)
        {
            float ty = tabY + 20f + i * 52f;
            bool active = _tab == i;
            if (active)
            {
                DrawRect(new Rect2(tabX + 2, ty - 4f, tabW - 2, 44f), new Color(0.15f, 0.2f, 0.35f, 0.7f));
                DrawRect(new Rect2(tabX + tabW - 3, ty - 4f, 3, 44f), new Color(0.4f, 0.65f, 0.9f));
            }
            DrawString(ThemeDB.FallbackFont, new Vector2(tabX + 22f, ty + 8f), TabLabels[i],
                fontSize: 16, modulate: active ? new Color(0.85f, 0.9f, 1f) : new Color(0.4f, 0.45f, 0.55f));
        }

        // === 右侧内容 ===
        switch (_tab)
        {
            case 0: DrawStats(contentX, contentY, contentW, contentH); break;
            case 1: DrawSkills(contentX, contentY, contentW, contentH); break;
            case 2: DrawStar(contentX, contentY, contentW, contentH); break;
        }
    }

    // ───── 属性页 ───────────────────────────────────────────
    private void DrawStats(float x, float y, float w, float h)
    {
        DrawString(ThemeDB.FallbackFont, new Vector2(x, y - 4f), "基础属性", fontSize: 20,
            modulate: new Color(0.7f, 0.8f, 1f));

        float rowH = (h - 60f) / StatDefs.Length;
        float barW = w * 0.55f, labelW = 72f;

        for (int i = 0; i < StatDefs.Length; i++)
        {
            var (name, id, pct) = StatDefs[i];
            float rowY = y + 44f + i * rowH;
            float val = _player.GetStat(id);

            // 行背景
            DrawRect(new Rect2(x, rowY - 2f, w - 10f, rowH - 4f), new Color(0.08f, 0.09f, 0.14f, 0.5f));

            // 标签
            var labelCol = i < 3 ? new Color(0.75f, 0.8f, 0.9f) : new Color(0.55f, 0.5f, 0.4f);
            DrawString(ThemeDB.FallbackFont, new Vector2(x + 16f, rowY + 4f), name, fontSize: 15, modulate: labelCol);

            // 条
            float barX = x + labelW + 20f, barH = rowH * 0.4f;
            DrawRect(new Rect2(barX, rowY + rowH * 0.3f, barW, barH), new Color(0.1f, 0.11f, 0.16f));
            float ratio = pct ? Mathf.Clamp(val / 100f, 0, 1) : Mathf.Clamp(val / 2000f, 0, 1);
            var c = i switch { 0 => new Color(0.2f, 0.68f, 0.3f), 1 => new Color(0.88f, 0.5f, 0.12f),
                2 => new Color(0.25f, 0.5f, 0.85f), _ => new Color(0.7f, 0.55f, 0.18f) };
            DrawRect(new Rect2(barX, rowY + rowH * 0.3f, barW * ratio, barH), c);

            string s = pct ? $"{val:F1}%" : $"{val:F0}";
            DrawString(ThemeDB.FallbackFont, new Vector2(barX + barW + 16f, rowY + 4f), s, fontSize: 14,
                modulate: Colors.White);
        }
    }

    // ───── 技能页 ───────────────────────────────────────────
    private void DrawSkills(float x, float y, float w, float h)
    {
        DrawString(ThemeDB.FallbackFont, new Vector2(x, y - 4f), "技能", fontSize: 20,
            modulate: new Color(0.7f, 0.8f, 1f));

        float cardH = Mathf.Min((h - 60f) / 3, 100f);
        for (int i = 0; i < SkillDefs.Length; i++)
        {
            var (name, file, key, color) = SkillDefs[i];
            float cy = y + 44f + i * (cardH + 12f);

            var spec = _player.GetAbilitySpecByName(file);
            int lv = spec?.GetEffectiveLevel() ?? 0;

            // 卡片
            DrawRect(new Rect2(x, cy, w - 10f, cardH), new Color(0.07f, 0.08f, 0.13f, 0.7f));
            DrawRect(new Rect2(x, cy, 4f, cardH), color);
            DrawRect(new Rect2(x, cy, w - 10f, cardH), new Color(0.25f, 0.3f, 0.45f, 0.3f), false);

            // 快捷键圆圈
            float kcx = x + 42f, kcy = cy + cardH * 0.5f, kr = 22f;
            DrawCircle(new Vector2(kcx, kcy), kr, new Color(0.1f, 0.11f, 0.18f));
            DrawArc(new Vector2(kcx, kcy), kr, 0, Mathf.Tau, 32, color, 2.5f);
            DrawString(ThemeDB.FallbackFont, new Vector2(kcx - 8f, kcy - 10f), key, fontSize: 18,
                modulate: color);

            // 名称 + 等级
            DrawString(ThemeDB.FallbackFont, new Vector2(kcx + kr + 18f, kcy - 12f), name, fontSize: 17,
                modulate: Colors.White);
            DrawString(ThemeDB.FallbackFont, new Vector2(kcx + kr + 18f, kcy + 8f), $"Lv.{lv}", fontSize: 13,
                modulate: new Color(0.4f, 0.65f, 0.9f));
        }
    }

    // ───── 星魂页 ───────────────────────────────────────────
    private void DrawStar(float x, float y, float w, float h)
    {
        DrawString(ThemeDB.FallbackFont, new Vector2(x, y - 4f), "星魂", fontSize: 20,
            modulate: new Color(0.7f, 0.8f, 1f));

        float cx = x + w * 0.45f, cy = y + h * 0.48f, r = Mathf.Min(w, h) * 0.35f;
        var unlocked = _constMgr?.Unlocked;
        float nr = r * 0.10f;

        // 连线
        for (int e = 0; e < StarEdges.Length; e++)
        {
            var (a, b) = StarEdges[e];
            var p1 = StarPos[a] * r + new Vector2(cx, cy);
            var p2 = StarPos[b] * r + new Vector2(cx, cy);
            bool both = unlocked != null && a < unlocked.Length && b < unlocked.Length && unlocked[a] && unlocked[b];
            DrawLine(p1, p2, both ? new Color(1f, 0.7f, 0.15f, 0.6f) : new Color(0.25f, 0.25f, 0.35f, 0.35f),
                both ? 2f : 1.2f);
        }

        // 节点
        for (int i = 0; i < 6; i++)
        {
            var pos = StarPos[i] * r + new Vector2(cx, cy);
            bool on = unlocked != null && i < unlocked.Length && unlocked[i];

            if (on) DrawCircle(pos, nr * 2f, new Color(1f, 0.65f, 0.1f, 0.12f));
            DrawCircle(pos, nr, on ? new Color(0.85f, 0.6f, 0.1f) : new Color(0.18f, 0.18f, 0.28f));
            DrawArc(pos, nr, 0, Mathf.Tau, 28, on ? new Color(1f, 0.75f, 0.2f) : new Color(0.35f, 0.35f, 0.45f), 2f);

            DrawString(ThemeDB.FallbackFont, pos - new Vector2(6, 8), on ? $"{i + 1}✓" : $"{i + 1}",
                fontSize: 11, modulate: on ? Colors.White : new Color(0.45f, 0.45f, 0.55f));
        }
    }
}

