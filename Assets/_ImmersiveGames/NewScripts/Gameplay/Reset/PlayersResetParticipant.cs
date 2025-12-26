using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Reset;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace _ImmersiveGames.NewScripts.Gameplay.Reset
{
    /// <summary>
    /// Participante de soft reset do WorldLifecycle para o escopo Players.
    /// Implementação de gameplay (não infra).
    /// Ponte: WorldLifecycle(ResetScope.Players) -> GameplayReset(PlayersOnly).
    /// </summary>
    public sealed class PlayersResetParticipant : IResetScopeParticipant
    {
        private readonly List<IActor> _actorBuffer = new(32);

        private IActorRegistry _actorRegistry;
        private IGameplayResetOrchestrator _gameplayReset;
        private string _sceneName = string.Empty;
        private bool _dependenciesResolved;

        public ResetScope Scope => ResetScope.Players;
        public int Order => 0;

        public async Task ResetAsync(ResetContext context)
        {
            EnsureDependencies();

            var reason = string.IsNullOrWhiteSpace(context.Reason)
                ? "WorldLifecycle/SoftReset"
                : context.Reason;

            var actorIds = CollectPlayerActorIdsWithFallback();

            DebugUtility.Log(typeof(PlayersResetParticipant),
                $"[PlayersResetParticipant] Bridge start => GameplayReset PlayersOnly (players={actorIds.Count}, reason='{reason}')");

            if (_gameplayReset == null)
            {
                DebugUtility.LogWarning(typeof(PlayersResetParticipant),
                    "[PlayersResetParticipant] IGameplayResetOrchestrator ausente. Soft reset Players não executará GameplayReset.");
                return;
            }

            var request = new GameplayResetRequest(
                GameplayResetTarget.PlayersOnly,
                reason,
                actorIds);

            await _gameplayReset.RequestResetAsync(request);

            DebugUtility.Log(typeof(PlayersResetParticipant),
                $"[PlayersResetParticipant] Bridge end => GameplayReset PlayersOnly (players={actorIds.Count}, reason='{reason}')");
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

            provider.TryGetForScene(_sceneName, out _actorRegistry);
            provider.TryGetForScene(_sceneName, out _gameplayReset);

            _dependenciesResolved = true;
        }

        private IReadOnlyList<string> CollectPlayerActorIdsWithFallback()
        {
            // 1) Tenta via registry (quando houver)…
            var ids = CollectPlayerActorIdsFromRegistry();

            // 2) …mas se vier vazio, faz fallback por scan de cena (mesmo que registry exista).
            if (ids.Count == 0)
            {
                ids = CollectPlayerActorIdsFromSceneScan();
            }

            ids = ids.Distinct(StringComparer.Ordinal).ToList();
            ids.Sort(StringComparer.Ordinal);
            return ids;
        }

        private List<string> CollectPlayerActorIdsFromRegistry()
        {
            var result = new List<string>(16);

            if (_actorRegistry == null)
            {
                return result;
            }

            _actorBuffer.Clear();
            _actorRegistry.GetActors(_actorBuffer);

            if (_actorBuffer.Count == 0)
            {
                return result;
            }

            result.AddRange(from actor in _actorBuffer where actor != null let t = actor.Transform where t != null where t.GetComponent<PlayerActor>() != null select actor.ActorId into id where !string.IsNullOrWhiteSpace(id) select id);

            return result;
        }

        private List<string> CollectPlayerActorIdsFromSceneScan()
        {
            var result = new List<string>(16);

            var behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            if (behaviours == null || behaviours.Length == 0)
            {
                return result;
            }

            foreach (var mb in behaviours)
            {
                if (mb is not IActor actor)
                {
                    continue;
                }

                if (mb.gameObject == null || mb.gameObject.scene.name != _sceneName)
                {
                    continue;
                }

                // PlayersOnly: precisa ter PlayerActor no mesmo GO do IActor, ou no próprio.
                if (mb.GetComponent<PlayerActor>() == null)
                {
                    continue;
                }

                var id = actor.ActorId;
                if (!string.IsNullOrWhiteSpace(id))
                {
                    result.Add(id);
                }
            }

            return result;
        }
    }
}
