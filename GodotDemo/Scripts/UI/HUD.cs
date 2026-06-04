using Godot;
using CombatFramework.Unit;
using System;

/// <summary>
/// HUD 管理：HP条、能量条、技能槽、命座按钮、战斗日志。
/// </summary>
public partial class HUD : Control
{
    [Export] public BattleScene Battle { get; set; }

    private UnitEntity _player;

    // ── 状态条 ──
    private ColorRect _hpFill;
    private ColorRect _energyFill;
    private Label _hpText;
    private Label _energyText;

    // ── 技能槽 ──
    private readonly ColorRect[] _slotDims = new ColorRect[3];
    private readonly float[] _slotCosts = { 30f, 40f, 50f };

    // ── 命座按钮 ──
    private const int ConstellationCount = 6;
    private readonly Button[] _constButtons = new Button[ConstellationCount];

    // ── 伤害统计标签 ──
    private Label _dpsLabel;
    private Label _totalDmgLabel;
    private Label _hitCountLabel;
    private Label _highestLabel;
    private Label _critLabel;

    // ── 日志 ──
    private Label _logLabel;

    public event Action<int> OnUnlockConstellation;

    public void Init(UnitEntity player)
    {
        _player = player;

        var bars = GetNode<Control>("Bars");
        var skillBar = GetNode<Control>("SkillBar");
        var constBar = GetNode<Control>("ConstellationBar");
        var statsPanel = GetNode<Control>("StatsPanel");
        _logLabel = GetNode<Label>("Log");

        BuildStatusBars(bars);
        BuildSkillBar(skillBar);
        BuildConstellationBar(constBar);
        BuildStatsPanel(statsPanel);
    }

    // ════════════════════════════════════════════════════════
    // 帧更新
    // ════════════════════════════════════════════════════════

    public void Update(float hp, float maxHp, float energy, float maxEnergy, bool[] constellationUnlocked)
    {
        UpdateBars(hp, maxHp, energy, maxEnergy);
        UpdateSkillSlots(energy);
        UpdateConstellationButtons(constellationUnlocked);
    }

    public void UpdateStatsPanel(DamageTracker tracker)
    {
        if (_dpsLabel != null)
            _dpsLabel.Text = $"DPS: {tracker.Dps:F0}";
        if (_totalDmgLabel != null)
            _totalDmgLabel.Text = $"总伤: {tracker.TotalDamage:F0}";
        if (_highestLabel != null)
            _highestLabel.Text = $"最高: {tracker.HighestHit:F0}";
        if (_hitCountLabel != null)
            _hitCountLabel.Text = $"命中: {tracker.HitCount}";
        if (_critLabel != null)
            _critLabel.Text = $"暴击: {tracker.CritCount} ({(tracker.HitCount > 0 ? tracker.CritCount * 100f / tracker.HitCount : 0f):F0}%)";
    }

    private void UpdateBars(float hp, float maxHp, float energy, float maxEnergy)
    {
        if (_hpFill != null)
            _hpFill.Size = new Vector2(200f * Mathf.Clamp(hp / maxHp, 0f, 1f), _hpFill.Size.Y);
        if (_hpText != null)
            _hpText.Text = $"{hp:F0}/{maxHp:F0}";

        if (_energyFill != null)
            _energyFill.Size = new Vector2(200f * Mathf.Clamp(energy / maxEnergy, 0f, 1f), _energyFill.Size.Y);
        if (_energyText != null)
            _energyText.Text = $"{energy:F0}/{maxEnergy:F0}";
    }

    private void UpdateSkillSlots(float energy)
    {
        for (int i = 0; i < _slotDims.Length; i++)
            if (_slotDims[i] != null)
                _slotDims[i].Visible = energy < _slotCosts[i];
    }

    private void UpdateConstellationButtons(bool[] unlocked)
    {
        for (int i = 0; i < ConstellationCount; i++)
        {
            if (_constButtons[i] == null) continue;
            _constButtons[i].Disabled = unlocked[i];
            _constButtons[i].Text = unlocked[i] ? $"{i + 1}✓" : $"{i + 1}";
            var style = new StyleBoxFlat();
            style.BgColor = unlocked[i]
                ? new Color(0.85f, 0.65f, 0.2f)
                : new Color(0.25f, 0.25f, 0.35f);
            _constButtons[i].AddThemeStyleboxOverride("normal", style);
        }
    }

    // ════════════════════════════════════════════════════════
    // 日志
    // ════════════════════════════════════════════════════════

    public void Log(string msg)
    {
        _logLabel.Text = msg + "\n" + _logLabel.Text;
        var lines = _logLabel.Text.Split('\n');
        if (lines.Length > 14)
            _logLabel.Text = string.Join('\n', lines[..14]);
    }

    public void ClearLog() => _logLabel.Text = "";

    // ════════════════════════════════════════════════════════
    // 构建 — 状态条
    // ════════════════════════════════════════════════════════

    private void BuildStatusBars(Control parent)
    {
        const float x = 20f, y = 0f, barW = 200f, barH = 18f, rowGap = 40f;

        var hpLabel = MakeLabel("HP", x, y, 12, Colors.LightGreen);
        parent.AddChild(hpLabel);
        parent.AddChild(MakeRect(x, y + 16f, barW, barH, new Color(0.15f, 0.15f, 0.15f, 0.85f)));
        _hpFill = MakeRect(x, y + 16f, barW, barH, new Color(0.18f, 0.75f, 0.25f));
        parent.AddChild(_hpFill);
        _hpText = MakeLabel("", x + barW + 6f, y + 16f, 12, Colors.White);
        parent.AddChild(_hpText);

        var enLabel = MakeLabel("能量", x, y + rowGap, 12, new Color(0.4f, 0.7f, 1f));
        parent.AddChild(enLabel);
        parent.AddChild(MakeRect(x, y + rowGap + 16f, barW, barH, new Color(0.15f, 0.15f, 0.15f, 0.85f)));
        _energyFill = MakeRect(x, y + rowGap + 16f, barW, barH, new Color(0.2f, 0.5f, 1f));
        parent.AddChild(_energyFill);
        _energyText = MakeLabel("", x + barW + 6f, y + rowGap + 16f, 12, Colors.White);
        parent.AddChild(_energyText);
    }

    // ════════════════════════════════════════════════════════
    // 构建 — 技能槽
    // ════════════════════════════════════════════════════════

    private void BuildSkillBar(Control parent)
    {
        var defs = new (string icon, string key, string cost)[]
        {
            ("res://Textures/icon_slash.png",  "Z", "30 EN"),
            ("res://Textures/icon_burst.png",  "X", "40 EN"),
            ("res://Textures/icon_charge.png", "C", "50 EN"),
        };

        const float slotW = 80f, slotH = 80f, slotGap = 12f;
        float totalW = defs.Length * slotW + (defs.Length - 1) * slotGap;
        float startX = (parent.Size.X - totalW) * 0.5f;
        float startY = 0f;

        for (int i = 0; i < defs.Length; i++)
        {
            var (iconPath, key, cost) = defs[i];
            float x = startX + i * (slotW + slotGap);

            parent.AddChild(MakeRect(x, startY, slotW, slotH, new Color(0.10f, 0.10f, 0.13f, 0.92f)));

            var tex = GD.Load<Texture2D>(iconPath);
            if (tex != null)
            {
                var icon = new TextureRect
                {
                    Texture = tex, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    Position = new Vector2(x + 10f, startY + 6f),
                    Size = new Vector2(slotW - 20f, slotH - 30f),
                };
                parent.AddChild(icon);
            }

            parent.AddChild(MakeLabel(key, x + 4f, startY + 3f, 14, Colors.Yellow));

            var costLbl = MakeLabel(cost, x, startY + slotH - 17f, 11, new Color(0.75f, 0.75f, 0.75f));
            costLbl.HorizontalAlignment = HorizontalAlignment.Center;
            costLbl.Size = new Vector2(slotW, 14f);
            parent.AddChild(costLbl);

            var dim = MakeRect(x, startY, slotW, slotH, new Color(0f, 0f, 0f, 0.58f));
            dim.Visible = false;
            parent.AddChild(dim);
            _slotDims[i] = dim;

            parent.AddChild(MakeRect(x, startY, slotW, slotH, new Color(0.5f, 0.5f, 0.6f, 0.5f)));
        }
    }

    // ════════════════════════════════════════════════════════
    // 构建 — 命座按钮
    // ════════════════════════════════════════════════════════

    private void BuildConstellationBar(Control parent)
    {
        const float btnW = 36f, btnH = 36f, gap = 6f;
        float totalW = ConstellationCount * btnW + (ConstellationCount - 1) * gap;
        float startX = (parent.Size.X - totalW) * 0.5f;

        for (int i = 0; i < ConstellationCount; i++)
        {
            int idx = i;
            var btn = new Button
            {
                Text = $"{i + 1}",
                Position = new Vector2(startX + i * (btnW + gap), 0f),
                Size = new Vector2(btnW, btnH),
                Disabled = false,
            };
            btn.Pressed += () => OnUnlockConstellation?.Invoke(idx + 1);

            var style = new StyleBoxFlat();
            style.BgColor = new Color(0.25f, 0.25f, 0.35f);
            btn.AddThemeStyleboxOverride("normal", style);
            btn.AddThemeFontSizeOverride("font_size", 16);

            parent.AddChild(btn);
            _constButtons[i] = btn;
        }
    }

    // ════════════════════════════════════════════════════════
    // 构建 — 伤害统计面板
    // ════════════════════════════════════════════════════════

    private void BuildStatsPanel(Control parent)
    {
        const float x = 0f, rowH = 18f;
        float y = 0f;
        var titleColor = new Color(0.9f, 0.7f, 0.2f);

        _dpsLabel = MakeLabel("DPS: 0", x, y, 14, titleColor);
        parent.AddChild(_dpsLabel);

        _totalDmgLabel = MakeLabel("总伤: 0", x, y + rowH, 12, Colors.White);
        parent.AddChild(_totalDmgLabel);

        _highestLabel = MakeLabel("最高: 0", x, y + rowH * 2, 12, new Color(1f, 0.5f, 0.3f));
        parent.AddChild(_highestLabel);

        _hitCountLabel = MakeLabel("命中: 0", x, y + rowH * 3, 12, Colors.LightGray);
        parent.AddChild(_hitCountLabel);

        _critLabel = MakeLabel("暴击: 0 (0%)", x, y + rowH * 4, 12, new Color(1f, 0.85f, 0.2f));
        parent.AddChild(_critLabel);
    }

    // ════════════════════════════════════════════════════════
    // 工具
    // ════════════════════════════════════════════════════════

    private static ColorRect MakeRect(float x, float y, float w, float h, Color color) =>
        new() { Position = new Vector2(x, y), Size = new Vector2(w, h), Color = color };

    private static Label MakeLabel(string text, float x, float y, int fontSize, Color color)
    {
        var lbl = new Label { Text = text, Position = new Vector2(x, y) };
        lbl.AddThemeFontSizeOverride("font_size", fontSize);
        lbl.AddThemeColorOverride("font_color", color);
        return lbl;
    }
}
