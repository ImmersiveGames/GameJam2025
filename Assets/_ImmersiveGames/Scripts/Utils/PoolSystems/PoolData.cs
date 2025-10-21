using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    [CreateAssetMenu(fileName = "PoolData", menuName = "ImmersiveGames/PoolData")]
    public class PoolData : ScriptableObject
    {
        [SerializeField] private string objectName;
        [SerializeField]
        protected int initialPoolSize = 5;
        [SerializeField] private bool canExpand;
        [SerializeField] private PoolableObjectData[] objectConfigs;
        [SerializeField] private bool reconfigureOnReturn = true;

        public string ObjectName => objectName;
        public int InitialPoolSize => initialPoolSize;
        public bool CanExpand { get => canExpand; set => canExpand = value; }
        public PoolableObjectData[] ObjectConfigs => objectConfigs;
        public bool ReconfigureOnReturn => reconfigureOnReturn;

        public static bool Validate(PoolData data, Object caller)
        {
            if (data == null || string.IsNullOrEmpty(data.ObjectName))
            {
                DebugUtility.LogError<ObjectPool>($"PoolData is null or ObjectName is empty in '{caller?.name ?? "Unknown"}'.", caller);
                return false;
            }

            if (data.InitialPoolSize <= 0)
            {
                DebugUtility.LogError<ObjectPool>($"PoolData '{data.ObjectName}' has invalid InitialPoolSize: {data.InitialPoolSize} in '{caller?.name ?? "Unknown"}'.", caller);
                return false;
            }

            if (data.ObjectConfigs == null || data.ObjectConfigs.Length == 0)
            {
                DebugUtility.LogError<ObjectPool>($"PoolData '{data.ObjectName}' has no ObjectConfigs in '{caller?.name ?? "Unknown"}'.", caller);
                return false;
            }

            for (int i = 0; i < data.ObjectConfigs.Length; i++)
            {
                if (!ValidatePoolableObjectData(data.ObjectConfigs[i], caller))
                {
                    DebugUtility.LogError<ObjectPool>($"Invalid PoolableObjectData at index {i} in PoolData '{data.ObjectName}' in '{caller?.name ?? "Unknown"}'.", caller);
                    return false;
                }
            }

            DebugUtility.LogVerbose<ObjectPool>($"PoolData '{data.ObjectName}' validated successfully in '{caller?.name ?? "Unknown"}'.", "green", caller);
            return true;
        }

        private static bool ValidatePoolableObjectData(PoolableObjectData data, Object caller)
        {
            if (data == null || string.IsNullOrEmpty(data.name))
            {
                DebugUtility.LogError<ObjectPool>($"PoolableObjectData is null or name is empty in '{caller?.name ?? "Unknown"}'.", caller);
                return false;
            }

            if (data.Prefab == null)
            {
                DebugUtility.LogError<ObjectPool>($"PoolableObjectData '{data.name}' has null Prefab in '{caller?.name ?? "Unknown"}'.", caller);
                return false;
            }

            if (data.Lifetime <= 0)
            {
                DebugUtility.LogError<ObjectPool>($"PoolableObjectData '{data.name}' has invalid Lifetime: {data.Lifetime} in '{caller?.name ?? "Unknown"}'.", caller);
                return false;
            }

            DebugUtility.LogVerbose<ObjectPool>($"PoolableObjectData '{data.name}' validated successfully in '{caller?.name ?? "Unknown"}'.", "green", caller);
            return true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(objectName))
            {
                DebugUtility.LogWarning<ObjectPool>($"ObjectName não configurado em {name}.", this);
            }
            if (initialPoolSize < 0)
            {
                DebugUtility.LogWarning<ObjectPool>($"InitialPoolSize não pode ser negativo em {name}. Definindo como 0.", this);
                initialPoolSize = 0;
            }
            if (objectConfigs == null || objectConfigs.Length == 0)
            {
                DebugUtility.LogWarning<ObjectPool>($"ObjectConfigs não configurado em {name}. Pelo menos uma configuração é necessária.", this);
            }
        }
#endif
    }
}