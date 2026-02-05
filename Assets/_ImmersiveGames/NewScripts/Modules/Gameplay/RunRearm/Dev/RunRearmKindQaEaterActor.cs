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
    /// QA Actor Eater para validar GameplayReset com alvo EaterOnly.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RunRearmKindQaEaterActor : MonoBehaviour, IActor, IActorKindProvider, IRunRearmable
    {
        [SerializeField]
        private string actorId = string.Empty;

        [SerializeField]
        [Tooltip("Opcional: nome amigável exibido nos logs de QA.")]
        private string displayName = "QA_Eater_Kind";

        public string ActorId => actorId;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName)
            ? (gameObject != null ? gameObject.name : nameof(RunRearmKindQaEaterActor))
            : displayName;

        public Transform Transform => transform;

        public bool IsActive => isActiveAndEnabled;

        public ActorKind Kind => ActorKind.Eater;

        public void Initialize(string id)
        {
            actorId = id ?? string.Empty;
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
            string name = DisplayName;
            string id = string.IsNullOrWhiteSpace(actorId) ? "<unknown>" : actorId;

            DebugUtility.Log(typeof(RunRearmKindQaEaterActor),
                $"[QA][GameplayResetKind] Eater Probe -> {step} (actor='{name}', id={id}, kind={Kind}, target={ctx.Request.Target})");
        }
    }
}
#endif
