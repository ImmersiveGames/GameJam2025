using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.ActorGroupRearm.Core;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.Actors.Core;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.ActorGroupRearm.Interop
{
    /// <summary>
    /// Participante de soft reset do WorldLifecycle para o escopo Players.
    /// Implementa??o de gameplay (n?o infra).
    /// Ponte: WorldLifecycle(WorldResetScope.Players) -> ActorGroupRearm(ByActorKind(Player)).
    /// </summary>
    public sealed class PlayersActorGroupRearmWorldParticipant : IActorGroupRearmWorldParticipant
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

            DebugUtility.Log(typeof(PlayersActorGroupRearmWorldParticipant),
                $"[IActorGroupRearmWorldBridge] Bridge start => ActorGroupRearm ByActorKind(Player) (reason='{reason}')");

            if (_actorGroupRearm == null)
            {
                DebugUtility.LogWarning(typeof(PlayersActorGroupRearmWorldParticipant),
                    "[IActorGroupRearmWorldBridge] IActorGroupRearmOrchestrator ausente. Soft reset Players n?o executar? ActorGroupRearm.");
                return;
            }

            var request = ActorGroupRearmRequest.ByActorKind(ActorKind.Player, reason);

            await _actorGroupRearm.RequestResetAsync(request);

            DebugUtility.Log(typeof(PlayersActorGroupRearmWorldParticipant),
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

