using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings
{
    /// <summary>
    /// Chave de cena para evitar string solta em configurações de rota.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SceneKey",
        menuName = "ImmersiveGames/NewScripts/SceneFlow/Scene Key",
        order = 14)]
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
