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
        private readonly HashSet<WorldResetCorrelationKey> _inFlight = new();
        private readonly Dictionary<WorldResetCorrelationKey, long> _recentCompleted = new();
        private readonly WorldResetLifecyclePublisher _lifecyclePublisher;
        private readonly WorldResetOrchestrator _orchestrator;

        public WorldResetService(
            WorldResetOrchestrator orchestrator,
            WorldResetLifecyclePublisher lifecyclePublisher)
        {
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _lifecyclePublisher = lifecyclePublisher ?? throw new ArgumentNullException(nameof(lifecyclePublisher));
        }

        public async Task<WorldResetResult> TriggerResetAsync(WorldResetRequest request)
        {
            WorldResetCorrelationKey correlationKey = request.CorrelationKey;
            string rsn = string.IsNullOrWhiteSpace(request.Reason) ? string.Empty : request.Reason;

            if (request.HasCorrelationKey)
            {
                lock (_lock)
                {
                    if (IsRecentlyCompletedLocked(correlationKey))
                    {
                        LogLifecycleDedupe("recent_completed", correlationKey, rsn);
                        return WorldResetResult.Completed;
                    }

                    if (!_inFlight.Add(correlationKey))
                    {
                        LogLifecycleDedupe("in_flight", correlationKey, rsn);
                        return WorldResetResult.Completed;
                    }
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
                    $"[WorldResetService] Falha durante TriggerResetAsync correlationKey='{correlationKey}' signature='{request.ContextSignature}' reason='{rsn}' ex={ex}");

                _lifecyclePublisher.PublishCompleted(
                    request,
                    WorldResetOutcome.FailedService,
                    $"{WorldResetReasons.FailedServiceExceptionPrefix}:{ex.GetType().Name}");
                return WorldResetResult.Failed;
            }
            finally
            {
                if (request.HasCorrelationKey)
                {
                    lock (_lock)
                    {
                        _inFlight.Remove(correlationKey);
                        if (result != WorldResetResult.Failed)
                        {
                            _recentCompleted[correlationKey] = Stopwatch.GetTimestamp();
                            PruneRecentCompletedLocked();
                        }
                    }
                }
            }
        }

        private bool IsRecentlyCompletedLocked(WorldResetCorrelationKey correlationKey)
        {
            if (!correlationKey.IsValid || !_recentCompleted.TryGetValue(correlationKey, out long completedAt))
            {
                return false;
            }

            long elapsedTicks = Stopwatch.GetTimestamp() - completedAt;
            double elapsedMs = elapsedTicks * 1000d / Stopwatch.Frequency;
            if (elapsedMs < 0d || elapsedMs > RecentCompletionWindowMs)
            {
                _recentCompleted.Remove(correlationKey);
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

        private static void LogLifecycleDedupe(string dedupeKind, WorldResetCorrelationKey correlationKey, string reason)
        {
            DebugUtility.LogVerbose<WorldResetService>(
                $"[OBS][WorldReset][Dedupe] lifecycle='dedupe' kind='{dedupeKind}' correlationKey='{correlationKey}' reason='{reason}'.",
                DebugUtility.Colors.Info);
        }
    }
}

