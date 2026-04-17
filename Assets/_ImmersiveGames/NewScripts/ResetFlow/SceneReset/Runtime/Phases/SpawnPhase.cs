using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
namespace _ImmersiveGames.NewScripts.ResetFlow.SceneReset.Runtime.Phases
{
    internal sealed class SpawnPhase : ISceneResetPhase
    {
        private static readonly SceneResetSpawnOwnerExecutor Executor = new();

        public async Task ExecuteAsync(SceneResetContext context, SceneResetHookRunner hookRunner, CancellationToken ct)
        {
            await hookRunner.RunWorldHooksAsync(context, "OnBeforeSpawn", hook => hook.OnBeforeSpawnAsync());
            if (context != null && context.TryGetCurrentParticipationSnapshot(out var participation))
            {
                DebugUtility.Log(typeof(SceneResetPipeline),
                    $"Spawn phase consuming participation signature='{participation.Signature}' readiness='{participation.Readiness.State}' primaryId='{participation.PrimaryParticipantId}' localId='{participation.LocalParticipantId}'.");
            }
            DebugUtility.Log(typeof(SceneResetPipeline),
                "Spawn phase delegating to Spawn owner services.");
            await Executor.ExecuteAsync(context, "Spawn", service => service.SpawnAsync());
            context.LogActorRegistryCount("After Spawn");
        }
    }
}

