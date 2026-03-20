#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gates;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Domain;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Guards;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Policies;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Validation;
namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Application
{
    /// <summary>
    /// Servico canonico do reset do WorldLifecycle.
    /// Fonte de verdade para policy/guards/validation/orchestration.
    /// Ownership (Baseline 3.1):
    /// - OWNER do macro reset (world reset) no runtime.
    /// - API canonica chamada por SceneFlow driver e comandos de macro reset.
    /// - Encapsula construcao/execucao do WorldResetOrchestrator sem expor detalhes.
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
                sourceSignature: contextSignature ?? string.Empty);

            return await TriggerResetAsync(request);
        }

        public async Task<WorldResetResult> TriggerResetAsync(WorldResetRequest request)
        {
            // WL-1.1: este metodo e o ponto de entrada canonico para world reset.
            // Nao alterar fluxo/callsites nesta etapa.
            EnsureDependencies();

            string ctx = string.IsNullOrWhiteSpace(request.ContextSignature) ? string.Empty : request.ContextSignature;
            string rsn = string.IsNullOrWhiteSpace(request.Reason) ? string.Empty : request.Reason;

            lock (_lock)
            {
                if (!_inFlight.Add(ctx))
                {
                    DebugUtility.LogWarning<WorldResetService>(
                        $"[{ResetLogTags.Guarded}][DEGRADED_MODE] Reset ja em andamento para signature='{ctx}'. Ignorando duplicate.");
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
                WorldResetOrchestrator.PublishResetCompletedV1(ctx, rsn);
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




