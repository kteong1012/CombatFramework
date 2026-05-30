using System.Numerics;

namespace CombatFramework.Core;

/// <summary>
/// NavMesh 采样接口——由 Unity 游戏项目实现。
/// CF EffectInvoker 在计算 picker 中心点时调用以修正位置。
/// </summary>
public interface INavMeshService
{
    /// <summary>在 source 附近采样 NavMesh 上的有效位置。</summary>
    bool SamplePosition(Vector3 source, out Vector3 result);
}
