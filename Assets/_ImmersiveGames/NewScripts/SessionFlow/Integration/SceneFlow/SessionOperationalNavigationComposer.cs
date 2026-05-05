using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.Config;
using _ImmersiveGames.NewScripts.SceneFlow.NavigationDispatch.NavigationMacro;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline;

namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.SceneFlow
{
    public static class SessionOperationalNavigationComposer
    {
        private static bool _installed;
        private static bool _runtimeComposed;
        private static SessionOperationalPipeline _pipeline;
        private static SessionOperationalNavigationService _navigationService;
        private static Base11SandboxStartupNavigationProducer _startupProducer;
        private static ISessionOperationalTransitionPort _transitionPort;
        private static SceneFlowSessionOperationalTransitionAdapter _transitionAdapter;

        public static void Install(BootstrapConfigAsset bootstrapConfig)
        {
            if (_installed)
            {
                return;
            }

            if (bootstrapConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline] BootstrapConfigAsset obrigatorio ausente para compor SessionOperationalNavigation.");
            }

            EnsurePipeline();
            EnsureStartupProducer();

            _installed = true;
            DebugUtility.Log(typeof(SessionOperationalNavigationComposer),
                "[OBS][SessionOperationalPipeline][Navigation] installer concluded.",
                DebugUtility.Colors.Info);
        }

        public static void ComposeRuntime(BootstrapConfigAsset bootstrapConfig)
        {
            CompositionPipelineExecutor.RequireBootstrapPhaseOpen(nameof(SessionOperationalNavigationComposer));

            if (_runtimeComposed)
            {
                return;
            }

            if (bootstrapConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline] BootstrapConfigAsset obrigatorio ausente para compor SessionOperationalNavigation runtime.");
            }

            EnsureTransitionPort();
            EnsureNavigationService();

            _runtimeComposed = true;
            DebugUtility.Log(typeof(SessionOperationalNavigationComposer),
                "[OBS][SessionOperationalPipeline][Transition] runtime composer concluded.",
                DebugUtility.Colors.Info);
        }

        private static void EnsurePipeline()
        {
            if (DependencyManager.Provider.TryGetGlobal<SessionOperationalPipeline>(out var existingPipeline) && existingPipeline != null)
            {
                _pipeline = existingPipeline;
                return;
            }

            _pipeline = new SessionOperationalPipeline();
            DependencyManager.Provider.RegisterGlobal(_pipeline);
        }

        private static void EnsureNavigationService()
        {
            if (_navigationService != null)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<SessionOperationalNavigationService>(out var existingService) && existingService != null)
            {
                _navigationService = existingService;
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameNavigationCatalog>(out var catalog) || catalog == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline] IGameNavigationCatalog obrigatorio ausente para compor SessionOperationalNavigation.");
            }

            if (_transitionPort == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline] ISessionOperationalTransitionPort obrigatorio ausente para compor SessionOperationalNavigation.");
            }

            _navigationService = new SessionOperationalNavigationService(_pipeline, catalog, _transitionPort);
            DependencyManager.Provider.RegisterGlobal(_navigationService);
        }

        private static void EnsureStartupProducer()
        {
            if (_startupProducer != null)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<Base11SandboxStartupNavigationProducer>(out var existingProducer) && existingProducer != null)
            {
                _startupProducer = existingProducer;
                return;
            }

            _startupProducer = new Base11SandboxStartupNavigationProducer();
            DependencyManager.Provider.RegisterGlobal(_startupProducer);
        }

        private static void EnsureTransitionPort()
        {
            if (_transitionPort != null)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<ISessionOperationalTransitionPort>(out var existingPort) && existingPort != null)
            {
                _transitionPort = existingPort;
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameNavigationService>(out var navigationService) || navigationService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline] IGameNavigationService obrigatorio ausente para compor o adapter tecnico de RequestRouteTransition.");
            }

            _transitionAdapter = new SceneFlowSessionOperationalTransitionAdapter(navigationService);
            _transitionPort = _transitionAdapter;
            DependencyManager.Provider.RegisterGlobal<ISessionOperationalTransitionPort>(_transitionPort);
        }
    }
}
