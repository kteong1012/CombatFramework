namespace CombatFramework.Core;

/// <summary>
/// CF 全局服务注册槽位——游戏方（Unity）注入实现。
/// </summary>
public static class CFServices
{
    /// <summary>NavMesh 采样（爆炸/投射物落点修正）</summary>
    public static INavMeshService? NavMesh { get; set; }

    private static readonly Random _defaultRng = new();

    /// <summary>提供 [0, 1) 随机数（默认 System.Random；Unity 侧注入 RandomService）</summary>
    public static Func<double> RandomProvider { get; set; } = () => _defaultRng.NextDouble();
}
