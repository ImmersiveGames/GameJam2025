using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.GameplayRuntime.GameplayReset.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.GameplayRuntime.GameplayReset.Execution
{
    internal sealed class ActorGroupGameplayResetExecutor
        : IActorGroupGameplayResetExecutor
    {
        private readonly string _sceneName;
        private readonly ActorGroupGameplayResetComponentResolver _componentResolver;

        public ActorGroupGameplayResetExecutor(string sceneName)
        {
            _sceneName = sceneName ?? string.Empty;
            _componentResolver = new ActorGroupGameplayResetComponentResolver(_sceneName);
        }

        public async Task ExecuteAsync(ResetTarget target, ActorGroupGameplayResetRequest request, int serial)
        {
            var components = _componentResolver.ResolveResettableComponents(target, request);
            if (components.Count == 0)
            {
                return;
            }

            await RunStepAsync(components, ActorGroupGameplayResetStep.Cleanup, request, serial);
            await RunStepAsync(components, ActorGroupGameplayResetStep.Restore, request, serial);
            await RunStepAsync(components, ActorGroupGameplayResetStep.Rebind, request, serial);
        }

        private async Task RunStepAsync(IReadOnlyList<ResetEntry> components, ActorGroupGameplayResetStep step, ActorGroupGameplayResetRequest request, int serial)
        {
            var ctx = new ActorGroupGameplayResetContext(
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

        private static Task InvokeStepAsync(IActorGroupGameplayResettable component, ActorGroupGameplayResetStep step, ActorGroupGameplayResetContext ctx)
        {
            if (component == null)
            {
                return Task.CompletedTask;
            }

            return step switch
            {
                ActorGroupGameplayResetStep.Cleanup => component.ResetCleanupAsync(ctx),
                ActorGroupGameplayResetStep.Restore => component.ResetRestoreAsync(ctx),
                ActorGroupGameplayResetStep.Rebind => component.ResetRebindAsync(ctx),
                _ => Task.CompletedTask
            };
        }
    }
}


