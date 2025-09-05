using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Editor
{
    [CustomEditor(typeof(SpawnSystem))]
    public class SpawnSystemEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Exibir a lista de poolConfigs
            var poolConfigsProp = serializedObject.FindProperty("poolConfigs");
            EditorGUILayout.PropertyField(poolConfigsProp, new GUIContent("Pool Configurations"), true);

            // Adicionar botão para validar configurações
            if (GUILayout.Button("Validate Configurations"))
            {
                ValidateConfigurations();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void ValidateConfigurations()
        {
            var spawnSystem = (SpawnSystem)target;
            var poolConfigs = spawnSystem.GetType().GetField("poolConfigs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(spawnSystem) as List<SpawnSystem.PoolConfig>;

            if (poolConfigs == null || poolConfigs.Count == 0)
            {
                Debug.LogWarning("No PoolConfigs assigned in SpawnSystem.");
                return;
            }

            bool hasErrors = false;
            for (int i = 0; i < poolConfigs.Count; i++)
            {
                var config = poolConfigs[i];
                if (config.poolData == null)
                {
                    Debug.LogError($"PoolConfig {i}: PoolData is null.");
                    hasErrors = true;
                    continue;
                }

                if (!SpawnTriggerFactory.SupportedTriggerTypes.Contains(config.triggerConfig.type, System.StringComparer.OrdinalIgnoreCase))
                {
                    Debug.LogError($"PoolConfig {i} (Pool: {config.poolData.ObjectName}): Invalid trigger type '{config.triggerConfig.type}'. Supported types: {string.Join(", ", SpawnTriggerFactory.SupportedTriggerTypes)}.");
                    hasErrors = true;
                }

                if (!SpawnStrategyFactory.SupportedStrategyTypes.Contains(config.strategyConfig.type, System.StringComparer.OrdinalIgnoreCase))
                {
                    Debug.LogError($"PoolConfig {i} (Pool: {config.poolData.ObjectName}): Invalid strategy type '{config.strategyConfig.type}'. Supported types: {string.Join(", ", SpawnStrategyFactory.SupportedStrategyTypes)}.");
                    hasErrors = true;
                }

                if (config.strategyConfig.spawnCount < 1)
                {
                    Debug.LogError($"PoolConfig {i} (Pool: {config.poolData.ObjectName}): Invalid spawnCount {config.strategyConfig.spawnCount}. Must be at least 1.");
                    hasErrors = true;
                }

                if (config.strategyConfig.type.ToLower() == "circular" && config.strategyConfig.radius < 0.1f)
                {
                    Debug.LogError($"PoolConfig {i} (Pool: {config.poolData.ObjectName}): Invalid radius {config.strategyConfig.radius}. Must be at least 0.1.");
                    hasErrors = true;
                }
            }

            if (!hasErrors)
            {
                Debug.Log("All PoolConfigs are valid.");
            }
        }
    }
}