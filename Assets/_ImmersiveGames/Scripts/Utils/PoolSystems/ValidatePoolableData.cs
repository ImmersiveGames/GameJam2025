using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    public static class PoolValidationUtility
    {
        public static bool ValidatePoolData(PoolData data, MonoBehaviour context)
        {
            if (data == null)
            {
                DebugUtility.LogError(context.GetType(), "PoolData é nulo.");
                return false;
            }

            if (string.IsNullOrEmpty(data.ObjectName))
            {
                DebugUtility.LogError(context.GetType(), $"ObjectName não configurado em {data.name}.");
                return false;
            }

            if (data.InitialPoolSize < 0)
            {
                DebugUtility.LogError(context.GetType(), $"InitialPoolSize não pode ser negativo em {data.name}.");
                return false;
            }

            if (data.ObjectConfigs == null || data.ObjectConfigs.Length == 0)
            {
                DebugUtility.LogError(context.GetType(), $"ObjectConfigs não configurado em {data.name}.");
                return false;
            }

            foreach (var config in data.ObjectConfigs)
            {
                if (!ValidatePoolableObjectData(config, context, data.name))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool ValidatePoolableObjectData(PoolableObjectData data, MonoBehaviour context, string parentName = null)
        {
            if (data == null)
            {
                DebugUtility.LogError(context.GetType(), $"PoolableObjectData é nulo{(parentName != null ? $" em {parentName}" : "")}.");
                return false;
            }

            if (data.Prefab == null)
            {
                DebugUtility.LogError(context.GetType(), $"Prefab não configurado em {data.name}{(parentName != null ? $" (usado em {parentName})" : "")}.");
                return false;
            }

            if (data.Prefab.GetComponent<IPoolable>() == null)
            {
                DebugUtility.LogError(context.GetType(), $"Prefab {data.Prefab.name} em {data.name}{(parentName != null ? $" (usado em {parentName})" : "")} não possui componente IPoolable.");
                return false;
            }

            if (data.Lifetime < 0)
            {
                DebugUtility.LogError(context.GetType(), $"Lifetime não pode ser negativo em {data.name}{(parentName != null ? $" (usado em {parentName})" : "")}.");
                return false;
            }

            return true;
        }

        public static bool ValidatePoolKey(string key, MonoBehaviour context)
        {
            if (string.IsNullOrEmpty(key))
            {
                DebugUtility.LogError(context.GetType(), "Pool key é nulo ou vazio.");
                return false;
            }
            return true;
        }
    }
}