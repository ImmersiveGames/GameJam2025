using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.Actors.Core;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Hooks;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime.SceneReset
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

                    await RunWorldHookAsync(hookName, hookEntry.Label, hookEntry.Hook, hookAction);
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

        private async Task RunActorHooksAsync(
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

                string actorLabel = SceneResetContext.GetActorLabel(actor);
                var actorWatch = Stopwatch.StartNew();
                DebugUtility.LogVerbose(typeof(SceneResetFacade),
                    $"{hookName} actor started: {actorLabel}");

                try
                {
                    Transform transform = actor.Transform;
                    if (transform == null)
                    {
                        DebugUtility.LogWarning(typeof(SceneResetFacade),
                            $"{hookName} ignorado para {actorLabel}: Transform ausente.");
                        continue;
                    }

                    if (!context.TryGetCachedActorHooks(transform, out List<(string Label, IActorLifecycleHook Hook)> actorHooks))
                    {
                        actorHooks = context.CollectActorHooks(transform);
                        context.CacheActorHooks(transform, actorHooks);
                    }

                    if (actorHooks.Count == 0)
                    {
                        continue;
                    }

                    SceneResetContext.LogHookOrder($"{hookName} ({actorLabel})", actorHooks);
                    foreach ((string Label, IActorLifecycleHook Hook) actorHook in actorHooks)
                    {
                        await RunActorHookAsync(hookName, actorLabel, actorHook.Label, actorHook.Hook, hookAction);
                    }
                }
                finally
                {
                    actorWatch.Stop();
                    DebugUtility.LogVerbose(typeof(SceneResetFacade),
                        $"{hookName} actor duration: {actorLabel} => {actorWatch.ElapsedMilliseconds}ms");
                    DebugUtility.LogVerbose(typeof(SceneResetFacade),
                        $"{hookName} actor completed: {actorLabel}");
                }
            }

            stepWatch.Stop();
            DebugUtility.LogVerbose(typeof(SceneResetFacade),
                $"{hookName} actor hooks step duration: {stepWatch.ElapsedMilliseconds}ms");
        }

        private static async Task RunWorldHookAsync(
            string hookName,
            string serviceName,
            ISceneResetHook hook,
            Func<ISceneResetHook, Task> hookAction)
        {
            var hookWatch = Stopwatch.StartNew();
            DebugUtility.LogVerbose(typeof(SceneResetFacade),
                $"{hookName} started: {serviceName}");

            try
            {
                await hookAction(hook);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(SceneResetFacade),
                    $"{hookName} falhou para {serviceName}: {ex}");
                throw;
            }
            finally
            {
                hookWatch.Stop();
                if (hookWatch.ElapsedMilliseconds > SceneResetContext.SlowHookWarningMs)
                {
                    DebugUtility.LogWarning(typeof(SceneResetFacade),
                        $"{hookName} lento: {serviceName} levou {hookWatch.ElapsedMilliseconds}ms.");
                }

                DebugUtility.LogVerbose(typeof(SceneResetFacade),
                    $"{hookName} duration: {serviceName} => {hookWatch.ElapsedMilliseconds}ms");
            }

            DebugUtility.LogVerbose(typeof(SceneResetFacade),
                $"{hookName} completed: {serviceName}");
        }

        private static async Task RunActorHookAsync(
            string hookName,
            string actorLabel,
            string hookLabel,
            IActorLifecycleHook hook,
            Func<IActorLifecycleHook, Task> hookAction)
        {
            var hookWatch = Stopwatch.StartNew();
            DebugUtility.LogVerbose(typeof(SceneResetFacade),
                $"{hookName} started: {hookLabel} (actor={actorLabel})");

            try
            {
                await hookAction(hook);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(SceneResetFacade),
                    $"{hookName} falhou para {hookLabel} (actor={actorLabel}): {ex}");
                throw;
            }
            finally
            {
                hookWatch.Stop();
                if (hookWatch.ElapsedMilliseconds > SceneResetContext.SlowHookWarningMs)
                {
                    DebugUtility.LogWarning(typeof(SceneResetFacade),
                        $"{hookName} lento: {hookLabel} (actor={actorLabel}) levou {hookWatch.ElapsedMilliseconds}ms.");
                }

                DebugUtility.LogVerbose(typeof(SceneResetFacade),
                    $"{hookName} duration: {hookLabel} (actor={actorLabel}) => {hookWatch.ElapsedMilliseconds}ms");
            }

            DebugUtility.LogVerbose(typeof(SceneResetFacade),
                $"{hookName} completed: {hookLabel} (actor={actorLabel})");
        }
    }
}
