using System;
using _ImmersiveGames.NewScripts.CameraPresentation.Contracts;
using _ImmersiveGames.NewScripts.CameraPresentation.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.SaveRuntime.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionOperational.Adapters;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;
namespace _ImmersiveGames.NewScripts.SessionOperational.Runtime
{
    public static class SessionOperationalRuntimeComposer
    {
        private static bool _runtimeComposed;
        private static StartupRouteEmitter _startupRouteEmitter;
        private static SessionOperationalPipeline _sessionOperationalPipeline;
        private static SceneCompositionAdapter _routeTransitionAdapter;
        private static FadeAdapter _fadeAdapter;
        private static LoadingAdapter _loadingAdapter;
        private static AudioAdapter _audioAdapter;
        private static SessionOperationalActivitySaveAdapter _activitySaveAdapter;
        private static DefaultProgressionSlotContextResolver _progressionSlotContextResolver;
        private static SessionOperationalRouteCameraAdapter _routeCameraAdapter;
        private static SessionOperationalActivityCameraAdapter _activityCameraAdapter;

        public static void Install(RuntimeModeConfig runtimeModeConfig)
        {
            ValidateRuntimeModeConfigOrFail(runtimeModeConfig);
            EnsureStartupRequestEmitter();
            EnsureStartupRouteEmitter();

            DebugUtility.Log(typeof(SessionOperationalRuntimeComposer),
                "[OBS][SessionOperationalPipeline][Composer] SessionOperationalRuntime installer concluded.",
                DebugUtility.Colors.Info);
        }

        public static void ComposeRuntime(RuntimeModeConfig runtimeModeConfig)
        {
            CompositionPipelineExecutor.RequireBootstrapPhaseOpen(nameof(SessionOperationalRuntimeComposer));

            if (_runtimeComposed)
            {
                return;
            }

            ValidateRuntimeModeConfigOrFail(runtimeModeConfig);
            EnsureStartupRequestEmitter();
            EnsureStartupRouteEmitter();

            EnsureSessionOperationalAudioAdapter();
            EnsureSessionOperationalFadeAdapter();
            EnsureSessionOperationalLoadingAdapter();
            EnsureProgressionSlotContextResolver();
            EnsureSessionOperationalActivitySaveAdapter();
            EnsureSessionOperationalSceneCompositionAdapter();
            EnsureSessionOperationalRouteCameraAdapter();
            EnsureSessionOperationalActivityCameraAdapter();
            EnsureSessionOperationalPipeline(runtimeModeConfig);

            _runtimeComposed = true;

            DebugUtility.Log(typeof(SessionOperationalRuntimeComposer),
                "[OBS][SessionOperationalPipeline][Composer] runtime composed for SessionOperational runtime routing.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureStartupRouteEmitter()
        {
            _startupRouteEmitter = StartupRouteEmitter.EnsureInstalled();
        }

        private static void EnsureStartupRequestEmitter()
        {
            StartupRequestEmitter.EnsureInstalled();
        }

        private static void ValidateRuntimeModeConfigOrFail(RuntimeModeConfig runtimeModeConfig)
        {
            if (runtimeModeConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline] RuntimeModeConfig obrigatorio ausente para compor o SessionOperational runtime routing.");
            }

            if (runtimeModeConfig.compositionProfile != CompositionProfileKind.Base11Sandbox)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline] SessionOperationalRuntimeComposer requer compositionProfile canonico.");
            }
        }

        private static void EnsureSessionOperationalPipeline(RuntimeModeConfig runtimeModeConfig)
        {
            if (_sessionOperationalPipeline != null)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<SessionOperationalPipeline>(out var existingPipeline) && existingPipeline != null)
            {
                _sessionOperationalPipeline = existingPipeline;
                DependencyManager.Provider.RegisterGlobal<IRouteActivityLoadedSnapshotPayloadProvider>(_sessionOperationalPipeline);
                return;
            }

            SessionOperationalPipelineDependencies dependencies = CreatePipelineDependencies(runtimeModeConfig);
            _sessionOperationalPipeline = new SessionOperationalPipeline(dependencies);
            DependencyManager.Provider.RegisterGlobal(_sessionOperationalPipeline);
            DependencyManager.Provider.RegisterGlobal<IRouteActivityLoadedSnapshotPayloadProvider>(_sessionOperationalPipeline);

            DebugUtility.Log(typeof(SessionOperationalRuntimeComposer),
                "[OBS][SessionOperationalPipeline][Composer] SessionOperationalPipeline registered for canonical operational runtime.",
                DebugUtility.Colors.Info);
        }

        private static SessionOperationalPipelineDependencies CreatePipelineDependencies(RuntimeModeConfig runtimeModeConfig)
        {
            RuntimePersistentScenesPolicyAsset persistentScenesPolicy =
                RuntimePolicyConfigResolver.ResolvePersistentScenesPolicyOrFail(runtimeModeConfig);

            return new SessionOperationalPipelineDependencies(
                runtimeModeConfig,
                persistentScenesPolicy,
                _routeTransitionAdapter,
                _fadeAdapter,
                _loadingAdapter,
                _audioAdapter,
                _routeCameraAdapter,
                _activityCameraAdapter,
                _activitySaveAdapter,
                _progressionSlotContextResolver,
                ResolveOptionalDependency<ISessionActivityEntryHandoffReceiver>,
                ResolveOptionalDependency<ISessionActivityPredefinedVisualReadinessBoundary>,
                ResolveOptionalDependency<ISessionActivityRouteExitTeardownBoundary>,
                ResolveOptionalDependency<ISessionActivitySnapshotPayloadProvider>,
                ResolveOptionalDependency<ISaveStateService>);
        }

        private static T ResolveOptionalDependency<T>() where T : class
        {
            if (DependencyManager.Provider != null &&
                DependencyManager.Provider.TryGetGlobal<T>(out var dependency) &&
                dependency != null)
            {
                return dependency;
            }

            return null;
        }

        private static void EnsureSessionOperationalSceneCompositionAdapter()
        {
            if (_routeTransitionAdapter != null)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<SceneCompositionAdapter>(out var existingAdapter) && existingAdapter != null)
            {
                _routeTransitionAdapter = existingAdapter;
                DependencyManager.Provider.RegisterGlobal<ISceneCompositionAdapter>(_routeTransitionAdapter);
                return;
            }

            _routeTransitionAdapter = new SceneCompositionAdapter();
            DependencyManager.Provider.RegisterGlobal(_routeTransitionAdapter);
            DependencyManager.Provider.RegisterGlobal<ISceneCompositionAdapter>(_routeTransitionAdapter);

            DebugUtility.Log(typeof(SessionOperationalRuntimeComposer),
                "[OBS][SessionOperationalPipeline][Composer] adapter='SceneCompositionAdapter' registered for canonical operational runtime.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureSessionOperationalAudioAdapter()
        {
            if (_audioAdapter != null)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<AudioAdapter>(out var existingAdapter) && existingAdapter != null)
            {
                _audioAdapter = existingAdapter;
                DependencyManager.Provider.RegisterGlobal<IAudioAdapter>(_audioAdapter);
                return;
            }

            _audioAdapter = new AudioAdapter();
            DependencyManager.Provider.RegisterGlobal(_audioAdapter);
            DependencyManager.Provider.RegisterGlobal<IAudioAdapter>(_audioAdapter);

            DebugUtility.Log(typeof(SessionOperationalRuntimeComposer),
                "[OBS][SessionOperationalPipeline][Composer] adapter='AudioAdapter' registered for canonical operational runtime.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureSessionOperationalFadeAdapter()
        {
            if (_fadeAdapter != null)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<FadeAdapter>(out var existingAdapter) && existingAdapter != null)
            {
                _fadeAdapter = existingAdapter;
                DependencyManager.Provider.RegisterGlobal<IFadeAdapter>(_fadeAdapter);
                return;
            }

            _fadeAdapter = new FadeAdapter();
            DependencyManager.Provider.RegisterGlobal(_fadeAdapter);
            DependencyManager.Provider.RegisterGlobal<IFadeAdapter>(_fadeAdapter);

            DebugUtility.Log(typeof(SessionOperationalRuntimeComposer),
                "[OBS][SessionOperationalPipeline][Composer] adapter='FadeAdapter' registered for canonical operational runtime.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureSessionOperationalLoadingAdapter()
        {
            if (_loadingAdapter != null)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<LoadingAdapter>(out var existingAdapter) && existingAdapter != null)
            {
                _loadingAdapter = existingAdapter;
                DependencyManager.Provider.RegisterGlobal<ILoadingAdapter>(_loadingAdapter);
                return;
            }

            _loadingAdapter = new LoadingAdapter();
            DependencyManager.Provider.RegisterGlobal(_loadingAdapter);
            DependencyManager.Provider.RegisterGlobal<ILoadingAdapter>(_loadingAdapter);

            DebugUtility.Log(typeof(SessionOperationalRuntimeComposer),
                "[OBS][SessionOperationalPipeline][Composer] adapter='LoadingAdapter' registered for canonical operational runtime.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureSessionOperationalActivitySaveAdapter()
        {
            if (_activitySaveAdapter != null)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<SessionOperationalActivitySaveAdapter>(out var existingAdapter) && existingAdapter != null)
            {
                _activitySaveAdapter = existingAdapter;
                DependencyManager.Provider.RegisterGlobal<ISessionOperationalActivitySaveAdapter>(_activitySaveAdapter);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISaveService>(out var saveService) || saveService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline][RouteActivitySave] ISaveService obrigatorio ausente para compor o adapter de load-on-enter/save-on-exit.");
            }

            _activitySaveAdapter = new SessionOperationalActivitySaveAdapter(saveService);
            DependencyManager.Provider.RegisterGlobal(_activitySaveAdapter);
            DependencyManager.Provider.RegisterGlobal<ISessionOperationalActivitySaveAdapter>(_activitySaveAdapter);

            DebugUtility.Log(typeof(SessionOperationalRuntimeComposer),
                "[OBS][SessionOperationalPipeline][Composer] adapter='SessionOperationalActivitySaveAdapter' registered for RouteActivitySave load-on-enter/save-on-exit.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureProgressionSlotContextResolver()
        {
            if (_progressionSlotContextResolver != null)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<DefaultProgressionSlotContextResolver>(out var existingResolver) && existingResolver != null)
            {
                _progressionSlotContextResolver = existingResolver;
                DependencyManager.Provider.RegisterGlobal<IProgressionSlotContextResolver>(_progressionSlotContextResolver);
                return;
            }

            _progressionSlotContextResolver = new DefaultProgressionSlotContextResolver();
            DependencyManager.Provider.RegisterGlobal(_progressionSlotContextResolver);
            DependencyManager.Provider.RegisterGlobal<IProgressionSlotContextResolver>(_progressionSlotContextResolver);

            DebugUtility.Log(typeof(SessionOperationalRuntimeComposer),
                "[OBS][SessionOperationalPipeline][Composer] resolver='DefaultProgressionSlotContextResolver' registered for RouteActivitySave ProgressionSlotContext.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureSessionOperationalRouteCameraAdapter()
        {
            if (_routeCameraAdapter != null)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<SessionOperationalRouteCameraAdapter>(out var existingAdapter) && existingAdapter != null)
            {
                _routeCameraAdapter = existingAdapter;
                DependencyManager.Provider.RegisterGlobal<ISessionOperationalRouteCameraAdapter>(_routeCameraAdapter);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IRouteCameraPreparationExecutor>(out var routeCameraExecutor) || routeCameraExecutor == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline][RouteCamera] IRouteCameraPreparationExecutor obrigatorio ausente para compor route/surface camera adapter.");
            }

            var requirementResolver = new SurfaceCameraPresentationRequirementResolver();
            _routeCameraAdapter = new SessionOperationalRouteCameraAdapter(routeCameraExecutor, requirementResolver, DependencyManager.Provider);
            DependencyManager.Provider.RegisterGlobal(_routeCameraAdapter);
            DependencyManager.Provider.RegisterGlobal<ISessionOperationalRouteCameraAdapter>(_routeCameraAdapter);

            DebugUtility.Log(typeof(SessionOperationalRuntimeComposer),
                "[OBS][SessionOperationalPipeline][Composer] adapter='SessionOperationalRouteCameraAdapter' registered for Route/Surface camera presentation.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureSessionOperationalActivityCameraAdapter()
        {
            if (_activityCameraAdapter != null)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<SessionOperationalActivityCameraAdapter>(out var existingAdapter) && existingAdapter != null)
            {
                _activityCameraAdapter = existingAdapter;
                DependencyManager.Provider.RegisterGlobal<ISessionOperationalActivityCameraAdapter>(_activityCameraAdapter);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActivityCameraPreparationExecutor>(out var activityCameraExecutor) || activityCameraExecutor == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline][ActivityCamera] IActivityCameraPreparationExecutor obrigatorio ausente para compor activity camera adapter.");
            }

            if (DependencyManager.Provider == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline][ActivityCamera] IDependencyProvider obrigatorio ausente para compor activity camera adapter.");
            }

            var requirementResolver = new ActivityCameraPresentationRequirementResolver();
            _activityCameraAdapter = new SessionOperationalActivityCameraAdapter(activityCameraExecutor, requirementResolver, DependencyManager.Provider);
            DependencyManager.Provider.RegisterGlobal(_activityCameraAdapter);
            DependencyManager.Provider.RegisterGlobal<ISessionOperationalActivityCameraAdapter>(_activityCameraAdapter);

            DebugUtility.Log(typeof(SessionOperationalRuntimeComposer),
                "[OBS][SessionOperationalPipeline][Composer] adapter='SessionOperationalActivityCameraAdapter' registered for Activity camera presentation.",
                DebugUtility.Colors.Info);
        }
    }
}
