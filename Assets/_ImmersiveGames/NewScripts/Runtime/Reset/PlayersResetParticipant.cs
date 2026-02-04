using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Gameplay.CoreGameplay.Reset;
using _ImmersiveGames.NewScripts.Runtime.World.Reset;
using _ImmersiveGames.NewScripts.Runtime.Actors;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Runtime.Reset
{
    /// <summary>
    /// Participante de soft reset do WorldLifecycle para o escopo Players.
    /// Implementação de gameplay (não infra).
    /// Ponte: WorldLifecycle(ResetScope.Players) -> GameplayReset(PlayersOnly).
    /// </summary>
    public sealed class PlayersResetParticipant : IResetScopeParticipant
    {
        private IGameplayResetOrchestrator _gameplayReset;
        private string _sceneName = string.Empty;
        private bool _dependenciesResolved;

        public ResetScope Scope => ResetScope.Players;
        public int Order => 0;

        public async Task ResetAsync(ResetContext context)
        {
            EnsureDependencies();

            string reason = string.IsNullOrWhiteSpace(context.Reason)
                ? "WorldLifecycle/SoftReset"
                : context.Reason;

            DebugUtility.Log(typeof(PlayersResetParticipant),
                $"[PlayersResetParticipant] Bridge start => GameplayReset PlayersOnly (reason='{reason}')");

            if (_gameplayReset == null)
            {
                DebugUtility.LogWarning(typeof(PlayersResetParticipant),
                    "[PlayersResetParticipant] IGameplayResetOrchestrator ausente. Soft reset Players não executará GameplayReset.");
                return;
            }

            var request = new GameplayResetRequest(
                GameplayResetTarget.PlayersOnly,
                reason,
                actorKind: ActorKind.Player);

            await _gameplayReset.RequestResetAsync(request);

            DebugUtility.Log(typeof(PlayersResetParticipant),
                $"[PlayersResetParticipant] Bridge end => GameplayReset PlayersOnly (reason='{reason}')");
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



