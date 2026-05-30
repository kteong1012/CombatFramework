using System.Numerics;
using CombatFramework.Unit;

namespace CombatFramework.Core;

/// <summary>
/// VFX 表现服务接口——CF 通过此接口通知 Unity 播放/停止特效。
/// 实现者（Unity）负责资源加载、生命周期、目标跟随等。
///
/// 方法统一以 UnitEntity 描述"跟随谁"，Unity 桥接层通过
/// 内部映射 UnitEntity → GameObject → Transform 实现跟随。
/// </summary>
public interface IVfxEffectService
{
    /// <summary>在世界坐标位置播一次不跟随的特效。</summary>
    int PlayAtPoint(string assetPath, Vector3 position, float? lifeTime = null);

    /// <summary>附着在单位上播特效，可跟随。</summary>
    int PlayOnUnit(string assetPath, UnitEntity target, float? lifeTime = null);

    /// <summary>停止指定句柄的特效。</summary>
    void Stop(int vfxId);
}
