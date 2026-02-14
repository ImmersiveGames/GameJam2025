using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Fade.Bindings;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Fade.Runtime
{
    /// <summary>
    /// Serviço de Fade do NewScripts.
    ///
    /// Contrato:
    /// - Depende de uma FadeScene (Additive) contendo FadeController.
    /// - NÃO cria UI "em voo" como fallback silencioso.
    /// - Quando o fade é solicitado (dur > 0), ausência de cena/controller é tratada como falha.
    ///
    /// Observação:
    /// - Política Strict/Release e DEGRADED_MODE são responsabilidades do adapter/orquestrador
    ///   (ex.: SceneTransitionService + ISceneFlowFadeAdapter). Este serviço apenas expõe
    ///   uma execução determinística e falha de forma explícita quando pré-condições não são atendidas.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class FadeService : IFadeService
    {
        private readonly SemaphoreSlim _ensureGate = new(1, 1);
        private readonly string _fadeSceneName;

        private FadeController _controller;
        private bool _isPermanentlyUnavailable;
        private string _permanentFailureDetail = string.Empty;

        private FadeConfig _config;

        // Defaults mínimos (ciclo mínimo).
        private static readonly FadeConfig DefaultConfig =
            new(
                fadeInDuration: 0.5f,
                fadeOutDuration: 0.5f,
                fadeInCurve: AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                fadeOutCurve: AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));

        public FadeService(string fadeSceneName)
        {
            _fadeSceneName = (fadeSceneName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(_fadeSceneName))
            {
                AbortAsFatal(
                    "fade_scene_name_missing",
                    "Fade scene name is required in bootstrap config.");
            }

            _config = DefaultConfig;
        }

        public Task EnsureReadyAsync()
        {
            return EnsureControllerAsync();
        }

        public void Configure(FadeConfig config)
        {
            _config = config;
        }

        public async Task FadeInAsync(string? contextSignature = null)
        {
            if (IsNoOpFadeIn())
            {
                return;
            }

            await EnsureControllerAsync();

            _controller.Configure(_config);
            await _controller.FadeInAsync(contextSignature);
        }

        public async Task FadeOutAsync(string? contextSignature = null)
        {
            if (IsNoOpFadeOut())
            {
                return;
            }

            await EnsureControllerAsync();

            _controller.Configure(_config);
            await _controller.FadeOutAsync(contextSignature);
        }

        private bool IsNoOpFadeIn()
        {
            // Comentário: no-op de fade é uma configuração válida (profile UseFade=false).
            return _config.FadeInDuration <= 0f;
        }

        private bool IsNoOpFadeOut()
        {
            return _config.FadeOutDuration <= 0f;
        }

        private async Task EnsureControllerAsync()
        {
            if (_controller != null)
            {
                return;
            }

            if (_isPermanentlyUnavailable)
            {
                throw new InvalidOperationException(
                    $"[Fade] Fade indisponível: {_permanentFailureDetail}");
            }

            // Garantir que apenas uma tentativa ocorra por vez.
            if (!await _ensureGate.WaitAsync(0))
            {
                // Alguém já está garantindo; aguardar breve yield e revalidar.
                await Task.Yield();

                if (_controller != null)
                {
                    return;
                }

                if (_isPermanentlyUnavailable)
                {
                    throw new InvalidOperationException(
                        $"[Fade] Fade indisponível: {_permanentFailureDetail}");
                }

                // Se ainda não temos controller, caímos para uma tentativa "normal".
                await _ensureGate.WaitAsync();
            }

            try
            {
                if (_controller != null)
                {
                    return;
                }

                var fadeScene = SceneManager.GetSceneByName(_fadeSceneName);

                if (!fadeScene.IsValid() || !fadeScene.isLoaded)
                {
                    DebugUtility.LogVerbose<FadeService>(
                        $"[Fade] Ensuring FadeScene '{_fadeSceneName}' (Additive)...");

                    var loadOp = SceneManager.LoadSceneAsync(_fadeSceneName, LoadSceneMode.Additive);
                    if (loadOp == null)
                    {
                        AbortAsFatal(
                            reason: "load_scene_async_null",
                            detail: $"LoadSceneAsync returned null for '{_fadeSceneName}'. Check Build Settings.");
                    }

                    while (!loadOp.isDone)
                    {
                        await Task.Yield();
                    }

                    // Um yield extra para garantir que objetos estejam prontos.
                    await Task.Yield();

                    fadeScene = SceneManager.GetSceneByName(_fadeSceneName);
                }

                if (!fadeScene.IsValid() || !fadeScene.isLoaded)
                {
                    AbortAsFatal(
                        reason: "fade_scene_not_loaded",
                        detail: $"FadeScene '{_fadeSceneName}' is not loaded after LoadSceneAsync.");
                }

                _controller = FindControllerInScene(fadeScene);

                if (_controller == null)
                {
                    AbortAsFatal(
                        reason: "fade_controller_missing",
                        detail: $"No {nameof(FadeController)} found in FadeScene '{_fadeSceneName}'.");
                }

                DebugUtility.LogVerbose<FadeService>(
                    $"[OBS][Fade] FadeScene ready (source=FadeService/EnsureController, scene='{_fadeSceneName}').");
            }
            finally
            {
                _ensureGate.Release();
            }
        }

        private static FadeController FindControllerInScene(UnityEngine.SceneManagement.Scene scene)
        {
            // Comentário: evita FindAnyObjectByType (custo + risco de pegar controller errado fora da FadeScene).
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++)
                {
                    var controller = roots[i].GetComponentInChildren<FadeController>(true);
                    if (controller != null)
                    {
                        return controller;
                    }
                }
            }
            catch (Exception)
            {
                // Comentário: exceção aqui não deve ser engolida silenciosamente; o método chamador tratará como falha.
            }

            return null;
        }

        private void AbortAsFatal(string reason, string detail)
        {
            _isPermanentlyUnavailable = true;
            _permanentFailureDetail = $"reason='{reason}' detail='{detail}'";

            DebugUtility.LogError<FadeService>(
                $"[FATAL][Fade] Fade bootstrap failed. {_permanentFailureDetail}");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            if (!Application.isEditor)
            {
                Application.Quit();
            }

            throw new InvalidOperationException(
                $"[FATAL][Fade] Required FadeScene/FadeController unavailable. {_permanentFailureDetail}");
        }
    }
}
