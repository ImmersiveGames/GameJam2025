using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.ActorSystems;

namespace _ImmersiveGames.Scripts.ResourceSystems.Bridges
{
    public class ResourceIntegrationTester : MonoBehaviour
    {
        private IActor _actor;

        private void Awake()
        {
            _actor = GetComponent<IActor>();
            if (_actor == null)
            {
                Debug.LogWarning($"[ResourceIntegrationTester] No IActor found on {gameObject.name}. Disabling.");
                enabled = false;
            }
        }

        [ContextMenu("Debug: Print Registered Actors/Canvases")]
        private void DebugRegistry()
        {
            if (!DependencyManager.Instance.TryGetGlobal<IActorResourceOrchestrator>(out var arch))
            {
                Debug.LogWarning("Orchestrador não registrado.");
                return;
            }
            //Debug.Log($"Atores registrados: {string.Join(", ", arch.RegisteredActors)}");
            //Debug.Log($"Canvases registrados: {string.Join(", ", arch.RegisteredCanvases)}");
        }

        [ContextMenu("Debug: Damage 10")]
        private void Damage10()
        {
            if (_actor == null)
            {
                Debug.LogWarning("IActor não configurado.");
                return;
            }
            if (!DependencyManager.Instance.TryGetForObject<ResourceSystemService>(_actor.ActorId, out var svc))
            {
                Debug.LogWarning($"Serviço ResourceSystemService não encontrado para ActorId: {_actor.ActorId}");
                return;
            }
            svc.Modify(ResourceType.Health, -10);
            Debug.Log("Dano de 10 aplicado.");
        }

        [ContextMenu("Debug: Heal 20")]
        private void Heal20()
        {
            if (_actor == null)
            {
                Debug.LogWarning("IActor não configurado.");
                return;
            }
            if (!DependencyManager.Instance.TryGetForObject<ResourceSystemService>(_actor.ActorId, out var svc))
            {
                Debug.LogWarning($"Serviço ResourceSystemService não encontrado para ActorId: {_actor.ActorId}");
                return;
            }
            svc.Modify(ResourceType.Health, 20);
            Debug.Log("Cura de 20 aplicada.");
        }
    }
}