using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SceneFlow.Authoring.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.NavigationDispatch.NavigationMacro;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SessionOperationalNavigationService : IDisposable
    {
        private readonly SessionOperationalPipeline _pipeline;
        private readonly IGameNavigationCatalog _catalog;
        private readonly ISessionOperationalTransitionPort _transitionPort;
        private readonly EventBinding<NavigateToRouteCommand> _navigateBinding;
        private readonly object _sync = new();
        private int _routeSequence;
        private bool _disposed;

        public SessionOperationalNavigationService(
            SessionOperationalPipeline pipeline,
            IGameNavigationCatalog catalog,
            ISessionOperationalTransitionPort transitionPort)
        {
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _transitionPort = transitionPort ?? throw new ArgumentNullException(nameof(transitionPort));
            _navigateBinding = new EventBinding<NavigateToRouteCommand>(OnNavigateToRoute);
            EventBus<NavigateToRouteCommand>.Register(_navigateBinding);
        }

        public SessionOperationalPipeline Pipeline => _pipeline;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<NavigateToRouteCommand>.Unregister(_navigateBinding);
        }

        public void NavigateToRoute(NavigateToRouteCommand command)
        {
            if (_disposed)
            {
                return;
            }

            if (!command.IsValid)
            {
                DebugUtility.LogWarning<SessionOperationalNavigationService>(
                    "[OBS][SessionOperationalPipeline][Navigation] command ignored reason='invalid_command_payload'.");
                return;
            }

            DebugUtility.Log(typeof(SessionOperationalNavigationService),
                $"[OBS][SessionOperationalPipeline][Navigation] command='NavigateToRoute' routeId='{command.RouteId}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            if (!_catalog.TryGet(command.RouteId.Value, out GameNavigationEntry entry) || !entry.IsValid || entry.RouteRef == null)
            {
                DebugUtility.LogWarning<SessionOperationalNavigationService>(
                    $"[OBS][SessionOperationalPipeline][Navigation] command rejected reason='route_not_resolved' routeId='{command.RouteId}' source='{command.Source}' reason='{command.Reason}'.");
                return;
            }

            TransitionStyleDefinition styleDefinition = entry.StyleRef.ToDefinitionOrFail(
                nameof(SessionOperationalNavigationService),
                $"NavigateToRoute routeId='{command.RouteId}'");

            SceneRouteDefinition routeDefinition = entry.RouteRef.ToDefinition();
            SceneTransitionPayload payload = routeDefinition.RouteKind == SceneRouteKind.Gameplay
                ? SceneTransitionPayload.GameplayInitialEntry
                : SceneTransitionPayload.Empty;

            string routeProfileId = ResolveRouteProfileId(entry.RouteRef);
            string transitionId = ComputeTransitionSignature(routeDefinition, entry.RouteRef, entry.StyleRef, command.RouteId, payload, styleDefinition);
            int sequence;
            string routeOperationId;

            lock (_sync)
            {
                _routeSequence += 1;
                sequence = _routeSequence;
                routeOperationId = BuildRouteOperationId(command.RouteId, routeProfileId, routeDefinition.TargetActiveScene, sequence, transitionId);
            }

            if (!_pipeline.TryBeginRouteOperation(
                    routeOperationId,
                    transitionId,
                    sequence,
                    command.RouteId.Value,
                    routeProfileId,
                    command.Source,
                    command.Reason))
            {
                DebugUtility.LogWarning<SessionOperationalNavigationService>(
                    $"[OBS][SessionOperationalPipeline][Navigation] command rejected reason='route_operation_start_rejected' routeId='{command.RouteId}' source='{command.Source}' reason='{command.Reason}'.");
                return;
            }

            if (!_pipeline.TryObserveNavigationIntent(
                    routeOperationId,
                    transitionId,
                    sequence,
                    command.RouteId.Value,
                    routeProfileId,
                    command.Source,
                    command.Reason))
            {
                return;
            }

            DebugUtility.Log(typeof(SessionOperationalNavigationService),
                $"[OBS][SessionOperationalPipeline][Navigation] fact='NavigationIntentObserved' routeId='{command.RouteId}' routeProfileId='{routeProfileId}' routeKind='{routeDefinition.RouteKind}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            RouteResolvedFact resolvedFact = new(command.RouteId, routeProfileId, routeDefinition.RouteKind, command.Source, command.Reason);
            EventBus<RouteResolvedFact>.Raise(resolvedFact);

            if (!_pipeline.TryObserveRouteResolved(
                    routeOperationId,
                    transitionId,
                    sequence,
                    command.RouteId.Value,
                    routeProfileId,
                    command.Source,
                    command.Reason))
            {
                return;
            }

            DebugUtility.Log(typeof(SessionOperationalNavigationService),
                $"[OBS][SessionOperationalPipeline][Navigation] fact='RouteResolved' routeId='{command.RouteId}' routeProfileId='{routeProfileId}' routeKind='{routeDefinition.RouteKind}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            if (!_pipeline.TryObserveTransitionRequested(
                    routeOperationId,
                    transitionId,
                    sequence,
                    command.RouteId.Value,
                    routeProfileId,
                    command.Source,
                    command.Reason))
            {
                return;
            }

            DebugUtility.Log(typeof(SessionOperationalNavigationService),
                $"[OBS][SessionOperationalPipeline][Transition] command='RequestRouteTransition' routeId='{command.RouteId}' routeProfileId='{routeProfileId}' routeKind='{routeDefinition.RouteKind}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            _transitionPort.RequestRouteTransition(new RequestRouteTransitionCommand(
                command.RouteId,
                routeProfileId,
                routeDefinition.RouteKind,
                command.Source,
                command.Reason));
        }

        private void OnNavigateToRoute(NavigateToRouteCommand command)
        {
            NavigateToRoute(command);
        }

        private static string ResolveRouteProfileId(SceneRouteDefinitionAsset routeRef)
        {
            if (routeRef != null &&
                routeRef.RouteProfile != null &&
                routeRef.RouteProfile.ProfileId.IsValid)
            {
                return routeRef.RouteProfile.ProfileId.Value;
            }

            return string.Empty;
        }

        private static string ComputeTransitionSignature(
            SceneRouteDefinition routeDefinition,
            SceneRouteDefinitionAsset routeRef,
            TransitionStyleAsset transitionStyle,
            SceneRouteId routeId,
            SceneTransitionPayload payload,
            TransitionStyleDefinition styleDefinition)
        {
            SceneTransitionRequest request = new(
                routeDefinition,
                routeId,
                transitionStyle,
                payload,
                styleDefinition.Profile,
                useFade: styleDefinition.UseFade,
                requestedBy: nameof(SessionOperationalNavigationService),
                reason: $"SessionOperationalNavigation routeId='{routeId}'",
                resolvedRouteRef: routeRef);

            SceneTransitionContext context = SceneTransitionSignature.BuildContext(request, routeDefinition.RouteKind);
            return SceneTransitionSignature.Compute(context);
        }

        private static string BuildRouteOperationId(
            SceneRouteId routeId,
            string routeProfileId,
            string targetActiveScene,
            int transitionSequence,
            string transitionId)
        {
            return $"{routeId.Value}|{routeProfileId}|{targetActiveScene}|{transitionSequence}|{transitionId}";
        }
    }
}
