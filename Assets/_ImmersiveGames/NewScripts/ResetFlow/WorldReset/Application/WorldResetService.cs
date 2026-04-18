#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Contracts;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Domain;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Runtime;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
namespace _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Application
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
        private readonly WorldResetLifecyclePublisher _lifecyclePublisher;
        private readonly WorldResetOrchestrator _orchestrator;

        public WorldResetService(
            WorldResetOrchestrator orchestrator,
            WorldResetLifecyclePublisher lifecyclePublisher)
        {
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _lifecyclePublisher = lifecyclePublisher ?? throw new ArgumentNullException(nameof(lifecyclePublisher));
        }

        public async Task<WorldResetResult> TriggerResetAsync(string? contextSignature, string? reason)
        {
            var request = new WorldResetRequest(
                kind: ResetKind.Macro,
                contextSignature: contextSignature ?? string.Empty,
                reason: reason ?? string.Empty,
                targetScene: string.Empty,
                origin: WorldResetOrigin.Manual,
                macroRouteId: SceneRouteId.None,
                phaseSignature: PhaseContextSignature.Empty,
                sourceSignature: contextSignature ?? string.Empty);

            return await TriggerResetAsync(request);
        }

        public async Task<WorldResetResult> TriggerResetAsync(WorldResetRequest request)
        {
            string ctx = string.IsNullOrWhiteSpace(request.ContextSignature) ? string.Empty : request.ContextSignature;
            string rsn = string.IsNullOrWhiteSpace(request.Reason) ? string.Empty : request.Reason;

            lock (_lock)
            {
                if (IsRecentlyCompletedLocked(ctx))
                {
                    LogLifecycleDedupe("recent_completed", ctx, rsn);
                    return WorldResetResult.Completed;
                }

                if (!_inFlight.Add(ctx))
                {
                    LogLifecycleDedupe("in_flight", ctx, rsn);
                    return WorldResetResult.Completed;
                }
            }

            WorldResetResult result = WorldResetResult.Failed;
            try
            {
                result = await _orchestrator.ExecuteAsync(request);
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

        private static void LogLifecycleDedupe(string dedupeKind, string contextSignature, string reason)
        {
            DebugUtility.LogVerbose<WorldResetService>(
                $"[OBS][WorldReset][Dedupe] lifecycle='dedupe' kind='{dedupeKind}' signature='{contextSignature}' reason='{reason}'.",
                DebugUtility.Colors.Info);
        }
    }
}

