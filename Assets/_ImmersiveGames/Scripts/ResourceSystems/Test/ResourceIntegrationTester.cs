using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    public class ResourceIntegrationTester : MonoBehaviour
    {
        private IActor _actor;

        private void Awake()
        {
            _actor = GetComponent<IActor>();
            if (_actor != null) return;
            DebugUtility.LogWarning<ResourceIntegrationTester>($"No IActor found on {gameObject.name}. Disabling.");
            enabled = false;
        }

        [ContextMenu("Debug: Damage 20")]
        private void Damage10()
        {
            if (_actor == null)
            {
                DebugUtility.LogWarning<ResourceIntegrationTester>("IActor não configurado.");
                return;
            }
            if (!DependencyManager.Instance.TryGetForObject<ResourceSystem>(_actor.ActorId, out var svc))
            {
                DebugUtility.LogWarning<ResourceIntegrationTester>($"Serviço ResourceSystem não encontrado para ActorId: {_actor.ActorId}");
                return;
            }
            svc.Modify(ResourceType.Health, -20);
            DebugUtility.LogVerbose<ResourceIntegrationTester>("Dano de 20 aplicado.");
        }

        [ContextMenu("Debug: Heal 20")]
        private void Heal30()
        {
            if (_actor == null)
            {
                DebugUtility.LogWarning<ResourceIntegrationTester>("IActor não configurado.");
                return;
            }
            if (!DependencyManager.Instance.TryGetForObject<ResourceSystem>(_actor.ActorId, out var svc))
            {
                DebugUtility.LogWarning<ResourceIntegrationTester>($"Serviço ResourceSystem não encontrado para ActorId: {_actor.ActorId}");
                return;
            }
            svc.Modify(ResourceType.Health, 30);
            DebugUtility.LogVerbose<ResourceIntegrationTester>("Cura de 30 aplicada.");
        }
    }
}