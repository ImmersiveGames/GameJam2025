using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings
{
    [CreateAssetMenu(
        fileName = "SceneRouteDefinition",
        menuName = "ImmersiveGames/NewScripts/SceneFlow/Scene Route Definition",
        order = 16)]
    public sealed class SceneRouteDefinitionAsset : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private SceneRouteId routeId;

        [Header("Scene Data")]
        [SerializeField] private SceneKeyAsset[] scenesToLoadKeys;
        [SerializeField] private SceneKeyAsset[] scenesToUnloadKeys;
        [SerializeField] private SceneKeyAsset targetActiveSceneKey;

        [Header("Route Policy")]
        [SerializeField] private SceneRouteKind routeKind = SceneRouteKind.Unspecified;
        [SerializeField] private bool requiresWorldReset;

        public SceneRouteId RouteId => routeId;

        public SceneRouteDefinition ToDefinition()
        {
            var load = ResolveKeys(scenesToLoadKeys);
            var unload = ResolveKeys(scenesToUnloadKeys);
            var active = ResolveSingleKey(targetActiveSceneKey);
            return new SceneRouteDefinition(load, unload, active, routeKind, requiresWorldReset);
        }

        private static string[] ResolveKeys(SceneKeyAsset[] keys)
        {
            if (keys == null || keys.Length == 0)
            {
                return Array.Empty<string>();
            }

            var resolved = new List<string>(keys.Length);
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                if (key == null || string.IsNullOrWhiteSpace(key.SceneName))
                {
                    continue;
                }

                resolved.Add(key.SceneName.Trim());
            }

            return resolved
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        private static string ResolveSingleKey(SceneKeyAsset key)
        {
            if (key == null || string.IsNullOrWhiteSpace(key.SceneName))
            {
                return string.Empty;
            }

            return key.SceneName.Trim();
        }
    }
}
