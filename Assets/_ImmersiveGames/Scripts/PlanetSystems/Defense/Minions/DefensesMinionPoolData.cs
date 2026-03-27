using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems.Defense.Minions
{
    /// <summary>
    /// Representa um TIPO de minion de defesa para o sistema de pools (prefab, lifetime, etc.).
    /// Pode opcionalmente referenciar um DefenseMinionBehaviorProfileSO padrÃ£o para este tipo de minion.
    /// A seleÃ§Ã£o e aplicaÃ§Ã£o do perfil (incluindo overrides por role ou wave) Ã© feita em serviÃ§os externos.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DefensesMinionPoolData",
        menuName = "ImmersiveGames/Legacy/PlanetSystems/Defense/Minions/Minion Data")]
    public class DefensesMinionPoolData : PoolableObjectData
    {
        [Header("Comportamento padrÃ£o deste tipo de minion")]
        [Tooltip("Profile v2 (com estratÃ©gias) aplicado a todos os minions que usam este data.")]
        [SerializeField]
        private DefenseMinionBehaviorProfileSo behaviorProfileV2;

        public DefenseMinionBehaviorProfileSo BehaviorProfileV2 => behaviorProfileV2;
    }
}
