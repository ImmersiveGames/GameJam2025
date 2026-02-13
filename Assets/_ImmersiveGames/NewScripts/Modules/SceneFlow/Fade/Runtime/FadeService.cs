using System;
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
    /// - Depende de uma FadeScene já carregada contendo FadeController.
    /// - Não executa carregamento automático.
    /// - Falha de pré-condição obrigatória dispara erro fatal imediato.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class FadeService : IFadeService
    {
        public const string RequiredFadeSceneName = "FadeScene";

        private FadeController _controller;
        private FadeConfig _config;

        // Defaults mínimos (ciclo mínimo).
        private static readonly FadeConfig DefaultConfig =
            new(
                fadeInDuration: 0.5f,
                fadeOutDuration: 0.5f,
                fadeInCurve: AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                fadeOutCurve: AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));

        public FadeService()
        {
            _config = DefaultConfig;
        }

        public void Configure(FadeConfig config)
        {
            _config = config;
        }

        public async Task FadeInAsync(string? contextSignature = null)
        {
            if (RuntimeFailFastUtility.IsFatalLatched)
            {
                DebugUtility.LogVerbose<FadeService>("[OBS][Boot] Aborting fade request due to fatal latch.", DebugUtility.Colors.Info);
                return;
            }

            if (IsNoOpFadeIn())
            {
                return;
            }

            EnsureControllerOrThrow();

            _controller.Configure(_config);
            await _controller.FadeInAsync(contextSignature);
        }

        public async Task FadeOutAsync(string? contextSignature = null)
        {
            if (RuntimeFailFastUtility.IsFatalLatched)
            {
                DebugUtility.LogVerbose<FadeService>("[OBS][Boot] Aborting fade request due to fatal latch.", DebugUtility.Colors.Info);
                return;
            }

            if (IsNoOpFadeOut())
            {
                return;
            }

            EnsureControllerOrThrow();

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

        private void EnsureControllerOrThrow()
        {
            if (_controller != null)
            {
                return;
            }

            var fadeScene = SceneManager.GetSceneByName(RequiredFadeSceneName);
            if (!fadeScene.IsValid() || !fadeScene.isLoaded)
            {
                FailFast(
                    reason: "fade_scene_not_loaded",
                    detail: $"Required FadeScene '{RequiredFadeSceneName}' is not loaded.");
            }

            _controller = FindControllerInScene(fadeScene);
            if (_controller == null)
            {
                FailFast(
                    reason: "fade_controller_missing",
                    detail: $"No {nameof(FadeController)} found in required FadeScene '{RequiredFadeSceneName}'.");
            }

            DebugUtility.LogVerbose<FadeService>(
                "[OBS][Fade] FadeController resolved from required FadeScene.");
        }

        private static FadeController FindControllerInScene(Scene scene)
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

            return null;
        }

        private static void FailFast(string reason, string detail)
        {
            throw RuntimeFailFastUtility.FailFastAndCreateException(
                "Fade",
                $"Required fade dependency missing. reason='{reason}' detail='{detail}'.");
        }
    }
}
