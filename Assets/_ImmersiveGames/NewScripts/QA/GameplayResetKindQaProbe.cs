using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.Reset;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.GameplayReset
{
    /// <summary>
    /// Probe de QA que loga quais atores executaram cada fase do GameplayReset.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplayResetKindQaProbe : MonoBehaviour, IGameplayResettable
    {
        [SerializeField]
        private string label = "QA GameplayResetKind Probe";

        [SerializeField]
        private bool verboseLogs = true;

        private IActor _actor;
        private IActorKindProvider _kindProvider;

        private void Awake()
        {
            _actor = GetComponent<IActor>();
            _kindProvider = GetComponent<IActorKindProvider>();
        }

        public void Configure(string newLabel, bool verbose)
        {
            if (!string.IsNullOrWhiteSpace(newLabel))
            {
                label = newLabel;
            }

            verboseLogs = verbose;
        }

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

            string actorId = _actor?.ActorId ?? "<unknown>";
            string actorName = _actor?.DisplayName ?? (gameObject != null ? gameObject.name : "<null>");
            string kind = _kindProvider != null ? _kindProvider.Kind.ToString() : "Unknown";

            DebugUtility.Log(typeof(GameplayResetKindQaProbe),
                $"[QA][GameplayResetKind] {label} -> {phase} (actor='{actorName}', id={actorId}, kind={kind}, target={ctx.Request.Target})");
        }
    }
}
