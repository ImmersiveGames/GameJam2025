using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Game.Gameplay.Actors.Core;
using _ImmersiveGames.NewScripts.Orchestration.SceneReset.Hooks;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Orchestration.SceneReset.Runtime
{
    internal static class SceneResetHookExecution
    {
        public static async Task RunWorldHookAsync(
            string hookName,
            string serviceName,
            ISceneResetHook hook,
            Func<ISceneResetHook, Task> hookAction)
        {
            var hookWatch = Stopwatch.StartNew();
            DebugUtility.LogVerbose(typeof(SceneResetPipeline),
                $"{hookName} started: {serviceName}");

            try
            {
                await hookAction(hook);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(SceneResetPipeline),
                    $"{hookName} falhou para {serviceName}: {ex}");
                throw;
            }
            finally
            {
                hookWatch.Stop();
                if (hookWatch.ElapsedMilliseconds > SceneResetContext.SlowHookWarningMs)
                {
                    DebugUtility.LogWarning(typeof(SceneResetPipeline),
                        $"{hookName} lento: {serviceName} levou {hookWatch.ElapsedMilliseconds}ms.");
                }

                DebugUtility.LogVerbose(typeof(SceneResetPipeline),
                    $"{hookName} duration: {serviceName} => {hookWatch.ElapsedMilliseconds}ms");
            }

            DebugUtility.LogVerbose(typeof(SceneResetPipeline),
                $"{hookName} completed: {serviceName}");
        }

        public static async Task RunActorHooksForActorAsync(
            SceneResetContext context,
            string hookName,
            IActor actor,
            Func<IActorLifecycleHook, Task> hookAction)
        {
            string actorLabel = SceneResetContext.GetActorLabel(actor);
            var actorWatch = Stopwatch.StartNew();
            DebugUtility.LogVerbose(typeof(SceneResetPipeline),
                $"{hookName} actor started: {actorLabel}");

            try
            {
                Transform transform = actor.Transform;
                if (transform == null)
                {
                    DebugUtility.LogWarning(typeof(SceneResetPipeline),
                        $"{hookName} ignorado para {actorLabel}: Transform ausente.");
                    return;
                }

                if (!context.TryGetCachedActorHooks(transform, out List<(string Label, IActorLifecycleHook Hook)> actorHooks))
                {
                    actorHooks = context.CollectActorHooks(transform);
                    context.CacheActorHooks(transform, actorHooks);
                }

                if (actorHooks.Count == 0)
                {
                    return;
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
                DebugUtility.LogVerbose(typeof(SceneResetPipeline),
                    $"{hookName} actor duration: {actorLabel} => {actorWatch.ElapsedMilliseconds}ms");
                DebugUtility.LogVerbose(typeof(SceneResetPipeline),
                    $"{hookName} actor completed: {actorLabel}");
            }
        }

        private static async Task RunActorHookAsync(
            string hookName,
            string actorLabel,
            string hookLabel,
            IActorLifecycleHook hook,
            Func<IActorLifecycleHook, Task> hookAction)
        {
            var hookWatch = Stopwatch.StartNew();
            DebugUtility.LogVerbose(typeof(SceneResetPipeline),
                $"{hookName} started: {hookLabel} (actor={actorLabel})");

            try
            {
                await hookAction(hook);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(SceneResetPipeline),
                    $"{hookName} falhou para {hookLabel} (actor={actorLabel}): {ex}");
                throw;
            }
            finally
            {
                hookWatch.Stop();
                if (hookWatch.ElapsedMilliseconds > SceneResetContext.SlowHookWarningMs)
                {
                    DebugUtility.LogWarning(typeof(SceneResetPipeline),
                        $"{hookName} lento: {hookLabel} (actor={actorLabel}) levou {hookWatch.ElapsedMilliseconds}ms.");
                }

                DebugUtility.LogVerbose(typeof(SceneResetPipeline),
                    $"{hookName} duration: {hookLabel} (actor={actorLabel}) => {hookWatch.ElapsedMilliseconds}ms");
            }

            DebugUtility.LogVerbose(typeof(SceneResetPipeline),
                $"{hookName} completed: {hookLabel} (actor={actorLabel})");
        }
    }
}
