using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace _ImmersiveGames.Scripts.FadeSystem
{
    /// <summary>
    /// Implementação padrão do IFadeService usando apenas Task/async-await.
    /// - Carrega a FadeScene uma única vez (lazy ou via PreloadAsync).
    /// - Instancia um FadeController persistente (DontDestroyOnLoad).
    /// - Sincroniza chamadas concorrentes com SemaphoreSlim.
    /// </summary>
    public class FadeService : IFadeService
    {
        private const string FadeSceneName = "FadeScene";

        private FadeController _fadeController;
        private readonly SemaphoreSlim _fadeLock = new SemaphoreSlim(1, 1);
        private Task _initializationTask;

        private readonly EventBinding<FadeInRequestedEvent> _fadeInBinding;
        private readonly EventBinding<FadeOutRequestedEvent> _fadeOutBinding;

        public FadeService()
        {
            _fadeInBinding  = new EventBinding<FadeInRequestedEvent>(_ => RequestFadeIn());
            _fadeOutBinding = new EventBinding<FadeOutRequestedEvent>(_ => RequestFadeOut());

            EventBus<FadeInRequestedEvent>.Register(_fadeInBinding);
            EventBus<FadeOutRequestedEvent>.Register(_fadeOutBinding);
        }

        ~FadeService()
        {
            EventBus<FadeInRequestedEvent>.Unregister(_fadeInBinding);
            EventBus<FadeOutRequestedEvent>.Unregister(_fadeOutBinding);
        }

        #region IFadeService (API pública)

        public void RequestFadeIn()
        {
            // Fire-and-forget para código legado baseado em eventos ou input.
            _ = FadeInAsync();
        }

        public void RequestFadeOut()
        {
            _ = FadeOutAsync();
        }

        public Task FadeInAsync()  => RunFadeAsync(1f);
        public Task FadeOutAsync() => RunFadeAsync(0f);

        #endregion

        /// <summary>
        /// Método explícito para pré-carregar a FadeScene e inicializar o FadeController,
        /// sem executar nenhum fade. Usado pelo bootstrap para evitar custo na primeira transição.
        /// </summary>
        public Task PreloadAsync()
        {
            return EnsureInitializedAsync();
        }

        #region Internals

        private async Task RunFadeAsync(float targetAlpha)
        {
            await _fadeLock.WaitAsync();
            try
            {
                await EnsureInitializedAsync();

                if (_fadeController != null)
                {
                    if (targetAlpha >= 1f - 0.0001f)
                    {
                        await _fadeController.FadeInAsync();
                    }
                    else if (targetAlpha <= 0f + 0.0001f)
                    {
                        await _fadeController.FadeOutAsync();
                    }
                    else
                    {
                        await _fadeController.FadeToAsync(targetAlpha);
                    }
                }
                else
                {
                    Debug.LogWarning("[FadeService] Fade solicitado, mas FadeController é nulo.");
                }
            }
            finally
            {
                _fadeLock.Release();
            }
        }

        private async Task EnsureInitializedAsync()
        {
            if (_fadeController != null)
                return;

            if (_initializationTask != null)
            {
                await _initializationTask;
                return;
            }

            _initializationTask = InitializeControllerAsync();
            await _initializationTask;
        }

        private async Task InitializeControllerAsync()
        {
            Debug.Log("[FadeService] Carregando FadeScene para inicialização do fade...");

            AsyncOperation loadOp;
            try
            {
                loadOp = SceneManager.LoadSceneAsync(FadeSceneName, LoadSceneMode.Additive);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FadeService] Erro ao iniciar load da cena '{FadeSceneName}': {e}");
                return;
            }

            if (loadOp == null)
            {
                Debug.LogError($"[FadeService] LoadSceneAsync retornou null para '{FadeSceneName}'.");
                return;
            }

            while (!loadOp.isDone)
                await Task.Yield();

            var scene = SceneManager.GetSceneByName(FadeSceneName);
            if (!scene.IsValid())
            {
                Debug.LogError("[FadeService] Cena FadeScene não encontrada ou inválida.");
                return;
            }

            FadeController prefabController = null;
            GameObject prefabRoot = null;

            foreach (var root in scene.GetRootGameObjects())
            {
                var controller = root.GetComponentInChildren<FadeController>(true);
                if (controller != null)
                {
                    prefabController = controller;
                    prefabRoot = controller.gameObject;
                    break;
                }
            }

            if (prefabController == null || prefabRoot == null)
            {
                Debug.LogError("[FadeService] FadeController não encontrado na FadeScene.");
                return;
            }

            var instance = Object.Instantiate(prefabRoot);
            Object.DontDestroyOnLoad(instance);

            _fadeController = instance.GetComponent<FadeController>();
            if (_fadeController == null)
            {
                Debug.LogError("[FadeService] FadeController do clone persistente é nulo.");
            }
            else
            {
                Debug.Log("[FadeService] FadeController persistente inicializado com sucesso.");
            }

            Debug.Log("[FadeService] Descarregando FadeScene container após inicialização.");
            AsyncOperation unloadOp = null;
            try
            {
                unloadOp = SceneManager.UnloadSceneAsync(FadeSceneName);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FadeService] Erro ao iniciar unload da cena '{FadeSceneName}': {e}");
            }

            if (unloadOp != null)
            {
                while (!unloadOp.isDone)
                    await Task.Yield();
            }
        }

        #endregion
    }
}
