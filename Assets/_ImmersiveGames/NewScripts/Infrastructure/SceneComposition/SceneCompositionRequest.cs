using System;
using System.Collections.Generic;

namespace _ImmersiveGames.NewScripts.Infrastructure.SceneComposition
{
    public readonly struct SceneCompositionRequest
    {
        public SceneCompositionRequest(
            SceneCompositionScope scope,
            string reason,
            string correlationId,
            IReadOnlyList<string> scenesToLoad,
            IReadOnlyList<string> scenesToUnload,
            string activeScene)
        {
            Scope = scope;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? string.Empty : correlationId.Trim();
            ScenesToLoad = scenesToLoad ?? Array.Empty<string>();
            ScenesToUnload = scenesToUnload ?? Array.Empty<string>();
            ActiveScene = string.IsNullOrWhiteSpace(activeScene) ? string.Empty : activeScene.Trim();
        }

        public SceneCompositionScope Scope { get; }
        public string Reason { get; }
        public string CorrelationId { get; }
        public IReadOnlyList<string> ScenesToLoad { get; }
        public IReadOnlyList<string> ScenesToUnload { get; }
        public string ActiveScene { get; }

        public bool HasOperations =>
            (ScenesToLoad?.Count ?? 0) > 0 ||
            (ScenesToUnload?.Count ?? 0) > 0 ||
            !string.IsNullOrWhiteSpace(ActiveScene);
    }
}
