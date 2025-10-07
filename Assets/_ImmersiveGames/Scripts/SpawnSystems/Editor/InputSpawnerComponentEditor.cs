using UnityEditor;
using _ImmersiveGames.Scripts.AudioSystem.Configs;

namespace _ImmersiveGames.Scripts.SpawnSystems.Editor
{
    [CustomEditor(typeof(InputSpawnerComponent))]
    public class InputSpawnerComponentEditor : UnityEditor.Editor
    {
        private SerializedProperty _poolData;
        private SerializedProperty _actionName;
        private SerializedProperty _cooldown;
        private SerializedProperty _enableShootSounds;
        private SerializedProperty _audioConfig;
        private SerializedProperty _strategyType;
        private SerializedProperty _singleStrategy;
        private SerializedProperty _multipleLinearStrategy;
        private SerializedProperty _circularStrategy;

        private void OnEnable()
        {
            _poolData = serializedObject.FindProperty("poolData");
            _actionName = serializedObject.FindProperty("actionName");
            _cooldown = serializedObject.FindProperty("cooldown");
            _enableShootSounds = serializedObject.FindProperty("enableShootSounds");
            _audioConfig = serializedObject.FindProperty("audioConfig");
            _strategyType = serializedObject.FindProperty("strategyType");
            _singleStrategy = serializedObject.FindProperty("singleStrategy");
            _multipleLinearStrategy = serializedObject.FindProperty("multipleLinearStrategy");
            _circularStrategy = serializedObject.FindProperty("circularStrategy");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("⚙️ Pool Config", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_poolData);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("🎮 Input Config", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_actionName);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("⏳ Cooldown Config", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_cooldown);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("🔊 Audio Config", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_enableShootSounds);
            
            if (_enableShootSounds.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_audioConfig);
                
                // Mostrar informações sobre a configuração de áudio atual
                var audioConfig = _audioConfig.objectReferenceValue as AudioConfig;
                if (audioConfig != null)
                {
                    EditorGUILayout.HelpBox(
                        $"AudioConfig: {audioConfig.name}\n" +
                        $"Shoot Sound: {(audioConfig.shootSound != null ? audioConfig.shootSound.clip?.name : "None")}",
                        MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "No AudioConfig assigned. Shoot sounds will only play if configured in the strategy.",
                        MessageType.Warning);
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("🎯 Spawn Strategy Config", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_strategyType);

            // Mostrar apenas os campos da estratégia escolhida
            EditorGUI.indentLevel++;
            switch ((SpawnStrategyType)_strategyType.enumValueIndex)
            {
                case SpawnStrategyType.Single:
                    DrawStrategyBox("Single Strategy", _singleStrategy);
                    break;

                case SpawnStrategyType.MultipleLinear:
                    DrawStrategyBox("Multiple Linear Strategy", _multipleLinearStrategy);
                    break;

                case SpawnStrategyType.Circular:
                    DrawStrategyBox("Circular Strategy", _circularStrategy);
                    break;
            }
            EditorGUI.indentLevel--;

            // Mostrar informações de áudio da estratégia atual
            DrawStrategyAudioInfo();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawStrategyBox(string title, SerializedProperty property)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"📦 {title}", EditorStyles.boldLabel);
            
            // Desenhar todas as propriedades da estratégia
            var iterator = property.Copy();
            var end = iterator.GetEndProperty();
            
            iterator.NextVisible(true);
            while (!SerializedProperty.EqualContents(iterator, end))
            {
                EditorGUILayout.PropertyField(iterator, true);
                iterator.NextVisible(false);
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawStrategyAudioInfo()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("🔊 Current Strategy Audio", EditorStyles.boldLabel);
            
            SerializedProperty currentStrategy = null;
            string strategyName = "";
            
            switch ((SpawnStrategyType)_strategyType.enumValueIndex)
            {
                case SpawnStrategyType.Single:
                    currentStrategy = _singleStrategy;
                    strategyName = "Single";
                    break;
                case SpawnStrategyType.MultipleLinear:
                    currentStrategy = _multipleLinearStrategy;
                    strategyName = "Multiple Linear";
                    break;
                case SpawnStrategyType.Circular:
                    currentStrategy = _circularStrategy;
                    strategyName = "Circular";
                    break;
            }

            if (currentStrategy != null)
            {
                EditorGUI.indentLevel++;
                
                // Encontrar a propriedade shootSound dentro da estratégia
                var shootSoundProperty = currentStrategy.FindPropertyRelative("shootSound");
                if (shootSoundProperty != null)
                {
                    var clipProperty = shootSoundProperty.FindPropertyRelative("clip");
                    var volumeProperty = shootSoundProperty.FindPropertyRelative("volume");
                    var frequentSoundProperty = shootSoundProperty.FindPropertyRelative("frequentSound");
                    var randomPitchProperty = shootSoundProperty.FindPropertyRelative("randomPitch");
                    
                    if (clipProperty.objectReferenceValue != null)
                    {
                        EditorGUILayout.HelpBox(
                            $"{strategyName} Strategy Audio:\n" +
                            $"• Clip: {clipProperty.objectReferenceValue.name}\n" +
                            $"• Volume: {volumeProperty.floatValue:F2}\n" +
                            $"• Frequent: {(frequentSoundProperty.boolValue ? "Yes" : "No")}\n" +
                            $"• Random Pitch: {(randomPitchProperty.boolValue ? "Yes" : "No")}",
                            MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(
                            $"{strategyName} Strategy: No shoot sound configured.\n" +
                            "Will use AudioConfig fallback if available.",
                            MessageType.Warning);
                    }
                }
                
                EditorGUI.indentLevel--;
            }
        }
    }
}