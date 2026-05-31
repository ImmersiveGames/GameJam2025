using System;
using _ImmersiveGames.NewScripts.Actors.Semantic.Participation;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.PlayerParticipation.Runtime;
using _ImmersiveGames.NewScripts.SaveRuntime.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionOperational.Adapters;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public sealed class SessionOperationalPipelineDependencies
    {
        private readonly Func<IOperationalSceneCompositionPort> _sceneCompositionPortResolver;
        private readonly Func<IOperationalRouteAudioPort> _routeAudioPortResolver;
        private readonly Func<IOperationalFadePort> _fadePortResolver;
        private readonly Func<IOperationalInputModeRequestPort> _inputModeRequestPortResolver;
        private readonly Func<IOperationalRouteConsumerEntryPort> _routeConsumerEntryPortResolver;
        private readonly Func<IOperationalRouteConsumerReadinessPort> _routeConsumerReadinessPortResolver;
        private readonly Func<IOperationalRouteHandoffExitPort> _routeHandoffExitPortResolver;
        private readonly Func<ISessionActivityRouteExitTeardownBoundary> _sessionActivityRouteExitBoundaryResolver;
        private readonly Func<IRoutePlayerParticipationEndpoint> _routePlayerPreparationEndpointResolver;
        private readonly Func<IPlayerParticipationRuntime> _playerParticipationRuntimeResolver;
        private readonly Func<ISessionActivitySnapshotPayloadProvider> _activitySnapshotPayloadProviderResolver;
        private readonly Func<ISaveStateService> _saveStateServiceResolver;

        public SessionOperationalPipelineDependencies(
            RuntimeModeConfig runtimeModeConfig,
            RuntimePersistentScenesPolicyAsset persistentScenesPolicy,
            Func<IOperationalSceneCompositionPort> sceneCompositionPortResolver,
            Func<IOperationalRouteAudioPort> routeAudioPortResolver,
            Func<IOperationalFadePort> fadePortResolver,
            ILoadingAdapter loadingAdapter,
            ISessionOperationalRouteCameraAdapter routeCameraAdapter,
            ISessionOperationalActivitySaveAdapter activitySaveAdapter,
            IProgressionSlotContextResolver progressionSlotContextResolver,
            Func<IOperationalInputModeRequestPort> inputModeRequestPortResolver,
            Func<IOperationalRouteConsumerEntryPort> routeConsumerEntryPortResolver,
            Func<IOperationalRouteConsumerReadinessPort> routeConsumerReadinessPortResolver,
            ISessionOperationalActivityCameraAdapter activityCameraAdapter,
            Func<IOperationalRouteHandoffExitPort> routeHandoffExitPortResolver,
            Func<ISessionActivityRouteExitTeardownBoundary> sessionActivityRouteExitBoundaryResolver,
            Func<IRoutePlayerParticipationEndpoint> routePlayerPreparationEndpointResolver,
            Func<IPlayerParticipationRuntime> playerParticipationRuntimeResolver,
            Func<ISessionActivitySnapshotPayloadProvider> activitySnapshotPayloadProviderResolver,
            Func<ISaveStateService> saveStateServiceResolver)
        {
            RuntimeModeConfig = runtimeModeConfig ?? throw new ArgumentNullException(nameof(runtimeModeConfig));
            PersistentScenesPolicy = persistentScenesPolicy ?? throw new ArgumentNullException(nameof(persistentScenesPolicy));
            _sceneCompositionPortResolver = sceneCompositionPortResolver ?? throw new ArgumentNullException(nameof(sceneCompositionPortResolver));
            _routeAudioPortResolver = routeAudioPortResolver ?? throw new ArgumentNullException(nameof(routeAudioPortResolver));
            _fadePortResolver = fadePortResolver ?? throw new ArgumentNullException(nameof(fadePortResolver));
            LoadingAdapter = loadingAdapter ?? throw new ArgumentNullException(nameof(loadingAdapter));
            RouteCameraAdapter = routeCameraAdapter ?? throw new ArgumentNullException(nameof(routeCameraAdapter));
            ActivitySaveAdapter = activitySaveAdapter ?? throw new ArgumentNullException(nameof(activitySaveAdapter));
            ProgressionSlotContextResolver = progressionSlotContextResolver ?? throw new ArgumentNullException(nameof(progressionSlotContextResolver));
            _inputModeRequestPortResolver = inputModeRequestPortResolver ?? throw new ArgumentNullException(nameof(inputModeRequestPortResolver));
            _routeConsumerEntryPortResolver = routeConsumerEntryPortResolver ?? throw new ArgumentNullException(nameof(routeConsumerEntryPortResolver));
            _routeConsumerReadinessPortResolver = routeConsumerReadinessPortResolver ?? throw new ArgumentNullException(nameof(routeConsumerReadinessPortResolver));
            ActivityCameraAdapter = activityCameraAdapter ?? throw new ArgumentNullException(nameof(activityCameraAdapter));
            _routeHandoffExitPortResolver = routeHandoffExitPortResolver ?? throw new ArgumentNullException(nameof(routeHandoffExitPortResolver));
            _sessionActivityRouteExitBoundaryResolver = sessionActivityRouteExitBoundaryResolver ?? throw new ArgumentNullException(nameof(sessionActivityRouteExitBoundaryResolver));
            _routePlayerPreparationEndpointResolver = routePlayerPreparationEndpointResolver ?? throw new ArgumentNullException(nameof(routePlayerPreparationEndpointResolver));
            _playerParticipationRuntimeResolver = playerParticipationRuntimeResolver ?? throw new ArgumentNullException(nameof(playerParticipationRuntimeResolver));
            _activitySnapshotPayloadProviderResolver = activitySnapshotPayloadProviderResolver ?? throw new ArgumentNullException(nameof(activitySnapshotPayloadProviderResolver));
            _saveStateServiceResolver = saveStateServiceResolver ?? throw new ArgumentNullException(nameof(saveStateServiceResolver));
        }

        public RuntimeModeConfig RuntimeModeConfig { get; }
        public RuntimePersistentScenesPolicyAsset PersistentScenesPolicy { get; }
        public ILoadingAdapter LoadingAdapter { get; }
        public ISessionOperationalRouteCameraAdapter RouteCameraAdapter { get; }
        public ISessionOperationalActivityCameraAdapter ActivityCameraAdapter { get; }
        public ISessionOperationalActivitySaveAdapter ActivitySaveAdapter { get; }
        public IProgressionSlotContextResolver ProgressionSlotContextResolver { get; }

        public IOperationalSceneCompositionPort ResolveSceneCompositionPort()
        {
            return _sceneCompositionPortResolver();
        }

        public IOperationalRouteAudioPort ResolveRouteAudioPort()
        {
            return _routeAudioPortResolver();
        }

        public IOperationalFadePort ResolveFadePort()
        {
            return _fadePortResolver();
        }

        public IOperationalInputModeRequestPort ResolveInputModeRequestPort()
        {
            return _inputModeRequestPortResolver();
        }

        public IOperationalRouteConsumerEntryPort ResolveRouteConsumerEntryPort()
        {
            return _routeConsumerEntryPortResolver();
        }

        public IOperationalRouteConsumerReadinessPort ResolveRouteConsumerReadinessPort()
        {
            return _routeConsumerReadinessPortResolver();
        }

        public IOperationalRouteHandoffExitPort ResolveRouteHandoffExitPort()
        {
            return _routeHandoffExitPortResolver();
        }

        public ISessionActivityRouteExitTeardownBoundary ResolveSessionActivityRouteExitTeardownBoundary()
        {
            return _sessionActivityRouteExitBoundaryResolver();
        }

        public IRoutePlayerParticipationEndpoint ResolveRoutePlayerPreparationEndpoint()
        {
            return _routePlayerPreparationEndpointResolver();
        }

        public IPlayerParticipationRuntime ResolvePlayerParticipationRuntime()
        {
            return _playerParticipationRuntimeResolver();
        }

        public bool TryResolveActivitySnapshotPayloadProvider(out ISessionActivitySnapshotPayloadProvider provider)
        {
            provider = _activitySnapshotPayloadProviderResolver();
            return provider != null;
        }

        public bool TryResolveSaveStateService(out ISaveStateService saveStateService)
        {
            saveStateService = _saveStateServiceResolver();
            return saveStateService != null;
        }
    }
}
