using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.SessionOperational.Adapters;
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

            EnsureSessionOperationalPipeline();
            EnsureSessionOperationalAudioAdapter();
            EnsureSessionOperationalFadeAdapter();
            EnsureSessionOperationalLoadingAdapter();
            EnsureSessionOperationalSceneCompositionAdapter();

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

        private static void EnsureSessionOperationalPipeline()
        {
            if (_sessionOperationalPipeline != null)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<SessionOperationalPipeline>(out var existingPipeline) && existingPipeline != null)
            {
                _sessionOperationalPipeline = existingPipeline;
                return;
            }

            _sessionOperationalPipeline = new SessionOperationalPipeline();
            DependencyManager.Provider.RegisterGlobal(_sessionOperationalPipeline);

            DebugUtility.Log(typeof(SessionOperationalRuntimeComposer),
                "[OBS][SessionOperationalPipeline][Composer] SessionOperationalPipeline registered for canonical operational runtime.",
                DebugUtility.Colors.Info);
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
    }
}
