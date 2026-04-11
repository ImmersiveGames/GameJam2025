using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Config;
using _ImmersiveGames.NewScripts.Orchestration.SceneComposition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    public static class LevelSceneCompositionRequestFactory
    {
        public static SceneCompositionRequest CreateApplyRequest(
            PhaseDefinitionAsset previousPhaseDefinitionRef,
            PhaseDefinitionAsset targetPhaseDefinitionRef,
            string reason,
            string correlationId)
        {
            if (targetPhaseDefinitionRef == null)
            {
                HardFailFastH1.Trigger(typeof(LevelSceneCompositionRequestFactory),
                    $"[FATAL][H1][GameplaySessionFlow] Target phaseDefinitionRef is null for local scene composition. correlationId='{correlationId}' reason='{reason}'.");
            }

            targetPhaseDefinitionRef.ValidateOrFail($"LevelSceneCompositionRequestFactory/Apply correlationId='{correlationId}'");
            if (previousPhaseDefinitionRef != null)
            {
                previousPhaseDefinitionRef.ValidateOrFail($"LevelSceneCompositionRequestFactory/Previous correlationId='{correlationId}'");
            }

            PhaseDefinitionAsset.PhaseSwapBlock swap = targetPhaseDefinitionRef.Swap;
            if (swap == null)
            {
                HardFailFastH1.Trigger(typeof(LevelSceneCompositionRequestFactory),
                    $"[FATAL][H1][GameplaySessionFlow] Target phaseDefinitionRef '{targetPhaseDefinitionRef.name}' has no swap block. correlationId='{correlationId}' reason='{reason}'.");
            }

            return new SceneCompositionRequest(
                SceneCompositionScope.Local,
                reason,
                correlationId,
                scenesToLoad: BuildSceneNameList(swap.additiveScenes, targetPhaseDefinitionRef.name, "Target", correlationId, reason),
                scenesToUnload: previousPhaseDefinitionRef != null
                    ? BuildSceneNameList(previousPhaseDefinitionRef.Swap != null ? previousPhaseDefinitionRef.Swap.additiveScenes : null, previousPhaseDefinitionRef.name, "Previous", correlationId, reason)
                    : ArrayEmpty<string>.Value,
                activeScene: string.Empty);
        }

        private static IReadOnlyList<string> BuildSceneNameList(
            IReadOnlyList<SceneBuildIndexRef> additiveScenes,
            string ownerName,
            string label,
            string correlationId,
            string reason)
        {
            HashSet<string> seenNames = new HashSet<string>(System.StringComparer.Ordinal);
            List<string> sceneNames = new List<string>();

            if (additiveScenes == null || additiveScenes.Count == 0)
            {
                HardFailFastH1.Trigger(typeof(LevelSceneCompositionRequestFactory),
                    $"[FATAL][H1][GameplaySessionFlow] Phase '{ownerName}' has no additive scenes for label='{label}'. correlationId='{correlationId}' reason='{reason}'.");
            }

            for (int i = 0; i < additiveScenes.Count; i++)
            {
                SceneBuildIndexRef sceneRef = additiveScenes[i];
                if (sceneRef == null)
                {
                    HardFailFastH1.Trigger(typeof(LevelSceneCompositionRequestFactory),
                        $"[FATAL][H1][GameplaySessionFlow] Phase '{ownerName}' has null additive scene ref at index='{i}' for label='{label}'. correlationId='{correlationId}' reason='{reason}'.");
                }

                string sceneName = string.IsNullOrWhiteSpace(sceneRef.SceneName) ? string.Empty : sceneRef.SceneName.Trim();
                if (string.IsNullOrEmpty(sceneName))
                {
                    HardFailFastH1.Trigger(typeof(LevelSceneCompositionRequestFactory),
                        $"[FATAL][H1][GameplaySessionFlow] Phase '{ownerName}' has empty additive scene name at index='{i}' buildIndex='{sceneRef.BuildIndex}' for label='{label}'. correlationId='{correlationId}' reason='{reason}'.");
                }

                if (!seenNames.Add(sceneName))
                {
                    HardFailFastH1.Trigger(typeof(LevelSceneCompositionRequestFactory),
                        $"[FATAL][H1][GameplaySessionFlow] Phase '{ownerName}' has duplicate additive scene name='{sceneName}' for label='{label}'. correlationId='{correlationId}' reason='{reason}'.");
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
