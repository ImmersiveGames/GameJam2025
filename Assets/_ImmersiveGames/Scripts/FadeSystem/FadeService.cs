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

            _fadeInBinding  = new EventBinding<FadeInRequestedEvent>(_ => RequestFadeIn());
            _fadeOutBinding = new EventBinding<FadeOutRequestedEvent>(_ => RequestFadeOut());

            EventBus<FadeInRequestedEvent>.Register(_fadeInBinding);
            EventBus<FadeOutRequestedEvent>.Register(_fadeOutBinding);
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

            // Fade até preto (alpha = 1). Mantemos a cena de fade carregada.
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

            // Fade até transparente (alpha = 0). Depois descarregamos a cena de fade.
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

            if (_fadeController != null)
            {
                // Executa o fade com a duração configurada no FadeController (in/out).
                yield return _fadeController.FadeTo(targetAlpha);
            }
            else
            {
                Debug.LogWarning("[FadeService] FadeController nulo em FadeRoutine.");
            }

            // Regra de descarregamento:
            // - Se estamos fazendo FadeIn (targetAlpha ~ 1): mantemos a cena de fade carregada
            //   para cobrir o carregamento / descarregamento das outras cenas.
            // - Se estamos fazendo FadeOut (targetAlpha ~ 0): após clarear removemos a FadeScene.
            bool shouldUnloadAfter = _isLoaded && targetAlpha <= 0f + 0.001f;

            if (shouldUnloadAfter)
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
