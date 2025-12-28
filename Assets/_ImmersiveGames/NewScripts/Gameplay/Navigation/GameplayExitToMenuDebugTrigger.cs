#if UNITY_EDITOR || DEVELOPMENT_BUILD
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Gameplay.Navigation
{
    /// <summary>
    /// Trigger simples (dev/debug) para emitir ExitToMenu durante Gameplay.
    /// Use apenas quando não houver UI/overlay configurado.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplayExitToMenuDebugTrigger : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField]
        [Tooltip("Habilita o trigger em builds de Editor/Development.")]
        private bool debugEnabled = true;

        [SerializeField]
        [Tooltip("Tecla para solicitar ExitToMenu em Gameplay.")]
        private KeyCode triggerKey = KeyCode.Escape;

        [SerializeField]
        [Tooltip("Razão usada para logs quando o ExitToMenu é disparado.")]
        private string reason = "Debug/ExitToMenuKey";

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!debugEnabled)
            {
                return;
            }

            if (!Input.GetKeyDown(triggerKey))
            {
                return;
            }

            DebugUtility.Log(typeof(GameplayExitToMenuDebugTrigger),
                $"[Gameplay][Debug] ExitToMenu solicitado por tecla ({triggerKey}). reason='{reason}'.",
                DebugUtility.Colors.Warning);

            EventBus<GameExitToMenuRequestedEvent>.Raise(new GameExitToMenuRequestedEvent());
#else
            // Trigger desativado em builds de produção.
#endif
        }
    }
}
#endif
