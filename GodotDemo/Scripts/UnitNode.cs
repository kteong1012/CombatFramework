using Godot;
using CombatFramework.Unit;

/// <summary>
/// 代表场景中一个战斗单位的 Node2D。
/// 玩家（IsPlayer=true）：WASD 移动，显示攻击盒范围预览。
/// </summary>
public partial class UnitNode : Node2D
{
    public UnitEntity Entity { get; private set; }
    public bool IsPlayer { get; set; }
    public float MoveSpeed { get; set; } = 200f;

    /// <summary>普攻盒预览：offset(局部) + 全尺寸。设为 null 则不显示。</summary>
    public (Godot.Vector2 offset, Godot.Vector2 size)? AttackBox { get; set; }

    private Color _bodyColor = Colors.CornflowerBlue;
    private float _maxHp = 1000f;

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
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (Entity == null) return;

        float hp   = Entity.GetStat("HP");
        bool  dead = hp <= 0f;
        var bodyColor = dead ? new Color(0.4f, 0.4f, 0.4f) : _bodyColor;

        // 攻击盒预览（仅玩家，半透明橙色矩形）
        if (IsPlayer && AttackBox.HasValue && !dead)
        {
            var (boxOffset, boxSize) = AttackBox.Value;
            var boxPos = boxOffset - boxSize * 0.5f;   // Rect2 左上角（局部坐标）
            DrawRect(new Rect2(boxPos, boxSize), new Color(1f, 0.6f, 0.1f, 0.18f));
            DrawRect(new Rect2(boxPos, boxSize), new Color(1f, 0.6f, 0.1f, 0.55f), filled: false, width: 1.5f);
        }

        // 身体圆形
        DrawCircle(Godot.Vector2.Zero, 32f, bodyColor);
        DrawArc(Godot.Vector2.Zero, 32f, 0f, Mathf.Tau, 48, Colors.White, 2f);

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
