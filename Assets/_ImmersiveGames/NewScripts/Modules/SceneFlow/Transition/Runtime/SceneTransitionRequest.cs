#nullable enable
using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Bindings;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime
{
    public sealed class SceneTransitionRequest
    {
        public IReadOnlyList<string> ScenesToLoad { get; }
        public IReadOnlyList<string> ScenesToUnload { get; }
        public string TargetActiveScene { get; }
        public bool UseFade { get; }
        public SceneRouteId RouteId { get; }
        public TransitionStyleAsset? TransitionStyle { get; }
        public string StyleLabel => TransitionStyle != null ? TransitionStyle.StyleLabel : string.Empty;
        public SceneTransitionPayload Payload { get; }
        public string Reason { get; }
        public SceneTransitionProfile? TransitionProfile { get; }
        public string TransitionProfileName => TransitionProfile != null ? NormalizeLabel(TransitionProfile.name) : string.Empty;
        public string ContextSignature { get; }
        public string RequestedBy { get; }

        public bool HasInlineSceneData =>
            (ScenesToLoad != null && ScenesToLoad.Count > 0) ||
            (ScenesToUnload != null && ScenesToUnload.Count > 0) ||
            !string.IsNullOrWhiteSpace(TargetActiveScene);

        public SceneTransitionRequest(
            IReadOnlyList<string> scenesToLoad,
            IReadOnlyList<string> scenesToUnload,
            string targetActiveScene,
            SceneTransitionProfile? transitionProfile,
            bool useFade = true,
            string? contextSignature = null,
            string? requestedBy = null,
            string? reason = null)
            : this(
                scenesToLoad,
                scenesToUnload,
                targetActiveScene,
                SceneRouteId.None,
                null,
                SceneTransitionPayload.Empty,
                transitionProfile,
                useFade,
                contextSignature,
                requestedBy,
                reason)
        {
        }

        public SceneTransitionRequest(
            SceneRouteId routeId,
            TransitionStyleAsset? transitionStyle,
            SceneTransitionPayload payload,
            SceneTransitionProfile? transitionProfile,
            bool useFade = true,
            string? contextSignature = null,
            string? requestedBy = null,
            string? reason = null)
            : this(
                Array.Empty<string>(),
                Array.Empty<string>(),
                string.Empty,
                routeId,
                transitionStyle,
                payload,
                transitionProfile,
                useFade,
                contextSignature,
                requestedBy,
                reason)
        {
        }

        public SceneTransitionRequest(
            IReadOnlyList<string> scenesToLoad,
            IReadOnlyList<string> scenesToUnload,
            string targetActiveScene,
            SceneRouteId routeId,
            TransitionStyleAsset? transitionStyle,
            SceneTransitionPayload payload,
            SceneTransitionProfile? transitionProfile,
            bool useFade,
            string? contextSignature,
            string? requestedBy,
            string? reason)
        {
            ScenesToLoad = scenesToLoad ?? Array.Empty<string>();
            ScenesToUnload = scenesToUnload ?? Array.Empty<string>();
            TargetActiveScene = targetActiveScene ?? string.Empty;
            RouteId = routeId;
            TransitionStyle = transitionStyle;
            Payload = payload ?? SceneTransitionPayload.Empty;
            TransitionProfile = transitionProfile;
            UseFade = useFade && transitionProfile != null;
            ContextSignature = string.IsNullOrWhiteSpace(contextSignature) ? string.Empty : contextSignature.Trim();
            RequestedBy = NormalizeLabel(requestedBy);
            Reason = NormalizeLabel(reason);
        }

        private static string NormalizeLabel(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
