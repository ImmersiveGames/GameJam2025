using System;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.RuntimeCore;

namespace _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime
{
    /// <summary>
    /// Owner macro da escolha de ActorSetRef no contexto de route.
    /// </summary>
    public sealed class SceneFlowRouteActorSetRefService : ISceneFlowRouteActorSetRefContext, IDisposable
    {
        private const string GameplayActorSetValue = "route.macro.gameplay";

        private readonly EventBinding<SceneTransitionStartedEvent> _startedBinding;
        private ActorSetRef _currentActorSetRef;
        private SceneRouteKind _currentRouteKind;
        private string _source;
        private bool _hasCurrent;
        private bool _disposed;

        public SceneFlowRouteActorSetRefService()
        {
            _startedBinding = new EventBinding<SceneTransitionStartedEvent>(OnTransitionStarted);
            EventBus<SceneTransitionStartedEvent>.Register(_startedBinding);
        }

        public bool TryGetCurrent(out ActorSetRef actorSetRef, out SceneRouteKind routeKind, out string source)
        {
            actorSetRef = _currentActorSetRef;
            routeKind = _currentRouteKind;
            source = _source ?? string.Empty;
            return _hasCurrent;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<SceneTransitionStartedEvent>.Unregister(_startedBinding);
        }

        private void OnTransitionStarted(SceneTransitionStartedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            SceneRouteKind routeKind = evt.context.RouteKind;
            if (evt.context.RouteRef == null)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsExecution] Missing routeRef while resolving actor set behavior. routeId='{evt.context.RouteId}'.");
            }

            if (evt.context.RouteRef.RouteProfile == null)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsExecution] Missing route profile while resolving actor set behavior. routeId='{evt.context.RouteId}'.");
            }

            evt.context.RouteRef.RouteProfile.ValidateProfileOrFailFast();
            SceneRouteProfile profile = evt.context.RouteRef.RouteProfile.ToProfile();
            SceneRouteProfileActorSetBehavior actorSetBehavior = profile.ActorSetBehavior;

            _currentRouteKind = routeKind;
            _source = "SceneFlow/RouteMacro";

            if (actorSetBehavior == SceneRouteProfileActorSetBehavior.GameplayRouteActorSet)
            {
                ActorSetRef actorSetRef = new(GameplayActorSetValue);
                if (!actorSetRef.IsValid)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][Config][ActorsExecution] Missing ActorSetRef mapping for routeProfileId='{profile.ProfileId}' actorSetBehavior='{actorSetBehavior}'.");
                }

                _currentActorSetRef = actorSetRef;
                _hasCurrent = true;

                DebugUtility.Log(typeof(SceneFlowRouteActorSetRefService),
                    $"[OBS][ActorsExecution] ActorSetRefChosen source='{_source}' routeKind='{routeKind}' routeProfileId='{profile.ProfileId}' actorSetBehavior='{actorSetBehavior}' decisionSource='routeProfile.actorSetBehavior' routeId='{evt.context.RouteId}' actorSetRef='{actorSetRef.Value}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            _currentActorSetRef = ActorSetRef.None;
            _hasCurrent = false;

            DebugUtility.Log(typeof(SceneFlowRouteActorSetRefService),
                $"[OBS][ActorsExecution] ActorSetRefCleared source='{_source}' routeKind='{routeKind}' routeProfileId='{profile.ProfileId}' actorSetBehavior='{actorSetBehavior}' decisionSource='routeProfile.actorSetBehavior' routeId='{evt.context.RouteId}'.",
                DebugUtility.Colors.Info);
        }
    }
}
