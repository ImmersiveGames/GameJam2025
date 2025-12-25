using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.Fade
{
    /// <summary>
    /// Serviço de Fade do NewScripts:
    /// - Garante que a FadeScene esteja carregada (Additive).
    /// - Localiza o NewScriptsFadeController dentro da FadeScene.
    /// - Fail-safe: não lança exceção se não achar cena/controller (apenas loga e segue).
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class NewScriptsFadeService : INewScriptsFadeService
    {
        private const string FadeSceneName = "FadeScene";

        private readonly SemaphoreSlim _ensureGate = new(1, 1);
        private NewScriptsFadeController _controller;
        private NewScriptsFadeConfig _config;

        // Defaults mínimos (ciclo mínimo).
        private static readonly NewScriptsFadeConfig DefaultConfig =
            new NewScriptsFadeConfig(
                fadeInDuration: 0.5f,
                fadeOutDuration: 0.5f,
                fadeInCurve: AnimationCurve.EaseInOut(0f, 0f, 1f, 1f),
                fadeOutCurve: AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));

        public NewScriptsFadeService()
        {
            _config = DefaultConfig;
        }

        public void Configure(NewScriptsFadeConfig config)
        {
            _config = config;
        }

        public async Task FadeInAsync()
        {
            await EnsureControllerAsync();
            if (_controller == null)
            {
                DebugUtility.LogWarning<NewScriptsFadeService>(
                    "[Fade] FadeIn solicitado, mas controller não está disponível. Continuando sem fade.");
                return;
            }

            _controller.Configure(_config);
            await _controller.FadeInAsync();
        }

        public async Task FadeOutAsync()
        {
            await EnsureControllerAsync();
            if (_controller == null)
            {
                DebugUtility.LogWarning<NewScriptsFadeService>(
                    "[Fade] FadeOut solicitado, mas controller não está disponível. Continuando sem fade.");
                return;
            }

            _controller.Configure(_config);
            await _controller.FadeOutAsync();
        }

        private async Task EnsureControllerAsync()
        {
            if (_controller != null)
            {
                return;
            }

            // Garantir que apenas uma tentativa ocorra por vez.
            if (!await _ensureGate.WaitAsync(0))
            {
                // Alguém já está garantindo; aguardar brevemente e sair.
                await Task.Yield();
                return;
            }

            try
            {
                if (_controller != null)
                {
                    return;
                }

                try
                {
                    var fadeScene = SceneManager.GetSceneByName(FadeSceneName);

                    if (!fadeScene.IsValid() || !fadeScene.isLoaded)
                    {
                        DebugUtility.LogVerbose<NewScriptsFadeService>(
                            $"[Fade] Carregando FadeScene '{FadeSceneName}' (Additive)...");

                        var loadOp = SceneManager.LoadSceneAsync(FadeSceneName, LoadSceneMode.Additive);
                        if (loadOp == null)
                        {
                            DebugUtility.LogWarning<NewScriptsFadeService>(
                                $"[Fade] LoadSceneAsync retornou null para '{FadeSceneName}'. Verifique Build Settings.");
                            return;
                        }

                        while (!loadOp.isDone)
                        {
                            await Task.Yield();
                        }
                    }

                    // Um yield extra para garantir que objetos estejam prontos.
                    await Task.Yield();

                    // Procurar controller (o ADR exige controller NewScripts na FadeScene).
                    _controller = UnityEngine.Object.FindAnyObjectByType<NewScriptsFadeController>();

                    if (_controller == null)
                    {
                        DebugUtility.LogWarning<NewScriptsFadeService>(
                            "[Fade] Nenhum NewScriptsFadeController encontrado. " +
                            "Confirme se a FadeScene contém CanvasGroup + NewScriptsFadeController.");
                    }
                    else
                    {
                        DebugUtility.LogVerbose<NewScriptsFadeService>(
                            "[Fade] NewScriptsFadeController localizado com sucesso.");
                    }
                }
                catch (Exception ex)
                {
                    DebugUtility.LogWarning<NewScriptsFadeService>(
                        $"[Fade] Falha ao garantir FadeScene/controller. Continuando sem fade. ({ex.Message})");
                }
            }
            finally
            {
                _ensureGate.Release();
            }
        }
    }
}
