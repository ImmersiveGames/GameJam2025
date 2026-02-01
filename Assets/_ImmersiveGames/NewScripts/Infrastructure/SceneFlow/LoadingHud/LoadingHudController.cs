using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.DebugLog;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.LoadingHud
{
    public sealed class LoadingHudController : MonoBehaviour
    {
        private TaskCompletionSource<bool> _readyTcs;
        private string _lastContextSignature;

        // Eventos externos
        public event Action<string> OnLoadingHudStarted;
        public event Action<string> OnLoadingHudReady;

        public string LastSignature => _lastContextSignature;

        private void Awake()
        {
            _readyTcs = new TaskCompletionSource<bool>();
        }

        public void StartLoading(string contextSignature)
        {
            if (!string.IsNullOrEmpty(contextSignature) && contextSignature != "no-signature")
            {
                _lastContextSignature = contextSignature;
            }

            // reinicializa sinal para novo ciclo
            if (_readyTcs.Task.IsCompleted)
            {
                _readyTcs = new TaskCompletionSource<bool>();
            }

            DebugUtility.LogVerbose<LoadingHudController>($"[OBS][LoadingHud] LoadingStarted signature={_lastContextSignature ?? contextSignature}");
            OnLoadingHudStarted?.Invoke(_lastContextSignature ?? contextSignature);
        }

        public void SetReady(string contextSignature)
        {
            if (!string.IsNullOrEmpty(contextSignature) && contextSignature != "no-signature")
            {
                _lastContextSignature = contextSignature;
            }

            if (!_readyTcs.Task.IsCompleted)
            {
                _readyTcs.TrySetResult(true);
            }

            DebugUtility.LogVerbose<LoadingHudController>($"[OBS][LoadingHud] LoadingReady signature={_lastContextSignature ?? contextSignature}");
            OnLoadingHudReady?.Invoke(_lastContextSignature ?? contextSignature);
        }

        public Task WaitReadyAsync(string contextSignature)
        {
            string used = string.IsNullOrEmpty(contextSignature) || contextSignature == "no-signature"
                ? _lastContextSignature ?? "no-signature"
                : contextSignature;
            DebugUtility.LogVerbose<LoadingHudController>($"[LoadingHud] WaitReadyAsync called signature={used}");
            return _readyTcs.Task;
        }
    }
}

// Adicionar/alterar:
// - Expor evento OnLoadingHudReady(signature).
// - Publicar `[OBS][LoadingHud] LoadingStarted/LoadingReady` com contextSignature.
// - Compatibilizar espera conjunta Fade+LoadingHud para gerar ScenesReadyEvent.
