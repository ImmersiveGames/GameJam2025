using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Game.Gameplay.Actors.Core;
using _ImmersiveGames.NewScripts.Game.Gameplay.GameplayReset.Core;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Domain;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Game.Gameplay.GameplayReset.Integration
{
    /// <summary>
    /// Participante de soft reset do SceneReset para o escopo Players.
    /// Implementa??o de gameplay (n?o infra).
    /// Ponte: SceneReset(WorldResetScope.Players) -> ActorGroupGameplayReset(ByActorKind(Player)).
    /// </summary>
    public sealed class PlayerActorGroupGameplayResetWorldParticipant : IActorGroupGameplayResetWorldParticipant
    {
        private IActorGroupGameplayResetOrchestrator _actorGroupGameplayReset;
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

            DebugUtility.Log(typeof(PlayerActorGroupGameplayResetWorldParticipant),
                $"[IActorGroupGameplayResetWorldBridge] Bridge start => ActorGroupGameplayReset ByActorKind(Player) (reason='{reason}')");

            if (_actorGroupGameplayReset == null)
            {
                DebugUtility.LogWarning(typeof(PlayerActorGroupGameplayResetWorldParticipant),
                    "[IActorGroupGameplayResetWorldBridge] IActorGroupGameplayResetOrchestrator ausente. Soft reset Players n?o executar? ActorGroupGameplayReset.");
                return;
            }

            var request = ActorGroupGameplayResetRequest.ByActorKind(ActorKind.Player, reason);

            await _actorGroupGameplayReset.RequestResetAsync(request);

            DebugUtility.Log(typeof(PlayerActorGroupGameplayResetWorldParticipant),
                $"[IActorGroupGameplayResetWorldBridge] Bridge end => ActorGroupGameplayReset ByActorKind(Player) (reason='{reason}')");
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

            provider.TryGetForScene(_sceneName, out _actorGroupGameplayReset);

            _dependenciesResolved = true;
        }
    }
}


