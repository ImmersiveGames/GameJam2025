using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings
{
    [CreateAssetMenu(
        fileName = "SceneRouteDefinitionAsset",
        menuName = "ImmersiveGames/NewScripts/Modules/SceneFlow/Navigation/Definitions/SceneRouteDefinitionAsset",
        order = 30)]
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
            var load = ResolveKeys(scenesToLoadKeys, nameof(scenesToLoadKeys));
            var unload = ResolveKeys(scenesToUnloadKeys, nameof(scenesToUnloadKeys));
            var active = ResolveSingleKey(targetActiveSceneKey, nameof(targetActiveSceneKey));
            return new SceneRouteDefinition(load, unload, active, routeKind, requiresWorldReset);
        }

        private static string[] ResolveKeys(SceneKeyAsset[] keys, string fieldName)
        {
            if (keys == null || keys.Length == 0)
            {
                return Array.Empty<string>();
            }

            var resolved = new List<string>(keys.Length);
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                if (key == null)
                {
                    FailFast($"field='{fieldName}' possui key null em index={i}.");
                }

                if (string.IsNullOrWhiteSpace(key.SceneName))
                {
                    FailFast($"field='{fieldName}' possui SceneKeyAsset inválido em index={i} (SceneName vazio). asset='{key.name}'.");
                }

                resolved.Add(key.SceneName.Trim());
            }

            return resolved
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        private static string ResolveSingleKey(SceneKeyAsset key, string fieldName)
        {
            if (key == null)
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(key.SceneName))
            {
                FailFast($"field='{fieldName}' possui SceneKeyAsset inválido (SceneName vazio). asset='{key.name}'.");
            }

            return key.SceneName.Trim();
        }

        private static void FailFast(string message)
        {
            DebugUtility.LogError(typeof(SceneRouteDefinitionAsset), $"[FATAL][Config] {message}");
            throw new InvalidOperationException(message);
        }
    }
}
