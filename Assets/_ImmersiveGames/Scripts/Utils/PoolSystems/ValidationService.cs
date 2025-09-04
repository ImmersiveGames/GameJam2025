using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public static class ValidationService
    {
        private static readonly System.Collections.Generic.Dictionary<string, bool> _cache = new();
        private static float _lastValidationTime;

        public static bool ValidatePoolData(PoolData data, Object caller)
        {
            if (data == null || string.IsNullOrEmpty(data.ObjectName))
            {
                DebugUtility.LogError<ObjectPool>("PoolData is null or ObjectName is empty.", caller);
                return false;
            }

            string cacheKey = data.ObjectName;
            if (_cache.TryGetValue(cacheKey, out bool cachedResult) && Time.time == _lastValidationTime)
            {
                DebugUtility.LogVerbose<ObjectPool>($"PoolData '{data.ObjectName}' validated successfully and cached.", "green", caller);
                return cachedResult;
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

            bool allConfigsValid = true;
            for (int i = 0; i < data.ObjectConfigs.Length; i++)
            {
                if (!ValidatePoolableObjectData(data.ObjectConfigs[i], caller))
                {
                    DebugUtility.LogError<ObjectPool>($"Invalid PoolableObjectData at index {i} in PoolData '{data.ObjectName}'.", caller);
                    allConfigsValid = false;
                }
            }

            if (!allConfigsValid)
            {
                return false;
            }

            _cache[cacheKey] = true;
            _lastValidationTime = Time.time;
            DebugUtility.LogVerbose<ObjectPool>($"PoolData '{data.ObjectName}' validated successfully and cached.", "green", caller);
            return true;
        }

        public static bool ValidatePoolableObjectData(PoolableObjectData data, Object caller)
        {
            if (data == null || string.IsNullOrEmpty(data.name))
            {
                DebugUtility.LogError<ObjectPool>("PoolableObjectData is null or name is empty.", caller);
                return false;
            }

            string cacheKey = data.name;
            if (_cache.TryGetValue(cacheKey, out bool cachedResult) && Time.time == _lastValidationTime)
            {
                DebugUtility.LogVerbose<ObjectPool>($"PoolableObjectData '{data.name}' validated successfully and cached.", "green", caller);
                return cachedResult;
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

            _cache[cacheKey] = true;
            _lastValidationTime = Time.time;
            DebugUtility.LogVerbose<ObjectPool>($"PoolableObjectData '{data.name}' validated successfully and cached.", "green", caller);
            return true;
        }

        public static void ClearCache()
        {
            _cache.Clear();
            if (Time.time > 0)
            {
                DebugUtility.LogVerbose<ObjectPool>("Validation cache cleared.", "cyan", null);
            }
        }
    }
}