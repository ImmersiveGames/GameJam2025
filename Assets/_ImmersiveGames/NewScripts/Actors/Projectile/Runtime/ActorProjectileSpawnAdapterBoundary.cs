using _ImmersiveGames.NewScripts.Actors.Projectile.Contracts;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Projectile.Runtime
{
    [DisallowMultipleComponent]
    public sealed class ActorProjectileSpawnAdapterBoundary : MonoBehaviour, IActorProjectileSpawnAdapter
    {
        [SerializeField, Tooltip("Identificador técnico da fronteira de spawn. Este adapter não executa spawn neste corte.")]
        private string adapterId = "actor.projectile.spawn.adapter.player.primary";

        public string AdapterId => Normalize(adapterId);

        public ActorProjectileSpawnAdapterResult Execute(ActorProjectileFireCommand command)
        {
            if (!command.IsValid)
            {
                DebugUtility.Log(
                    typeof(ActorProjectileSpawnAdapterBoundary),
                    $"[OBS][ActorProjectileFire] event='ActorProjectileSpawnAdapterRejected' adapterId='{AdapterId}' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' fireModeId='{command.FireModeId}' spawnExecuted='False' poolCalled='False' source='{nameof(ActorProjectileSpawnAdapterBoundary)}' reason='invalid_projectile_fire_command'.",
                    DebugUtility.Colors.Info);

                return ActorProjectileSpawnAdapterResult.Failed(
                    command,
                    "invalid_projectile_fire_command",
                    "Projectile spawn adapter boundary received an invalid command.");
            }

            DebugUtility.Log(
                typeof(ActorProjectileSpawnAdapterBoundary),
                $"[OBS][ActorProjectileFire] event='ActorProjectileSpawnAdapterNotConfigured' adapterId='{AdapterId}' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' fireModeId='{command.FireModeId}' spawnExecuted='False' poolCalled='False' source='{nameof(ActorProjectileSpawnAdapterBoundary)}' reason='projectile_spawn_adapter_not_configured'.",
                DebugUtility.Colors.Info);

            return ActorProjectileSpawnAdapterResult.NotConfigured(
                command,
                "projectile_spawn_adapter_not_configured",
                "Projectile spawn adapter boundary is present, but real spawn execution is intentionally not configured in ACT-PROJ-2A.");
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
