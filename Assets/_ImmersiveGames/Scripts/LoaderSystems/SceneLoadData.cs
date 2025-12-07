using UnityEngine.SceneManagement;

namespace _ImmersiveGames.Scripts.LoaderSystems
{
    /// <summary>
    /// Dados para uma operação de carregamento de cena.
    /// </summary>
    public struct SceneLoadData
    {
        public readonly string sceneName;
        public readonly LoadSceneMode loadMode;

        public SceneLoadData(string sceneName, LoadSceneMode mode = LoadSceneMode.Additive)
        {
            this.sceneName = sceneName;
            loadMode = mode;
        }
    }
}