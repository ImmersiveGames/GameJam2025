using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.GameplayRuntime.Spawn;
namespace _ImmersiveGames.NewScripts.ResetFlow.SceneReset.Runtime.Phases
{
    internal sealed class SpawnPhase : ISceneResetPhase
    {
        public async Task ExecuteAsync(SceneResetContext context, SceneResetHookRunner hookRunner, CancellationToken ct)
        {
            await hookRunner.RunWorldHooksAsync(context, "OnBeforeSpawn", hook => hook.OnBeforeSpawnAsync());
            if (context != null && context.TryGetCurrentParticipationSnapshot(out var participation))
            {
                DebugUtility.Log(typeof(SceneResetPipeline),
                    $"Spawn phase consuming participation signature='{participation.Signature}' readiness='{participation.ReadinessState}' primaryId='{participation.PrimaryParticipantId}' localId='{participation.LocalParticipantId}'.");
            }
            DebugUtility.Log(typeof(SceneResetPipeline),
                "Spawn phase preserving reset/cleanup only; canonical actor spawn owned by ActorsExecution.");

            if (context?.SpawnServices != null)
            {
                for (int index = 0; index < context.SpawnServices.Count; index += 1)
                {
                    IWorldSpawnService service = context.SpawnServices[index];
                    if (service == null)
                    {
                        continue;
                    }

                    if (!context.ShouldIncludeForScopes(service))
                    {
                        DebugUtility.LogVerbose(typeof(SceneResetPipeline),
                            $"Spawn phase service skipped by scope filter: {service.Name}");
                        continue;
                    }

                    DebugUtility.Log(typeof(SceneResetPipeline),
                        $"[OBS][SceneReset] canonical actor spawn skipped owner='ActorsExecution' decision='skip_canonical_actor_spawn' service='{service.Name}' actorKind='{service.SpawnedActorKind}'.");
                }
            }

            context.LogActorRegistryCount("After Spawn");
        }
    }
}

