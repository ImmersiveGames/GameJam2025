using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.ResourceSystems.Services;

namespace _ImmersiveGames.Scripts.ResourceSystems.Bridges
{
    public class ResourceIntegrationTester : MonoBehaviour
    {
        [SerializeField] private string actorId;

        [ContextMenu("Debug: Print Registered Actors/Canvases")]
        private void DebugRegistry()
        {
            if (!DependencyManager.Instance.TryGetGlobal<IActorResourceOrchestrator>(out var arch))
            {
                Debug.LogWarning("Orchestrator not registered.");
                return;
            }
            Debug.Log($"Actors: {string.Join(", ", arch.RegisteredActors)}");
            Debug.Log($"Canvases: {string.Join(", ", arch.RegisteredCanvases)}");
        }

        [ContextMenu("Debug: Damage 10")]
        private void Damage10()
        {
            if (string.IsNullOrEmpty(actorId)) { Debug.LogWarning("ActorId not set"); return; }
            if (!DependencyManager.Instance.TryGetForObject<ResourceSystemService>(actorId, out var svc)) { Debug.LogWarning("Service not found"); return; }
            svc.Modify(ResourceType.Health, -10);
            Debug.Log("Damage applied");
        }

        [ContextMenu("Debug: Heal 20")]
        private void Heal20()
        {
            if (string.IsNullOrEmpty(actorId)) { Debug.LogWarning("ActorId not set"); return; }
            if (!DependencyManager.Instance.TryGetForObject<ResourceSystemService>(actorId, out var svc)) { Debug.LogWarning("Service not found"); return; }
            svc.Modify(ResourceType.Health, 20);
            Debug.Log("Heal applied");
        }
    }
}