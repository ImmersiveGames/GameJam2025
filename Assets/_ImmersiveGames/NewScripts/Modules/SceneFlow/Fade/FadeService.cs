using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Fade.Bindings;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Fade
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
        private const string FadeSceneName = "FadeScene";

        private readonly SemaphoreSlim _ensureGate = new(1, 1);

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

        public FadeService()
        {
            _config = DefaultConfig;
        }

        public void Configure(FadeConfig config)
        {
            _config = config;
        }

        public async Task FadeInAsync()
        {
            if (IsNoOpFadeIn())
            {
                return;
            }

            await EnsureControllerAsync();

            _controller.Configure(_config);
            await _controller.FadeInAsync();
        }

        public async Task FadeOutAsync()
        {
            if (IsNoOpFadeOut())
            {
                return;
            }

            await EnsureControllerAsync();

            _controller.Configure(_config);
            await _controller.FadeOutAsync();
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

                var fadeScene = SceneManager.GetSceneByName(FadeSceneName);

                if (!fadeScene.IsValid() || !fadeScene.isLoaded)
                {
                    DebugUtility.LogVerbose<FadeService>(
                        $"[Fade] Garantindo FadeScene '{FadeSceneName}' (Additive)...",
                        isFallback: true);

                    var loadOp = SceneManager.LoadSceneAsync(FadeSceneName, LoadSceneMode.Additive);
                    if (loadOp == null)
                    {
                        MarkPermanentlyUnavailable(
                            reason: "load_scene_async_null",
                            detail: $"LoadSceneAsync retornou null para '{FadeSceneName}'. Verifique Build Settings.");
                    }

                    while (!loadOp.isDone)
                    {
                        await Task.Yield();
                    }

                    // Um yield extra para garantir que objetos estejam prontos.
                    await Task.Yield();

                    fadeScene = SceneManager.GetSceneByName(FadeSceneName);
                }

                if (!fadeScene.IsValid() || !fadeScene.isLoaded)
                {
                    MarkPermanentlyUnavailable(
                        reason: "fade_scene_not_loaded",
                        detail: $"FadeScene '{FadeSceneName}' não está carregada após LoadSceneAsync.");
                }

                _controller = FindControllerInScene(fadeScene);

                if (_controller == null)
                {
                    MarkPermanentlyUnavailable(
                        reason: "fade_controller_missing",
                        detail: $"Nenhum {nameof(FadeController)} encontrado na FadeScene '{FadeSceneName}'.");
                }

                DebugUtility.LogVerbose<FadeService>(
                    "[Fade] FadeController localizado com sucesso.");
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

        private void MarkPermanentlyUnavailable(string reason, string detail)
        {
            _isPermanentlyUnavailable = true;
            _permanentFailureDetail = $"reason='{reason}' detail='{detail}'";

            DebugUtility.LogError<FadeService>(
                $"[Fade] Falha ao garantir FadeScene/controller. {_permanentFailureDetail}");

            throw new InvalidOperationException(
                $"[Fade] Pré-condição inválida. {_permanentFailureDetail}");
        }
    }
}
