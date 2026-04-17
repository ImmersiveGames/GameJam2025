using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImmersiveGames.GameJam2025.Core.Logging;
using ImmersiveGames.GameJam2025.Game.Gameplay.GameplayReset.Integration;
using ImmersiveGames.GameJam2025.Game.Gameplay.Spawn;
using ImmersiveGames.GameJam2025.Orchestration.SceneReset.Runtime.Phases;
namespace ImmersiveGames.GameJam2025.Orchestration.SceneReset.Runtime
{
    internal sealed class SceneResetPipeline
    {
        private readonly IReadOnlyList<ISceneResetPhase> _phases;
        private readonly SceneResetHookRunner _hookRunner;

        public SceneResetPipeline(IReadOnlyList<ISceneResetPhase> phases, SceneResetHookRunner hookRunner)
        {
            _phases = phases ?? throw new ArgumentNullException(nameof(phases));
            _hookRunner = hookRunner ?? throw new ArgumentNullException(nameof(hookRunner));
        }

        public static SceneResetPipeline CreateDefault()
        {
            var hookRunner = new SceneResetHookRunner();
            return new SceneResetPipeline(
                new ISceneResetPhase[]
                {
                    new AcquireResetGatePhase(),
                    new BeforeDespawnHooksPhase(),
                    new DespawnPhase(),
                    new AfterDespawnHooksPhase(),
                    new ScopedParticipantsResetPhase(),
                    new SpawnPhase(),
                    new AfterSpawnHooksPhase(),
                },
                hookRunner);
        }

        public async Task ExecuteAsync(SceneResetContext context, CancellationToken ct)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var resetWatch = Stopwatch.StartNew();
            DebugUtility.Log(typeof(SceneResetPipeline), context.StartLog);
            bool completed = false;

            try
            {
                context.LogActorRegistryCount("Reset start");
                context.WarnIfNoSpawnServices();

                foreach (ISceneResetPhase phase in _phases)
                {
                    ct.ThrowIfCancellationRequested();
                    await phase.ExecuteAsync(context, _hookRunner, ct);
                }

                completed = true;
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(SceneResetPipeline), $"World reset failed: {ex}");
                throw;
            }
            finally
            {
                resetWatch.Stop();
                context.ClearActorHookCacheForCycle();
                context.ReleaseGateIfNeeded();

                if (completed)
                {
                    DebugUtility.Log(typeof(SceneResetPipeline), context.CompletionLog);
                }

                DebugUtility.LogVerbose(typeof(SceneResetPipeline),
                    $"Reset duration: {resetWatch.ElapsedMilliseconds}ms");
            }
        }
    }

    internal sealed class SceneResetSpawnOwnerExecutor
    {
        public async Task ExecuteAsync(SceneResetContext context, string stepName, Func<IWorldSpawnService, Task> stepAction)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (stepAction == null)
            {
                throw new ArgumentNullException(nameof(stepAction));
            }

            var stepWatch = Stopwatch.StartNew();
            DebugUtility.Log(typeof(SceneResetPipeline), $"{stepName} started");

            if (context.SpawnServices.Count == 0)
            {
                DebugUtility.LogWarning(typeof(SceneResetPipeline),
                    $"{stepName} skipped (no spawn services registered).");
                stepWatch.Stop();
                DebugUtility.LogVerbose(typeof(SceneResetPipeline),
                    $"{stepName} duration: {stepWatch.ElapsedMilliseconds}ms");
                return;
            }

            foreach (IWorldSpawnService service in context.SpawnServices)
            {
                if (service == null)
                {
                    DebugUtility.LogError(typeof(SceneResetPipeline),
                        $"{stepName} service é nulo e será ignorado.");
                    continue;
                }

                if (!context.ShouldIncludeForScopes(service))
                {
                    DebugUtility.LogVerbose(typeof(SceneResetPipeline),
                        $"{stepName} service skipped by scope filter: {service.Name}");
                    continue;
                }

                DebugUtility.Log(typeof(SceneResetPipeline),
                    $"{stepName} service started: {service.Name}");

                var serviceWatch = Stopwatch.StartNew();
                try
                {
                    await stepAction(service);
                }
                finally
                {
                    serviceWatch.Stop();
                    DebugUtility.LogVerbose(typeof(SceneResetPipeline),
                        $"{stepName} service duration: {service.Name} => {serviceWatch.ElapsedMilliseconds}ms");
                }

                DebugUtility.Log(typeof(SceneResetPipeline),
                    $"{stepName} service completed: {service.Name}");
            }

            stepWatch.Stop();
            DebugUtility.LogVerbose(typeof(SceneResetPipeline),
                $"{stepName} duration: {stepWatch.ElapsedMilliseconds}ms");
        }
    }

    internal sealed class SceneResetGameplayResetExecutor
    {
        public async Task ExecuteAsync(SceneResetContext context, CancellationToken ct)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.ResetContext == null)
            {
                return;
            }

            List<IActorGroupGameplayResetWorldParticipant> participants = context.CollectScopedParticipants();
            if (participants.Count == 0)
            {
                DebugUtility.LogVerbose(typeof(SceneResetPipeline),
                    "Scoped reset participants step skipped (participants=0)");
                return;
            }

            var filtered = new List<IActorGroupGameplayResetWorldParticipant>();
            foreach (IActorGroupGameplayResetWorldParticipant participant in participants)
            {
                if (participant == null)
                {
                    DebugUtility.LogError(typeof(SceneResetPipeline),
                        "Scoped reset participant é nulo e será ignorado.");
                    continue;
                }

                if (!context.ResetContext.Value.ContainsScope(participant.Scope))
                {
                    DebugUtility.LogVerbose(typeof(SceneResetPipeline),
                        $"Scoped reset participant skipped by scope: {participant.GetType().Name}");
                    continue;
                }

                filtered.Add(participant);
            }

            if (filtered.Count == 0)
            {
                DebugUtility.LogVerbose(typeof(SceneResetPipeline),
                    "Scoped reset participants step skipped (filtered=0)");
                return;
            }

            filtered.Sort(context.CompareResetScopeParticipants);
            LogScopedParticipantOrder(filtered);

            foreach (IActorGroupGameplayResetWorldParticipant participant in filtered)
            {
                ct.ThrowIfCancellationRequested();
                string participantType = participant.GetType().FullName ?? participant.GetType().Name;
                var watch = Stopwatch.StartNew();
                DebugUtility.LogVerbose(typeof(SceneResetPipeline),
                    $"Scoped reset started: {participantType}");

                try
                {
                    await participant.ResetAsync(context.ResetContext.Value);
                }
                catch (Exception ex)
                {
                    DebugUtility.LogError(typeof(SceneResetPipeline),
                        $"Scoped reset falhou para {participantType}: {ex}");
                    throw;
                }
                finally
                {
                    watch.Stop();
                    if (watch.ElapsedMilliseconds > SceneResetContext.SlowHookWarningMs)
                    {
                        DebugUtility.LogWarning(typeof(SceneResetPipeline),
                            $"Scoped reset lento: {participantType} levou {watch.ElapsedMilliseconds}ms.");
                    }

                    DebugUtility.LogVerbose(typeof(SceneResetPipeline),
                        $"Scoped reset duration: {participantType} => {watch.ElapsedMilliseconds}ms");
                }

                DebugUtility.LogVerbose(typeof(SceneResetPipeline),
                    $"Scoped reset completed: {participantType}");
            }
        }

        private static void LogScopedParticipantOrder(List<IActorGroupGameplayResetWorldParticipant> participants)
        {
            string[] orderedLabels = participants
                .Select(participant =>
                {
                    string typeName = participant?.GetType().Name ?? "<null participant>";
                    return $"{typeName}(scope={participant?.Scope}, order={participant?.Order})";
                })
                .ToArray();

            DebugUtility.LogVerbose(typeof(SceneResetPipeline),
                $"Scoped reset execution order: {string.Join(", ", orderedLabels)}");
        }
    }
}

