using UnityEditor;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems.Editor
{
    [CustomEditor(typeof(PlanetResourcesSo))]
    public class PlanetResourcesSoEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var resource = (PlanetResourcesSo)target;

            // Exibe os campos padrão
            DrawDefaultInspector();
            EditorGUILayout.Space(10);

            var sprite = resource.ResourceIcon;

            if (sprite)
            {
                EditorGUILayout.LabelField("Prévia do Ícone (Recorte)", EditorStyles.boldLabel);

                float previewSize = 100f;
                var rect = GUILayoutUtility.GetRect(previewSize, previewSize, GUILayout.ExpandWidth(false));

                // Aplica o recorte correto (textureRect → UV)
                GUI.DrawTextureWithTexCoords(
                    rect,
                    sprite.texture,
                    GetSpriteUVRect(sprite)
                );
            }
            else
            {
                EditorGUILayout.HelpBox("Nenhum ícone atribuído.", MessageType.Info);
            }
        }

        private Rect GetSpriteUVRect(Sprite sprite)
        {
            var texRect = sprite.textureRect;
            Texture tex = sprite.texture;

            return new Rect(
                texRect.x / tex.width,
                texRect.y / tex.height,
                texRect.width / tex.width,
                texRect.height / tex.height
            );
        }
    }
}