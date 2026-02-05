#if UNITY_EDITOR || DEVELOPMENT_BUILD || NEWSCRIPTS_QA
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors.Runtime;
using _ImmersiveGames.NewScripts.Modules.Gameplay.RunRearm.Runtime;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.RunRearm.Dev
{
    /// <summary>
    /// Instrumentação de QA para tornar o Player observável no GameplayReset.
    /// Não altera input/movimento; apenas loga as etapas executadas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RunRearmStepLogger : MonoBehaviour, IRunRearmable
    {
        [SerializeField]
        [Tooltip("Se true, logs detalhados de cada etapa.")]
        private bool verboseLogs = true;

        private IActor _actor;

        private void Awake()
        {
            _actor = GetComponent<IActor>();
        }

        public Task ResetCleanupAsync(RunRearmContext ctx)
        {
            LogStep("Cleanup", ctx);
            return Task.CompletedTask;
        }

        public Task ResetRestoreAsync(RunRearmContext ctx)
        {
            LogStep("Restore", ctx);
            return Task.CompletedTask;
        }

        public Task ResetRebindAsync(RunRearmContext ctx)
        {
            LogStep("Rebind", ctx);
            return Task.CompletedTask;
        }

        private void LogStep(string step, RunRearmContext ctx)
        {
            if (!verboseLogs)
            {
                return;
            }

            string name = _actor?.DisplayName ?? (gameObject != null ? gameObject.name : "<null>");
            string actorId = _actor?.ActorId ?? "<unknown>";
            DebugUtility.Log(typeof(RunRearmStepLogger),
                $"[GameplayReset][Player] {step} (actor='{name}', id={actorId}, target={ctx.Request.Target})");
        }
    }
}
#endif


