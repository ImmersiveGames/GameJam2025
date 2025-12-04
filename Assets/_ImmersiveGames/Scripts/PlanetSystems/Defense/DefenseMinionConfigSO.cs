using _ImmersiveGames.Scripts.PlanetSystems.Defense.Minions;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Descreve completamente um tipo de minion de defesa, unificando dados de pool,
    /// prefab, lifetime e perfil de comportamento em um único ScriptableObject.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DefenseMinionConfig",
        menuName = "ImmersiveGames/PlanetSystems/Defense/Minions/Minion Config")]
    public sealed class DefenseMinionConfigSO : ScriptableObject
    {
        [Header("Identidade do minion")]
        [Tooltip("Nome lógico deste minion para debugging e organização.")]
        [SerializeField]
        private string objectName;

        [Tooltip("Prefab obrigatório usado pelo PoolSystem ao instanciar o minion.")]
        [SerializeField]
        private GameObject prefab;

        [Tooltip("Tempo de vida em segundos antes do retorno automático ao pool.")]
        [SerializeField, Min(0f)]
        private float lifetime = 5f;

        [Header("Pool")]
        [Tooltip("PoolData associado a este minion. Deve conter este próprio config na lista de objetos.")]
        [SerializeField]
        private PoolData poolData;

        [Header("Comportamento")]
        [Tooltip("Perfil de comportamento aplicado a todos os minions deste tipo.")]
        [SerializeField]
        private DefenseMinionBehaviorProfileSO behaviorProfile;

        public string ObjectName => objectName;

        public GameObject Prefab => prefab;

        public float Lifetime => lifetime;

        public PoolData PoolData => poolData;

        public DefenseMinionBehaviorProfileSO BehaviorProfile => behaviorProfile;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(objectName))
            {
                DebugUtility.LogWarning<DefenseMinionConfigSO>($"ObjectName não configurado em {name}.", this);
            }

            if (prefab == null)
            {
                DebugUtility.LogError<DefenseMinionConfigSO>($"Prefab é obrigatório em {name}.", this);
            }

            if (lifetime < 0f)
            {
                DebugUtility.LogWarning<DefenseMinionConfigSO>($"Lifetime não pode ser negativo em {name}. Definindo como 0.", this);
                lifetime = 0f;
            }

            if (behaviorProfile == null)
            {
                DebugUtility.LogWarning<DefenseMinionConfigSO>($"BehaviorProfile não configurado em {name}.", this);
            }

            if (poolData == null)
            {
                DebugUtility.LogWarning<DefenseMinionConfigSO>($"PoolData não configurado em {name}.", this);
            }
        }
#endif
    }
}
