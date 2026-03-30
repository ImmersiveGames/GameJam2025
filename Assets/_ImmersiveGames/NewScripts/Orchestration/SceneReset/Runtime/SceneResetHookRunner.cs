using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Game.Gameplay.Actors.Core;
using _ImmersiveGames.NewScripts.Orchestration.SceneReset.Hooks;
namespace _ImmersiveGames.NewScripts.Orchestration.SceneReset.Runtime
{
    internal sealed class SceneResetHookRunner
    {
        public async Task RunWorldHooksAsync(
            SceneResetContext context,
            string hookName,
            Func<ISceneResetHook, Task> hookAction)
        {
            List<(string Label, ISceneResetHook Hook)> collectedHooks = context.CollectWorldHooks();
            if (collectedHooks.Count == 0)
            {
                DebugUtility.LogVerbose(typeof(SceneResetFacade),
                    $"{hookName} step skipped (hooks=0)");
                return;
            }

            var stepWatch = Stopwatch.StartNew();
            DebugUtility.LogVerbose(typeof(SceneResetFacade),
                $"{hookName} step started (hooks={collectedHooks.Count})");

            try
            {
                SceneResetContext.LogHookOrder(hookName, collectedHooks);

                foreach ((string Label, ISceneResetHook Hook) hookEntry in collectedHooks)
                {
                    if (hookEntry.Hook == null)
                    {
                        DebugUtility.LogError(typeof(SceneResetFacade),
                            $"{hookName} hook '{hookEntry.Label}' é nulo e será ignorado.");
                        continue;
                    }

                    await SceneResetHookExecution.RunWorldHookAsync(hookName, hookEntry.Label, hookEntry.Hook, hookAction);
                }
            }
            finally
            {
                stepWatch.Stop();
                DebugUtility.LogVerbose(typeof(SceneResetFacade),
                    $"{hookName} step duration: {stepWatch.ElapsedMilliseconds}ms");
                DebugUtility.LogVerbose(typeof(SceneResetFacade),
                    $"{hookName} step completed");
            }
        }

        public async Task RunActorHooksBeforeDespawnAsync(SceneResetContext context)
        {
            List<IActor> actors = context.SnapshotActors();
            if (actors.Count == 0)
            {
                return;
            }

            await RunActorHooksAsync(context, "OnBeforeActorDespawn", actors, hook => hook.OnBeforeActorDespawnAsync());
        }

        public async Task RunActorHooksAfterSpawnAsync(SceneResetContext context)
        {
            List<IActor> actors = context.SnapshotActors();
            if (actors.Count == 0)
            {
                return;
            }

            await RunActorHooksAsync(context, "OnAfterActorSpawn", actors, hook => hook.OnAfterActorSpawnAsync());
        }

        private static async Task RunActorHooksAsync(
            SceneResetContext context,
            string hookName,
            List<IActor> actors,
            Func<IActorLifecycleHook, Task> hookAction)
        {
            var stepWatch = Stopwatch.StartNew();
            DebugUtility.LogVerbose(typeof(SceneResetFacade),
                $"{hookName} actor hooks step started (actors={actors.Count})");

            foreach (IActor actor in actors)
            {
                if (actor == null)
                {
                    DebugUtility.LogError(typeof(SceneResetFacade),
                        $"{hookName} actor é nulo e será ignorado.");
                    continue;
                }

                await SceneResetHookExecution.RunActorHooksForActorAsync(context, hookName, actor, hookAction);
            }

            stepWatch.Stop();
            DebugUtility.LogVerbose(typeof(SceneResetFacade),
                $"{hookName} actor hooks step duration: {stepWatch.ElapsedMilliseconds}ms");
        }
    }
}
