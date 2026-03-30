using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Rearm.Core
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
        public ResetEntry(IActorGroupRearmable component, int order)
        {
            Component = component;
            Order = order;
        }

        public IActorGroupRearmable Component { get; }
        public int Order { get; }
    }

    internal sealed class SyncAdapter : IActorGroupRearmable
    {
        private readonly IActorGroupRearmableSync _sync;

        public SyncAdapter(IActorGroupRearmableSync sync)
        {
            _sync = sync;
        }

        public System.Threading.Tasks.Task ResetCleanupAsync(ActorGroupRearmContext ctx)
        {
            _sync.ResetCleanup(ctx);
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public System.Threading.Tasks.Task ResetRestoreAsync(ActorGroupRearmContext ctx)
        {
            _sync.ResetRestore(ctx);
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public System.Threading.Tasks.Task ResetRebindAsync(ActorGroupRearmContext ctx)
        {
            _sync.ResetRebind(ctx);
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }
}
