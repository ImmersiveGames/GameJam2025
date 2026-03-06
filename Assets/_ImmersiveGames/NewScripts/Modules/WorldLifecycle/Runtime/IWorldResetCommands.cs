using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime
{
    public interface IWorldResetCommands
    {
        Task ResetMacroAsync(SceneRouteId macroRouteId, string reason, string macroSignature, CancellationToken ct);
        Task ResetLevelAsync(LevelDefinitionAsset levelRef, string reason, LevelContextSignature levelSignature, CancellationToken ct);

        [System.Obsolete("Legacy LevelId path is disabled in canonical LevelFlow.")]
        Task ResetLevelAsync(LevelId levelId, string reason, LevelContextSignature levelSignature, CancellationToken ct);
    }
}
