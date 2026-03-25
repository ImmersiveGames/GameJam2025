#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
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
    /// Fonte de verdade para dedupe e delegação ao pipeline macro já composto.
    /// </summary>
    public sealed class WorldResetService : IWorldResetService
    {
        private const int RecentCompletionWindowMs = 750;

        private readonly object _lock = new();
        private readonly HashSet<string> _inFlight = new(StringComparer.Ordinal);
        private readonly Dictionary<string, long> _recentCompleted = new(StringComparer.Ordinal);
        private readonly WorldResetLifecyclePublisher _lifecyclePublisher = new();

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
            EnsureDependencies();

            string ctx = string.IsNullOrWhiteSpace(request.ContextSignature) ? string.Empty : request.ContextSignature;
            string rsn = string.IsNullOrWhiteSpace(request.Reason) ? string.Empty : request.Reason;

            lock (_lock)
            {
                if (IsRecentlyCompletedLocked(ctx))
                {
                    DebugUtility.LogVerbose<WorldResetService>(
                        $"[{ResetLogTags.Skipped}] Reset duplicado ignorado (recent completed). signature='{ctx}' reason='{rsn}'.",
                        DebugUtility.Colors.Info);
                    return WorldResetResult.Completed;
                }

                if (!_inFlight.Add(ctx))
                {
                    DebugUtility.LogVerbose<WorldResetService>(
                        $"[{ResetLogTags.Guarded}][DEGRADED_MODE] Reset duplicado ignorado (in-flight). signature='{ctx}' reason='{rsn}'.",
                        DebugUtility.Colors.Info);
                    return WorldResetResult.Completed;
                }
            }

            WorldResetResult result = WorldResetResult.Failed;
            try
            {
                WorldResetOrchestrator orchestrator = _orchestrator
                    ?? throw new InvalidOperationException("WorldResetOrchestrator was not initialized.");
                result = await orchestrator.ExecuteAsync(request);
                return result;
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<WorldResetService>(
                    $"[WorldResetService] Falha durante TriggerResetAsync signature='{ctx}' reason='{rsn}' ex={ex}");

                _lifecyclePublisher.PublishCompleted(
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
                    if (result != WorldResetResult.Failed && !string.IsNullOrWhiteSpace(ctx))
                    {
                        _recentCompleted[ctx] = Stopwatch.GetTimestamp();
                        PruneRecentCompletedLocked();
                    }
                }
            }
        }

        private bool IsRecentlyCompletedLocked(string contextSignature)
        {
            if (string.IsNullOrWhiteSpace(contextSignature))
            {
                return false;
            }

            if (!_recentCompleted.TryGetValue(contextSignature, out long completedAt))
            {
                return false;
            }

            long elapsedTicks = Stopwatch.GetTimestamp() - completedAt;
            double elapsedMs = elapsedTicks * 1000d / Stopwatch.Frequency;
            if (elapsedMs < 0d || elapsedMs > RecentCompletionWindowMs)
            {
                _recentCompleted.Remove(contextSignature);
                return false;
            }

            return true;
        }

        private void PruneRecentCompletedLocked()
        {
            if (_recentCompleted.Count <= 128)
            {
                return;
            }

            _recentCompleted.Clear();
        }

        private void EnsureDependencies()
        {
            if (_dependenciesResolved)
            {
                return;
            }

            IDependencyProvider provider = DependencyManager.Provider;

            provider.TryGetGlobal<IWorldResetPolicy>(out IWorldResetPolicy policy);
            provider.TryGetGlobal<ISimulationGateService>(out ISimulationGateService gateService);

            var guards = new List<IWorldResetGuard>(1)
            {
                new SimulationGateWorldResetGuard(gateService)
            };

            var validators = new List<IWorldResetValidator>(1)
            {
                new WorldResetSignatureValidator()
            };

            WorldResetValidationPipeline validationPipeline = new WorldResetValidationPipeline(validators);
            WorldResetExecutor executor = new WorldResetExecutor();
            WorldResetPostResetValidator postResetValidator = new WorldResetPostResetValidator(provider);

            _orchestrator = new WorldResetOrchestrator(
                policy,
                guards,
                validationPipeline,
                executor,
                postResetValidator,
                _lifecyclePublisher);

            _dependenciesResolved = true;
        }
    }
}
