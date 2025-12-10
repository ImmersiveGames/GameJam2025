// Path: _ImmersiveGames/Scripts/PlayerControllerSystem/Shooting/Editor/PlayerShootControllerEditor.cs

using _ImmersiveGames.Scripts.SpawnSystems;
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlayerControllerSystem.Shooting.Editor
{
    [CustomEditor(typeof(PlayerShootController))]
    public class PlayerShootControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty _poolData;
        private SerializedProperty _actionName;
        private SerializedProperty _cooldown;
        private SerializedProperty _strategyType;
        private SerializedProperty _singleStrategy;
        private SerializedProperty _multipleLinearStrategy;
        private SerializedProperty _circularStrategy;

        private void OnEnable()
        {
            _poolData = serializedObject.FindProperty("poolData");
            _actionName = serializedObject.FindProperty("actionName");
            _cooldown = serializedObject.FindProperty("cooldown");
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
            EditorGUILayout.LabelField("🎯 Spawn Strategy Config", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_strategyType);

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

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawStrategyBox(string title, SerializedProperty property)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"📦 {title}", EditorStyles.boldLabel);

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
    }
}