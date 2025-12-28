#if UNITY_EDITOR || DEVELOPMENT_BUILD || NEWSCRIPTS_QA
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Gameplay.Reset
{
    /// <summary>
    /// Instrumentação de QA para tornar o Player observável no GameplayReset.
    /// Não altera input/movimento; apenas loga as fases executadas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplayResetPhaseLogger : MonoBehaviour, IGameplayResettable
    {
        [SerializeField]
        [Tooltip("Se true, logs detalhados de cada fase.")]
        private bool verboseLogs = true;

        public Task ResetCleanupAsync(GameplayResetContext ctx)
        {
            LogPhase("Cleanup", ctx);
            return Task.CompletedTask;
        }

        public Task ResetRestoreAsync(GameplayResetContext ctx)
        {
            LogPhase("Restore", ctx);
            return Task.CompletedTask;
        }

        public Task ResetRebindAsync(GameplayResetContext ctx)
        {
            LogPhase("Rebind", ctx);
            return Task.CompletedTask;
        }

        private void LogPhase(string phase, GameplayResetContext ctx)
        {
            if (!verboseLogs)
            {
                return;
            }

            string name = gameObject != null ? gameObject.name : "<null>";
            DebugUtility.Log(typeof(GameplayResetPhaseLogger),
                $"[GameplayReset][Player] {phase} (actor='{name}', target={ctx.Request.Target})");
        }
    }
}
#endif
