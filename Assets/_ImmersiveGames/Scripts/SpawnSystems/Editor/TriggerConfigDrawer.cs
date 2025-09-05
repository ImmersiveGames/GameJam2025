using _ImmersiveGames.Scripts.SpawnSystems;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SpawnSystem.TriggerConfig))]
public class TriggerConfigDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Obter propriedades
        var typeProp = property.FindPropertyRelative("type");
        var keyProp = property.FindPropertyRelative("key");

        // Calcular posições
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float padding = EditorGUIUtility.standardVerticalSpacing;
        Rect typeRect = new Rect(position.x, position.y, position.width, lineHeight);
        Rect keyRect = new Rect(position.x, position.y + lineHeight + padding, position.width, lineHeight);

        // Dropdown para type
        int selectedIndex = Mathf.Max(0, SpawnTriggerFactory.SupportedTriggerTypes.IndexOf(typeProp.stringValue));
        selectedIndex = EditorGUI.Popup(typeRect, "Trigger Type", selectedIndex, SpawnTriggerFactory.SupportedTriggerTypes.ToArray());
        typeProp.stringValue = SpawnTriggerFactory.SupportedTriggerTypes[selectedIndex];

        // Campos condicionais
        if (typeProp.stringValue.ToLower() == "keypress")
        {
            EditorGUI.PropertyField(keyRect, keyProp, new GUIContent("Key"));
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var typeProp = property.FindPropertyRelative("type");
        float height = EditorGUIUtility.singleLineHeight;
        if (typeProp.stringValue.ToLower() == "keypress")
        {
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }
        return height;
    }
}