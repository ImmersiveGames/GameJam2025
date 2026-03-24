#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Guards;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Contracts;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Domain;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Guards;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Policies;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldReset.Validation;

namespace _ImmersiveGames.NewScripts.Modules.WorldReset.Application
{
    /// <summary>
    /// Serviço canônico do reset do WorldReset.
    /// Fonte de verdade para policy/guards/validation/orchestration.
    /// Ownership (Baseline 3.1):
    /// - OWNER do macro reset (world reset) no runtime.
    /// - API canônica chamada por SceneFlow driver e comandos de macro reset.
    /// - Encapsula construção/execução do WorldResetOrchestrator sem expor detalhes.
    /// </summary>
    public sealed class WorldResetService : IWorldResetService
    {
        private readonly object _lock = new();
        private readonly HashSet<string> _inFlight = new(StringComparer.Ordinal);

        private bool _dependenciesResolved;
        private WorldResetOrchestrator? _orchestrator;

        public async Task<WorldResetResult> TriggerResetAsync(string? contextSignature, string? reason)
        {
            var request = new WorldResetRequest(
                contextSignature: contextSignature ?? string.Empty,
                reason: reason ?? string.Empty,
                targetScene: string.Empty,
                origin: WorldResetOrigin.Manual,
                macroRouteId: SceneRouteId.None,
                levelSignature: LevelContextSignature.Empty,
                sourceSignature: contextSignature ?? string.Empty);

            return await TriggerResetAsync(request);
        }

        public async Task<WorldResetResult> TriggerResetAsync(WorldResetRequest request)
        {
            // WL-1.1: este método é o ponto de entrada canônico para world reset.
            // Não alterar fluxo/callsites nesta etapa.
            EnsureDependencies();

            string ctx = string.IsNullOrWhiteSpace(request.ContextSignature) ? string.Empty : request.ContextSignature;
            string rsn = string.IsNullOrWhiteSpace(request.Reason) ? string.Empty : request.Reason;

            lock (_lock)
            {
                if (!_inFlight.Add(ctx))
                {
                    DebugUtility.LogWarning<WorldResetService>(
                        $"[{ResetLogTags.Guarded}][DEGRADED_MODE] Reset já em andamento para signature='{ctx}'. Ignorando duplicate.");
                    return WorldResetResult.Failed;
                }
            }

            try
            {
                var orchestrator = _orchestrator
                    ?? throw new InvalidOperationException("WorldResetOrchestrator was not initialized.");
                return await orchestrator.ExecuteAsync(request);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<WorldResetService>(
                    $"[WorldResetService] Falha durante TriggerResetAsync signature='{ctx}' reason='{rsn}' ex={ex}");

                // Best-effort: ainda publica completed para evitar deadlock no SceneFlow gate.
                WorldResetOrchestrator.PublishResetCompletedEvent(
                    request,
                    WorldResetOutcome.FailedService,
                    $"{WorldResetReasons.FailedServiceExceptionPrefix}:{ex.GetType().Name}");
                return WorldResetResult.Failed;
            }
            finally
            {
                lock (_lock)
                {
                    _inFlight.Remove(ctx);
                }
            }
        }

        private void EnsureDependencies()
        {
            if (_dependenciesResolved)
            {
                return;
            }

            var provider = DependencyManager.Provider;

            provider.TryGetGlobal<IWorldResetPolicy>(out var policy);
            provider.TryGetGlobal<ISimulationGateService>(out var gateService);

            var guards = new List<IWorldResetGuard>(1)
            {
                new SimulationGateWorldResetGuard(gateService)
            };

            var validators = new List<IWorldResetValidator>(1)
            {
                new WorldResetSignatureValidator()
            };

            _orchestrator = new WorldResetOrchestrator(
                policy,
                guards,
                new WorldResetValidationPipeline(validators),
                new WorldResetExecutor(provider));

            _dependenciesResolved = true;
        }
    }
}
