/// <summary>
/// 命座配置模型。Configs/Data/Constellation/xxx.json。
/// </summary>
public class ConstellationConfig
{
    /// <summary>显示名（如 "战技强化"）</summary>
    public string Name { get; set; }

    /// <summary>描述</summary>
    public string Desc { get; set; }

    /// <summary>对应的能力 JSON 文件名（在 Abilities/ 目录下）</summary>
    public string AbilityFile { get; set; }
}
