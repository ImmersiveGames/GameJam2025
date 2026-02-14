using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Bindings;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime
{
    public readonly struct SceneTransitionContext : IEquatable<SceneTransitionContext>
    {
        public IReadOnlyList<string> ScenesToLoad { get; }
        public IReadOnlyList<string> ScenesToUnload { get; }
        public string TargetActiveScene { get; }
        public bool UseFade { get; }
        public SceneRouteId RouteId { get; }
        public TransitionStyleId StyleId { get; }
        public string Reason { get; }
        public SceneFlowProfileId TransitionProfileId { get; }
        public SceneTransitionProfile TransitionProfile { get; }
        public string TransitionProfileName => TransitionProfileId.Value;
        public bool RequiresWorldReset { get; }
        public string ResetDecisionSource { get; }
        public string ResetDecisionReason { get; }
        public string ContextSignature { get; }

        public SceneTransitionContext(
            IReadOnlyList<string> scenesToLoad,
            IReadOnlyList<string> scenesToUnload,
            string targetActiveScene,
            bool useFade,
            SceneRouteId routeId,
            TransitionStyleId styleId,
            string reason,
            SceneFlowProfileId transitionProfileId,
            SceneTransitionProfile transitionProfile,
            bool requiresWorldReset = false,
            string resetDecisionSource = null,
            string resetDecisionReason = null,
            string contextSignature = null)
        {
            ScenesToLoad = scenesToLoad;
            ScenesToUnload = scenesToUnload;
            TargetActiveScene = targetActiveScene;
            UseFade = useFade;
            RouteId = routeId;
            StyleId = styleId;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            TransitionProfileId = transitionProfileId;
            TransitionProfile = transitionProfile;
            RequiresWorldReset = requiresWorldReset;
            ResetDecisionSource = string.IsNullOrWhiteSpace(resetDecisionSource) ? string.Empty : resetDecisionSource.Trim();
            ResetDecisionReason = string.IsNullOrWhiteSpace(resetDecisionReason) ? string.Empty : resetDecisionReason.Trim();

            ContextSignature = !string.IsNullOrWhiteSpace(contextSignature)
                ? contextSignature.Trim()
                : ComputeSignature(scenesToLoad, scenesToUnload, targetActiveScene, routeId, styleId, useFade, transitionProfileId, transitionProfile);
        }

        public SceneTransitionContext WithRouteResetDecision(bool requiresWorldReset, string decisionSource, string decisionReason)
            => new(
                ScenesToLoad,
                ScenesToUnload,
                TargetActiveScene,
                UseFade,
                RouteId,
                StyleId,
                Reason,
                TransitionProfileId,
                TransitionProfile,
                requiresWorldReset,
                decisionSource,
                decisionReason,
                ContextSignature);

        private static string ComputeSignature(
            IReadOnlyList<string> scenesToLoad,
            IReadOnlyList<string> scenesToUnload,
            string targetActiveScene,
            SceneRouteId routeId,
            TransitionStyleId styleId,
            bool useFade,
            SceneFlowProfileId transitionProfileId,
            SceneTransitionProfile transitionProfile)
        {
            string load = JoinList(scenesToLoad);
            string unload = JoinList(scenesToUnload);
            string active = (targetActiveScene ?? string.Empty).Trim();
            string route = routeId.Value ?? string.Empty;
            string style = styleId.Value ?? string.Empty;
            string profile = transitionProfileId.Value ?? string.Empty;
            string profileAsset = transitionProfile != null ? transitionProfile.name : string.Empty;
            string fade = useFade ? "1" : "0";
            return $"r:{route}|s:{style}|p:{profile}|pa:{profileAsset}|a:{active}|f:{fade}|l:{load}|u:{unload}";
        }

        private static string JoinList(IReadOnlyList<string> list)
        {
            if (list == null || list.Count == 0)
            {
                return string.Empty;
            }

            string result = string.Empty;
            for (int i = 0; i < list.Count; i++)
            {
                string entry = (list[i] ?? string.Empty).Trim();
                if (entry.Length == 0)
                {
                    continue;
                }

                result = result.Length == 0 ? entry : result + "|" + entry;
            }

            return result;
        }


        public override string ToString()
        {
            return $"Route='{RouteId}', Style='{StyleId}', Reason='{Reason}', " +
                   $"Load=[{string.Join(", ", ScenesToLoad)}], Unload=[{string.Join(", ", ScenesToUnload)}], " +
                   $"Active='{TargetActiveScene}', UseFade={UseFade}, Profile='{TransitionProfileName}', " +
                   $"ProfileAsset='{(TransitionProfile != null ? TransitionProfile.name : "<null>")}', " +
                   $"RequiresWorldReset={RequiresWorldReset}, DecisionSource='{ResetDecisionSource}', DecisionReason='{ResetDecisionReason}'";
        }

        public bool Equals(SceneTransitionContext other)
            => Equals(ScenesToLoad, other.ScenesToLoad) &&
               Equals(ScenesToUnload, other.ScenesToUnload) &&
               TargetActiveScene == other.TargetActiveScene &&
               UseFade == other.UseFade &&
               RouteId.Equals(other.RouteId) &&
               StyleId.Equals(other.StyleId) &&
               Reason == other.Reason &&
               TransitionProfileId.Equals(other.TransitionProfileId) &&
               Equals(TransitionProfile, other.TransitionProfile);

        public override bool Equals(object obj) => obj is SceneTransitionContext other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ScenesToLoad != null ? ScenesToLoad.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (ScenesToUnload != null ? ScenesToUnload.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (TargetActiveScene != null ? TargetActiveScene.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ UseFade.GetHashCode();
                hashCode = (hashCode * 397) ^ RouteId.GetHashCode();
                hashCode = (hashCode * 397) ^ StyleId.GetHashCode();
                hashCode = (hashCode * 397) ^ (Reason != null ? Reason.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ TransitionProfileId.GetHashCode();
                hashCode = (hashCode * 397) ^ (TransitionProfile != null ? TransitionProfile.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(SceneTransitionContext left, SceneTransitionContext right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SceneTransitionContext left, SceneTransitionContext right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct SceneTransitionStartedEvent : IEvent { public readonly SceneTransitionContext Context; public SceneTransitionStartedEvent(SceneTransitionContext context) { Context = context; } }
    public readonly struct SceneTransitionFadeInCompletedEvent : IEvent { public readonly SceneTransitionContext Context; public SceneTransitionFadeInCompletedEvent(SceneTransitionContext context) { Context = context; } }
    public readonly struct SceneTransitionScenesReadyEvent : IEvent { public readonly SceneTransitionContext Context; public SceneTransitionScenesReadyEvent(SceneTransitionContext context) { Context = context; } }
    public readonly struct SceneTransitionBeforeFadeOutEvent : IEvent { public readonly SceneTransitionContext Context; public SceneTransitionBeforeFadeOutEvent(SceneTransitionContext context) { Context = context; } }
    public readonly struct SceneTransitionCompletedEvent : IEvent { public readonly SceneTransitionContext Context; public SceneTransitionCompletedEvent(SceneTransitionContext context) { Context = context; } }
}
