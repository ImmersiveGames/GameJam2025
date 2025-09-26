using UnityEngine;
namespace _ImmersiveGames.Scripts.ResourceSystems.Configs
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Resources/Resource Auto Flow Config")]
    public class ResourceAutoFlowConfig : ScriptableObject
    {
        [Tooltip("Qual recurso este config controla")]
        public ResourceType resourceType = ResourceType.Health;

        [Tooltip("Se verdadeiro, regenera até 100%")]
        public bool autoFill = false;

        [Tooltip("Se verdadeiro, drena até 0%")]
        public bool autoDrain = false;

        [Tooltip("Intervalo entre ticks em segundos")]
        [Min(0.1f)] public float tickInterval = 1f;

        [Tooltip("Quantidade adicionada/removida por tick")]
        public int amountPerTick = 1;

        [Tooltip("Se verdadeiro, usa porcentagem do valor máximo ao invés de valor fixo")]
        public bool usePercentage = false;

        [Tooltip("Delay após tomar dano antes de começar a regenerar (apenas para AutoFill)")]
        public float regenDelayAfterDamage = 0f;
    }
}