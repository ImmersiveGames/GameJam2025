using UnityEngine;
namespace ImmersiveGames.RuntimeAttributes.Configs
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Resources/Resource Auto Flow Config")]
    public class RuntimeAttributeAutoFlowConfig : ScriptableObject
    {
        [Tooltip("Se verdadeiro, regenera automaticamente")]
        public bool autoFill;

        [Tooltip("Se verdadeiro, drena automaticamente")]
        public bool autoDrain;

        [Tooltip("Intervalo entre ticks em segundos")]
        [Min(0.1f)] 
        public float tickInterval = 1f;

        [Tooltip("Quantidade adicionada/removida por tick")]
        public float amountPerTick = 1f;

        [Tooltip("Se verdadeiro, usa porcentagem do valor máximo ao invés de valor fixo")]
        public bool usePercentage;

        [Tooltip("Delay após tomar dano antes de começar a regenerar (apenas para AutoFill)")]
        public float regenDelayAfterDamage;
    }
}