using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Gameplay.CoreGameplay.GameLoop
{
    public class IntroStageController : MonoBehaviour
    {
        private TaskCompletionSource<bool> _confirmTcs;

        public event Action<string> OnIntroStarted;
        public event Action<string> OnIntroConfirmed;

        public Task StartIntro(string contextSignature)
        {
            DebugUtility.LogVerbose<IntroStageController>($"[OBS] IntroStarted signature={contextSignature}");
            OnIntroStarted?.Invoke(contextSignature);

            _confirmTcs = new TaskCompletionSource<bool>();
            // aqui bloqueamos sim.gameplay até ConfirmIntro ser chamado
            return _confirmTcs.Task;
        }

        public void ConfirmIntro(string contextSignature)
        {
            if (_confirmTcs != null && !_confirmTcs.Task.IsCompleted)
            {
                _confirmTcs.TrySetResult(true);
            }

            DebugUtility.LogVerbose<IntroStageController>($"[OBS] IntroConfirmed signature={contextSignature}");
            OnIntroConfirmed?.Invoke(contextSignature);
        }
    }
}
