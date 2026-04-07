using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.SceneComposition;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Bindings;

namespace _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime
{
    public static class PhaseDefinitionSceneCompositionRequestFactory
    {
        public static SceneCompositionRequest CreateApplyRequest(
            PhaseDefinitionAsset phaseDefinitionRef,
            string reason,
            string correlationId)
        {
            if (phaseDefinitionRef == null)
            {
                HardFailFastH1.Trigger(typeof(PhaseDefinitionSceneCompositionRequestFactory),
                    $"[FATAL][H1][PhaseDefinition] Target phaseDefinitionRef is null for scene composition. correlationId='{correlationId}' reason='{reason}'.");
            }

            phaseDefinitionRef.ValidateOrFail($"PhaseDefinitionSceneCompositionRequestFactory/Apply correlationId='{correlationId}'");

            IReadOnlyList<string> scenesToLoad = BuildSceneListOrFail(phaseDefinitionRef, correlationId, reason, out string contentMainScene);
            IReadOnlyList<string> scenesToUnload = BuildSceneUnloadListOrFail(correlationId, reason, scenesToLoad);

            DebugUtility.Log(typeof(PhaseDefinitionSceneCompositionRequestFactory),
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseDefinitionContentTranslated phaseId='{phaseDefinitionRef.PhaseId}' phaseRef='{phaseDefinitionRef.name}' scenesToLoad=[{string.Join(",", scenesToLoad)}] scenesToUnload=[{string.Join(",", scenesToUnload)}] contentMainScene='{contentMainScene}' correlationId='{correlationId}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            return new SceneCompositionRequest(
                SceneCompositionScope.Local,
                reason,
                correlationId,
                scenesToLoad,
                scenesToUnload,
                activeScene: string.Empty);
        }

        public static SceneCompositionRequest CreateClearRequest(string reason, string correlationId)
        {
            IReadOnlyList<string> scenesToUnload = PhaseContentSceneRuntimeApplier.ActiveAppliedSceneNames;

            DebugUtility.Log(typeof(PhaseDefinitionSceneCompositionRequestFactory),
                $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseDefinitionContentClearTranslated activeScenes=[{string.Join(",", scenesToUnload)}] correlationId='{correlationId}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            return new SceneCompositionRequest(
                SceneCompositionScope.Local,
                reason,
                correlationId,
                scenesToLoad: Array.Empty<string>(),
                scenesToUnload: scenesToUnload,
                activeScene: string.Empty);
        }

        private static IReadOnlyList<string> BuildSceneListOrFail(
            PhaseDefinitionAsset phaseDefinitionRef,
            string correlationId,
            string reason,
            out string contentMainScene)
        {
            List<string> sceneNames = new List<string>();
            HashSet<string> seenSceneNames = new HashSet<string>(StringComparer.Ordinal);
            string resolvedMainScene = string.Empty;
            IReadOnlyList<PhaseDefinitionAsset.PhaseContentEntry> entries = phaseDefinitionRef.Content?.entries;

            if (entries == null || entries.Count == 0)
            {
                HardFailFastH1.Trigger(typeof(PhaseDefinitionSceneCompositionRequestFactory),
                    $"[FATAL][H1][PhaseDefinition] Phase '{phaseDefinitionRef.name}' has no content entries. phaseId='{phaseDefinitionRef.PhaseId}' correlationId='{correlationId}' reason='{reason}'.");
            }

            for (int i = 0; i < entries.Count; i++)
            {
                PhaseDefinitionAsset.PhaseContentEntry entry = entries[i];
                if (entry == null)
                {
                    HardFailFastH1.Trigger(typeof(PhaseDefinitionSceneCompositionRequestFactory),
                        $"[FATAL][H1][PhaseDefinition] Phase '{phaseDefinitionRef.name}' has null content entry at index='{i}'. phaseId='{phaseDefinitionRef.PhaseId}' correlationId='{correlationId}' reason='{reason}'.");
                }

                SceneKeyAsset sceneRef = entry.sceneRef;
                if (sceneRef == null)
                {
                    HardFailFastH1.Trigger(typeof(PhaseDefinitionSceneCompositionRequestFactory),
                        $"[FATAL][H1][PhaseDefinition] Phase '{phaseDefinitionRef.name}' has null sceneRef at index='{i}'. phaseId='{phaseDefinitionRef.PhaseId}' correlationId='{correlationId}' reason='{reason}'.");
                }

                string sceneName = NormalizeSceneNameOrFail(sceneRef.SceneName, phaseDefinitionRef, i, correlationId, reason);
                if (!seenSceneNames.Add(sceneName))
                {
                    HardFailFastH1.Trigger(typeof(PhaseDefinitionSceneCompositionRequestFactory),
                        $"[FATAL][H1][PhaseDefinition] Phase '{phaseDefinitionRef.name}' has duplicate scene name='{sceneName}' at index='{i}'. phaseId='{phaseDefinitionRef.PhaseId}' correlationId='{correlationId}' reason='{reason}'.");
                }

                if (entry.role == PhaseDefinitionAsset.PhaseSceneRole.Main)
                {
                    if (!string.IsNullOrWhiteSpace(resolvedMainScene))
                    {
                        HardFailFastH1.Trigger(typeof(PhaseDefinitionSceneCompositionRequestFactory),
                            $"[FATAL][H1][PhaseDefinition] Phase '{phaseDefinitionRef.name}' has more than one Main scene. previousMainScene='{resolvedMainScene}' duplicateScene='{sceneName}' phaseId='{phaseDefinitionRef.PhaseId}' correlationId='{correlationId}' reason='{reason}'.");
                    }

                    resolvedMainScene = sceneName;
                }

                sceneNames.Add(sceneName);
            }

            if (string.IsNullOrWhiteSpace(resolvedMainScene))
            {
                HardFailFastH1.Trigger(typeof(PhaseDefinitionSceneCompositionRequestFactory),
                    $"[FATAL][H1][PhaseDefinition] Phase '{phaseDefinitionRef.name}' has no Main scene to anchor the local composition. phaseId='{phaseDefinitionRef.PhaseId}' correlationId='{correlationId}' reason='{reason}'.");
            }

            contentMainScene = resolvedMainScene;
            return sceneNames;
        }

        private static IReadOnlyList<string> BuildSceneUnloadListOrFail(string correlationId, string reason, IReadOnlyList<string> scenesToLoad)
        {
            IReadOnlyList<string> previousScenes = PhaseContentSceneRuntimeApplier.ActiveAppliedSceneNames;
            if (previousScenes == null || previousScenes.Count == 0)
            {
                return Array.Empty<string>();
            }

            HashSet<string> loadSet = new HashSet<string>(scenesToLoad ?? Array.Empty<string>(), StringComparer.Ordinal);
            List<string> scenesToUnload = new List<string>();

            for (int i = 0; i < previousScenes.Count; i++)
            {
                string sceneName = NormalizeSceneNameOrFail(previousScenes[i], phaseDefinitionRef: null, i, correlationId, reason);
                if (loadSet.Contains(sceneName))
                {
                    continue;
                }

                scenesToUnload.Add(sceneName);
            }

            return scenesToUnload;
        }

        private static string NormalizeSceneNameOrFail(
            string sceneName,
            PhaseDefinitionAsset phaseDefinitionRef,
            int index,
            string correlationId,
            string reason)
        {
            string normalized = string.IsNullOrWhiteSpace(sceneName) ? string.Empty : sceneName.Trim();
            if (!string.IsNullOrEmpty(normalized))
            {
                return normalized;
            }

            string phaseLabel = phaseDefinitionRef != null ? $" phaseId='{phaseDefinitionRef.PhaseId}' phaseRef='{phaseDefinitionRef.name}'" : string.Empty;
            HardFailFastH1.Trigger(typeof(PhaseDefinitionSceneCompositionRequestFactory),
                $"[FATAL][H1][PhaseDefinition] Empty scene name detected at index='{index}'.{phaseLabel} correlationId='{correlationId}' reason='{reason}'.");
            return string.Empty;
        }
    }
}
