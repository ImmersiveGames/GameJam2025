using _ImmersiveGames.NewScripts.Core.DebugLog;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    public class GameLoopController : MonoBehaviour
    {
        [SerializeField] private IntroStageController introStage;
        [SerializeField] private WorldLifecycle.WorldLifecycleService worldLifecycle;

        private bool _isPlaying;

        private void Awake()
        {
            if (introStage == null)
            {
                introStage = GetComponentInChildren<IntroStageController>();
            }

            if (worldLifecycle == null)
            {
                worldLifecycle = FindFirstObjectByType<WorldLifecycle.WorldLifecycleService>();
            }

            if (worldLifecycle != null)
            {
                worldLifecycle.ResetWorldCompleted += OnResetCompleted;
            }
        }

        private void OnDestroy()
        {
            if (worldLifecycle != null)
            {
                worldLifecycle.ResetWorldCompleted -= OnResetCompleted;
            }
        }

        private async void OnResetCompleted(string reason, string contextSignature)
        {
            // Ao completar reset, iniciar IntroStageController que bloqueia a simulação até confirmação.
            DebugUtility.LogVerbose<GameLoopController>($"[GameLoop] ResetCompleted received reason={reason} signature={contextSignature}");
            if (introStage == null)
            {
                DebugUtility.LogWarning<GameLoopController>("[GameLoop] IntroStageController não configurado.");
                EnterPlaying(contextSignature);
                return;
            }

            // Start and wait for confirmation
            var introTask = introStage.StartIntro(contextSignature);
            await introTask; // espera até ConfirmIntro
            // Ao confirmar, publicar que a simulação está liberada.
            DebugUtility.LogVerbose<GameLoopController>($"[OBS] GameplaySimulationUnblocked signature={contextSignature}");
            DebugUtility.LogVerbose<GameLoopController>($"[GameLoop] ENTER Playing signature={contextSignature}");
            _isPlaying = true;
        }

        private void EnterPlaying(string contextSignature)
        {
            DebugUtility.LogVerbose<GameLoopController>($"[GameLoop] ENTER Playing signature={contextSignature}");
            _isPlaying = true;
        }

        // Expor API para PostGame idempotência (esqueleto)
        public void OnVictory(string contextSignature)
        {
            if (!_isPlaying)
            {
                return;
            }
            _isPlaying = false;
            DebugUtility.LogVerbose<GameLoopController>($"[GameLoop] PostGame Victory processed signature={contextSignature}");
            // TODO: aplicar UI/estado (garantir idempotência)
        }

        public void OnDefeat(string contextSignature)
        {
            if (!_isPlaying)
            {
                return;
            }
            _isPlaying = false;
            DebugUtility.LogVerbose<GameLoopController>($"[GameLoop] PostGame Defeat processed signature={contextSignature}");
            // TODO: aplicar UI/estado (garantir idempotência)
        }
    }
}
