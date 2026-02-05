using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.RunRearm
{
    /// <summary>
    /// Participante de soft reset do WorldLifecycle para o escopo Players.
    /// Implementação de gameplay (não infra).
    /// Ponte: WorldLifecycle(WorldResetScope.Players) -> GameplayReset(PlayersOnly).
    /// </summary>
    public sealed class PlayerResetParticipant : IGameplayResetParticipant
    {
        private IGameplayResetOrchestrator _gameplayReset;
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

            DebugUtility.Log(typeof(PlayerResetParticipant),
                $"[PlayerResetParticipant] Bridge start => GameplayReset PlayersOnly (reason='{reason}')");

            if (_gameplayReset == null)
            {
                DebugUtility.LogWarning(typeof(PlayerResetParticipant),
                    "[PlayerResetParticipant] IGameplayResetOrchestrator ausente. Soft reset Players não executará GameplayReset.");
                return;
            }

            var request = new GameplayResetRequest(
                GameplayResetTarget.PlayersOnly,
                reason,
                actorKind: ActorKind.Player);

            await _gameplayReset.RequestResetAsync(request);

            DebugUtility.Log(typeof(PlayerResetParticipant),
                $"[PlayerResetParticipant] Bridge end => GameplayReset PlayersOnly (reason='{reason}')");
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

            provider.TryGetForScene(_sceneName, out _gameplayReset);

            _dependenciesResolved = true;
        }
    }
}



