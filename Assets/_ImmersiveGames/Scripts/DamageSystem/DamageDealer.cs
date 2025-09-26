using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.DamageSystem
{
    [DebugLevel(DebugLevel.Verbose)]
    public class DamageDealer : MonoBehaviour
    {
        [SerializeField] private float damageDeal = 20f;
        [SerializeField] public ResourceType resourceType;
        [Header("Damage Settings")]
        [SerializeField] private LayerMask damageableLayers;
        [SerializeField] private bool damageSelf = false; // Para casos específicos

        private void OnCollisionEnter(Collision other)
        {
            // Verifica se a layer do objeto colidido está na mask de layers damageáveis
            if (!IsInDamageableLayer(other.gameObject)) return;
            
            // Opcional: ainda pode verificar se não é si mesmo (dependendo da configuração)
            if (!damageSelf && other.gameObject == this.gameObject) return;
            var resource = other.gameObject.GetComponentInParent<EntityResourceSystem>();
            if(!resource) return;
            resource.ModifyResource(resourceType,damageDeal);
            DebugUtility.LogVerbose<DamageDealer>($"Dealt {damageDeal} damage to {other.gameObject.name}", "yellow");
        }
        
        private bool IsInDamageableLayer(GameObject target)
        {
            // Retorna true se a layer do target está incluída no damageableLayers
            return (damageableLayers.value & (1 << target.layer)) != 0;
        }

    }
}