#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using _ImmersiveGames.Scripts.SpawnSystems.Triggers;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.SpawnSystems.Editor
{
    [CustomEditor(typeof(SpawnPoint), true)]
    [CanEditMultipleObjects, DebugLevel(DebugLevel.Error)]
    public class SpawnPointEditor : UnityEditor.Editor
    {
        private SerializedProperty _poolableDataProp;
        private SerializedProperty _triggerDataProp;
        private SerializedProperty _strategyDataProp;
        private SerializedProperty _useManagerLockingProp;
        private SerializedProperty _playerInputProp;
        private bool _showPoolableData = true;
        private bool _showTrigger = true;
        private bool _showStrategy = true;
        private bool _showRuntimeInfo = true;

        private enum TestPredicateType { AlwaysTrue, TimeGreaterThan10, NoEnemies }
        private TestPredicateType _testPredicateType = TestPredicateType.AlwaysTrue;

        // Campos para teste de GlobalEventTrigger
        private Vector3 _testEventPosition = Vector3.zero;
        private GameObject _testEventSourceObject = null;

        private void OnEnable()
        {
            _poolableDataProp = serializedObject.FindProperty("poolableData");
            _triggerDataProp = serializedObject.FindProperty("triggerData");
            _strategyDataProp = serializedObject.FindProperty("strategyData");
            _useManagerLockingProp = serializedObject.FindProperty("useManagerLocking");
            _playerInputProp = serializedObject.FindProperty("playerInput");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawHeader();
            DrawGeneralSettings();
            DrawPoolableSection();
            DrawTriggerSection();
            DrawStrategySection();
            DrawRuntimeSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("🔧 Spawn Point Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();
        }

        private void DrawGeneralSettings()
        {
            EditorGUILayout.PropertyField(_useManagerLockingProp, new GUIContent("Use Manager Locking", "Se verdadeiro, o SpawnManager controla os limites de spawn."));

            if (_playerInputProp != null)
                EditorGUILayout.PropertyField(_playerInputProp, new GUIContent("Player Input", "Componente PlayerInput necessário para triggers do tipo InputSystem."));

            ValidateRequiredField(_poolableDataProp, "Poolable Object Data é obrigatório!");
        }

        private void DrawPoolableSection()
        {
            _showPoolableData = EditorGUILayout.BeginFoldoutHeaderGroup(_showPoolableData, "📦 Poolable Object Data");
            if (_showPoolableData)
                EditorGUILayout.PropertyField(_poolableDataProp);
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawTriggerSection()
        {
            _showTrigger = EditorGUILayout.BeginFoldoutHeaderGroup(_showTrigger, "🎯 Trigger Data");
            if (_showTrigger)
            {
                EditorGUILayout.PropertyField(_triggerDataProp);
                ValidateRequiredField(_triggerDataProp, "Trigger Data é obrigatório!");
                ValidateTriggerType();

                DrawTriggerButtons();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawTriggerButtons()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Trigger Test Controls", EditorStyles.boldLabel);

            if (GUILayout.Button("Apply Trigger Template"))
                ApplyTemplate(_triggerDataProp, "Trigger", typeof(EnhancedTriggerData));

            if (GUILayout.Button("Reset Trigger"))
                ForEachSpawnPoint(sp => {
                    sp.TriggerReset();
                    DebugUtility.LogVerbose<SpawnPointEditor>($"Trigger resetado para '{sp.name}' com tipo '{sp.GetTriggerData()?.triggerType}'.");
                });

            if (GUILayout.Button("Toggle Trigger"))
                ForEachSpawnPoint(sp => {
                    sp.SetTriggerActive(!sp.GetTriggerActive());
                    DebugUtility.LogVerbose<SpawnPointEditor>($"Trigger de '{sp.name}' {(sp.GetTriggerActive() ? "ativado" : "desativado")}.");
                });

            if (Application.isPlaying)
            {
                if (GUILayout.Button("Test Spawn"))
                    ForEachSpawnPoint(sp => {
                        FilteredEventBus.RaiseFiltered(new SpawnRequestEvent(sp.GetPoolKey(), sp.gameObject, sp.transform.position));
                        DebugUtility.LogVerbose<SpawnPointEditor>($"Teste de spawn disparado para '{sp.name}'.");
                    });

                // Itera sobre cada SpawnPoint para exibir botões específicos
                foreach (var obj in targets)
                {
                    if (obj is SpawnPoint sp)
                    {
                        var triggerData = sp.GetTriggerData();
                        if (triggerData == null) continue;

                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField($"Trigger Controls for '{sp.name}' ({triggerData.triggerType})", EditorStyles.boldLabel);

                        if (triggerData.triggerType == TriggerType.GlobalEventTrigger)
                        {
                            _testEventPosition = EditorGUILayout.Vector3Field("Event Position", _testEventPosition);
                            _testEventSourceObject = (GameObject)EditorGUILayout.ObjectField("Event Source Object", _testEventSourceObject, typeof(GameObject), true);

                            if (GUILayout.Button($"Test Global Event for '{sp.name}'"))
                            {
                                string eventName = triggerData.GetProperty("eventName", "GlobalSpawnEvent");
                                EventBus<ISpawnEvent>.Raise(new GlobalSpawnEvent(eventName, _testEventPosition, _testEventSourceObject));
                                DebugUtility.LogVerbose<SpawnPointEditor>($"Disparado GlobalSpawnEvent '{eventName}' para '{sp.name}' na posição {_testEventPosition} com sourceObject {(_testEventSourceObject != null ? _testEventSourceObject.name : "null")}.");
                            }
                        }
                        else if (triggerData.triggerType == TriggerType.GenericGlobalEventTrigger)
                        {
                            if (GUILayout.Button($"Test Generic Global Event for '{sp.name}'"))
                            {
                                string eventName = triggerData.GetProperty("eventName", "GlobalGenericSpawnEvent");
                                EventBus<GlobalGenericSpawnEvent>.Raise(new GlobalGenericSpawnEvent(eventName));
                                DebugUtility.LogVerbose<SpawnPointEditor>($"Disparado GlobalGenericSpawnEvent '{eventName}' para '{sp.name}'.");
                            }
                        }
                        else if (triggerData.triggerType == TriggerType.PredicateTrigger)
                        {
                            _testPredicateType = (TestPredicateType)EditorGUILayout.EnumPopup("Test Predicate", _testPredicateType);
                            if (GUILayout.Button($"Test Predicate Trigger for '{sp.name}'"))
                            {
                                Func<SpawnPoint, bool> predicate = _testPredicateType switch
                                {
                                    TestPredicateType.AlwaysTrue => (_) => true,
                                    TestPredicateType.TimeGreaterThan10 => (_) => Time.time > 10f,
                                    TestPredicateType.NoEnemies => (_) => GameObject.FindGameObjectsWithTag("Enemy").Length == 0,
                                    _ => (_) => false
                                };

                                if (sp.SpawnTrigger is PredicateTrigger concreteTrigger)
                                {
                                    concreteTrigger.SetPredicate(predicate);
                                    concreteTrigger.CheckTrigger(out _, out _);
                                    DebugUtility.LogVerbose<SpawnPointEditor>($"Testado PredicateTrigger para '{sp.name}' com predicado '{_testPredicateType}'.");
                                }
                                else
                                {
                                    Debug.LogWarning($"'{sp.name}' não possui PredicateTrigger concreto.");
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DrawStrategySection()
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

        private void DrawRuntimeSection()
        {
            _showRuntimeInfo = EditorGUILayout.BeginFoldoutHeaderGroup(_showRuntimeInfo, "🧪 Runtime Info (Read-only)");
            if (_showRuntimeInfo && Application.isPlaying)
            {
                foreach (var o in targets)
                {
                    var sp = (SpawnPoint)o;
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField($"🔍 {sp.name}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Pool Key", sp.GetPoolKey() ?? "N/A");
                    EditorGUILayout.LabelField("Can Spawn", sp.IsSpawnValid ? "✔️ Yes" : "❌ No");
                    EditorGUILayout.LabelField("Trigger Active", sp.GetTriggerActive() ? "✔️ Yes" : "❌ No");

                    ObjectPool pool = PoolManager.Instance?.GetPool(sp.GetPoolKey());
                    EditorGUILayout.LabelField("Pool Available", pool?.GetAvailableCount().ToString() ?? "N/A");

                    if (sp.useManagerLocking && SpawnManager.Instance != null)
                    {
                        var sm = SpawnManager.Instance;
                        EditorGUILayout.LabelField("Spawn Count", sm.GetSpawnCount(sp).ToString());
                        EditorGUILayout.LabelField("Is Locked", sm.IsLocked(sp) ? "🔒 Yes" : "🔓 No");
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void ValidateRequiredField(SerializedProperty property, string message)
        {
            if (property.objectReferenceValue == null)
                EditorGUILayout.HelpBox(message, MessageType.Error);
        }

        private void ValidateTriggerType()
        {
            foreach (var obj in targets)
            {
                if (obj is SpawnPoint sp)
                {
                    var triggerData = sp.GetTriggerData();
                    if (triggerData == null) continue;

                    if (triggerData.triggerType == TriggerType.InputSystemTrigger)
                    {
                        if (!(sp is InputSpawnPoint))
                            EditorGUILayout.HelpBox($"⚠️ InputSystemTrigger só pode ser usado com InputSpawnPoint, não {sp.GetType().Name}.", MessageType.Error);
                        else if (_playerInputProp != null && _playerInputProp.objectReferenceValue == null)
                            EditorGUILayout.HelpBox("⚠️ InputSystemTrigger requer um componente PlayerInput atribuído.", MessageType.Warning);
                    }
                    else if (triggerData.triggerType == TriggerType.GlobalEventTrigger || 
                             triggerData.triggerType == TriggerType.GenericGlobalEventTrigger)
                    {
                        string eventName = triggerData.GetProperty("eventName", "");
                        if (string.IsNullOrEmpty(eventName))
                            EditorGUILayout.HelpBox($"⚠️ {triggerData.triggerType} requer um eventName não vazio.", MessageType.Error);
                    }
                }
            }
        }

        private void ApplyTemplate(SerializedProperty property, string label, Type expectedType)
        {
            foreach (UnityEngine.Object targetObj in targets)
            {
                var so = new SerializedObject(targetObj);
                var dataProp = so.FindProperty(property.name);
                var data = dataProp.objectReferenceValue;

                if (data == null || data.GetType() != expectedType)
                {
                    DebugUtility.LogWarning<SpawnPointEditor>($"⚠️ Nenhum {label} Data válido encontrado para aplicar template.");
                    continue;
                }

                if (data is EnhancedTriggerData td) td.ApplyTemplate();
                else if (data is EnhancedStrategyData sd) sd.ApplyTemplate();

                EditorUtility.SetDirty(data);
            }
        }

        private void ForEachSpawnPoint(Action<SpawnPoint> action)
        {
            foreach (var obj in targets)
            {
                if (obj is SpawnPoint sp)
                    action.Invoke(sp);
            }
        }
    }
}
#endif