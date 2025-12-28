using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.Loading
{
    /// <summary>
    /// Serviço de Loading HUD do NewScripts:
    /// - Garante que a LoadingHudScene esteja carregada (Additive).
    /// - Localiza o NewScriptsLoadingHudController dentro da LoadingHudScene.
    /// - Aplica pendências de show/hide/progress quando o controller estiver disponível.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class NewScriptsLoadingHudService : INewScriptsLoadingHudService
    {
        private const string LoadingHudSceneName = "LoadingHudScene";

        private readonly SemaphoreSlim _ensureGate = new(1, 1);

        private NewScriptsLoadingHudController _controller;
        private bool _pendingVisible;
        private bool _hasPending;
        private string _pendingTitle;
        private string _pendingDetails;
        private float _pendingProgress01;

        public void Show(string title, string details, float progress01)
        {
            _pendingVisible = true;
            _pendingTitle = title;
            _pendingDetails = details;
            _pendingProgress01 = Clamp01(progress01);
            _hasPending = true;

            _ = EnsureAndApplyAsync("show");
        }

        public void SetProgress01(float progress01)
        {
            _pendingProgress01 = Clamp01(progress01);
            _hasPending = true;

            _ = EnsureAndApplyAsync("progress");
        }

        public void Hide()
        {
            _pendingVisible = false;
            _hasPending = true;

            _ = EnsureAndApplyAsync("hide");
        }

        private async Task EnsureAndApplyAsync(string reason)
        {
            await EnsureControllerAsync();
            ApplyPendingState(reason);
        }

        private void ApplyPendingState(string reason)
        {
            if (!_hasPending)
            {
                return;
            }

            if (_controller == null)
            {
                DebugUtility.LogVerbose<NewScriptsLoadingHudService>(
                    $"[Loading HUD] Controller indisponível ao aplicar '{reason}'. Pendência mantida.");
                return;
            }

            if (_pendingVisible)
            {
                _controller.Show(_pendingTitle, _pendingDetails);
                _controller.SetProgress01(_pendingProgress01);
            }
            else
            {
                _controller.Hide();
            }
        }

        private async Task EnsureControllerAsync()
        {
            if (_controller != null)
            {
                return;
            }

            if (!await _ensureGate.WaitAsync(0))
            {
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
                    var hudScene = SceneManager.GetSceneByName(LoadingHudSceneName);

                    if (!hudScene.IsValid() || !hudScene.isLoaded)
                    {
                        DebugUtility.LogVerbose<NewScriptsLoadingHudService>(
                            $"[Loading HUD] Carregando LoadingHudScene '{LoadingHudSceneName}' (Additive)...");

                        var loadOp = SceneManager.LoadSceneAsync(LoadingHudSceneName, LoadSceneMode.Additive);
                        if (loadOp == null)
                        {
                            DebugUtility.LogWarning<NewScriptsLoadingHudService>(
                                $"[Loading HUD] LoadSceneAsync retornou null para '{LoadingHudSceneName}'. Verifique Build Settings.");
                            return;
                        }

                        while (!loadOp.isDone)
                        {
                            await Task.Yield();
                        }
                    }

                    await Task.Yield();

                    _controller = UnityEngine.Object.FindAnyObjectByType<NewScriptsLoadingHudController>();

                    if (_controller == null)
                    {
                        DebugUtility.LogWarning<NewScriptsLoadingHudService>(
                            "[Loading HUD] Nenhum NewScriptsLoadingHudController encontrado. " +
                            "Confirme se a LoadingHudScene contém Canvas + NewScriptsLoadingHudController.");
                    }
                    else
                    {
                        DebugUtility.LogVerbose<NewScriptsLoadingHudService>(
                            "[Loading HUD] NewScriptsLoadingHudController localizado com sucesso.");
                    }
                }
                catch (Exception ex)
                {
                    DebugUtility.LogWarning<NewScriptsLoadingHudService>(
                        $"[Loading HUD] Falha ao garantir LoadingHudScene/controller. Continuando sem HUD. ({ex.Message})");
                }
            }
            finally
            {
                _ensureGate.Release();
            }
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }
    }
}
