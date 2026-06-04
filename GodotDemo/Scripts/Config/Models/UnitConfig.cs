/// <summary>
/// 角色/单位配置模型。Configs/Data/Hero/ 和 Enemy/ 共用。
/// </summary>
public class UnitConfig
{
    /// <summary>显示名</summary>
    public string Name { get; set; }

    /// <summary>最大生命值</summary>
    public float Hp { get; set; } = 1000f;

    /// <summary>攻击力基础值</summary>
    public float AtkBase { get; set; } = 200f;

    /// <summary>防御力</summary>
    public float DefFinal { get; set; }

    /// <summary>最大能量</summary>
    public float MaxEnergy { get; set; } = 100f;

    /// <summary>移动速度</summary>
    public float MoveSpeed { get; set; } = 200f;

    /// <summary>韧性值上限（0 = 无韧性条）</summary>
    public float ToughnessMax { get; set; }

    /// <summary>装备的能力 JSON 文件名列表（在 Abilities/ 目录下）</summary>
    public string[] Abilities { get; set; }

    /// <summary>命座配置 key 列表（对应 Constellation/xxx.json）</summary>
    public int[] Constellations { get; set; }

    /// <summary>外观颜色 (R,G,B)</summary>
    public float[] BodyColor { get; set; } = new[] { 0.4f, 0.6f, 0.9f };
}
