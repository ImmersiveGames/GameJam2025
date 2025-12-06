﻿using System.Collections;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace _ImmersiveGames.Scripts.FadeSystem
{
    public class FadeService : IFadeService
    {
        private const string FadeSceneName = "FadeScene";

        private FadeController _fadeController;
        private bool _isInitializing;
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

        ~FadeService()
        {
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

            _runner.Run(FadeInAsync());
        }

        public void RequestFadeOut()
        {
            if (_runner == null)
            {
                Debug.LogWarning("[FadeService] RequestFadeOut ignorado: nenhum CoroutineRunner registrado.");
                return;
            }

            _runner.Run(FadeOutAsync());
        }

        /// <summary>
        /// Garante que o FadeController persistente foi criado a partir da FadeScene.
        /// </summary>
        private IEnumerator EnsureFadeControllerInitialized()
        {
            if (_fadeController != null)
                yield break;

            if (_isInitializing)
            {
                // Se já está inicializando em outra corrotina, apenas espera.
                while (_isInitializing)
                    yield return null;
                yield break;
            }

            _isInitializing = true;

            Debug.Log("[FadeService] Carregando FadeScene para inicialização do fade...");

            var loadOp = SceneManager.LoadSceneAsync(FadeSceneName, LoadSceneMode.Additive);
            if (loadOp != null)
                yield return loadOp;

            var scene = SceneManager.GetSceneByName(FadeSceneName);
            if (!scene.IsValid())
            {
                Debug.LogError("[FadeService] Cena FadeScene não encontrada ou inválida.");
                _isInitializing = false;
                yield break;
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

            if (prefabController == null)
            {
                Debug.LogError("[FadeService] FadeController não encontrado na FadeScene.");
                _isInitializing = false;
                yield break;
            }

            // Clona o objeto do FadeController para um objeto persistente.
            var instance = Object.Instantiate(prefabRoot);
            Object.DontDestroyOnLoad(instance);

            _fadeController = instance.GetComponent<FadeController>();

            // Opcional: descarrega a cena container, já que o overlay persistente está criado.
            Debug.Log("[FadeService] Descarregando FadeScene container após inicialização.");
            var unloadOp = SceneManager.UnloadSceneAsync(FadeSceneName);
            if (unloadOp != null)
                yield return unloadOp;

            if (_fadeController == null)
            {
                Debug.LogError("[FadeService] FadeController do clone persistente é nulo.");
            }
            else
            {
                Debug.Log("[FadeService] FadeController persistente inicializado com sucesso.");
            }

            _isInitializing = false;
        }

        public IEnumerator FadeInAsync()
        {
            // Se já está em um fade, espera terminar (ou poderia cancelar o anterior, se quiser).
            while (_isFading)
                yield return null;

            _isFading = true;

            yield return EnsureFadeControllerInitialized();

            if (_fadeController != null)
                yield return _fadeController.FadeTo(1f);
            else
                Debug.LogWarning("[FadeService] FadeInAsync chamado, mas FadeController é nulo.");

            _isFading = false;
        }

        public IEnumerator FadeOutAsync()
        {
            while (_isFading)
                yield return null;

            _isFading = true;

            yield return EnsureFadeControllerInitialized();

            if (_fadeController != null)
                yield return _fadeController.FadeTo(0f);
            else
                Debug.LogWarning("[FadeService] FadeOutAsync chamado, mas FadeController é nulo.");

            _isFading = false;
        }
    }
}
