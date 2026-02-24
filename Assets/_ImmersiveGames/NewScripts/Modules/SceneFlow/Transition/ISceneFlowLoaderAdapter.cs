#nullable enable
using System.Threading.Tasks;
namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition
{
    /// <summary>
    /// Adapter para operações de loading/unloading/ActiveScene independente da fonte (SceneManager ou legado).
    /// </summary>
    public interface ISceneFlowLoaderAdapter
    {
        Task LoadSceneAsync(string sceneName);
        Task UnloadSceneAsync(string sceneName);
        bool IsSceneLoaded(string sceneName);
        Task<bool> TrySetActiveSceneAsync(string sceneName);
        string GetActiveSceneName();
    }
}
