using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Configuração de um TIPO de minion de defesa para o PoolSystem.
    ///
    /// - Herdado de PoolableObjectData (lifetime, prefab, etc.)
    /// - Agora também referencia um DefenseMinionBehaviorProfileSO,
    ///   que descreve o comportamento padrão deste minion (com estratégias),
    ///   e mantém o DefenseMinionBehaviorProfile como fallback legado.
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

        [Tooltip("Profile legado (sem estratégia) mantido como fallback para compatibilidade.")]
        [SerializeField]
        private DefenseMinionBehaviorProfile defaultProfile;

        public DefenseMinionBehaviorProfileSO BehaviorProfileV2 => behaviorProfileV2;
        public DefenseMinionBehaviorProfile DefaultProfile => defaultProfile;
    }
}