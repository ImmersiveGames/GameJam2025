using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.SceneComposition;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    public static class LevelSceneCompositionRequestFactory
    {
        public static SceneCompositionRequest CreateApplyRequest(
            LevelDefinitionAsset previousLevelRef,
            LevelDefinitionAsset targetLevelRef,
            string reason,
            string correlationId)
        {
            if (targetLevelRef == null)
            {
                HardFailFastH1.Trigger(typeof(LevelSceneCompositionRequestFactory),
                    $"[FATAL][H1][LevelFlow] Target levelRef is null for local scene composition. correlationId='{correlationId}' reason='{reason}'.");
            }

            targetLevelRef.ValidateOrFailFast($"LevelSceneCompositionRequestFactory/Apply correlationId='{correlationId}'");
            if (previousLevelRef != null)
            {
                previousLevelRef.ValidateOrFailFast($"LevelSceneCompositionRequestFactory/Previous correlationId='{correlationId}'");
            }

            return new SceneCompositionRequest(
                SceneCompositionScope.Local,
                reason,
                correlationId,
                scenesToLoad: BuildSceneNameList(targetLevelRef, "Target", correlationId, reason),
                scenesToUnload: previousLevelRef != null
                    ? BuildSceneNameList(previousLevelRef, "Previous", correlationId, reason)
                    : ArrayEmpty<string>.Value,
                activeScene: string.Empty);
        }

        public static SceneCompositionRequest CreateClearRequest(
            LevelDefinitionAsset previousLevelRef,
            string reason,
            string correlationId)
        {
            if (previousLevelRef == null)
            {
                return new SceneCompositionRequest(
                    SceneCompositionScope.Local,
                    reason,
                    correlationId,
                    scenesToLoad: ArrayEmpty<string>.Value,
                    scenesToUnload: ArrayEmpty<string>.Value,
                    activeScene: string.Empty);
            }

            previousLevelRef.ValidateOrFailFast($"LevelSceneCompositionRequestFactory/Clear correlationId='{correlationId}'");

            return new SceneCompositionRequest(
                SceneCompositionScope.Local,
                reason,
                correlationId,
                scenesToLoad: ArrayEmpty<string>.Value,
                scenesToUnload: BuildSceneNameList(previousLevelRef, "Previous", correlationId, reason),
                activeScene: string.Empty);
        }

        private static IReadOnlyList<string> BuildSceneNameList(
            LevelDefinitionAsset levelRef,
            string label,
            string correlationId,
            string reason)
        {
            HashSet<string> seenNames = new HashSet<string>(System.StringComparer.Ordinal);
            List<string> sceneNames = new List<string>();

            IReadOnlyList<SceneBuildIndexRef> additiveScenes = levelRef.AdditiveScenes;
            if (additiveScenes == null || additiveScenes.Count == 0)
            {
                HardFailFastH1.Trigger(typeof(LevelSceneCompositionRequestFactory),
                    $"[FATAL][H1][LevelFlow] Level '{levelRef.name}' has no additive scenes for label='{label}'. correlationId='{correlationId}' reason='{reason}'.");
            }

            for (int i = 0; i < additiveScenes.Count; i++)
            {
                SceneBuildIndexRef sceneRef = additiveScenes[i];
                if (sceneRef == null)
                {
                    HardFailFastH1.Trigger(typeof(LevelSceneCompositionRequestFactory),
                        $"[FATAL][H1][LevelFlow] Level '{levelRef.name}' has null additive scene ref at index='{i}' for label='{label}'. correlationId='{correlationId}' reason='{reason}'.");
                }

                string sceneName = string.IsNullOrWhiteSpace(sceneRef.SceneName) ? string.Empty : sceneRef.SceneName.Trim();
                if (string.IsNullOrEmpty(sceneName))
                {
                    HardFailFastH1.Trigger(typeof(LevelSceneCompositionRequestFactory),
                        $"[FATAL][H1][LevelFlow] Level '{levelRef.name}' has empty additive scene name at index='{i}' buildIndex='{sceneRef.BuildIndex}' for label='{label}'. correlationId='{correlationId}' reason='{reason}'.");
                }

                if (!seenNames.Add(sceneName))
                {
                    HardFailFastH1.Trigger(typeof(LevelSceneCompositionRequestFactory),
                        $"[FATAL][H1][LevelFlow] Level '{levelRef.name}' has duplicate additive scene name='{sceneName}' for label='{label}'. correlationId='{correlationId}' reason='{reason}'.");
                }

                sceneNames.Add(sceneName);
            }

            return sceneNames;
        }

        private static class ArrayEmpty<T>
        {
            public static readonly T[] Value = System.Array.Empty<T>();
        }
    }
}
