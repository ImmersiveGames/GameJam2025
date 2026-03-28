using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Audio.Config;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings
{
    /// <summary>
     /// OWNER: definicao de uma rota (cenas/policy) e validacoes de consistencia.
     /// NAO E OWNER: aplicacao de load/unload/fade no runtime.
     /// PUBLISH/CONSUME: sem EventBus; convertido para SceneRouteDefinition diretamente pelo consumidor.
     /// Fases tocadas: RouteExecutionPlan (dados de rota resolvidos antes do ApplyRoute).
     /// </summary>
    [CreateAssetMenu(
        fileName = "SceneRouteDefinitionAsset",
        menuName = "ImmersiveGames/NewScripts/Modules/SceneFlow/Navigation/Definitions/SceneRouteDefinitionAsset",
        order = 30)]

    public sealed partial class SceneRouteDefinitionAsset : ScriptableObject
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
        [SerializeField] private LevelCollectionAsset levelCollection;

        [Header("Audio (Optional)")]
        [SerializeField] private AudioBgmCueAsset bgmCue;

        public SceneRouteId RouteId => routeId;
        public SceneRouteKind RouteKind => routeKind;
        public bool RequiresWorldReset => requiresWorldReset;
        public LevelCollectionAsset LevelCollection => levelCollection;
        public AudioBgmCueAsset BgmCue => bgmCue;

        public SceneRouteDefinition ToDefinition()
        {
            EnsureValidRoutePolicy();

            string[] load = ResolveKeys(scenesToLoadKeys, nameof(scenesToLoadKeys));
            string[] unload = ResolveKeys(scenesToUnloadKeys, nameof(scenesToUnloadKeys));
            string active = ResolveSingleKey(targetActiveSceneKey, nameof(targetActiveSceneKey));

            DebugUtility.Log(typeof(SceneRouteDefinitionAsset),
                $"[OBS][SceneFlow] RouteSceneListResolved routeId='{routeId}' field='{nameof(scenesToUnloadKeys)}' scenes=[{FormatSceneDetails(unload)}].",
                DebugUtility.Colors.Info);

            return new SceneRouteDefinition(load, unload, active, routeKind, requiresWorldReset);
        }

        public void ValidateRoutePolicyOrFailFast()
        {
            EnsureValidRoutePolicy();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnValidate()
        {
            ValidateRoutePolicyEditorOnly();
        }
#endif

        private void ValidateRoutePolicyEditorOnly()
        {
            string validationError = GetRoutePolicyValidationError();
            if (string.IsNullOrWhiteSpace(validationError))
            {
                return;
            }

            DebugUtility.LogWarning(
                typeof(SceneRouteDefinitionAsset),
                $"[Config][Editor] routeId='{routeId}' invalida. detail='{validationError}'");
        }

        private void EnsureValidRoutePolicy()
        {
            string validationError = GetRoutePolicyValidationError();
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                FailFast(validationError);
            }
        }

        private string GetRoutePolicyValidationError()
        {
            if (routeKind == SceneRouteKind.Unspecified)
            {
                return $"routeId='{routeId}' com RouteKind='{SceneRouteKind.Unspecified}' e invalido para policy de reset.";
            }

            if (routeKind == SceneRouteKind.Gameplay && !requiresWorldReset)
            {
                return $"routeId='{routeId}' Gameplay exige requiresWorldReset=true.";
            }

            if (routeKind == SceneRouteKind.Frontend && requiresWorldReset)
            {
                return $"routeId='{routeId}' Frontend/Menu exige requiresWorldReset=false.";
            }

            if (routeKind == SceneRouteKind.Gameplay)
            {
                if (levelCollection == null)
                {
                    return $"routeId='{routeId}' Gameplay exige levelCollection configurado.";
                }

                if (!levelCollection.TryValidateRuntime(out string collectionError))
                {
                    return $"routeId='{routeId}' Gameplay com levelCollection invalida. detail='{collectionError}'.";
                }
            }

            if (routeKind == SceneRouteKind.Frontend && levelCollection != null)
            {
                return $"routeId='{routeId}' Frontend/Menu exige levelCollection=null.";
            }

            return string.Empty;
        }

        private static string[] ResolveKeys(SceneKeyAsset[] keys, string fieldName)
        {
            if (keys == null || keys.Length == 0)
            {
                DebugUtility.Log(typeof(SceneRouteDefinitionAsset),
                    $"[OBS][SceneFlow] ResolveRouteDefinitionKeys field='{fieldName}' before=[] after=[].",
                    DebugUtility.Colors.Info);
                return Array.Empty<string>();
            }

            var beforeFilter = new List<string>(keys.Length);
            var resolved = new List<string>(keys.Length);
            var seenNames = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                if (key == null)
                {
                    FailFast($"field='{fieldName}' possui key null em index={i}.");
                }

                if (string.IsNullOrWhiteSpace(key.SceneName))
                {
                    FailFast($"field='{fieldName}' possui SceneKeyAsset invalido em index={i} (SceneName vazio). asset='{key.name}'.");
                }

                string sceneName = key.SceneName.Trim();
                beforeFilter.Add(sceneName);

                if (!seenNames.Add(sceneName))
                {
                    DebugUtility.LogWarning(typeof(SceneRouteDefinitionAsset),
                        $"[OBS][SceneFlow] ResolveRouteDefinitionKeysFiltered field='{fieldName}' key='{key.name}' scene='{sceneName}' reason='duplicate_scene_name'.");
                    continue;
                }

                if (!string.Equals(key.name, sceneName, StringComparison.Ordinal))
                {
                    DebugUtility.LogWarning(typeof(SceneRouteDefinitionAsset),
                        $"[OBS][SceneFlow] ResolveRouteDefinitionKeysFiltered field='{fieldName}' key='{key.name}' scene='{sceneName}' reason='invalid_key_name_mismatch'.");
                }

                resolved.Add(sceneName);
            }

            DebugUtility.Log(typeof(SceneRouteDefinitionAsset),
                $"[OBS][SceneFlow] ResolveRouteDefinitionKeys field='{fieldName}' before=[{FormatSceneDetails(beforeFilter)}] after=[{FormatSceneDetails(resolved)}].",
                DebugUtility.Colors.Info);

            return resolved.ToArray();
        }

        private static string FormatSceneDetails(IReadOnlyList<string> scenes)
        {
            if (scenes == null || scenes.Count == 0)
            {
                return "<none>";
            }

            var details = new List<string>(scenes.Count);
            for (int i = 0; i < scenes.Count; i++)
            {
                string sceneName = scenes[i] ?? string.Empty;
                int buildIndex = ResolveBuildIndex(sceneName);
                bool isInBuildSettings = Application.CanStreamedLevelBeLoaded(sceneName);
                details.Add($"name='{sceneName}', buildIndex={buildIndex}, isInBuildSettings={isInBuildSettings}");
            }

            return string.Join(" | ", details);
        }

        private static int ResolveBuildIndex(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return -1;
            }

            Scene loadedScene = SceneManager.GetSceneByName(sceneName);
            if (loadedScene.IsValid())
            {
                return loadedScene.buildIndex;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return ResolveBuildIndexEditorOnly(sceneName);
#else
            return -1;
#endif
        }

        private static int ResolveBuildIndexEditorOnly(string sceneName)
        {
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return -1;
            }

            EditorBuildSettingsScene[] scenes = UnityEditor.EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                var scene = scenes[i];
                if (scene == null || string.IsNullOrWhiteSpace(scene.path))
                {
                    continue;
                }

                string name = System.IO.Path.GetFileNameWithoutExtension(scene.path);
                if (string.Equals(name, sceneName, StringComparison.Ordinal))
                {
                    return i;
                }
            }
#endif
            return -1;
        }

        private static string ResolveSingleKey(SceneKeyAsset key, string fieldName)
        {
            if (key == null)
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(key.SceneName))
            {
                FailFast($"field='{fieldName}' possui SceneKeyAsset invalido (SceneName vazio). asset='{key.name}'.");
            }

            return key.SceneName.Trim();
        }

        private static void FailFast(string message)
        {
            HardFailFastH1.Trigger(typeof(SceneRouteDefinitionAsset), $"[Config] {message}");
        }
    }
}
