using System.Threading.Tasks;
using ImmersiveGames.GameJam2025.Infrastructure.Composition;
using ImmersiveGames.GameJam2025.Core.Logging;
using ImmersiveGames.GameJam2025.Game.Gameplay.Actors.Core;
using ImmersiveGames.GameJam2025.Game.Gameplay.GameplayReset.Core;
using ImmersiveGames.GameJam2025.Orchestration.SessionIntegration.Runtime;
using ImmersiveGames.GameJam2025.Orchestration.WorldReset.Domain;
using UnityEngine.SceneManagement;
namespace ImmersiveGames.GameJam2025.Game.Gameplay.GameplayReset.Integration
{
    /// <summary>
    /// Participante de soft reset do SceneReset para o escopo Players.
    /// Implementa??o de gameplay (n?o infra).
    /// Ponte: SceneReset(WorldResetScope.Players) -> ActorGroupGameplayReset(ByActorKind(Player)).
    /// </summary>
    public sealed class PlayerActorGroupGameplayResetWorldParticipant : IActorGroupGameplayResetWorldParticipant
    {
        private IActorGroupGameplayResetOrchestrator _actorGroupGameplayReset;
        private ISessionIntegrationContextService _sessionIntegrationContextService;
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
                $"[IActorGroupGameplayResetWorldBridge] Bridge start => ActorGroupGameplayReset ByActorKind(Player) (reason='{reason}'){DescribeParticipation()}");

            if (_actorGroupGameplayReset == null)
            {
                DebugUtility.LogWarning(typeof(PlayerActorGroupGameplayResetWorldParticipant),
                    "[IActorGroupGameplayResetWorldBridge] IActorGroupGameplayResetOrchestrator ausente. Soft reset Players n?o executar? ActorGroupGameplayReset.");
                return;
            }

            var request = ActorGroupGameplayResetRequest.ByActorKind(ActorKind.Player, reason);

            await _actorGroupGameplayReset.RequestResetAsync(request);

            DebugUtility.Log(typeof(PlayerActorGroupGameplayResetWorldParticipant),
                $"[IActorGroupGameplayResetWorldBridge] Bridge end => ActorGroupGameplayReset ByActorKind(Player) (reason='{reason}'){DescribeParticipation()}");
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
            provider.TryGetGlobal<ISessionIntegrationContextService>(out _sessionIntegrationContextService);

            _dependenciesResolved = true;
        }

        private string DescribeParticipation()
        {
            if (_sessionIntegrationContextService == null || !_sessionIntegrationContextService.TryGetCurrentParticipation(out var snapshot))
            {
                return string.Empty;
            }

            string localBinding = "<none>";
            if (snapshot.TryGetLocalBindingCandidate(out var localParticipant))
            {
                localBinding = localParticipant.BindingHint.ToString();
            }

            return $" participationSignature='{snapshot.Signature}' readiness='{snapshot.Readiness.State}' localBinding='{localBinding}'";
        }
    }
}



