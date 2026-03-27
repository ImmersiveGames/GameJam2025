using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Rearm.Core
{
    internal sealed class ActorGroupRearmExecutor
    {
        private readonly string _sceneName;

        public ActorGroupRearmExecutor(string sceneName)
        {
            _sceneName = sceneName ?? string.Empty;
        }

        public async Task RunAllStepsAsync(IReadOnlyList<ResetEntry> components, ActorGroupRearmRequest request, int serial)
        {
            await RunStepAsync(components, ActorGroupRearmStep.Cleanup, request, serial);
            await RunStepAsync(components, ActorGroupRearmStep.Restore, request, serial);
            await RunStepAsync(components, ActorGroupRearmStep.Rebind, request, serial);
        }

        private async Task RunStepAsync(IReadOnlyList<ResetEntry> components, ActorGroupRearmStep step, ActorGroupRearmRequest request, int serial)
        {
            var ctx = new ActorGroupRearmContext(
                ResolveSceneName(),
                request,
                serial,
                Time.frameCount,
                step);

            foreach (var entry in components)
            {
                await InvokeStepAsync(entry.Component, step, ctx);
            }
        }

        private string ResolveSceneName()
        {
            if (!string.IsNullOrWhiteSpace(_sceneName))
            {
                return _sceneName;
            }

            return SceneManager.GetActiveScene().name;
        }

        private static Task InvokeStepAsync(IActorGroupRearmable component, ActorGroupRearmStep step, ActorGroupRearmContext ctx)
        {
            if (component == null)
            {
                return Task.CompletedTask;
            }

            return step switch
            {
                ActorGroupRearmStep.Cleanup => component.ResetCleanupAsync(ctx),
                ActorGroupRearmStep.Restore => component.ResetRestoreAsync(ctx),
                ActorGroupRearmStep.Rebind => component.ResetRebindAsync(ctx),
                _ => Task.CompletedTask
            };
        }
    }
}
