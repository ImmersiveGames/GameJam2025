using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.SceneComposition;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime
{
    public static class RouteSceneCompositionRequestFactory
    {
        public static SceneCompositionRequest CreateMacroApplyRequest(
            IReadOnlyList<string> scenesToLoad,
            IReadOnlyList<string> scenesToUnload,
            string reason,
            string correlationId)
        {
            IReadOnlyList<string> normalizedLoad = NormalizeSceneListOrFail(scenesToLoad, "Load", correlationId, reason);
            IReadOnlyList<string> normalizedUnload = NormalizeSceneListOrFail(scenesToUnload, "Unload", correlationId, reason);
            return CreateRequest(normalizedLoad, normalizedUnload, reason, correlationId);
        }

        public static SceneCompositionRequest CreateLoadRequest(string sceneName, string reason, string correlationId)
        {
            return CreateRequest(new[] { NormalizeSceneNameOrFail(sceneName, "Load", correlationId, reason) }, Array.Empty<string>(), reason, correlationId);
        }

        public static SceneCompositionRequest CreateUnloadRequest(string sceneName, string reason, string correlationId)
        {
            return CreateRequest(Array.Empty<string>(), new[] { NormalizeSceneNameOrFail(sceneName, "Unload", correlationId, reason) }, reason, correlationId);
        }

        public static SceneCompositionRequest CreateRequest(IReadOnlyList<string> scenesToLoad, IReadOnlyList<string> scenesToUnload, string reason, string correlationId)
        {
            return new SceneCompositionRequest(
                SceneCompositionScope.Macro,
                string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim(),
                string.IsNullOrWhiteSpace(correlationId) ? string.Empty : correlationId.Trim(),
                scenesToLoad ?? Array.Empty<string>(),
                scenesToUnload ?? Array.Empty<string>(),
                activeScene: string.Empty);
        }

        private static IReadOnlyList<string> NormalizeSceneListOrFail(IReadOnlyList<string>? scenes, string phase, string correlationId, string reason)
        {
            if (scenes == null || scenes.Count == 0)
            {
                return Array.Empty<string>();
            }

            return scenes
                .Select(scene => NormalizeSceneNameOrFail(scene, phase, correlationId, reason))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        private static string NormalizeSceneNameOrFail(string sceneName, string phase, string correlationId, string reason)
        {
            string normalized = string.IsNullOrWhiteSpace(sceneName) ? string.Empty : sceneName.Trim();
            if (!string.IsNullOrEmpty(normalized))
            {
                return normalized;
            }

            HardFailFastH1.Trigger(typeof(RouteSceneCompositionRequestFactory),
                $"[FATAL][H1][SceneFlow] Empty macro scene name detected during phase='{phase}'. correlationId='{correlationId}' reason='{reason}'.");
            return string.Empty;
        }
    }
}
