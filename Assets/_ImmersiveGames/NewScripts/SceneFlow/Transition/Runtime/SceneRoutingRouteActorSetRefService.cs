using System;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SceneRouting.Contracts.RuntimeCore;

namespace _ImmersiveGames.NewScripts.SceneRouting.Transition.Runtime
{
    /// <summary>
    /// Owner macro da escolha de ActorSetRef no contexto de route.
    /// </summary>
    public sealed class SceneRoutingRouteActorSetRefService : ISceneRoutingRouteActorSetRefContext, IDisposable
    {
        private const string GameplayActorSetValue = "route.macro.gameplay";

        private readonly EventBinding<SceneTransitionStartedEvent> _startedBinding;
        private ActorSetRef _currentActorSetRef;
        private string _currentRouteIdentity;
        private string _source;
        private bool _hasCurrent;
        private bool _disposed;

        public SceneRoutingRouteActorSetRefService()
        {
            _startedBinding = new EventBinding<SceneTransitionStartedEvent>(OnTransitionStarted);
            EventBus<SceneTransitionStartedEvent>.Register(_startedBinding);
        }

        public bool TryGetCurrent(out ActorSetRef actorSetRef, out string routeIdentity, out string source)
        {
            actorSetRef = _currentActorSetRef;
            routeIdentity = _currentRouteIdentity ?? string.Empty;
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

            string routeIdentity = evt.context.RouteIdentity;
            _currentRouteIdentity = routeIdentity;
            _source = "SceneRouting/ActorSetRef";

            bool isGameplayEntry = evt.context.IsGameplayInitialEntry || evt.context.IsGameplayReentry;
            if (isGameplayEntry)
            {
                ActorSetRef actorSetRef = new(GameplayActorSetValue);
                if (!actorSetRef.IsValid)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][Config][ActorsExecution] Missing ActorSetRef mapping for gameplay route. routeId='{evt.context.RouteId}'.");
                }

                _currentActorSetRef = actorSetRef;
                _hasCurrent = true;

                DebugUtility.Log(typeof(SceneRoutingRouteActorSetRefService),
                    $"[OBS][ActorsExecution] ActorSetRefChosen source='{_source}' routeIdentity='{routeIdentity}' decisionSource='gameplayEntry' routeId='{evt.context.RouteId}' actorSetRef='{actorSetRef.Value}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            _currentActorSetRef = ActorSetRef.None;
            _hasCurrent = false;

            DebugUtility.Log(typeof(SceneRoutingRouteActorSetRefService),
                $"[OBS][ActorsExecution] ActorSetRefCleared source='{_source}' routeIdentity='{routeIdentity}' decisionSource='not_gameplay_entry' routeId='{evt.context.RouteId}'.",
                DebugUtility.Colors.Info);
        }
    }
}
