using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.Reset;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.GameplayReset
{
    /// <summary>
    /// Componente de QA que participa do GameplayReset e loga Cleanup/Restore/Rebind.
    /// Útil para provar que a execução por alvo/fase está funcional.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplayResetQaProbe : MonoBehaviour, IGameplayResettable, IGameplayResetOrder, IGameplayResetTargetFilter
    {
        [SerializeField] private bool verboseLogs = true;
        [SerializeField] private int delayMsPerPhase = 0;
        [SerializeField] private int resetOrder = -10;

        private int _cleanupCount;
        private int _restoreCount;
        private int _rebindCount;

        public int ResetOrder => resetOrder;

        public void Configure(bool verbose, int delayMs)
        {
            verboseLogs = verbose;
            delayMsPerPhase = delayMs;
        }

        public bool ShouldParticipate(GameplayResetTarget target)
        {
            // Participa de todos por padrão, pois é um probe.
            // Se quiser restringir, altere aqui.
            return true;
        }

        public async Task ResetCleanupAsync(GameplayResetContext ctx)
        {
            _cleanupCount++;
            if (verboseLogs)
            {
                DebugUtility.LogVerbose(typeof(GameplayResetQaProbe),
                    $"[QAProbe] Cleanup #{_cleanupCount} on '{name}'. ctx={ctx}");
            }

            if (delayMsPerPhase > 0)
                await Task.Delay(delayMsPerPhase);
        }

        public async Task ResetRestoreAsync(GameplayResetContext ctx)
        {
            _restoreCount++;
            if (verboseLogs)
            {
                DebugUtility.LogVerbose(typeof(GameplayResetQaProbe),
                    $"[QAProbe] Restore #{_restoreCount} on '{name}'. ctx={ctx}");
            }

            if (delayMsPerPhase > 0)
                await Task.Delay(delayMsPerPhase);
        }

        public async Task ResetRebindAsync(GameplayResetContext ctx)
        {
            _rebindCount++;
            if (verboseLogs)
            {
                DebugUtility.LogVerbose(typeof(GameplayResetQaProbe),
                    $"[QAProbe] Rebind #{_rebindCount} on '{name}'. ctx={ctx}");
            }

            if (delayMsPerPhase > 0)
                await Task.Delay(delayMsPerPhase);
        }
    }
}
