#nullable enable
using System;
using System.Collections.Generic;
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
        public TransitionStyleId StyleId { get; }
        public SceneTransitionPayload Payload { get; }
        public string Reason { get; }
        public SceneFlowProfileId TransitionProfileId { get; }
        public SceneTransitionProfile TransitionProfile { get; }
        public string TransitionProfileName => TransitionProfileId.Value;
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
            SceneTransitionProfile transitionProfile,
            bool useFade = true,
            SceneFlowProfileId transitionProfileId = default,
            string? contextSignature = null,
            string? requestedBy = null,
            string? reason = null)
            : this(
                scenesToLoad,
                scenesToUnload,
                targetActiveScene,
                SceneRouteId.None,
                TransitionStyleId.None,
                SceneTransitionPayload.Empty,
                transitionProfile,
                useFade,
                transitionProfileId,
                contextSignature,
                requestedBy,
                reason)
        {
        }

        public SceneTransitionRequest(
            SceneRouteId routeId,
            TransitionStyleId styleId,
            SceneTransitionPayload payload,
            SceneTransitionProfile transitionProfile,
            SceneFlowProfileId transitionProfileId = default,
            bool useFade = true,
            string? contextSignature = null,
            string? requestedBy = null,
            string? reason = null)
            : this(
                Array.Empty<string>(),
                Array.Empty<string>(),
                string.Empty,
                routeId,
                styleId,
                payload,
                transitionProfile,
                useFade,
                transitionProfileId,
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
            TransitionStyleId styleId,
            SceneTransitionPayload payload,
            SceneTransitionProfile transitionProfile,
            bool useFade,
            SceneFlowProfileId transitionProfileId,
            string? contextSignature,
            string? requestedBy,
            string? reason)
        {
            ScenesToLoad = scenesToLoad ?? Array.Empty<string>();
            ScenesToUnload = scenesToUnload ?? Array.Empty<string>();
            TargetActiveScene = targetActiveScene ?? string.Empty;
            RouteId = routeId;
            StyleId = styleId;
            Payload = payload ?? SceneTransitionPayload.Empty;
            TransitionProfile = transitionProfile ?? throw new InvalidOperationException("[FATAL][Config] SceneTransitionRequest exige SceneTransitionProfile não nulo.");
            UseFade = useFade;
            TransitionProfileId = transitionProfileId;
            ContextSignature = string.IsNullOrWhiteSpace(contextSignature) ? string.Empty : contextSignature.Trim();
            RequestedBy = string.IsNullOrWhiteSpace(requestedBy) ? string.Empty : requestedBy.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }
    }
}
