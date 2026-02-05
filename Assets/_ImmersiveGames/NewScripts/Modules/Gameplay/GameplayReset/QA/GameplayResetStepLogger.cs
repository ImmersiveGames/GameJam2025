#if UNITY_EDITOR || DEVELOPMENT_BUILD || NEWSCRIPTS_QA
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.GameplayReset.QA
{
    /// <summary>
    /// Instrumentação de QA para tornar o Player observável no GameplayReset.
    /// Não altera input/movimento; apenas loga as etapas executadas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplayResetStepLogger : MonoBehaviour, IGameplayResettable
    {
        [SerializeField]
        [Tooltip("Se true, logs detalhados de cada etapa.")]
        private bool verboseLogs = true;

        private IActor _actor;

        private void Awake()
        {
            _actor = GetComponent<IActor>();
        }

        public Task ResetCleanupAsync(GameplayResetContext ctx)
        {
            LogStep("Cleanup", ctx);
            return Task.CompletedTask;
        }

        public Task ResetRestoreAsync(GameplayResetContext ctx)
        {
            LogStep("Restore", ctx);
            return Task.CompletedTask;
        }

        public Task ResetRebindAsync(GameplayResetContext ctx)
        {
            LogStep("Rebind", ctx);
            return Task.CompletedTask;
        }

        private void LogStep(string step, GameplayResetContext ctx)
        {
            if (!verboseLogs)
            {
                return;
            }

            string name = _actor?.DisplayName ?? (gameObject != null ? gameObject.name : "<null>");
            string actorId = _actor?.ActorId ?? "<unknown>";
            DebugUtility.Log(typeof(GameplayResetStepLogger),
                $"[GameplayReset][Player] {step} (actor='{name}', id={actorId}, target={ctx.Request.Target})");
        }
    }
}
#endif


