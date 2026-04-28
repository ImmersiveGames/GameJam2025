using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Runtime;
namespace _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Application
{
    /// <summary>
    /// Executa o trilho local de reset em boundary neutro resolvido pelo pipeline macro.
    /// Nao publica lifecycle; apenas agrega o resultado operacional real dos executores locais.
    /// </summary>
    public sealed class WorldResetExecutor
    {
        private readonly IWorldResetLocalExecutorRegistry _localExecutorRegistry;

        public WorldResetExecutor(IWorldResetLocalExecutorRegistry localExecutorRegistry)
        {
            _localExecutorRegistry = localExecutorRegistry ?? throw new ArgumentNullException(nameof(localExecutorRegistry));
        }

        public bool TryResolveExecutors(
            string targetScene,
            out IReadOnlyList<IWorldResetLocalExecutor> executors)
        {
            executors = _localExecutorRegistry.GetExecutorsForScene(targetScene);
            return executors != null && executors.Count > 0;
        }

        public async Task<WorldResetLocalExecutionResult> TryExecuteAsync(string targetScene, string reason)
        {
            IReadOnlyList<IWorldResetLocalExecutor> executors = _localExecutorRegistry.GetExecutorsForScene(targetScene);
            if (executors == null || executors.Count == 0)
            {
                return WorldResetLocalExecutionResult.Rejected(
                    targetScene,
                    reason,
                    nameof(WorldResetExecutor),
                    $"No local reset executor found for scene='{Normalize(targetScene)}'.");
            }

            return await ExecuteResetOnControllersAsync(executors, targetScene, reason);
        }

        public async Task<WorldResetLocalExecutionResult> ExecuteAsync(
            IReadOnlyList<IWorldResetLocalExecutor> executors,
            string reason,
            string targetScene = null)
        {
            return await ExecuteResetOnControllersAsync(executors, targetScene, reason);
        }

        private static async Task<WorldResetLocalExecutionResult> ExecuteResetOnControllersAsync(
            IReadOnlyList<IWorldResetLocalExecutor> executors,
            string targetScene,
            string reason)
        {
            string scene = Normalize(targetScene);
            string normalizedReason = Normalize(reason);

            if (executors == null || executors.Count == 0)
            {
                return WorldResetLocalExecutionResult.Rejected(
                    scene,
                    normalizedReason,
                    nameof(WorldResetExecutor),
                    "No local reset executor was provided to the world reset executor.");
            }

            var filtered = new List<IWorldResetLocalExecutor>(executors.Count);
            for (int i = 0; i < executors.Count; i++)
            {
                IWorldResetLocalExecutor executor = executors[i];
                if (executor != null)
                {
                    filtered.Add(executor);
                }
            }

            if (filtered.Count == 0)
            {
                return WorldResetLocalExecutionResult.Rejected(
                    scene,
                    normalizedReason,
                    nameof(WorldResetExecutor),
                    "All local reset executors were null.");
            }

            filtered.Sort(static (a, b) => CompareExecutors(a, b));

            var tasks = new List<Task<WorldResetLocalExecutionResult>>(filtered.Count);
            for (int i = 0; i < filtered.Count; i++)
            {
                tasks.Add(ExecuteSingleExecutorAsync(filtered[i], normalizedReason));
            }

            WorldResetLocalExecutionResult[] results = await Task.WhenAll(tasks);
            return AggregateResults(results, scene, normalizedReason, filtered.Count);
        }

        private static async Task<WorldResetLocalExecutionResult> ExecuteSingleExecutorAsync(
            IWorldResetLocalExecutor executor,
            string reason)
        {
            if (executor == null)
            {
                return WorldResetLocalExecutionResult.Rejected(
                    string.Empty,
                    reason,
                    nameof(WorldResetExecutor),
                    "Null local reset executor.");
            }

            try
            {
                WorldResetLocalExecutionResult result = await executor.ResetWorldAsync(reason);
                if (result.Status == WorldResetLocalExecutionStatus.Completed && string.IsNullOrWhiteSpace(result.Source))
                {
                    return WorldResetLocalExecutionResult.Unconfirmed(
                        result.SceneName,
                        reason,
                        nameof(WorldResetExecutor),
                        $"Local executor returned Completed without source. result='{result}'.");
                }

                if (string.IsNullOrWhiteSpace(result.Source) && result.Status != WorldResetLocalExecutionStatus.Completed)
                {
                    return WorldResetLocalExecutionResult.Unconfirmed(
                        result.SceneName,
                        reason,
                        nameof(WorldResetExecutor),
                        $"Local executor returned non-completed result without source. result='{result}'.");
                }

                return result;
            }
            catch (Exception ex)
            {
                return WorldResetLocalExecutionResult.Failed(
                    string.Empty,
                    reason,
                    executor.GetType().Name,
                    $"Local reset executor threw '{ex.GetType().Name}': {ex.Message}");
            }
        }

        private static WorldResetLocalExecutionResult AggregateResults(
            IReadOnlyList<WorldResetLocalExecutionResult> results,
            string scene,
            string reason,
            int executorCount)
        {
            if (results == null || results.Count == 0)
            {
                return WorldResetLocalExecutionResult.Rejected(
                    scene,
                    reason,
                    nameof(WorldResetExecutor),
                    "No local reset execution result was produced.");
            }

            WorldResetLocalExecutionStatus aggregateStatus = WorldResetLocalExecutionStatus.Completed;
            var detail = new List<string>(results.Count + 1)
            {
                $"executorCount='{executorCount}'"
            };

            for (int i = 0; i < results.Count; i++)
            {
                WorldResetLocalExecutionResult result = results[i];
                detail.Add($"[{i}] {result}");

                if (result.Status == WorldResetLocalExecutionStatus.Failed)
                {
                    aggregateStatus = WorldResetLocalExecutionStatus.Failed;
                    continue;
                }

                if (aggregateStatus == WorldResetLocalExecutionStatus.Failed)
                {
                    continue;
                }

                if (result.Status == WorldResetLocalExecutionStatus.Unconfirmed)
                {
                    aggregateStatus = WorldResetLocalExecutionStatus.Unconfirmed;
                    continue;
                }

                if (aggregateStatus == WorldResetLocalExecutionStatus.Unconfirmed)
                {
                    continue;
                }

                if (result.Status == WorldResetLocalExecutionStatus.Rejected)
                {
                    aggregateStatus = WorldResetLocalExecutionStatus.Rejected;
                }
            }

            string combinedDetail = string.Join(" | ", detail);
            return aggregateStatus switch
            {
                WorldResetLocalExecutionStatus.Completed => WorldResetLocalExecutionResult.Completed(scene, reason, nameof(WorldResetExecutor), combinedDetail),
                WorldResetLocalExecutionStatus.Failed => WorldResetLocalExecutionResult.Failed(scene, reason, nameof(WorldResetExecutor), combinedDetail),
                WorldResetLocalExecutionStatus.Unconfirmed => WorldResetLocalExecutionResult.Unconfirmed(scene, reason, nameof(WorldResetExecutor), combinedDetail),
                _ => WorldResetLocalExecutionResult.Rejected(scene, reason, nameof(WorldResetExecutor), combinedDetail),
            };
        }

        private static int CompareExecutors(IWorldResetLocalExecutor left, IWorldResetLocalExecutor right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left is not UnityEngine.Object leftObject)
            {
                return 1;
            }

            if (right is not UnityEngine.Object rightObject)
            {
                return -1;
            }

            return leftObject.GetInstanceID().CompareTo(rightObject.GetInstanceID());
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    /// <summary>
    /// Executor operacional do handoff de phase-reset.
    /// Mantem o efeito final fora do SessionIntegration.
    /// </summary>
    public sealed class PhaseResetOperationalHandoffService : IPhaseResetOperationalHandoffService
    {
        private readonly WorldResetExecutor _executor;

        public PhaseResetOperationalHandoffService(IWorldResetLocalExecutorRegistry localExecutorRegistry)
        {
            _executor = new WorldResetExecutor(localExecutorRegistry ?? throw new ArgumentNullException(nameof(localExecutorRegistry)));
        }

        public async Task<PhaseResetOperationalHandoffResult> ExecuteAsync(PhaseResetHandoffRequest request, CancellationToken ct)
        {
            if (!request.IsValid)
            {
                return PhaseResetOperationalHandoffResult.Rejected(
                    request,
                    0,
                    $"Invalid phase-reset handoff request. scene='{request.ActiveScene}' reason='{request.Reason}' source='{request.Source}'.");
            }

            ct.ThrowIfCancellationRequested();

            if (!_executor.TryResolveExecutors(request.ActiveScene, out var executors) || executors == null || executors.Count == 0)
            {
                PhaseResetOperationalHandoffResult result = PhaseResetOperationalHandoffResult.Rejected(
                    request,
                    0,
                    $"No local reset executor found for scene='{request.ActiveScene}'. reason='{request.Reason}' source='{request.Source}'.");

                DebugUtility.LogWarning<PhaseResetOperationalHandoffService>(
                    $"[OBS][PhaseReset][Operational] HandoffRejected source='{request.Source}' scene='{request.ActiveScene}' reason='{request.Reason}' status='{result.Status}' detail='{result.Detail}'.");

                return result;
            }

            DebugUtility.Log<PhaseResetOperationalHandoffService>(
                $"[OBS][PhaseReset][Operational] HandoffAccepted source='{request.Source}' scene='{request.ActiveScene}' reason='{request.Reason}' executors='{executors.Count}'.",
                DebugUtility.Colors.Info);

            WorldResetLocalExecutionResult localResult = await _executor.ExecuteAsync(executors, request.Reason, request.ActiveScene);
            PhaseResetOperationalHandoffResult handoffResult = MapLocalResult(request, executors.Count, localResult);

            DebugUtility.Log<PhaseResetOperationalHandoffService>(
                $"[OBS][PhaseReset][Operational] HandoffCompleted source='{request.Source}' scene='{request.ActiveScene}' reason='{request.Reason}' executors='{executors.Count}' status='{handoffResult.Status}' detail='{handoffResult.Detail}'.",
                handoffResult.Succeeded ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);

            return handoffResult;
        }

        private static PhaseResetOperationalHandoffResult MapLocalResult(
            PhaseResetHandoffRequest request,
            int executorCount,
            WorldResetLocalExecutionResult localResult)
        {
            string detail = $"localResult='{localResult}'";
            return localResult.Status switch
            {
                WorldResetLocalExecutionStatus.Completed => PhaseResetOperationalHandoffResult.Completed(request, executorCount, detail),
                WorldResetLocalExecutionStatus.Failed => PhaseResetOperationalHandoffResult.Failed(request, executorCount, detail),
                WorldResetLocalExecutionStatus.Unconfirmed => PhaseResetOperationalHandoffResult.Unconfirmed(request, executorCount, detail),
                _ => PhaseResetOperationalHandoffResult.Rejected(request, executorCount, detail),
            };
        }
    }
}
