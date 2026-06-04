namespace CombatFramework.Core;

/// <summary>
/// VFX 特效服务接口，由游戏侧实现。modifier 的 OnCreated/OnDestroy 自动调用。
/// 参考 Dota 2 的 AttachEffect：特效跟随 modifier 生命周期，无需手动在 OnDestroy 清理。
/// </summary>
public interface IVfxEffectService
{
    /// <summary>在 unit 上播放命名特效。由 modifier.OnCreated 自动调用。</summary>
    void PlayOnUnit(string effectName, Unit.UnitEntity target);

    /// <summary>停止 unit 上的命名特效。由 modifier.OnDestroy 自动调用。</summary>
    void StopOnUnit(string effectName, Unit.UnitEntity target);
}
