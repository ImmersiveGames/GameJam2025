using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.Actors.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.ActorGroupRearm.Core
{
    public sealed class SceneScanActorGroupRearmDiscoveryStrategy : IActorGroupRearmDiscoveryStrategy
    {
        private readonly string _sceneName;

        public SceneScanActorGroupRearmDiscoveryStrategy(string sceneName)
        {
            _sceneName = sceneName ?? string.Empty;
        }

        public string Name => "SceneScanDiscovery";

        public int CollectTargets(ActorGroupRearmRequest request, List<IActor> results, out bool fallbackUsed)
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
            if (request is { Target: ActorGroupRearmTarget.ActorIdSet, ActorIds: { Count: > 0 } })
            {
                ids = new HashSet<string>(
                    request.ActorIds.Where(id => !string.IsNullOrWhiteSpace(id)),
                    StringComparer.Ordinal);
            }

            bool isKindTarget = request.Target == ActorGroupRearmTarget.ByActorKind;
            ActorKind requestedKind = request.ActorKind;

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

                if (request.Target == ActorGroupRearmTarget.ActorIdSet)
                {
                    if (ids == null || !ids.Contains(actor.ActorId ?? string.Empty))
                    {
                        continue;
                    }
                }
                else if (isKindTarget)
                {
                    if (!ActorKindMatching.MatchesActorKind(actor, requestedKind))
                    {
                        continue;
                    }
                }
                else
                {
                    continue;
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

