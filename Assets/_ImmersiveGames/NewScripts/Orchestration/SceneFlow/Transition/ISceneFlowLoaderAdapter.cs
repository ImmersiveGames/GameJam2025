#nullable enable
using System.Threading.Tasks;
namespace _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition
{
    /// <summary>
    /// Adapter para operações de loading/unloading/ActiveScene independente da fonte (SceneManager ou legado).
    /// </summary>
    public interface ISceneFlowLoaderAdapter
    {
        Task LoadSceneAsync(string sceneName, System.Action<float>? onProgress = null);
        Task UnloadSceneAsync(string sceneName, System.Action<float>? onProgress = null);
        bool IsSceneLoaded(string sceneName);
        Task<bool> TrySetActiveSceneAsync(string sceneName);
        string GetActiveSceneName();
    }
}
