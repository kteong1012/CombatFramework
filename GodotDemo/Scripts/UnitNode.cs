using Godot;
using CombatFramework.Unit;
using System.Collections.Generic;

/// <summary>
/// 代表场景中一个战斗单位的 Node2D。
/// 玩家（IsPlayer=true）：WASD 移动，显示攻击盒范围预览。
/// </summary>
public partial class UnitNode : Node2D
{
    public UnitEntity Entity { get; private set; }
    public bool IsPlayer { get; set; }
    public float MoveSpeed { get; set; } = 200f;

    private Color _bodyColor = Colors.CornflowerBlue;
    private float _maxHp = 1000f;

    // ── 技能区域预览（0.3 秒淡出）────────────────────────
    private float _skillAreaTimer;
    private Godot.Vector2 _skillAreaOffset;
    private Godot.Vector2 _skillAreaSize;
    private float _skillAreaRadius;
    private bool _skillAreaIsCircle;

    // ── VFX 标记（由 GodotVfxService 管理）────────────────
    private readonly HashSet<string> _activeVfx = new();

    public void AddVfx(string name)   { _activeVfx.Add(name); }
    public void RemoveVfx(string name) { _activeVfx.Remove(name); }

    /// <summary>显示盒形技能范围预览，0.3 秒后自动消失。</summary>
    public void ShowSkillBoxPreview(Godot.Vector2 offset, Godot.Vector2 size)
    {
        GD.Print($"[UnitNode] ShowSkillBoxPreview offset=({offset.X:F0},{offset.Y:F0}) size=({size.X:F0},{size.Y:F0})");
        _skillAreaTimer = 0.3f;
        _skillAreaOffset = offset;
        _skillAreaSize = size;
        _skillAreaRadius = 0f;
        _skillAreaIsCircle = false;
    }

    /// <summary>显示圆形技能范围预览，0.3 秒后自动消失。</summary>
    public void ShowSkillCirclePreview(float radius)
    {
        _skillAreaTimer = 0.3f;
        _skillAreaOffset = Godot.Vector2.Zero;
        _skillAreaSize = Godot.Vector2.Zero;
        _skillAreaRadius = radius;
        _skillAreaIsCircle = true;
    }

    public void Init(UnitEntity entity, Color color, float maxHp = 1000f, bool isPlayer = false)
    {
        Entity    = entity;
        _bodyColor = color;
        _maxHp    = maxHp;
        IsPlayer  = isPlayer;
    }

    public override void _Process(double delta)
    {
        if (IsPlayer && Entity != null)
        {
            var dir = Godot.Vector2.Zero;
            if (Input.IsKeyPressed(Key.W)) dir.Y -= 1;
            if (Input.IsKeyPressed(Key.S)) dir.Y += 1;
            if (Input.IsKeyPressed(Key.A)) dir.X -= 1;
            if (Input.IsKeyPressed(Key.D)) dir.X += 1;

            if (dir != Godot.Vector2.Zero)
            {
                dir = dir.Normalized();
                Position += dir * MoveSpeed * (float)delta;
                Entity.Position = new System.Numerics.Vector3(Position.X, Position.Y, 0f);
            }
        }

        // 技能区域预览计时器
        if (_skillAreaTimer > 0f)
            _skillAreaTimer -= (float)delta;

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (Entity == null) return;

        float hp   = Entity.GetStat("HP");
        bool  dead = hp <= 0f;
        var bodyColor = dead ? new Color(0.4f, 0.4f, 0.4f) : _bodyColor;

        // 技能区域预览（0.3 秒淡出，亮青色）
        if (_skillAreaTimer > 0f)
        {
            float alpha = Mathf.Clamp(_skillAreaTimer / 0.3f, 0f, 1f);
            var previewColor = new Color(0.2f, 0.9f, 1f, 0.25f * alpha);
            var borderColor  = new Color(0.2f, 0.9f, 1f, 0.7f * alpha);

            if (_skillAreaIsCircle)
            {
                DrawCircle(Godot.Vector2.Zero, _skillAreaRadius, previewColor);
                DrawArc(Godot.Vector2.Zero, _skillAreaRadius, 0f, Mathf.Tau, 64, borderColor, 2f);
            }
            else
            {
                var boxPos2 = _skillAreaOffset - _skillAreaSize * 0.5f;
                DrawRect(new Rect2(boxPos2, _skillAreaSize), previewColor);
                DrawRect(new Rect2(boxPos2, _skillAreaSize), borderColor, filled: false, width: 2f);
            }
        }

        // 身体圆形
        DrawCircle(Godot.Vector2.Zero, 32f, bodyColor);
        DrawArc(Godot.Vector2.Zero, 32f, 0f, Mathf.Tau, 48, Colors.White, 2f);

        // ── 光环范围特效 ──
        if (_activeVfx.Contains("aura_range"))
        {
            DrawCircle(Godot.Vector2.Zero, 200f, new Color(1f, 0.3f, 0.1f, 0.08f));
            DrawArc(Godot.Vector2.Zero, 200f, 0f, Mathf.Tau, 72, new Color(1f, 0.4f, 0.15f, 0.35f), 1.5f);
        }

        // ── 自动特效：有 VFX 标记时金色脉冲环（由 modifier.EffectName 驱动）──
        if (_activeVfx.Count > 0 && !dead)
        {
            float pulse = 1f + 0.15f * Mathf.Sin((float)Time.GetTicksMsec() * 0.005f);
            float ringR = 40f * pulse;
            var gold = new Color(1f, 0.75f, 0.15f, 0.6f);
            DrawArc(Godot.Vector2.Zero, ringR, 0f, Mathf.Tau, 48, gold, 3f);
        }

        if (dead)
        {
            DrawLine(new Godot.Vector2(-20, -20), new Godot.Vector2(20, 20), Colors.Red, 3f);
            DrawLine(new Godot.Vector2(20, -20), new Godot.Vector2(-20, 20), Colors.Red, 3f);
            return;
        }

        // HP 条
        DrawRect(new Rect2(-36f, -56f, 72f, 10f), new Color(0.2f, 0.2f, 0.2f));
        float ratio = Mathf.Clamp(hp / _maxHp, 0f, 1f);
        DrawRect(new Rect2(-36f, -56f, 72f * ratio, 10f), ratio > 0.5f ? Colors.LimeGreen : Colors.OrangeRed);
        DrawString(ThemeDB.FallbackFont, new Godot.Vector2(-36f, -60f), $"{hp:F0}/{_maxHp:F0}", fontSize: 12);
    }
}
