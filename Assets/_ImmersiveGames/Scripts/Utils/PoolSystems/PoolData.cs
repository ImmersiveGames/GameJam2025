using UnityEngine;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    [CreateAssetMenu(fileName = "PoolData", menuName = "ImmersiveGames/PoolData")]
    public class PoolData : ScriptableObject
    {
        [SerializeField] private string objectName;
        [SerializeField] private int initialPoolSize = 5;
        [SerializeField] private bool canExpand;
        [SerializeField] private PoolableObjectData[] objectConfigs; // Configurações variadas para objetos no pool
        [SerializeField] private bool reconfigureOnReturn = true;
        public string ObjectName => objectName;
        public int InitialPoolSize => initialPoolSize;
        public bool CanExpand
        {
            get => canExpand;
            set => canExpand = value;
        }
        public PoolableObjectData[] ObjectConfigs => objectConfigs;
        public bool ReconfigureOnReturn => reconfigureOnReturn;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(objectName))
            {
                Debug.LogWarning($"ObjectName não configurado em {name}.", this);
            }
            if (initialPoolSize < 0)
            {
                Debug.LogWarning($"InitialPoolSize não pode ser negativo em {name}. Definindo como 0.", this);
                initialPoolSize = 0;
            }
            if (objectConfigs == null || objectConfigs.Length == 0)
            {
                Debug.LogWarning($"ObjectConfigs não configurado em {name}. Pelo menos uma configuração é necessária.", this);
            }
        }
#endif
    }
}