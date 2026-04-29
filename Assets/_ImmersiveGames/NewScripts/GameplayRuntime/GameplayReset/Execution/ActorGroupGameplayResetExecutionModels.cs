using _ImmersiveGames.NewScripts.GameplayRuntime.GameplayReset.Core;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.GameplayRuntime.GameplayReset.Execution
{
    internal readonly struct ResetTarget
    {
        public ResetTarget(string actorId, GameObject root, Transform transform)
        {
            ActorId = actorId;
            Root = root;
            Transform = transform;
        }

        public string ActorId { get; }
        public GameObject Root { get; }
        public Transform Transform { get; }
    }

    internal readonly struct ResetEntry
    {
        public ResetEntry(IActorGroupGameplayResettable component, int order)
        {
            Component = component;
            Order = order;
        }

        public IActorGroupGameplayResettable Component { get; }
        public int Order { get; }
    }

    internal sealed class SyncAdapter : IActorGroupGameplayResettable
    {
        private readonly IActorGroupGameplayResettableSync _sync;

        public SyncAdapter(IActorGroupGameplayResettableSync sync)
        {
            _sync = sync;
        }

        public System.Threading.Tasks.Task ResetCleanupAsync(ActorGroupGameplayResetContext ctx)
        {
            _sync.ResetCleanup(ctx);
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public System.Threading.Tasks.Task ResetRestoreAsync(ActorGroupGameplayResetContext ctx)
        {
            _sync.ResetRestore(ctx);
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public System.Threading.Tasks.Task ResetRebindAsync(ActorGroupGameplayResetContext ctx)
        {
            _sync.ResetRebind(ctx);
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }
}


