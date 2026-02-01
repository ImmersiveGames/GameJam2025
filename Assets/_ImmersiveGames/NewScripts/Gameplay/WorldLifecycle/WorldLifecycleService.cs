// Adicionar/alterar:
// - Método public Task ResetWorld(string reason, string contextSignature)
//   * executar pipeline determinístico: Reset -> Spawn -> Rearm
//   * publicar ResetWorldStarted(reason, contextSignature) e ResetCompleted(reason, contextSignature)
//   * garantir ResetCompleted publicado exatamente uma vez por reset efetivo (locks/guards).
// - Emitir evidências/ancoras `[OBS] ResetWorldStarted/ResetCompleted`.

using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.DebugLog;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Gameplay.WorldLifecycle
{
    public sealed class WorldLifecycleService : MonoBehaviour
    {
        private readonly object _resetLock = new();
        private bool _isResetting;

        public event Action<string, string> ResetWorldStarted;
        public event Action<string, string> ResetWorldCompleted;

        [SerializeField] private ResetPipeline resetPipeline;

        private void Awake()
        {
            if (resetPipeline == null)
            {
                resetPipeline = GetComponentInChildren<ResetPipeline>();
            }
        }

        public async Task ResetWorld(string reason, string contextSignature)
        {
            // Guard: não permitir resets concorrentes
            lock (_resetLock)
            {
                if (_isResetting)
                {
                    DebugUtility.LogWarning<WorldLifecycleService>($"[ResetWorld] Ignorando reset concorrente reason={reason} signature={contextSignature}");
                    return;
                }
                _isResetting = true;
            }

            DebugUtility.LogVerbose<WorldLifecycleService>($"[OBS] ResetWorldStarted reason={reason} signature={contextSignature}");
            try
            {
                ResetWorldStarted?.Invoke(reason, contextSignature);

                if (resetPipeline == null)
                {
                    DebugUtility.LogWarning<WorldLifecycleService>("[ResetWorld] resetPipeline não configurado, pulando execução.");
                }
                else
                {
                    await resetPipeline.RunAsync(contextSignature);
                }

                DebugUtility.LogVerbose<WorldLifecycleService>($"[OBS] ResetWorldCompleted reason={reason} signature={contextSignature}");
                ResetWorldCompleted?.Invoke(reason, contextSignature);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<WorldLifecycleService>($"[ResetWorld] Falha ao resetar world reason={reason} signature={contextSignature} ex={ex}");
                throw;
            }
            finally
            {
                lock (_resetLock)
                {
                    _isResetting = false;
                }
            }
        }
    }
}
