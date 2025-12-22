using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.NewScripts.Infrastructure.Debug;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA
{
    public sealed class ActorLifecycleHookLogger : ActorLifecycleHookBase
    {
        [SerializeField] private string label = "ActorLifecycleHookLogger";

        public override Task OnBeforeActorDespawnAsync()
        {
            DebugUtility.Log(typeof(ActorLifecycleHookLogger),
                $"[QA] {label} -> OnBeforeActorDespawnAsync (go={gameObject.name})");
            return Task.CompletedTask;
        }

        public override Task OnAfterActorSpawnAsync()
        {
            DebugUtility.Log(typeof(ActorLifecycleHookLogger),
                $"[QA] {label} -> OnAfterActorSpawnAsync (go={gameObject.name})");
            return Task.CompletedTask;
        }

        // Mantemos os outros como no-op para evitar ruído.
    }
}
