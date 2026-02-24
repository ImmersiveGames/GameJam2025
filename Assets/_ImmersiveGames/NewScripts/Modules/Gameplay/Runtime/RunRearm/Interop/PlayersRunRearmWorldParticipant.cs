using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.Actors.Core;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.RunRearm.Core;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.RunRearm.Interop
{
    /// <summary>
    /// Participante de soft reset do WorldLifecycle para o escopo Players.
    /// Implementação de gameplay (não infra).
    /// Ponte: WorldLifecycle(WorldResetScope.Players) -> GameplayReset(PlayersOnly).
    /// </summary>
    public sealed class PlayersRunRearmWorldParticipant : IRunRearmWorldParticipant
    {
        private IRunRearmOrchestrator _runRearm;
        private string _sceneName = string.Empty;
        private bool _dependenciesResolved;

        public WorldResetScope Scope => WorldResetScope.Players;
        public int Order => 0;

        public async Task ResetAsync(WorldResetContext context)
        {
            EnsureDependencies();

            string reason = string.IsNullOrWhiteSpace(context.Reason)
                ? "WorldLifecycle/SoftReset"
                : context.Reason;

            DebugUtility.Log(typeof(PlayersRunRearmWorldParticipant),
                $"[IRunRearmWorldBridge] Bridge start => GameplayReset PlayersOnly (reason='{reason}')");

            if (_runRearm == null)
            {
                DebugUtility.LogWarning(typeof(PlayersRunRearmWorldParticipant),
                    "[IRunRearmWorldBridge] IRunRearmOrchestrator ausente. Soft reset Players não executará GameplayReset.");
                return;
            }

            var request = new RunRearmRequest(
                RunRearmTarget.PlayersOnly,
                reason,
                actorKind: ActorKind.Player);

            await _runRearm.RequestResetAsync(request);

            DebugUtility.Log(typeof(PlayersRunRearmWorldParticipant),
                $"[IRunRearmWorldBridge] Bridge end => GameplayReset PlayersOnly (reason='{reason}')");
        }

        private void EnsureDependencies()
        {
            if (_dependenciesResolved)
            {
                return;
            }

            _sceneName = string.IsNullOrWhiteSpace(_sceneName)
                ? SceneManager.GetActiveScene().name
                : _sceneName;

            var provider = DependencyManager.Provider;

            provider.TryGetForScene(_sceneName, out _runRearm);

            _dependenciesResolved = true;
        }
    }
}



