using System;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.SaveRuntime.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionOperational.Adapters;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public sealed class SessionOperationalPipelineDependencies
    {
        private readonly Func<ISessionActivityEntryHandoffReceiver> _activityReceiverResolver;
        private readonly Func<ISessionActivityPredefinedVisualReadinessBoundary> _activityVisualReadinessBoundaryResolver;
        private readonly Func<ISessionActivityRouteExitTeardownBoundary> _activityRouteExitTeardownBoundaryResolver;
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
            Func<ISessionActivityEntryHandoffReceiver> activityReceiverResolver,
            Func<ISessionActivityPredefinedVisualReadinessBoundary> activityVisualReadinessBoundaryResolver,
            Func<ISessionActivityRouteExitTeardownBoundary> activityRouteExitTeardownBoundaryResolver,
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
            _activityReceiverResolver = activityReceiverResolver ?? throw new ArgumentNullException(nameof(activityReceiverResolver));
            _activityVisualReadinessBoundaryResolver = activityVisualReadinessBoundaryResolver ?? throw new ArgumentNullException(nameof(activityVisualReadinessBoundaryResolver));
            _activityRouteExitTeardownBoundaryResolver = activityRouteExitTeardownBoundaryResolver ?? throw new ArgumentNullException(nameof(activityRouteExitTeardownBoundaryResolver));
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

        public ISessionActivityEntryHandoffReceiver ResolveActivityReceiver()
        {
            return _activityReceiverResolver();
        }

        public ISessionActivityPredefinedVisualReadinessBoundary ResolveActivityVisualReadinessBoundary()
        {
            return _activityVisualReadinessBoundaryResolver();
        }

        public ISessionActivityRouteExitTeardownBoundary ResolveActivityRouteExitTeardownBoundary()
        {
            return _activityRouteExitTeardownBoundaryResolver();
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
