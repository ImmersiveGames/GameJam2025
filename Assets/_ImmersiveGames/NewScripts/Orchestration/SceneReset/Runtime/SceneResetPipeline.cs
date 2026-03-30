using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.SceneReset.Runtime.Phases;
namespace _ImmersiveGames.NewScripts.Orchestration.SceneReset.Runtime
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

            var resetWatch = System.Diagnostics.Stopwatch.StartNew();
            DebugUtility.Log(typeof(SceneResetFacade), context.StartLog);
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
                DebugUtility.LogError(typeof(SceneResetFacade), $"World reset failed: {ex}");
                throw;
            }
            finally
            {
                resetWatch.Stop();
                context.ClearActorHookCacheForCycle();
                context.ReleaseGateIfNeeded();

                if (completed)
                {
                    DebugUtility.Log(typeof(SceneResetFacade), context.CompletionLog);
                }

                DebugUtility.LogVerbose(typeof(SceneResetFacade),
                    $"Reset duration: {resetWatch.ElapsedMilliseconds}ms");
            }
        }
    }
}
