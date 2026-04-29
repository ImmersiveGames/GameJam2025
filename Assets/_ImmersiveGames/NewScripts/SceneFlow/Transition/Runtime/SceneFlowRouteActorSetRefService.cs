using System;
using System.Collections.Generic;
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
        private static readonly IReadOnlyDictionary<SceneRouteKind, ActorSetRef> RouteKindToActorSetRef =
            new Dictionary<SceneRouteKind, ActorSetRef>
            {
                { SceneRouteKind.Gameplay, new ActorSetRef("route.macro.gameplay") }
            };

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
            if (routeKind == SceneRouteKind.Gameplay)
            {
                if (!RouteKindToActorSetRef.TryGetValue(routeKind, out ActorSetRef actorSetRef) || !actorSetRef.IsValid)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][Config][ActorsExecution] Missing ActorSetRef mapping for routeKind='{routeKind}'.");
                }

                _currentActorSetRef = actorSetRef;
                _currentRouteKind = routeKind;
                _source = "SceneFlow/RouteMacro";
                _hasCurrent = true;

                DebugUtility.Log(typeof(SceneFlowRouteActorSetRefService),
                    $"[OBS][ActorsExecution] ActorSetRefChosen source='{_source}' routeKind='{routeKind}' routeId='{evt.context.RouteId}' actorSetRef='{actorSetRef.Value}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            _currentActorSetRef = ActorSetRef.None;
            _currentRouteKind = routeKind;
            _source = "SceneFlow/RouteMacro";
            _hasCurrent = false;

            DebugUtility.Log(typeof(SceneFlowRouteActorSetRefService),
                $"[OBS][ActorsExecution] ActorSetRefCleared source='{_source}' routeKind='{routeKind}' routeId='{evt.context.RouteId}'.",
                DebugUtility.Colors.Info);
        }
    }
}
