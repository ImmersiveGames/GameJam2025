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
            string cacheKey = data.name + "_" + data.GetInstanceID();
            if (_cache.TryGetValue(cacheKey, out bool cachedResult) && Time.time == _lastValidationTime)
            {
                DebugUtility.LogVerbose<ObjectPool>($"PoolData '{data.name}' (ObjectName: {data.ObjectName}) validated successfully and cached.", "green", caller);
                return cachedResult;
            }

            if (data == null)
            {
                DebugUtility.LogError<ObjectPool>($"PoolData is null.", caller);
                return false;
            }

            if (string.IsNullOrEmpty(data.ObjectName))
            {
                DebugUtility.LogError<ObjectPool>($"PoolData '{data.name}' has empty ObjectName.", caller);
                return false;
            }

            if (data.InitialPoolSize <= 0)
            {
                DebugUtility.LogError<ObjectPool>($"PoolData '{data.name}' has invalid InitialPoolSize: {data.InitialPoolSize}.", caller);
                return false;
            }

            if (data.ObjectConfigs == null || data.ObjectConfigs.Length == 0)
            {
                DebugUtility.LogError<ObjectPool>($"PoolData '{data.name}' has no ObjectConfigs.", caller);
                return false;
            }

            bool allConfigsValid = true;
            for (int i = 0; i < data.ObjectConfigs.Length; i++)
            {
                if (!ValidatePoolableObjectData(data.ObjectConfigs[i], caller))
                {
                    DebugUtility.LogError<ObjectPool>($"Invalid PoolableObjectData at index {i} in PoolData '{data.name}'.", caller);
                    allConfigsValid = false;
                }
            }

            if (!allConfigsValid)
            {
                return false;
            }

            _cache[cacheKey] = true;
            _lastValidationTime = Time.time;
            DebugUtility.LogVerbose<ObjectPool>($"PoolData '{data.name}' (ObjectName: {data.ObjectName}) validated successfully and cached.", "green", caller);
            return true;
        }

        public static bool ValidatePoolableObjectData(PoolableObjectData data, Object caller)
        {
            string cacheKey = data.name + "_" + data.GetInstanceID();
            if (_cache.TryGetValue(cacheKey, out bool cachedResult) && Time.time == _lastValidationTime)
            {
                DebugUtility.LogVerbose<ObjectPool>($"PoolableObjectData '{data.name}' validated successfully and cached.", "green", caller);
                return cachedResult;
            }

            if (data == null)
            {
                DebugUtility.LogError<ObjectPool>("PoolableObjectData is null.", caller);
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