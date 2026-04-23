using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core;
using _ImmersiveGames.NewScripts.GameplayRuntime.GameplayReset.Core;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Domain;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Contracts;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.GameplayRuntime.GameplayReset.Integration
{
    /// <summary>
    /// Participante de soft reset do SceneReset para o escopo Players.
    /// Implementa??o de gameplay (n?o infra).
    /// Ponte: SceneReset(WorldResetScope.Players) -> ActorGroupGameplayReset(ByActorKind(Player)).
    /// </summary>
    public sealed class PlayerActorGroupGameplayResetWorldParticipant : IActorGroupGameplayResetWorldParticipant
    {
        private IActorGroupGameplayResetOrchestrator _actorGroupGameplayReset;
        private ISpawnResetParticipationReadPort _participationReadPort;
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
            provider.TryGetGlobal<ISpawnResetParticipationReadPort>(out _participationReadPort);

            _dependenciesResolved = true;
        }

        private string DescribeParticipation()
        {
            if (_participationReadPort == null || !_participationReadPort.TryGetCurrent(out var snapshot))
            {
                return string.Empty;
            }

            string localBinding = "<none>";
            if (!string.IsNullOrWhiteSpace(snapshot.LocalBindingHint))
            {
                localBinding = snapshot.LocalBindingHint;
            }

            return $" participationSignature='{snapshot.Signature}' readiness='{snapshot.ReadinessState}' localBinding='{localBinding}'";
        }
    }
}



