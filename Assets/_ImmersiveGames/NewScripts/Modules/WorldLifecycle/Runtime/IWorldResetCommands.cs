using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime
{
    /// <summary>
    /// Fachada de comandos explícitos de reset em dois níveis:
    /// - MacroReset: reset de rota macro (SceneFlow/WorldLifecycle)
    /// - LevelReset: reset local de level/conteúdo
    /// </summary>
    public interface IWorldResetCommands
    {
        Task ResetMacroAsync(SceneRouteId macroRouteId, string reason, string macroSignature, CancellationToken ct);
        Task ResetLevelAsync(LevelId levelId, string reason, LevelContextSignature levelSignature, CancellationToken ct);
    }
}
