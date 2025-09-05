using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public static class ValidationService
    {
        public static bool ValidatePoolData(PoolData data, Object caller)
        {
            if (data == null || string.IsNullOrEmpty(data.ObjectName))
            {
                DebugUtility.LogError<ObjectPool>("PoolData is null or ObjectName is empty.", caller);
                return false;
            }

            if (data.InitialPoolSize <= 0)
            {
                DebugUtility.LogError<ObjectPool>($"PoolData '{data.ObjectName}' has invalid InitialPoolSize: {data.InitialPoolSize}.", caller);
                return false;
            }

            if (data.ObjectConfigs == null || data.ObjectConfigs.Length == 0)
            {
                DebugUtility.LogError<ObjectPool>($"PoolData '{data.ObjectName}' has no ObjectConfigs.", caller);
                return false;
            }

            for (int i = 0; i < data.ObjectConfigs.Length; i++)
            {
                if (!ValidatePoolableObjectData(data.ObjectConfigs[i], caller))
                {
                    DebugUtility.LogError<ObjectPool>($"Invalid PoolableObjectData at index {i} in PoolData '{data.ObjectName}'.", caller);
                    return false;
                }
            }

            DebugUtility.LogVerbose<ObjectPool>($"PoolData '{data.ObjectName}' validated successfully.", "green", caller);
            return true;
        }

        public static bool ValidatePoolableObjectData(PoolableObjectData data, Object caller)
        {
            if (data == null || string.IsNullOrEmpty(data.name))
            {
                DebugUtility.LogError<ObjectPool>("PoolableObjectData is null or name is empty.", caller);
                return false;
            }

            if (data.Prefab == null)
            {
                DebugUtility.LogError<ObjectPool>($"PoolableObjectData '{data.name}' has null Prefab.", caller);
                return false;
            }

            if (data.Lifetime <= 0)
            {
                DebugUtility.LogError<ObjectPool>($"PoolableObjectData '{data.name}' has invalid Lifetime: {data.Lifetime}.", caller);
                return false;
            }

            DebugUtility.LogVerbose<ObjectPool>($"PoolableObjectData '{data.name}' validated successfully.", "green", caller);
            return true;
        }
    }
}