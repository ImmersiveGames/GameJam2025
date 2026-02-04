#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Lifecycle.World.Reset.Domain;
using _ImmersiveGames.NewScripts.Lifecycle.World.Reset.Guards;
using _ImmersiveGames.NewScripts.Lifecycle.World.Reset.Policies;
using _ImmersiveGames.NewScripts.Lifecycle.World.Reset.Validation;
using _ImmersiveGames.NewScripts.Runtime.Gates;
using _ImmersiveGames.NewScripts.Runtime.World;

namespace _ImmersiveGames.NewScripts.Lifecycle.World.Reset.Application
{
    /// <summary>
    /// Servico canonico do reset do WorldLifecycle.
    /// Fonte de verdade para policy/guards/validation/orchestration.
    /// </summary>
    public sealed class WorldResetService : IResetWorldService
    {
        private readonly object _lock = new();
        private readonly HashSet<string> _inFlight = new(StringComparer.Ordinal);

        private bool _dependenciesResolved;
        private WorldResetOrchestrator _orchestrator;

        public async Task TriggerResetAsync(string? contextSignature, string? reason)
        {
            var request = new WorldResetRequest(
                contextSignature: contextSignature ?? string.Empty,
                reason: reason ?? string.Empty,
                profileName: WorldResetReasons.ManualProfile,
                targetScene: string.Empty,
                origin: WorldResetOrigin.Manual,
                sourceSignature: contextSignature ?? string.Empty,
                isGameplayProfile: true);

            await TriggerResetAsync(request);
        }

        public async Task TriggerResetAsync(WorldResetRequest request)
        {
            EnsureDependencies();

            string ctx = string.IsNullOrWhiteSpace(request.ContextSignature) ? string.Empty : request.ContextSignature;
            string rsn = string.IsNullOrWhiteSpace(request.Reason) ? string.Empty : request.Reason;

            lock (_lock)
            {
                if (!_inFlight.Add(ctx))
                {
                    DebugUtility.LogWarning<WorldResetService>(
                        $"[{ResetLogTags.Guarded}][DEGRADED_MODE] Reset ja em andamento para signature='{ctx}'. Ignorando duplicate.");
                    return;
                }
            }

            try
            {
                await _orchestrator.ExecuteAsync(request);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<WorldResetService>(
                    $"[WorldResetService] Falha durante TriggerResetAsync signature='{ctx}' reason='{rsn}' ex={ex}");

                // Best-effort: ainda publica completed para evitar deadlock no SceneFlow gate.
                EventBus<WorldLifecycleResetCompletedEvent>.Raise(new WorldLifecycleResetCompletedEvent(ctx, rsn));
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

            provider.TryGetGlobal<IResetPolicy>(out var policy);
            provider.TryGetGlobal<ISimulationGateService>(out var gateService);

            var guards = new List<IResetGuard>(1)
            {
                new SimulationGateResetGuard(gateService)
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
