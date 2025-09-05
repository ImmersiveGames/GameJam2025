using _ImmersiveGames.Scripts.SpawnSystems;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SpawnSystem.StrategyConfig))]
public class StrategyConfigDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Obter propriedades
        var typeProp = property.FindPropertyRelative("type");
        var spawnCountProp = property.FindPropertyRelative("spawnCount");
        var radiusProp = property.FindPropertyRelative("radius");
        var spacingProp = property.FindPropertyRelative("spacing");
        var angleOffsetProp = property.FindPropertyRelative("angleOffset");

        // Calcular posições
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float padding = EditorGUIUtility.standardVerticalSpacing;
        Rect typeRect = new Rect(position.x, position.y, position.width, lineHeight);
        Rect spawnCountRect = new Rect(position.x, position.y + lineHeight + padding, position.width, lineHeight);
        Rect radiusRect = new Rect(position.x, position.y + 2 * (lineHeight + padding), position.width, lineHeight);
        Rect spacingRect = new Rect(position.x, position.y + 3 * (lineHeight + padding), position.width, lineHeight);
        Rect angleOffsetRect = new Rect(position.x, position.y + 4 * (lineHeight + padding), position.width, lineHeight);

        // Dropdown para type
        int selectedIndex = Mathf.Max(0, SpawnStrategyFactory.SupportedStrategyTypes.IndexOf(typeProp.stringValue));
        selectedIndex = EditorGUI.Popup(typeRect, "Strategy Type", selectedIndex, SpawnStrategyFactory.SupportedStrategyTypes.ToArray());
        typeProp.stringValue = SpawnStrategyFactory.SupportedStrategyTypes[selectedIndex];

        // Campo spawnCount (sempre visível)
        EditorGUI.PropertyField(spawnCountRect, spawnCountProp, new GUIContent("Spawn Count"));

        // Campos condicionais para Circular
        if (typeProp.stringValue.ToLower() == "circular")
        {
            EditorGUI.PropertyField(radiusRect, radiusProp, new GUIContent("Radius"));
            EditorGUI.PropertyField(spacingRect, spacingProp, new GUIContent("Spacing (degrees)"));
            EditorGUI.PropertyField(angleOffsetRect, angleOffsetProp, new GUIContent("Angle Offset (degrees)"));
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var typeProp = property.FindPropertyRelative("type");
        float height = EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing; // type + spawnCount
        if (typeProp.stringValue.ToLower() == "circular")
        {
            height += EditorGUIUtility.singleLineHeight * 3 + EditorGUIUtility.standardVerticalSpacing * 3; // radius, spacing, angleOffset
        }
        return height;
    }
}