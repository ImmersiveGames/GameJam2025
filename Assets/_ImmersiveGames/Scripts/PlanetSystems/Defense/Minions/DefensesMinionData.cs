using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Representa um TIPO de minion de defesa para o sistema de pools (prefab, lifetime, etc.).
    /// Pode opcionalmente referenciar um DefenseMinionBehaviorProfileSO padrão para este tipo de minion.
    /// A seleção e aplicação do perfil (incluindo overrides por role ou wave) é feita em serviços externos.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DefensesMinionData",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Minions/Minion Data")]
    public class DefensesMinionData : PoolableObjectData
    {
        [Header("Comportamento padrão deste tipo de minion")]
        [Tooltip("Profile v2 (com estratégias) aplicado a todos os minions que usam este data.")]
        [SerializeField]
        private DefenseMinionBehaviorProfileSO behaviorProfileV2;

        public DefenseMinionBehaviorProfileSO BehaviorProfileV2 => behaviorProfileV2;
    }
}
