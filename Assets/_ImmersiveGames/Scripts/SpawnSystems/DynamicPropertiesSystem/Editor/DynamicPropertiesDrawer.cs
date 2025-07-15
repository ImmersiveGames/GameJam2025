#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.SpawnSystems.Data;
using _ImmersiveGames.Scripts.SpawnSystems.Triggers;
using DG.Tweening;
using _ImmersiveGames.Scripts.Utils.DebugSystems; // Adicionado para DebugUtility

namespace _ImmersiveGames.Scripts.SpawnSystems.DynamicPropertiesSystem.Editor
{
    [CustomEditor(typeof(EnhancedTriggerData)), DebugLevel(DebugLevel.Warning)]
    public class EnhancedTriggerDataEditor : UnityEditor.Editor
    {
        private EnhancedTriggerData _triggerData;
        private SerializedProperty _triggerTypeProperty;

        private void OnEnable()
        {
            _triggerData = (EnhancedTriggerData)target;
            _triggerTypeProperty = serializedObject.FindProperty("triggerType");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Enhanced Trigger Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_triggerTypeProperty);
            bool triggerTypeChanged = EditorGUI.EndChangeCheck();

            EditorGUILayout.Space();

            if (GUILayout.Button("Apply Template for Current Type"))
                ApplyTemplate();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Dynamic Properties", EditorStyles.boldLabel);
            DrawDynamicProperties(_triggerData);

            EditorGUILayout.Space();
            DrawAddPropertyButtons(_triggerData);

            DrawTemplateInfo(_triggerData.triggerType);

            if (triggerTypeChanged)
                ShowTemplatePrompt(_triggerData.triggerType);

            serializedObject.ApplyModifiedProperties();
        }

        private void ApplyTemplate()
        {
            _triggerData.ApplyTemplate();
            ValidateProperties(_triggerData);
            EditorUtility.SetDirty(_triggerData);
            DebugUtility.LogVerbose<EnhancedTriggerDataEditor>($"Template aplicado para triggerType={_triggerData.triggerType}");
            Repaint();
        }

        private void DrawDynamicProperties(EnhancedTriggerData data)
        {
            var props = data.GetAllProperties().ToList();

            if (!props.Any())
            {
                EditorGUILayout.HelpBox("No properties configured. Click 'Apply Template' to add default ones.", MessageType.Info);
                return;
            }

            foreach (var prop in props)
                DrawPropertyField(prop, data);
        }

        private void DrawPropertyField(IConfigurableProperty prop, EnhancedTriggerData data)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.BeginHorizontal();

            string label = prop.Name + (prop.IsRequired ? " *" : "");
            EditorGUILayout.LabelField(label, GUILayout.Width(120));

            object newValue = DrawValueField(prop);
            if (newValue != null && !Equals(newValue, prop.GetValue()))
            {
                prop.SetValue(newValue);
                ValidateProperty(prop);
                EditorUtility.SetDirty(data);
                DebugUtility.LogVerbose<EnhancedTriggerDataEditor>($"Propriedade '{prop.Name}' alterada para {newValue}");
            }

            GUI.enabled = !prop.IsRequired;
            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                data.RemoveProperty(prop.Name);
                EditorUtility.SetDirty(data);
                DebugUtility.LogVerbose<EnhancedTriggerDataEditor>($"Propriedade '{prop.Name}' removida");
                Repaint();
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(prop.Description))
                EditorGUILayout.LabelField(prop.Description, EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
        }

        private object DrawValueField(IConfigurableProperty prop)
        {
            var value = prop.GetValue();
            var typeName = prop.PropertyType.Name;

            if (prop.Name == "sensorType" && prop.PropertyType == typeof(SensorTypes))
            {
                SensorTypes sensorType = (SensorTypes)(value ?? SensorTypes.OtherSensor);
                sensorType = (SensorTypes)EditorGUILayout.EnumPopup(sensorType);
                return sensorType;
            }

            return typeName switch
            {
                nameof(Single) => EditorGUILayout.FloatField((float)value),
                nameof(Int32) => EditorGUILayout.IntField((int)value),
                nameof(Boolean) => EditorGUILayout.Toggle((bool)value),
                nameof(String) => EditorGUILayout.TextField((string)value),
                nameof(Vector2) => EditorGUILayout.Vector2Field("", (Vector2)value),
                nameof(Vector3) => EditorGUILayout.Vector3Field("", (Vector3)value),
                _ => value
            };
        }

        private void DrawAddPropertyButtons(EnhancedTriggerData data)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Add Property:", GUILayout.Width(100));
            if (GUILayout.Button("Float")) AddProperty(data, "newFloat", 0f);
            if (GUILayout.Button("Int")) AddProperty(data, "newInt", 0);
            if (GUILayout.Button("Bool")) AddProperty(data, "newBool", false);
            if (GUILayout.Button("String")) AddProperty(data, "newString", "");
            if (GUILayout.Button("Vector2")) AddProperty(data, "newVector2", Vector2.zero);
            if (GUILayout.Button("Vector3")) AddProperty(data, "newVector3", Vector3.zero);
            if (data.triggerType == TriggerType.SensorTrigger && !data.HasProperty("sensorType"))
                if (GUILayout.Button("SensorType")) AddProperty(data, "sensorType", SensorTypes.OtherSensor);
            if (!data.HasProperty("continuous"))
                if (GUILayout.Button("Continuous")) AddProperty(data, "continuous", false);
            if (!data.HasProperty("spawnInterval"))
                if (GUILayout.Button("SpawnInterval")) AddProperty(data, "spawnInterval", 1.0f);
            if (!data.HasProperty("rearmDelay"))
                if (GUILayout.Button("RearmDelay")) AddProperty(data, "rearmDelay", 0.5f);
            if (!data.HasProperty("maxSpawns"))
                if (GUILayout.Button("MaxSpawns")) AddProperty(data, "maxSpawns", -1);
            EditorGUILayout.EndHorizontal();
        }

        private void AddProperty<T>(EnhancedTriggerData data, string baseName, T defaultValue)
        {
            string s = baseName;
            int count = 1;
            while (data.GetAllProperties().Any(p => p.Name == s))
                s = $"{baseName}_{count++}";

            data.SetProperty(s, defaultValue);
            ValidateProperty(new ConfigurableProperty<T>(s, defaultValue));
            EditorUtility.SetDirty(data);
            DebugUtility.LogVerbose<EnhancedTriggerDataEditor>($"Propriedade '{s}' adicionada com valor {defaultValue}");
        }

        private void DrawTemplateInfo(TriggerType type)
        {
            var template = PropertyTemplateRegistry.GetTriggerTemplate(type);
            if (template == null) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Template Info", EditorStyles.boldLabel);

            foreach (var prop in template.Properties)
            {
                string info = $"• {prop.Name} ({prop.PropertyType.Name})";
                if (prop.IsRequired) info += " - Required";
                if (!string.IsNullOrEmpty(prop.Description)) info += $" - {prop.Description}";
                EditorGUILayout.LabelField(info, EditorStyles.miniLabel);
            }
        }

        private void ShowTemplatePrompt(TriggerType type)
        {
            if (EditorUtility.DisplayDialog("Apply Template?",
                $"Do you want to apply the default template for '{type}'?\nThis will override all current properties.",
                "Apply", "Cancel"))
            {
                ApplyTemplate();
            }
        }

        private void ValidateProperties(EnhancedTriggerData data)
        {
            foreach (var prop in data.GetAllProperties())
                ValidateProperty(prop);
        }

        private void ValidateProperty(IConfigurableProperty prop)
        {
            switch (prop)
            {
                case FloatProperty floatProp when floatProp.Name == "spawnInterval":
                    if (floatProp.Value <= 0f)
                    {
                        floatProp.Value = 1.0f;
                        DebugUtility.LogWarning<EnhancedTriggerDataEditor>($"spawnInterval deve ser maior que 0. Ajustado para 1.0.");
                    }
                    break;
                case FloatProperty floatProp when floatProp.Name == "rearmDelay":
                    if (floatProp.Value < 0f)
                    {
                        floatProp.Value = 0.5f;
                        DebugUtility.LogWarning<EnhancedTriggerDataEditor>($"rearmDelay não pode ser negativo. Ajustado para 0.5.");
                    }
                    break;
                case IntProperty intProp when intProp.Name == "maxSpawns":
                    if (intProp.Value < -1)
                    {
                        intProp.Value = -1;
                        DebugUtility.LogWarning<EnhancedTriggerDataEditor>($"maxSpawns não pode ser menor que -1. Ajustado para -1.");
                    }
                    break;
            }
        }
    }

    [CustomEditor(typeof(EnhancedStrategyData)),DebugLevel(DebugLevel.Warning)]
    public class EnhancedStrategyDataEditor : UnityEditor.Editor
    {
        private EnhancedStrategyData _strategyData;
        private SerializedProperty _strategyTypeProperty;

        private void OnEnable()
        {
            _strategyData = (EnhancedStrategyData)target;
            _strategyTypeProperty = serializedObject.FindProperty("strategyType");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Enhanced Strategy Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_strategyTypeProperty);
            bool typeChanged = EditorGUI.EndChangeCheck();

            EditorGUILayout.Space();

            if (GUILayout.Button("Apply Template for Current Type"))
                ApplyTemplate();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Dynamic Properties", EditorStyles.boldLabel);
            DrawDynamicProperties(_strategyData);

            EditorGUILayout.Space();
            DrawAddPropertyButtons(_strategyData);

            DrawTemplateInfo(_strategyData.strategyType);

            if (typeChanged)
                ShowTemplatePrompt(_strategyData.strategyType);

            serializedObject.ApplyModifiedProperties();
        }

        private void ApplyTemplate()
        {
            _strategyData.ApplyTemplate();
            EditorUtility.SetDirty(_strategyData);
            DebugUtility.Log<EnhancedStrategyDataEditor>($"Template aplicado para strategyType={_strategyData.strategyType}");
            // Logar todas as propriedades carregadas
            foreach (var prop in _strategyData.GetAllProperties())
            {
                DebugUtility.Log<EnhancedStrategyDataEditor>($"Propriedade carregada: {prop.Name} (Tipo: {prop.PropertyType.Name}, Valor: {prop.GetValue()})");
            }
            Repaint();
        }

        private void DrawDynamicProperties(EnhancedStrategyData data)
        {
            var props = data.GetAllProperties().ToList();

            if (!props.Any())
            {
                EditorGUILayout.HelpBox("No properties configured. Click 'Apply Template' to add default ones.", MessageType.Info);
                return;
            }
            
            foreach (var prop in props)
            {
                DebugUtility.Log<EnhancedStrategyDataEditor>($"Propriedade: {prop.Name}, Tipo: {prop.PropertyType.Name}, Valor: {prop.GetValue()}");
                DrawPropertyField(prop, data);
            }
        }

        private void DrawPropertyField(IConfigurableProperty prop, EnhancedStrategyData data)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.BeginHorizontal();

            string label = prop.Name + (prop.IsRequired ? " *" : "");
            EditorGUILayout.LabelField(label, GUILayout.Width(120));

            object newValue = DrawValueField(prop);
            if (newValue != null && !Equals(newValue, prop.GetValue()))
            {
                prop.SetValue(newValue);
                EditorUtility.SetDirty(data);
                DebugUtility.Log<EnhancedStrategyDataEditor>($"Propriedade '{prop.Name}' alterada para {newValue}");
            }

            GUI.enabled = !prop.IsRequired;
            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                data.RemoveProperty(prop.Name);
                EditorUtility.SetDirty(data);
                DebugUtility.Log<EnhancedStrategyDataEditor>($"Propriedade '{prop.Name}' removida");
                Repaint();
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(prop.Description))
                EditorGUILayout.LabelField(prop.Description, EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
        }

        private object DrawValueField(IConfigurableProperty prop)
        {
            var value = prop.GetValue();
            var typeName = prop.PropertyType.Name;

            if (prop.Name == "easeType" && prop.PropertyType == typeof(Ease))
            {
                DebugUtility.Log<EnhancedStrategyDataEditor>("Detectado easeType, renderizando EnumPopup");
                Ease easeType = (Ease)(value ?? Ease.OutQuad);
                easeType = (Ease)EditorGUILayout.EnumPopup(easeType);
                return easeType;
            }

            return typeName switch
            {
                nameof(Single) => EditorGUILayout.FloatField((float)value),
                nameof(Int32) => EditorGUILayout.IntField((int)value),
                nameof(Boolean) => EditorGUILayout.Toggle((bool)value),
                nameof(String) => EditorGUILayout.TextField((string)value),
                nameof(Vector2) => EditorGUILayout.Vector2Field("", (Vector2)value),
                nameof(Vector3) => EditorGUILayout.Vector3Field("", (Vector3)value),
                _ => value
            };
        }

        private void DrawAddPropertyButtons(EnhancedStrategyData data)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Add Property:", GUILayout.Width(100));
            if (GUILayout.Button("Float")) AddProperty(data, "newFloat", 0f);
            if (GUILayout.Button("Int")) AddProperty(data, "newInt", 0);
            if (GUILayout.Button("Bool")) AddProperty(data, "newBool", false);
            if (GUILayout.Button("String")) AddProperty(data, "newString", "");
            if (GUILayout.Button("Vector2")) AddProperty(data, "newVector2", Vector2.zero);
            if (GUILayout.Button("Vector3")) AddProperty(data, "newVector3", Vector3.zero);
            if (data.strategyType == StrategyType.CircularZoomOutStrategy)
                if (GUILayout.Button("EaseType")) AddProperty(data, "easeType", Ease.OutQuad);
            EditorGUILayout.EndHorizontal();
        }

        private void AddProperty<T>(EnhancedStrategyData data, string baseName, T defaultValue)
        {
            string s = baseName;
            int count = 1;
            while (data.GetAllProperties().Any(p => p.Name == s))
                s = $"{baseName}_{count++}";

            data.SetProperty(s, defaultValue);
            EditorUtility.SetDirty(data);
        }

        private void DrawTemplateInfo(StrategyType type)
        {
            var template = PropertyTemplateRegistry.GetStrategyTemplate(type);
            if (template == null) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Template Info", EditorStyles.boldLabel);

            foreach (var prop in template.Properties)
            {
                string info = $"• {prop.Name} ({prop.PropertyType.Name})";
                if (prop.IsRequired) info += " - Required";
                if (!string.IsNullOrEmpty(prop.Description)) info += $" - {prop.Description}";
                EditorGUILayout.LabelField(info, EditorStyles.miniLabel);
            }
        }

        private void ShowTemplatePrompt(StrategyType type)
        {
            if (EditorUtility.DisplayDialog("Apply Template?",
                $"Do you want to apply the default template for '{type}'?\nThis will override all current properties.",
                "Apply", "Cancel"))
            {
                ApplyTemplate();
            }
        }
    }
}
#endif