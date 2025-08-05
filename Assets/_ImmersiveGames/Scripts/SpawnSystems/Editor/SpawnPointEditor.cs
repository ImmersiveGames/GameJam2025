#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using _ImmersiveGames.Scripts.SpawnSystems;
using _ImmersiveGames.Scripts.SpawnSystems.DynamicPropertiesSystem;
using _ImmersiveGames.Scripts.SpawnSystems.EventBus;
using _ImmersiveGames.Scripts.SpawnSystems.Triggers;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine.InputSystem;

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
        private bool _showSettings = true;
        private bool _showRuntimeInfo = true;

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
            DrawSettingsSection();
            DrawRuntimeSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = new Color(0.7f, 0.7f, 1f) }
            };
            EditorGUILayout.LabelField("🌟 Spawn Point Editor", headerStyle);
            EditorGUILayout.Space(5);
        }

        private void DrawSettingsSection()
        {
            _showSettings = EditorGUILayout.BeginFoldoutHeaderGroup(_showSettings, "⚙️ Settings", null, null, new GUIStyle(EditorStyles.foldoutHeader)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            });
            if (_showSettings)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.PropertyField(_useManagerLockingProp, new GUIContent("Use Manager Locking", "Controla limites de spawn pelo SpawnManager."));

                if (serializedObject.targetObject is InputSpawnPoint)
                    EditorGUILayout.PropertyField(_playerInputProp, new GUIContent("Player Input", "Componente PlayerInput para InputSystemTrigger."));

                EditorGUILayout.PropertyField(_poolableDataProp, new GUIContent("Poolable Data", "Configuração do pool de objetos."));
                ValidatePoolableData();

                EditorGUILayout.PropertyField(_triggerDataProp, new GUIContent("Trigger Data", "Configuração do gatilho de spawn."));
                ValidateTriggerData();

                EditorGUILayout.PropertyField(_strategyDataProp, new GUIContent("Strategy Data", "Estratégia de spawn."));
                ValidateRequiredField(_strategyDataProp, "Strategy Data é obrigatório!");

                DrawTriggerControls();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawTriggerControls()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("🛠️ Trigger Controls", EditorStyles.boldLabel);

            if (GUILayout.Button("Reset Trigger", GUILayout.Height(25)))
                ForEachSpawnPoint(sp => {
                    sp.TriggerReset();
                    DebugUtility.LogVerbose<SpawnPointEditor>($"Trigger resetado para '{sp.name}'.", "green", this);
                });

            if (GUILayout.Button("Toggle Trigger", GUILayout.Height(25)))
                ForEachSpawnPoint(sp => {
                    sp.SetTriggerActive(!sp.GetTriggerActive());
                    DebugUtility.LogVerbose<SpawnPointEditor>($"Trigger de '{sp.name}' {(sp.GetTriggerActive() ? "ativado" : "desativado")}.", "yellow", this);
                });

            if (Application.isPlaying)
            {
                if (GUILayout.Button("Test Spawn", GUILayout.Height(25)))
                    ForEachSpawnPoint(sp => {
                        Vector3 spawnPosition = new Vector3(sp.transform.position.x, 0f, sp.transform.position.z); // Visão top-down
                        FilteredEventBus<SpawnRequestEvent>.RaiseFiltered(new SpawnRequestEvent(sp.GetPoolKey(), sp.gameObject, spawnPosition), sp);
                        DebugUtility.LogVerbose<SpawnPointEditor>($"Teste de spawn disparado para '{sp.name}' na posição {spawnPosition}.", "blue", this);
                    });

                foreach (var obj in targets)
                {
                    if (obj is InputSpawnPoint isp && isp.GetTriggerData()?.triggerType == TriggerType.InputSystemTrigger)
                    {
                        EditorGUILayout.Space(5);
                        EditorGUILayout.LabelField($"🎮 Input Trigger for '{isp.name}'", EditorStyles.boldLabel);

                        var playerInput = isp.GetComponent<PlayerInput>();
                        string actionName = isp.GetTriggerData()?.GetProperty("actionName", "Fire") ?? "Fire";
                        EditorGUILayout.LabelField("Input Action", actionName, EditorStyles.miniLabel);

                        bool actionValid = playerInput != null && playerInput.actions != null && playerInput.actions.FindAction(actionName) != null;
                        EditorGUILayout.LabelField("Action Status", actionValid ? "✅ Valid" : "❌ Invalid", actionValid ? EditorStyles.boldLabel : EditorStyles.helpBox);

                        if (GUILayout.Button($"Test Input Trigger for '{isp.name}'", GUILayout.Height(25)))
                        {
                            if (actionValid)
                            {
                                Vector3 spawnPosition = new Vector3(isp.transform.position.x, 0f, isp.transform.position.z); // Visão top-down
                                FilteredEventBus<SpawnRequestEvent>.RaiseFiltered(new SpawnRequestEvent(isp.GetPoolKey(), isp.gameObject, spawnPosition), isp);
                                DebugUtility.LogVerbose<SpawnPointEditor>($"Teste de InputSystemTrigger disparado para '{isp.name}' com ação '{actionName}' na posição {spawnPosition}.", "cyan", this);
                            }
                            else
                            {
                                DebugUtility.LogWarning<SpawnPointEditor>($"Não foi possível testar InputSystemTrigger para '{isp.name}'. Verifique PlayerInput e actionName '{actionName}'.", this);
                            }
                        }
                    }
                }
            }
        }

        private void DrawRuntimeSection()
        {
            _showRuntimeInfo = EditorGUILayout.BeginFoldoutHeaderGroup(_showRuntimeInfo, "📊 Runtime Info", null, null, new GUIStyle(EditorStyles.foldoutHeader)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            });
            if (_showRuntimeInfo && Application.isPlaying)
            {
                foreach (var obj in targets)
                {
                    if (obj is SpawnPoint sp)
                    {
                        EditorGUILayout.Space(5);
                        EditorGUILayout.LabelField($"🔍 {sp.name}", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField("Pool Key", sp.GetPoolKey() ?? "N/A", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField("Can Spawn", sp.IsSpawnValid ? "✅ Yes" : "❌ No", sp.IsSpawnValid ? EditorStyles.boldLabel : EditorStyles.helpBox);
                        EditorGUILayout.LabelField("Trigger Active", sp.GetTriggerActive() ? "✅ Yes" : "❌ No", sp.GetTriggerActive() ? EditorStyles.boldLabel : EditorStyles.helpBox);

                        ObjectPool pool = PoolManager.Instance?.GetPool(sp.GetPoolKey());
                        EditorGUILayout.LabelField("Pool Status", pool != null && pool.IsInitialized ? "✅ Initialized" : "❌ Not Initialized", pool != null && pool.IsInitialized ? EditorStyles.boldLabel : EditorStyles.helpBox);
                        EditorGUILayout.LabelField("Available Objects", pool?.GetAvailableCount().ToString() ?? "N/A", EditorStyles.miniLabel);
                        //EditorGUILayout.LabelField("Can Expand", sp.GetPoolableData()?.CanExpand.ToString() ?? "N/A", EditorStyles.miniLabel);

                        if (sp.useManagerLocking && SpawnManager.Instance != null)
                        {
                            var sm = SpawnManager.Instance;
                            EditorGUILayout.LabelField("Spawn Count", sm.GetSpawnCount(sp).ToString(), EditorStyles.miniLabel);
                            EditorGUILayout.LabelField("Locked", sm.IsLocked(sp) ? "🔒 Yes" : "🔓 No", sm.IsLocked(sp) ? EditorStyles.helpBox : EditorStyles.boldLabel);
                        }
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

        private void ValidatePoolableData()
        {
            foreach (var obj in targets)
            {
                if (obj is SpawnPoint sp)
                {
                    var poolableData = sp.GetPoolableData();
                    if (poolableData == null)
                    {
                        EditorGUILayout.HelpBox("⚠️ PoolableObjectData é nulo. Configure um PoolableObjectData válido.", MessageType.Error);
                        continue;
                    }

                    if (string.IsNullOrEmpty(poolableData.ObjectName))
                        EditorGUILayout.HelpBox("⚠️ ObjectName está vazio em PoolableObjectData. Defina um nome único para o pool.", MessageType.Error);
                    if (poolableData.Prefab == null)
                        EditorGUILayout.HelpBox("⚠️ Prefab está nulo em PoolableObjectData.", MessageType.Error);
                    /*if (poolableData.ModelPrefab == null)
                        EditorGUILayout.HelpBox("⚠️ ModelPrefab está nulo em PoolableObjectData.", MessageType.Error);*/
                    /*if (poolableData.InitialPoolSize <= 0)
                        EditorGUILayout.HelpBox("⚠️ InitialPoolSize deve ser maior que 0 em PoolableObjectData.", MessageType.Warning);*/
                }
            }
        }

        private void ValidateTriggerData()
        {
            foreach (var obj in targets)
            {
                if (obj is SpawnPoint sp)
                {
                    var triggerData = sp.GetTriggerData();
                    if (triggerData == null)
                    {
                        EditorGUILayout.HelpBox("⚠️ Trigger Data é nulo. Configure um EnhancedTriggerData válido.", MessageType.Error);
                        continue;
                    }

                    if (triggerData.triggerType == TriggerType.InputSystemTrigger)
                    {
                        if (!(sp is InputSpawnPoint))
                            EditorGUILayout.HelpBox($"⚠️ InputSystemTrigger só pode ser usado com InputSpawnPoint, não {sp.GetType().Name}.", MessageType.Error);
                        else if (_playerInputProp != null && _playerInputProp.objectReferenceValue == null)
                            EditorGUILayout.HelpBox("⚠️ InputSystemTrigger requer um componente PlayerInput atribuído.", MessageType.Warning);
                        else
                        {
                            string actionName = triggerData.GetProperty("actionName", "Fire");
                            var isp = sp as InputSpawnPoint;
                            var playerInput = isp.GetComponent<PlayerInput>();
                            if (playerInput != null && playerInput.actions != null)
                            {
                                var action = playerInput.actions.FindAction(actionName);
                                if (action == null)
                                    EditorGUILayout.HelpBox($"⚠️ Ação '{actionName}' não encontrada no InputActionAsset do PlayerInput.", MessageType.Error);
                            }
                            else
                            {
                                EditorGUILayout.HelpBox("⚠️ PlayerInput ou InputActionAsset não configurado corretamente.", MessageType.Error);
                            }
                        }
                    }
                }
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