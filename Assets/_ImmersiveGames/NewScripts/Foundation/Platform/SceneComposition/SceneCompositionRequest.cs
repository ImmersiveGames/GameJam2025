using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.UnityUtils;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.SceneComposition
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
            Reason = reason.TrimToEmpty();
            CorrelationId = correlationId.TrimToEmpty();
            ScenesToLoad = scenesToLoad ?? Array.Empty<string>();
            ScenesToUnload = scenesToUnload ?? Array.Empty<string>();
            ActiveScene = activeScene.TrimToEmpty();
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


