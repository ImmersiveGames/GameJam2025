using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.RunPipeline.Contracts;
namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class StartupRouteEmitter : IDisposable
    {
        private const string StartupReason = "boot_start_plan_requested";

        private static StartupRouteEmitter _instance;

        private readonly EventBinding<BootStartPlanRequestedEvent> _binding;
        private bool _disposed;
        private bool _hasHandledBootStartPlan;

        private StartupRouteEmitter()
        {
            _binding = new EventBinding<BootStartPlanRequestedEvent>(OnBootStartPlanRequested);
            EventBus<BootStartPlanRequestedEvent>.Register(_binding);
        }

        public static StartupRouteEmitter EnsureInstalled()
        {
            bool newlyCreated = false;

            if (DependencyManager.HasInstance &&
                DependencyManager.Provider != null &&
                DependencyManager.Provider.TryGetGlobal<StartupRouteEmitter>(out var existing) &&
                existing != null)
            {
                _instance = existing;
                _instance.Rebind();
            }
            else if (_instance == null)
            {
                _instance = new StartupRouteEmitter();
                if (!DependencyManager.HasInstance || DependencyManager.Provider == null)
                {
                    throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline][StartupRoute] DependencyManager indisponivel para registrar o emitter do rail operacional.");
                }

                DependencyManager.Provider.RegisterGlobal(_instance);
                newlyCreated = true;
            }
            else
            {
                _instance.Rebind();
            }

            DebugUtility.Log(typeof(StartupRouteEmitter),
                newlyCreated
                    ? "[OBS][SessionOperationalPipeline][StartupRoute] emitter registered for canonical startup rail."
                    : "[OBS][SessionOperationalPipeline][StartupRoute] emitter rebound for canonical startup rail.",
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
                await RuntimePersistentScenesComposition.AwaitGuaranteedAsync(runtimeModeConfig);
                OperationalRouteAsset startupRoute = ResolveStartupRouteOrFail(runtimeModeConfig);
                SessionOperationalPipeline pipeline = ResolvePipelineOrFail();

                DebugUtility.Log(typeof(StartupRouteEmitter),
                    $"[OBS][SessionOperationalPipeline][StartupRoute] event='BootStartPlanRequestedEvent' routeIdentity='{startupRoute.RouteIdentity}' source='StartupRouteEmitter' reason='{StartupReason}'.",
                    DebugUtility.Colors.Info);

                await pipeline.RequestOperationalRouteAsync(
                    startupRoute,
                    source: nameof(StartupRouteEmitter),
                    reason: StartupReason);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(StartupRouteEmitter),
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
                string message = "[FATAL][Config][SessionOperationalPipeline][StartupRoute] RuntimeModeConfig obrigatorio ausente para o emitter do rail operacional.";
                DebugUtility.LogError(typeof(StartupRouteEmitter), message);
                throw new InvalidOperationException(message);
            }

            if (runtimeModeConfig.compositionProfile != CompositionProfileKind.Base11Sandbox)
            {
                string message = $"[FATAL][Config][SessionOperationalPipeline][StartupRoute] emitter requerido fora do profile canonical. compositionProfile='{runtimeModeConfig.compositionProfile}'.";
                DebugUtility.LogError(typeof(StartupRouteEmitter), message);
                throw new InvalidOperationException(message);
            }

            return runtimeModeConfig;
        }

        private static OperationalRouteAsset ResolveStartupRouteOrFail(RuntimeModeConfig runtimeModeConfig)
        {
            if (runtimeModeConfig == null)
            {
                throw new ArgumentNullException(nameof(runtimeModeConfig));
            }

            try
            {
                return SessionOperationalRuntimeConfigResolver.ResolveStartupRouteOrFail(runtimeModeConfig);
            }
            catch (Exception ex)
            {
                string message = $"[FATAL][Config][SessionOperationalPipeline][StartupRoute] startupRouteDefinition resolve failed. detail='{ex.Message}'.";
                DebugUtility.LogError(typeof(StartupRouteEmitter), message);
                throw new InvalidOperationException(message, ex);
            }
        }

        private static SessionOperationalPipeline ResolvePipelineOrFail()
        {
            if (DependencyManager.Provider.TryGetGlobal<SessionOperationalPipeline>(out var pipeline) && pipeline != null)
            {
                return pipeline;
            }

            string message = "[FATAL][Config][SessionOperationalPipeline][StartupRoute] SessionOperationalPipeline obrigatorio ausente para o rail operacional.";
            DebugUtility.LogError(typeof(StartupRouteEmitter), message);
            throw new InvalidOperationException(message);
        }
    }
}

