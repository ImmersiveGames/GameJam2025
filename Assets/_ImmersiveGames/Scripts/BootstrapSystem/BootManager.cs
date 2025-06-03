using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.Scripts.BootstrapSystem
{
    [DebugLevel(DebugLevel.Logs)]
    public class BootManager : MonoBehaviour
    {
        private static bool _initialized;
        private const string InitialSceneName = "Menu"; // Nome da sua cena aqui

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (DependencyManager.Instance.IsInTestMode)
            {
                DebugUtility.LogVerbose<BootManager>("BootManager desativado para testes unitários.");
                return;
            }

            if (_initialized) return;
            _initialized = true;

            DebugUtility.SetDefaultDebugLevel(DebugLevel.Verbose);
            DebugUtility.RegisterScriptDebugLevel(typeof(BootManager), DebugLevel.Verbose);

            // Acessar DependencyManager.Instance (cria automaticamente se necessário)
            if (!DependencyManager.HasInstance)
            {
                var dependencyManager = DependencyManager.Instance;
                DebugUtility.LogVerbose<BootManager>("DependencyManager inicializado pelo BootManager.");
            }
            else
            {
                DebugUtility.LogVerbose<BootManager>("DependencyManager já inicializado.");
            }

            // Inicializar DependencyBootstrapper
            // TODO: Confirmar implementação de DependencyBootstrapper
            DependencyBootstrapper.Instance.BootstrapOnDemand();

            DebugUtility.LogVerbose<BootManager>("Inicialização concluída.");
            
            LoadInitialScene();
        }
        private static void LoadInitialScene()
        {
            if (SceneManager.GetActiveScene().name != "Bootstrap") return;
            var loadOperation = SceneManager.LoadSceneAsync(InitialSceneName, LoadSceneMode.Additive);
            if (loadOperation != null)
                loadOperation.completed += (asyncOperation) => {
                    DebugUtility.Log<BootManager>($"Cena {InitialSceneName} carregada com sucesso.");
                };
        }
        

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _initialized = false;
            DebugUtility.LogVerbose<BootManager>("Estado resetado para nova execução.");
        }
#endif
    }
}