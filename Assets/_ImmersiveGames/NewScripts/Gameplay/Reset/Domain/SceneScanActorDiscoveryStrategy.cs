using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.NewScripts.Gameplay.CoreGameplay.Reset;
using _ImmersiveGames.NewScripts.Runtime.Actors;
using _ImmersiveGames.NewScripts.Runtime.Reset;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace _ImmersiveGames.NewScripts.Gameplay.Reset.Domain
{
    public sealed class SceneScanActorDiscoveryStrategy : IActorDiscoveryStrategy
    {
        private readonly string _sceneName;

        public SceneScanActorDiscoveryStrategy(string sceneName)
        {
            _sceneName = sceneName ?? string.Empty;
        }

        public string Name => "SceneScanDiscovery";

        public int CollectTargets(GameplayResetRequest request, List<IActor> results, out bool fallbackUsed)
        {
            results.Clear();
            fallbackUsed = false;

            MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            if (behaviours == null || behaviours.Length == 0)
            {
                return 0;
            }

            string sceneName = ResolveSceneName();

            HashSet<string> ids = null;
            if (request is { Target: GameplayResetTarget.ActorIdSet, ActorIds: { Count: > 0 } })
            {
                ids = new HashSet<string>(
                    request.ActorIds.Where(id => !string.IsNullOrWhiteSpace(id)),
                    StringComparer.Ordinal);
            }

            bool isKindTarget = request.Target == GameplayResetTarget.ByActorKind
                || request.Target == GameplayResetTarget.PlayersOnly;
            ActorKind requestedKind = request.Target == GameplayResetTarget.PlayersOnly
                ? ActorKind.Player
                : request.ActorKind;

            for (int i = 0; i < behaviours.Length; i++)
            {
                var mb = behaviours[i];
                if (mb is not IActor actor)
                {
                    continue;
                }

                if (mb.gameObject == null || mb.gameObject.scene.name != sceneName)
                {
                    continue;
                }

                if (request.Target == GameplayResetTarget.ActorIdSet)
                {
                    if (ids == null || !ids.Contains(actor.ActorId ?? string.Empty))
                    {
                        continue;
                    }
                }
                else if (isKindTarget)
                {
                    if (!GameplayResetTargetMatching.MatchesActorKind(actor, requestedKind))
                    {
                        continue;
                    }
                }
                else if (request.Target == GameplayResetTarget.EaterOnly)
                {
                    if (!GameplayResetTargetMatching.MatchesEaterKindFirstWithFallback(actor, out bool usedFallback))
                    {
                        continue;
                    }

                    if (usedFallback)
                    {
                        fallbackUsed = true;
                    }
                }

                results.Add(actor);
            }

            return results.Count;
        }

        private string ResolveSceneName()
        {
            if (!string.IsNullOrWhiteSpace(_sceneName))
            {
                return _sceneName;
            }

            return SceneManager.GetActiveScene().name ?? string.Empty;
        }
    }
}
