using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.Scripts.SceneManagement.Core
{
    /// <summary>
    /// Serviço de baixo nível para carregar e descarregar cenas sem usar corrotinas.
    /// Apenas wrappers async/await sobre SceneManager + AsyncOperation.
    /// </summary>
    public interface ISceneLoader
    {
        Task LoadSceneAsync(string sceneName, LoadSceneMode mode);
        Task UnloadSceneAsync(string sceneName);
        bool IsSceneLoaded(string sceneName);
        Scene GetSceneByName(string sceneName);
    }
}