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
    public sealed class NewScriptsLoadingHudService : INewScriptsLoadingHudService
    {
        private const string LoadingHudSceneName = "LoadingHudScene";

        private readonly SemaphoreSlim _ensureGate = new(1, 1);
        private NewScriptsLoadingHudController _controller;

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
                        DebugUtility.LogVerbose<NewScriptsLoadingHudService>(
                            $"[LoadingHUD] Carregando cena '{LoadingHudSceneName}' (Additive)...");

                        var loadOp = SceneManager.LoadSceneAsync(LoadingHudSceneName, LoadSceneMode.Additive);
                        if (loadOp == null)
                        {
                            DebugUtility.LogWarning<NewScriptsLoadingHudService>(
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
                        DebugUtility.LogWarning<NewScriptsLoadingHudService>(
                            "[LoadingHUD] Nenhum NewScriptsLoadingHudController encontrado na cena.");
                    }
                    else
                    {
                        DebugUtility.LogVerbose<NewScriptsLoadingHudService>(
                            "[LoadingHUD] NewScriptsLoadingHudController localizado com sucesso.");
                    }
                }
                catch (Exception ex)
                {
                    DebugUtility.LogWarning<NewScriptsLoadingHudService>(
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
                DebugUtility.LogWarning<NewScriptsLoadingHudService>(
                    $"[LoadingHUD] Show ignorado: controller indisponível. signature='{signature}', phase='{phase}'.");
                return;
            }

            _controller.Show(phase);

            DebugUtility.LogVerbose<NewScriptsLoadingHudService>(
                $"[LoadingHUD] Show aplicado. signature='{signature}', phase='{phase}'.");
        }

        public void Hide(string signature, string phase)
        {
            if (!TryEnsureController())
            {
                DebugUtility.LogWarning<NewScriptsLoadingHudService>(
                    $"[LoadingHUD] Hide ignorado: controller indisponível. signature='{signature}', phase='{phase}'.");
                return;
            }

            _controller.Hide(phase);

            DebugUtility.LogVerbose<NewScriptsLoadingHudService>(
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
            _controller = UnityEngine.Object.FindAnyObjectByType<NewScriptsLoadingHudController>();
        }

        private static bool IsControllerValid(NewScriptsLoadingHudController controller)
        {
            if (controller == null)
            {
                return false;
            }

            if (controller is Object unityObject && unityObject == null)
            {
                return false;
            }

            return true;
        }
    }
}
