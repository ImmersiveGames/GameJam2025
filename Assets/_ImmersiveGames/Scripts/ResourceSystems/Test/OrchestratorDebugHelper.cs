using UnityEngine;
using _ImmersiveGames.Scripts.ResourceSystems.Bind;
using _ImmersiveGames.Scripts.Utils.DependencySystems;

namespace _ImmersiveGames.Scripts.ResourceSystems.Test
{
    public class OrchestratorDebugHelper : MonoBehaviour
    {
        [ContextMenu("🔍 Debug Player_0 Registration")]
        public void DebugPlayer0()
        {
            DependencyManager.Instance.TryGetGlobal<IActorResourceOrchestrator>(out var orchestrator);
            if (orchestrator is ActorResourceOrchestratorService service)
            {
                service.DebugActorRegistration("Player_0");
            }
        }

        [ContextMenu("🔍 Debug Player_1 Registration")]
        public void DebugPlayer1()
        {
            DependencyManager.Instance.TryGetGlobal<IActorResourceOrchestrator>(out var orchestrator);
            if (orchestrator is ActorResourceOrchestratorService service)
            {
                service.DebugActorRegistration("Player_1");
            }
        }

        [ContextMenu("📊 Debug All Actors")]
        public void DebugAllActors()
        {
            DependencyManager.Instance.TryGetGlobal<IActorResourceOrchestrator>(out var orchestrator);
            if (orchestrator is ActorResourceOrchestratorService service)
            {
                service.DebugActorRegistration("Player_0");
                service.DebugActorRegistration("Player_1");
            }
        }
    }
}