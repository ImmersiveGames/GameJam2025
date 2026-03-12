#nullable enable
using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
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
        public string StyleLabel { get; }
        public SceneTransitionPayload Payload { get; }
        public string Reason { get; }
        public SceneTransitionProfile? TransitionProfile { get; }
        public string TransitionProfileName { get; }
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
            string? transitionProfileName = null,
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
                styleLabel: null,
                transitionProfileName: transitionProfileName,
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
            string? styleLabel = null,
            string? transitionProfileName = null,
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
                styleLabel,
                transitionProfileName,
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
            string? styleLabel,
            string? transitionProfileName,
            string? contextSignature,
            string? requestedBy,
            string? reason)
        {
            ScenesToLoad = scenesToLoad ?? Array.Empty<string>();
            ScenesToUnload = scenesToUnload ?? Array.Empty<string>();
            TargetActiveScene = targetActiveScene ?? string.Empty;
            RouteId = routeId;
            TransitionStyle = transitionStyle;
            StyleLabel = NormalizeLabel(styleLabel, transitionStyle != null ? transitionStyle.StyleLabel : string.Empty);
            Payload = payload ?? SceneTransitionPayload.Empty;
            TransitionProfile = transitionProfile;
            UseFade = useFade && transitionProfile != null;
            TransitionProfileName = NormalizeLabel(transitionProfileName, transitionProfile != null ? transitionProfile.name : string.Empty);
            ContextSignature = string.IsNullOrWhiteSpace(contextSignature) ? string.Empty : contextSignature.Trim();
            RequestedBy = string.IsNullOrWhiteSpace(requestedBy) ? string.Empty : requestedBy.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        private static string NormalizeLabel(string? explicitLabel, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(explicitLabel))
            {
                return explicitLabel.Trim();
            }

            return string.IsNullOrWhiteSpace(fallback) ? string.Empty : fallback.Trim();
        }
    }
}
