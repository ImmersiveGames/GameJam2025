using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors.Core;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Rearm.Core;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Domain;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Rearm.Integration
{
    /// <summary>
    /// Participante de soft reset do SceneReset para o escopo Players.
    /// Implementa??o de gameplay (n?o infra).
    /// Ponte: SceneReset(WorldResetScope.Players) -> ActorGroupRearm(ByActorKind(Player)).
    /// </summary>
    public sealed class PlayerActorGroupRearmWorldParticipant : IActorGroupRearmWorldParticipant
    {
        private IActorGroupRearmOrchestrator _actorGroupRearm;
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

            DebugUtility.Log(typeof(PlayerActorGroupRearmWorldParticipant),
                $"[IActorGroupRearmWorldBridge] Bridge start => ActorGroupRearm ByActorKind(Player) (reason='{reason}')");

            if (_actorGroupRearm == null)
            {
                DebugUtility.LogWarning(typeof(PlayerActorGroupRearmWorldParticipant),
                    "[IActorGroupRearmWorldBridge] IActorGroupRearmOrchestrator ausente. Soft reset Players n?o executar? ActorGroupRearm.");
                return;
            }

            var request = ActorGroupRearmRequest.ByActorKind(ActorKind.Player, reason);

            await _actorGroupRearm.RequestResetAsync(request);

            DebugUtility.Log(typeof(PlayerActorGroupRearmWorldParticipant),
                $"[IActorGroupRearmWorldBridge] Bridge end => ActorGroupRearm ByActorKind(Player) (reason='{reason}')");
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

            provider.TryGetForScene(_sceneName, out _actorGroupRearm);

            _dependenciesResolved = true;
        }
    }
}

