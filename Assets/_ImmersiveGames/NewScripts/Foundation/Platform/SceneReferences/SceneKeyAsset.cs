using UnityEngine;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.SceneReferences
{
    /// <summary>
    /// Chave de cena para evitar string solta em configuraÃ§Ãµes de rota.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SceneKeyAsset",
        menuName = "ImmersiveGames/Scene References/SceneKeyAsset",
        order = 30)]
    public sealed class SceneKeyAsset : ScriptableObject
    {
        [SerializeField] private string sceneName;

        public string SceneName => sceneName;

        private void OnValidate()
        {
            sceneName = string.IsNullOrWhiteSpace(sceneName)
                ? string.Empty
                : sceneName.Trim();
        }
    }
}


