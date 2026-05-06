using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.RunLifecycle.Core;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SessionOperationalStartupRouteEmitter : IDisposable
    {
        private const string StartupReason = "boot_start_plan_requested";

        private static SessionOperationalStartupRouteEmitter _instance;

        private readonly EventBinding<BootStartPlanRequestedEvent> _binding;
        private bool _disposed;
        private bool _hasHandledBootStartPlan;

        private SessionOperationalStartupRouteEmitter()
        {
            _binding = new EventBinding<BootStartPlanRequestedEvent>(OnBootStartPlanRequested);
            EventBus<BootStartPlanRequestedEvent>.Register(_binding);
        }

        public static SessionOperationalStartupRouteEmitter EnsureInstalled()
        {
            bool newlyCreated = false;

            if (DependencyManager.HasInstance &&
                DependencyManager.Provider != null &&
                DependencyManager.Provider.TryGetGlobal<SessionOperationalStartupRouteEmitter>(out var existing) &&
                existing != null)
            {
                _instance = existing;
                _instance.Rebind();
            }
            else if (_instance == null)
            {
                _instance = new SessionOperationalStartupRouteEmitter();
                if (!DependencyManager.HasInstance || DependencyManager.Provider == null)
                {
                    throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline][StartupRoute] DependencyManager indisponivel para registrar o emitter do Base11Sandbox.");
                }

                DependencyManager.Provider.RegisterGlobal(_instance);
                newlyCreated = true;
            }
            else
            {
                _instance.Rebind();
            }

            DebugUtility.Log(typeof(SessionOperationalStartupRouteEmitter),
                newlyCreated
                    ? "[OBS][SessionOperationalPipeline][StartupRoute] emitter registered for Base11Sandbox."
                    : "[OBS][SessionOperationalPipeline][StartupRoute] emitter rebound for Base11Sandbox.",
                DebugUtility.Colors.Info);

            return _instance;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<BootStartPlanRequestedEvent>.Unregister(_binding);
        }

        private async void OnBootStartPlanRequested(BootStartPlanRequestedEvent evt)
        {
            _ = evt;

            if (_disposed || _hasHandledBootStartPlan)
            {
                return;
            }

            _hasHandledBootStartPlan = true;

            try
            {
                RuntimeModeConfig runtimeModeConfig = ResolveRuntimeModeConfigOrFail();
                SessionOperationalRouteAsset startupRoute = ResolveStartupRouteOrFail(runtimeModeConfig);
                SessionOperationalPipeline pipeline = ResolvePipelineOrFail();

                DebugUtility.Log(typeof(SessionOperationalStartupRouteEmitter),
                    $"[OBS][SessionOperationalPipeline][StartupRoute] event='BootStartPlanRequestedEvent' routeIdentity='{startupRoute.RouteIdentity}' source='SessionOperationalStartupRouteEmitter' reason='{StartupReason}'.",
                    DebugUtility.Colors.Info);

                await pipeline.RequestOperationalRouteAsync(
                    startupRoute,
                    source: nameof(SessionOperationalStartupRouteEmitter),
                    reason: StartupReason);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(SessionOperationalStartupRouteEmitter),
                    $"[FATAL][Config][SessionOperationalPipeline][StartupRoute] boot_start_plan handling failed exceptionType='{ex.GetType().Name}' exceptionMessage='{ex.Message}'.");
                throw;
            }
        }

        private void Rebind()
        {
            if (_disposed)
            {
                _disposed = false;
            }

            _hasHandledBootStartPlan = false;
            EventBus<BootStartPlanRequestedEvent>.Register(_binding);
        }

        private static RuntimeModeConfig ResolveRuntimeModeConfigOrFail()
        {
            if (!DependencyManager.Provider.TryGetGlobal<RuntimeModeConfig>(out var runtimeModeConfig) || runtimeModeConfig == null)
            {
                string message = "[FATAL][Config][SessionOperationalPipeline][StartupRoute] RuntimeModeConfig obrigatorio ausente para o emitter do Base11Sandbox.";
                DebugUtility.LogError(typeof(SessionOperationalStartupRouteEmitter), message);
                throw new InvalidOperationException(message);
            }

            if (runtimeModeConfig.compositionProfile != CompositionProfileKind.Base11Sandbox)
            {
                string message = $"[FATAL][Config][SessionOperationalPipeline][StartupRoute] emitter requerido fora do Base11Sandbox. compositionProfile='{runtimeModeConfig.compositionProfile}'.";
                DebugUtility.LogError(typeof(SessionOperationalStartupRouteEmitter), message);
                throw new InvalidOperationException(message);
            }

            return runtimeModeConfig;
        }

        private static SessionOperationalRouteAsset ResolveStartupRouteOrFail(RuntimeModeConfig runtimeModeConfig)
        {
            if (runtimeModeConfig == null)
            {
                throw new ArgumentNullException(nameof(runtimeModeConfig));
            }

            SessionOperationalRouteAsset startupRoute = runtimeModeConfig.StartupRouteDefinition;
            if (startupRoute == null)
            {
                string message = "[FATAL][Config][SessionOperationalPipeline][StartupRoute] startupRouteDefinition obrigatoria ausente para Base11Sandbox.";
                DebugUtility.LogError(typeof(SessionOperationalStartupRouteEmitter), message);
                throw new InvalidOperationException(message);
            }

            if (!startupRoute.IsValid)
            {
                string message = "[FATAL][Config][SessionOperationalPipeline][StartupRoute] startupRouteDefinition asset invalida para Base11Sandbox.";
                DebugUtility.LogError(typeof(SessionOperationalStartupRouteEmitter), message);
                throw new InvalidOperationException(message);
            }

            return startupRoute;
        }

        private static SessionOperationalPipeline ResolvePipelineOrFail()
        {
            if (DependencyManager.Provider.TryGetGlobal<SessionOperationalPipeline>(out var pipeline) && pipeline != null)
            {
                return pipeline;
            }

            string message = "[FATAL][Config][SessionOperationalPipeline][StartupRoute] SessionOperationalPipeline obrigatorio ausente para o Base11Sandbox.";
            DebugUtility.LogError(typeof(SessionOperationalStartupRouteEmitter), message);
            throw new InvalidOperationException(message);
        }
    }
}
