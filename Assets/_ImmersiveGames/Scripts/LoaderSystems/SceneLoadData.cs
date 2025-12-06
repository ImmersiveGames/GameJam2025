using UnityEngine.SceneManagement;

namespace _ImmersiveGames.Scripts.LoaderSystems
{
    /// <summary>
    /// Dados para uma operação de carregamento de cena.
    /// </summary>
    public struct SceneLoadData
    {
        public string SceneName;
        public LoadSceneMode LoadMode;

        public SceneLoadData(string sceneName, LoadSceneMode mode = LoadSceneMode.Additive)
        {
            SceneName = sceneName;
            LoadMode = mode;
        }
    }
}