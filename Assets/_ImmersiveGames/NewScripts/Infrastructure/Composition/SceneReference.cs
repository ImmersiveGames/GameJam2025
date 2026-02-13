using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    /// <summary>
    /// Referência serializável de cena para uso em config de bootstrap.
    /// </summary>
    [Serializable]
    public sealed class SceneReference
    {
        [SerializeField] private string scenePath;
#if UNITY_EDITOR
        [SerializeField] private SceneAsset sceneAsset;
#endif

        // Caminho canônico da cena (Assets/.../*.unity).
        public string ScenePath => scenePath;

        // Nome derivado do path para APIs que exigem nome lógico da cena.
        public string SceneName => ExtractSceneName(scenePath);

        public bool IsAssigned => !string.IsNullOrWhiteSpace(scenePath);

        public string GetPathOrNameForLoad()
        {
            return scenePath;
        }

#if UNITY_EDITOR
        public void SyncFromEditorAsset()
        {
            if (sceneAsset == null)
            {
                return;
            }

            var assetPath = AssetDatabase.GetAssetPath(sceneAsset);
            if (!string.IsNullOrWhiteSpace(assetPath))
            {
                scenePath = assetPath;
            }
        }
#endif

        private static string ExtractSceneName(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            return fileName ?? string.Empty;
        }
    }
}
