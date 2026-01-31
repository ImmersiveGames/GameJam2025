using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.Loading
{
    /// <summary>
    /// Serviço global para garantir o carregamento da cena de HUD de loading (Additive).
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LoadingHudService : ILoadingHudService
    {
        private const string LoadingHudSceneName = "LoadingHudScene";

        private readonly SemaphoreSlim _ensureGate = new(1, 1);
        private LoadingHudController _controller;

        public async Task EnsureLoadedAsync()
        {
            if (IsControllerValid(_controller))
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
                if (IsControllerValid(_controller))
                {
                    return;
                }

                try
                {
                    var scene = SceneManager.GetSceneByName(LoadingHudSceneName);
                    if (!scene.IsValid() || !scene.isLoaded)
                    {
                        DebugUtility.LogVerbose<LoadingHudService>(
                            $"[LoadingHUD] Carregando cena '{LoadingHudSceneName}' (Additive)...");

                        var loadOp = SceneManager.LoadSceneAsync(LoadingHudSceneName, LoadSceneMode.Additive);
                        if (loadOp == null)
                        {
                            DebugUtility.LogWarning<LoadingHudService>(
                                $"[LoadingHUD] LoadSceneAsync retornou null para '{LoadingHudSceneName}'. Verifique Build Settings.");
                            return;
                        }

                        while (!loadOp.isDone)
                        {
                            await Task.Yield();
                        }
                    }

                    await Task.Yield();
                    TryResolveController();

                    if (!IsControllerValid(_controller))
                    {
                        DebugUtility.LogWarning<LoadingHudService>(
                            "[LoadingHUD] Nenhum LoadingHudController encontrado na cena.");
                    }
                    else
                    {
                        DebugUtility.LogVerbose<LoadingHudService>(
                            "[LoadingHUD] LoadingHudController localizado com sucesso.");
                    }
                }
                catch (Exception ex)
                {
                    DebugUtility.LogWarning<LoadingHudService>(
                        $"[LoadingHUD] Falha ao garantir LoadingHudScene/controller. Continuando sem HUD. ({ex.Message})");
                }
            }
            finally
            {
                _ensureGate.Release();
            }
        }

        public void Show(string signature, string phase)
        {
            if (!TryEnsureController())
            {
                DebugUtility.LogWarning<LoadingHudService>(
                    $"[LoadingHUD] Show ignorado: controller indisponível. signature='{signature}', phase='{phase}'.");
                return;
            }

            _controller.Show(phase);

            DebugUtility.LogVerbose<LoadingHudService>(
                $"[LoadingHUD] Show aplicado. signature='{signature}', phase='{phase}'.");
        }

        public void Hide(string signature, string phase)
        {
            if (!TryEnsureController())
            {
                DebugUtility.LogWarning<LoadingHudService>(
                    $"[LoadingHUD] Hide ignorado: controller indisponível. signature='{signature}', phase='{phase}'.");
                return;
            }

            _controller.Hide(phase);

            DebugUtility.LogVerbose<LoadingHudService>(
                $"[LoadingHUD] Hide aplicado. signature='{signature}', phase='{phase}'.");
        }

        private bool TryEnsureController()
        {
            if (IsControllerValid(_controller))
            {
                return true;
            }

            TryResolveController();
            return IsControllerValid(_controller);
        }

        private void TryResolveController()
        {
            _controller = Object.FindAnyObjectByType<LoadingHudController>();
        }

        private static bool IsControllerValid(LoadingHudController controller)
        {
            if (controller == null)
            {
                return false;
            }

            return controller is not Object unityObject || unityObject != null;

        }
    }
}
