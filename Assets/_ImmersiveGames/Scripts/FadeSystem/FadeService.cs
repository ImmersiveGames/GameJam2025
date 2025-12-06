using System.Collections;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.Scripts.FadeSystem
{
    public class FadeService : IFadeService
    {
        private const string FadeSceneName = "FadeScene";

        private FadeController _fadeController;
        private bool _isLoaded;
        private bool _isFading;
        private readonly ICoroutineRunner _runner;

        private readonly EventBinding<FadeInRequestedEvent> _fadeInBinding;
        private readonly EventBinding<FadeOutRequestedEvent> _fadeOutBinding;

        public FadeService()
        {
            if (DependencyManager.Provider.TryGetGlobal(out ICoroutineRunner runner))
            {
                _runner = runner;
                Debug.Log("[FadeService] CoroutineRunner injetado com sucesso.");
            }
            else
            {
                Debug.LogError("[FadeService] Falha ao obter CoroutineRunner.");
            }

            _fadeInBinding = new EventBinding<FadeInRequestedEvent>(_ => RequestFadeIn());
            _fadeOutBinding = new EventBinding<FadeOutRequestedEvent>(_ => RequestFadeOut());

            EventBus<FadeInRequestedEvent>.Register(_fadeInBinding);
            EventBus<FadeOutRequestedEvent>.Register(_fadeOutBinding);
        }

        ~FadeService()
        {
            // Em tempo de execução normal, o serviço vive até o fim do jogo.
            // Este destrutor é apenas um fallback de limpeza.
            EventBus<FadeInRequestedEvent>.Unregister(_fadeInBinding);
            EventBus<FadeOutRequestedEvent>.Unregister(_fadeOutBinding);
        }

        public void RequestFadeIn()
        {
            if (_runner == null)
            {
                Debug.LogWarning("[FadeService] RequestFadeIn ignorado: nenhum CoroutineRunner registrado.");
                return;
            }

            if (_isFading)
                return;

            _runner.Run(FadeRoutine(1f));
        }

        public void RequestFadeOut()
        {
            if (_runner == null)
            {
                Debug.LogWarning("[FadeService] RequestFadeOut ignorado: nenhum CoroutineRunner registrado.");
                return;
            }

            if (_isFading)
                return;

            _runner.Run(FadeRoutine(0f));
        }

        private IEnumerator FadeRoutine(float targetAlpha)
        {
            _isFading = true;

            if (!_isLoaded)
            {
                Debug.Log("[FadeService] Carregando cena FadeScene...");
                var loadOp = SceneManager.LoadSceneAsync(FadeSceneName, LoadSceneMode.Additive);
                if (loadOp != null)
                    yield return loadOp;

                var scene = SceneManager.GetSceneByName(FadeSceneName);
                if (!scene.IsValid())
                {
                    Debug.LogError("[FadeService] Cena FadeScene não encontrada ou inválida.");
                    _isFading = false;
                    yield break;
                }

                foreach (var root in scene.GetRootGameObjects())
                {
                    _fadeController = root.GetComponentInChildren<FadeController>(true);
                    if (_fadeController != null)
                        break;
                }

                if (_fadeController == null)
                {
                    Debug.LogError("[FadeService] FadeController não encontrado na cena FadeScene.");
                    _isFading = false;
                    yield break;
                }

                _isLoaded = true;
                Debug.Log("[FadeService] FadeController localizado.");
            }

            // Executa o fade na UI
            yield return _fadeController.FadeTo(targetAlpha);

            // Após completar, descarrega a cena de fade para não manter overlay o tempo todo.
            if (_isLoaded)
            {
                Debug.Log("[FadeService] Unloading cena FadeScene...");
                var unloadOp = SceneManager.UnloadSceneAsync(FadeSceneName);
                if (unloadOp != null)
                    yield return unloadOp;

                _isLoaded = false;
                _fadeController = null;
            }

            _isFading = false;
        }
    }
}
