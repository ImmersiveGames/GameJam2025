using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Configuração completa de um tipo de minion de defesa.
    /// Agrega PoolData (prefab, lifetime, pool size) e o profile de comportamento
    /// para que designers escolham um único asset por tipo de inimigo.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DefenseMinionConfig",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Minions/Minion Config")]
    public sealed class DefenseMinionConfigSo : ScriptableObject
    {
        [Header("Pool de minion")]
        [Tooltip("PoolData usado para registrar e spawnar este tipo de minion.")]
        [SerializeField]
        private PoolData poolData;

        [Header("Profile de comportamento")]
        [Tooltip("Perfil de comportamento padrão aplicado ao minion ao spawnar.")]
        [SerializeField]
        private DefenseMinionBehaviorProfileSO behaviorProfile;

        [Header("LEGACY")]
        [Tooltip("LEGACY: mantido apenas para compatibilidade com DefensesMinionData existentes.")]
        [SerializeField]
        private DefensesMinionData legacyMinionData;

        public PoolData PoolData => poolData;
        public DefenseMinionBehaviorProfileSO BehaviorProfile => behaviorProfile ?? legacyMinionData?.BehaviorProfileV2;
        public DefensesMinionData LegacyMinionData => legacyMinionData;
    }
}
