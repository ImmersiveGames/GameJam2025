using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;
namespace _ImmersiveGames.NewScripts.SessionOperational.Integration
{
    public static class SessionOperationalRuntimeComposer
    {
        private static bool _runtimeComposed;
        private static SessionOperationalStartupRouteEmitter _startupRouteEmitter;
        private static SessionOperationalPipeline _sessionOperationalPipeline;
        private static SessionOperationalSceneCompositionAdapter _routeTransitionAdapter;
        private static SessionOperationalFadeAdapter _fadeAdapter;
        private static SessionOperationalLoadingAdapter _loadingAdapter;
        private static SessionOperationalAudioAdapter _audioAdapter;

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
            if (_startupRouteEmitter != null)
            {
                _startupRouteEmitter = SessionOperationalStartupRouteEmitter.EnsureInstalled();
                return;
            }

            _startupRouteEmitter = SessionOperationalStartupRouteEmitter.EnsureInstalled();
        }

        private static void EnsureStartupRequestEmitter()
        {
            SessionOperationalStartupRequestEmitter.EnsureInstalled();
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

            if (DependencyManager.Provider.TryGetGlobal<SessionOperationalSceneCompositionAdapter>(out var existingAdapter) && existingAdapter != null)
            {
                _routeTransitionAdapter = existingAdapter;
                DependencyManager.Provider.RegisterGlobal<ISessionOperationalRouteTransitionExecutor>(_routeTransitionAdapter);
                return;
            }

            _routeTransitionAdapter = new SessionOperationalSceneCompositionAdapter();
            DependencyManager.Provider.RegisterGlobal(_routeTransitionAdapter);
            DependencyManager.Provider.RegisterGlobal<ISessionOperationalRouteTransitionExecutor>(_routeTransitionAdapter);

            DebugUtility.Log(typeof(SessionOperationalRuntimeComposer),
                "[OBS][SessionOperationalPipeline][Composer] adapter='SessionOperationalSceneCompositionAdapter' registered for canonical operational runtime.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureSessionOperationalAudioAdapter()
        {
            if (_audioAdapter != null)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<SessionOperationalAudioAdapter>(out var existingAdapter) && existingAdapter != null)
            {
                _audioAdapter = existingAdapter;
                DependencyManager.Provider.RegisterGlobal<ISessionOperationalAudioAdapter>(_audioAdapter);
                return;
            }

            _audioAdapter = new SessionOperationalAudioAdapter();
            DependencyManager.Provider.RegisterGlobal(_audioAdapter);
            DependencyManager.Provider.RegisterGlobal<ISessionOperationalAudioAdapter>(_audioAdapter);

            DebugUtility.Log(typeof(SessionOperationalRuntimeComposer),
                "[OBS][SessionOperationalPipeline][Composer] adapter='SessionOperationalAudioAdapter' registered for canonical operational runtime.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureSessionOperationalFadeAdapter()
        {
            if (_fadeAdapter != null)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<SessionOperationalFadeAdapter>(out var existingAdapter) && existingAdapter != null)
            {
                _fadeAdapter = existingAdapter;
                DependencyManager.Provider.RegisterGlobal<ISessionOperationalFadeAdapter>(_fadeAdapter);
                return;
            }

            _fadeAdapter = new SessionOperationalFadeAdapter();
            DependencyManager.Provider.RegisterGlobal(_fadeAdapter);
            DependencyManager.Provider.RegisterGlobal<ISessionOperationalFadeAdapter>(_fadeAdapter);

            DebugUtility.Log(typeof(SessionOperationalRuntimeComposer),
                "[OBS][SessionOperationalPipeline][Composer] adapter='SessionOperationalFadeAdapter' registered for canonical operational runtime.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureSessionOperationalLoadingAdapter()
        {
            if (_loadingAdapter != null)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<SessionOperationalLoadingAdapter>(out var existingAdapter) && existingAdapter != null)
            {
                _loadingAdapter = existingAdapter;
                DependencyManager.Provider.RegisterGlobal<ISessionOperationalLoadingAdapter>(_loadingAdapter);
                return;
            }

            _loadingAdapter = new SessionOperationalLoadingAdapter();
            DependencyManager.Provider.RegisterGlobal(_loadingAdapter);
            DependencyManager.Provider.RegisterGlobal<ISessionOperationalLoadingAdapter>(_loadingAdapter);

            DebugUtility.Log(typeof(SessionOperationalRuntimeComposer),
                "[OBS][SessionOperationalPipeline][Composer] adapter='SessionOperationalLoadingAdapter' registered for canonical operational runtime.",
                DebugUtility.Colors.Info);
        }
    }
}
