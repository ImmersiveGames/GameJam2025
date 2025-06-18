#if UNITY_EDITOR
using _ImmersiveGames.Scripts.SpawnSystems.DynamicPropertiesSystem;
using UnityEditor;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using System.Linq;
using _ImmersiveGames.Scripts.Utils.PoolSystems;

namespace _ImmersiveGames.Scripts.SpawnSystems.Editor
{
    [CustomEditor(typeof(SpawnPoint))]
    [CanEditMultipleObjects]
    public class SpawnPointEditor : UnityEditor.Editor
    {
        private SerializedProperty _spawnDataProp;
        private SerializedProperty _triggerDataProp;
        private SerializedProperty _strategyDataProp;
        private SerializedProperty _useManagerLockingProp;
        private SerializedProperty _inputActionAssetProp;

        private bool _showSpawnData = true;
        private bool _showTrigger = true;
        private bool _showStrategy = true;
        private bool _showRuntimeInfo = true;

        private enum TestPredicateType { AlwaysTrue, TimeGreaterThan10, NoEnemies }
        private TestPredicateType _testPredicateType = TestPredicateType.AlwaysTrue;

        private void OnEnable()
        {
            _spawnDataProp = serializedObject.FindProperty("spawnData");
            _triggerDataProp = serializedObject.FindProperty("triggerData");
            _strategyDataProp = serializedObject.FindProperty("strategyData");
            _useManagerLockingProp = serializedObject.FindProperty("useManagerLocking");
            _inputActionAssetProp = serializedObject.FindProperty("inputActionAsset");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawHeader();
            DrawGeneralSetup();
            DrawSpawnData();
            DrawTrigger();
            DrawStrategy();
            DrawRuntimeInfo();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("🔧 Spawn Point Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();
        }

        private void DrawGeneralSetup()
        {
            EditorGUILayout.PropertyField(_useManagerLockingProp, new GUIContent("Use Manager Locking", "Se verdadeiro, o SpawnManager controla os limites de spawn."));
            EditorGUILayout.PropertyField(_inputActionAssetProp, new GUIContent("Input Action Asset", "Asset de ações para InputSystemTrigger."));
            ValidateRequiredField(_spawnDataProp, "Spawn Data é obrigatório!");
        }

        private void DrawSpawnData()
        {
            _showSpawnData = EditorGUILayout.BeginFoldoutHeaderGroup(_showSpawnData, "📦 Spawn Data");
            if (_showSpawnData)
            {
                EditorGUILayout.PropertyField(_spawnDataProp);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawTrigger()
        {
            _showTrigger = EditorGUILayout.BeginFoldoutHeaderGroup(_showTrigger, "🎯 Trigger Data");
            if (_showTrigger)
            {
                EditorGUILayout.PropertyField(_triggerDataProp);
                ValidateRequiredField(_triggerDataProp, "Trigger Data é obrigatório!");
                ValidateInputTrigger();

                if (GUILayout.Button("Apply Trigger Template"))
                    ApplyTemplate(_triggerDataProp, "Trigger", typeof(EnhancedTriggerData));

                if (GUILayout.Button("Reset Trigger"))
                {
                    var spawnPoints = targets.Cast<SpawnPoint>().ToList();
                    foreach (var spawnPoint in spawnPoints)
                    {
                        spawnPoint.TriggerReset();
                        Debug.Log($"Trigger resetado para '{spawnPoint.name}' com tipo '{spawnPoint.GetTriggerData()?.triggerType}'.");
                    }
                }

                if (GUILayout.Button("Toggle Trigger"))
                {
                    var spawnPoints = targets.Cast<SpawnPoint>().ToList();
                    foreach (var spawnPoint in spawnPoints)
                    {
                        spawnPoint.SetTriggerActive(!spawnPoint.GetTriggerActive());
                        Debug.Log($"Trigger de '{spawnPoint.name}' {(spawnPoint.GetTriggerActive() ? "ativado" : "desativado")}.");
                    }
                }

                if (Application.isPlaying && GUILayout.Button("Test Spawn"))
                {
                    var spawnPoints = targets.Cast<SpawnPoint>().ToList();
                    foreach (var spawnPoint in spawnPoints)
                    {
                        EventBus<SpawnRequestEvent>.Raise(new SpawnRequestEvent(spawnPoint.GetPoolKey(), spawnPoint.gameObject));
                        Debug.Log($"Teste de spawn disparado para '{spawnPoint.name}'.");
                    }
                }

                if (Application.isPlaying && _triggerDataProp.objectReferenceValue is EnhancedTriggerData enhancedTriggerData &&
                    enhancedTriggerData.triggerType == TriggerType.GlobalEventTrigger && 
                    GUILayout.Button("Test Global Spawn Event"))
                {
                    var spawnPoints = targets.Cast<SpawnPoint>().ToList();
                    foreach (var spawnPoint in spawnPoints)
                    {
                        string eventName = enhancedTriggerData.GetProperty("eventName", "GlobalSpawnEvent");
                        EventBus<GlobalSpawnEvent>.Raise(new GlobalSpawnEvent(eventName));
                        Debug.Log($"Disparado GlobalSpawnEvent '{eventName}' para '{spawnPoint.name}'.");
                    }
                }

                if (Application.isPlaying && _triggerDataProp.objectReferenceValue is EnhancedTriggerData triggerData &&
                    triggerData.triggerType == TriggerType.PredicateTrigger)
                {
                    _testPredicateType = (TestPredicateType)EditorGUILayout.EnumPopup("Test Predicate", _testPredicateType);
                    if (GUILayout.Button("Test Predicate Trigger"))
                    {
                        var spawnPoints = targets.Cast<SpawnPoint>().ToList();
                        foreach (var spawnPoint in spawnPoints)
                        {
                            if (spawnPoint.SpawnTrigger is PredicateTrigger predicateTrigger)
                            {
                                predicateTrigger.SetPredicate(_testPredicateType switch
                                {
                                    TestPredicateType.AlwaysTrue => () => true,
                                    TestPredicateType.TimeGreaterThan10 => () => Time.time > 10f,
                                    TestPredicateType.NoEnemies => () => GameObject.FindGameObjectsWithTag("Enemy").Length == 0,
                                    _ => () => false
                                });
                                predicateTrigger.CheckTrigger(Vector3.zero, null); // Parâmetros ignorados
                                Debug.Log($"Testado PredicateTrigger para '{spawnPoint.name}' com predicado '{_testPredicateType}'.");
                            }
                        }
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawStrategy()
        {
            _showStrategy = EditorGUILayout.BeginFoldoutHeaderGroup(_showStrategy, "🧠 Strategy Data");
            if (_showStrategy)
            {
                EditorGUILayout.PropertyField(_strategyDataProp);
                ValidateRequiredField(_strategyDataProp, "Strategy Data é obrigatório!");

                if (GUILayout.Button("Apply Strategy Template"))
                    ApplyTemplate(_strategyDataProp, "Strategy", typeof(EnhancedStrategyData));
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawRuntimeInfo()
        {
            _showRuntimeInfo = EditorGUILayout.BeginFoldoutHeaderGroup(_showRuntimeInfo, "🧪 Runtime Info (Read-only)");
            if (_showRuntimeInfo && Application.isPlaying)
            {
                foreach (SpawnPoint spawnPoint in targets)
                {
                    EditorGUILayout.LabelField("Pool Key", spawnPoint.GetPoolKey() ?? "N/A");
                    EditorGUILayout.LabelField("Can Spawn", spawnPoint.IsSpawnValid() ? "Yes" : "No");
                    EditorGUILayout.LabelField("Trigger Active", spawnPoint.GetTriggerActive() ? "Yes" : "No");
                    var pool = PoolManager.Instance?.GetPool(spawnPoint.GetPoolKey());
                    EditorGUILayout.LabelField("Pool Available Objects", pool != null ? pool.GetAvailableCount().ToString() : "N/A");
                    if (spawnPoint.useManagerLocking)
                    {
                        var spawnManager = SpawnManager.Instance;
                        if (spawnManager != null)
                        {
                            EditorGUILayout.LabelField("Spawn Count", spawnManager.GetSpawnCount(spawnPoint).ToString());
                            EditorGUILayout.LabelField("Is Locked", spawnManager.IsLocked(spawnPoint) ? "Yes" : "No");
                        }
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void ValidateRequiredField(SerializedProperty property, string errorMessage)
        {
            if (property.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
            }
        }

        private void ValidateInputTrigger()
        {
            if (_triggerDataProp.objectReferenceValue is EnhancedTriggerData triggerData &&
                triggerData.triggerType == TriggerType.InputSystemTrigger &&
                _inputActionAssetProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox($"Trigger '{triggerData.triggerType}' requer um Input Action Asset configurado.", MessageType.Warning);
            }
        }

        private void ApplyTemplate(SerializedProperty property, string typeName, System.Type expectedType)
        {
            foreach (var targetObj in targets)
            {
                var so = new SerializedObject(targetObj);
                var dataProp = so.FindProperty(property.name);
                var data = dataProp.objectReferenceValue;
                if (data != null && data.GetType() == expectedType)
                {
                    if (data is EnhancedTriggerData triggerData)
                        triggerData.ApplyTemplate();
                    else if (data is EnhancedStrategyData strategyData)
                        strategyData.ApplyTemplate();
                    EditorUtility.SetDirty(data);
                }
                else
                {
                    Debug.LogWarning($"Nenhum {typeName} Data válido encontrado para aplicar template.");
                }
            }
        }
    }
}
#endif