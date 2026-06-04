/// <summary>
/// 训练场全局开关。由 TrainingPanel UI 控制。
/// </summary>
public static class TrainingConfig
{
    /// <summary>锁定能量（每帧回满），方便连招测试。</summary>
    public static bool EnergyLock { get; set; }

    /// <summary>敌人无敌（不受伤害）。</summary>
    public static bool EnemyInvincible { get; set; }

    /// <summary>显示伤害数字（框架暂不支持，预留）。</summary>
    public static bool ShowDamageNumbers { get; set; } = true;
}
