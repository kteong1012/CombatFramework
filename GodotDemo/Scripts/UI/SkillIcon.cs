using Godot;

/// <summary>
/// 纯代码绘制的技能图标控件。三组风格各异的几何图形。
/// </summary>
public partial class SkillIcon : Control
{
    public enum IconType { Slash, Burst, Charge }

    private IconType _type = IconType.Slash;
    [Export]
    public IconType Type
    {
        get => _type;
        set { _type = value; QueueRedraw(); }
    }

    public override void _Draw()
    {
        var r = new Rect2(Vector2.Zero, Size);
        float cx = r.Size.X / 2, cy = r.Size.Y / 2;
        float s = Mathf.Min(r.Size.X, r.Size.Y);

        switch (_type)
        {
            case IconType.Slash:  DrawSlash(cx, cy, s);  break;
            case IconType.Burst:  DrawBurst(cx, cy, s);  break;
            case IconType.Charge: DrawCharge(cx, cy, s); break;
        }
    }

    // ── 普攻 — 剑刃斜斩 ──────────────────────────────────
    private void DrawSlash(float cx, float cy, float s)
    {
        float hw = s * 0.33f, hh = s * 0.15f;
        float angle = -35f * Mathf.Pi / 180f;
        float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);

        // 刀身（金色菱形）
        var bladeColor = new Color(0.95f, 0.7f, 0.15f);
        var pts = new Vector2[]
        {
            new(cx - hw * cos, cy - hw * sin),
            new(cx - hh * sin, cx + hh * cos - (cy - s * 0.15f)),  // guard
            new(cx + hw * cos, cy + hw * sin),
            new(cx + hh * sin, cx - hh * cos - (cy - s * 0.15f)),
        };
        DrawColoredPolygon(pts, bladeColor);
        DrawPolyline(pts, Colors.White, 1.5f);

        // 护手（横杠）
        float guardW = s * 0.28f;
        var guard = new Vector2(cx - guardW, cy + s * 0.08f);
        DrawLine(guard, guard + new Vector2(guardW * 2, 0), new Color(0.8f, 0.8f, 0.85f), 2.5f);

        // 挥砍弧线
        float arcR = s * 0.32f;
        DrawArc(new Vector2(cx + s * 0.05f, cy - s * 0.02f), arcR,
            -110f * Mathf.Pi / 180f, -40f * Mathf.Pi / 180f, 24,
            new Color(1f, 0.85f, 0.3f, 0.5f), 2f);
    }

    // ── AOE — 环形冲击波 ──────────────────────────────────
    private void DrawBurst(float cx, float cy, float s)
    {
        float r1 = s * 0.32f, r2 = s * 0.40f;
        var cyan = new Color(0.15f, 0.75f, 0.85f);
        var lightCyan = new Color(0.3f, 0.9f, 1f, 0.3f);

        // 外环
        DrawArc(new Vector2(cx, cy), r2, 0, Mathf.Tau, 48, cyan, 2.5f);

        // 内环填充
        DrawCircle(new Vector2(cx, cy), r1, lightCyan);

        // 八方向放射线
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.Tau / 8;
            float inner = r1 * 0.5f;
            DrawLine(
                new Vector2(cx + Mathf.Cos(a) * inner, cy + Mathf.Sin(a) * inner),
                new Vector2(cx + Mathf.Cos(a) * r2, cy + Mathf.Sin(a) * r2),
                new Color(0.2f, 0.8f, 0.9f, 0.6f), 1.5f);
        }

        // 中心点
        DrawCircle(new Vector2(cx, cy), 4f, Colors.White);
    }

    // ── 充能 — 能量汇聚 ──────────────────────────────────
    private void DrawCharge(float cx, float cy, float s)
    {
        var purple = new Color(0.65f, 0.3f, 0.9f);
        var lightPurple = new Color(0.8f, 0.5f, 1f, 0.35f);

        // 中心能量球
        float orbR = s * 0.16f;
        DrawCircle(new Vector2(cx, cy), orbR, purple);
        DrawCircle(new Vector2(cx, cy), orbR * 0.5f, Colors.White);

        // 汇聚粒子（4 个菱形光点）
        float particleDist = s * 0.30f;
        for (int i = 0; i < 4; i++)
        {
            float a = i * Mathf.Tau / 4 - Mathf.Pi / 4;
            float px = cx + Mathf.Cos(a) * particleDist;
            float py = cy + Mathf.Sin(a) * particleDist;
            float pr = s * 0.045f;

            var ppts = new Vector2[]
            {
                new(px, py - pr), new(px + pr, py),
                new(px, py + pr), new(px - pr, py),
            };
            DrawColoredPolygon(ppts, lightPurple);
            DrawPolyline(ppts, purple, 1f);
        }

        // 连接线
        for (int i = 0; i < 4; i++)
        {
            float a = i * Mathf.Tau / 4 - Mathf.Pi / 4;
            DrawLine(
                new Vector2(cx, cy),
                new Vector2(cx + Mathf.Cos(a) * particleDist, cy + Mathf.Sin(a) * particleDist),
                new Color(0.6f, 0.3f, 0.85f, 0.25f), 1f);
        }
    }
}
