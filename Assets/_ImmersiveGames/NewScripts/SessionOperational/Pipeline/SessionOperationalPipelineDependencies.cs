using System;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.SaveRuntime.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionOperational.Adapters;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public sealed class SessionOperationalPipelineDependencies
    {
        private readonly Func<IOperationalRouteConsumerEntryPort> _routeConsumerEntryPortResolver;
        private readonly Func<IOperationalRouteConsumerReadinessPort> _routeConsumerReadinessPortResolver;
        private readonly Func<IOperationalRouteHandoffExitPort> _routeHandoffExitPortResolver;
        private readonly Func<ISessionActivitySnapshotPayloadProvider> _activitySnapshotPayloadProviderResolver;
        private readonly Func<ISaveStateService> _saveStateServiceResolver;

        public SessionOperationalPipelineDependencies(
            RuntimeModeConfig runtimeModeConfig,
            RuntimePersistentScenesPolicyAsset persistentScenesPolicy,
            ISceneCompositionAdapter sceneCompositionAdapter,
            IFadeAdapter fadeAdapter,
            ILoadingAdapter loadingAdapter,
            IAudioAdapter audioAdapter,
            ISessionOperationalRouteCameraAdapter routeCameraAdapter,
            ISessionOperationalActivityCameraAdapter activityCameraAdapter,
            ISessionOperationalActivitySaveAdapter activitySaveAdapter,
            IProgressionSlotContextResolver progressionSlotContextResolver,
            Func<IOperationalRouteConsumerEntryPort> routeConsumerEntryPortResolver,
            Func<IOperationalRouteConsumerReadinessPort> routeConsumerReadinessPortResolver,
            Func<IOperationalRouteHandoffExitPort> routeHandoffExitPortResolver,
            Func<ISessionActivitySnapshotPayloadProvider> activitySnapshotPayloadProviderResolver,
            Func<ISaveStateService> saveStateServiceResolver)
        {
            RuntimeModeConfig = runtimeModeConfig ?? throw new ArgumentNullException(nameof(runtimeModeConfig));
            PersistentScenesPolicy = persistentScenesPolicy ?? throw new ArgumentNullException(nameof(persistentScenesPolicy));
            SceneCompositionAdapter = sceneCompositionAdapter ?? throw new ArgumentNullException(nameof(sceneCompositionAdapter));
            FadeAdapter = fadeAdapter ?? throw new ArgumentNullException(nameof(fadeAdapter));
            LoadingAdapter = loadingAdapter ?? throw new ArgumentNullException(nameof(loadingAdapter));
            AudioAdapter = audioAdapter ?? throw new ArgumentNullException(nameof(audioAdapter));
            RouteCameraAdapter = routeCameraAdapter ?? throw new ArgumentNullException(nameof(routeCameraAdapter));
            ActivityCameraAdapter = activityCameraAdapter ?? throw new ArgumentNullException(nameof(activityCameraAdapter));
            ActivitySaveAdapter = activitySaveAdapter ?? throw new ArgumentNullException(nameof(activitySaveAdapter));
            ProgressionSlotContextResolver = progressionSlotContextResolver ?? throw new ArgumentNullException(nameof(progressionSlotContextResolver));
            _routeConsumerEntryPortResolver = routeConsumerEntryPortResolver ?? throw new ArgumentNullException(nameof(routeConsumerEntryPortResolver));
            _routeConsumerReadinessPortResolver = routeConsumerReadinessPortResolver ?? throw new ArgumentNullException(nameof(routeConsumerReadinessPortResolver));
            _routeHandoffExitPortResolver = routeHandoffExitPortResolver ?? throw new ArgumentNullException(nameof(routeHandoffExitPortResolver));
            _activitySnapshotPayloadProviderResolver = activitySnapshotPayloadProviderResolver ?? throw new ArgumentNullException(nameof(activitySnapshotPayloadProviderResolver));
            _saveStateServiceResolver = saveStateServiceResolver ?? throw new ArgumentNullException(nameof(saveStateServiceResolver));
        }

        public RuntimeModeConfig RuntimeModeConfig { get; }
        public RuntimePersistentScenesPolicyAsset PersistentScenesPolicy { get; }
        public ISceneCompositionAdapter SceneCompositionAdapter { get; }
        public IFadeAdapter FadeAdapter { get; }
        public ILoadingAdapter LoadingAdapter { get; }
        public IAudioAdapter AudioAdapter { get; }
        public ISessionOperationalRouteCameraAdapter RouteCameraAdapter { get; }
        public ISessionOperationalActivityCameraAdapter ActivityCameraAdapter { get; }
        public ISessionOperationalActivitySaveAdapter ActivitySaveAdapter { get; }
        public IProgressionSlotContextResolver ProgressionSlotContextResolver { get; }

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
