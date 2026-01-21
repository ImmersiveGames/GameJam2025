#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.Phases;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;

namespace _ImmersiveGames.NewScripts.Gameplay.Levels
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LevelManager : ILevelManager
    {
        private const string LevelChangePrefix = "LevelChange/";
        private const string QaLevelPrefix = "QA/Levels/";

        private readonly IPhaseChangeService _phaseChangeService;
        private int _inProgress;

        public LevelManager(IPhaseChangeService phaseChangeService)
        {
            _phaseChangeService = phaseChangeService ?? throw new ArgumentNullException(nameof(phaseChangeService));
        }

        public async Task RequestLevelInPlaceAsync(LevelPlan plan, string reason, LevelChangeOptions? options = null)
        {
            if (!plan.IsValid)
            {
                DebugUtility.LogWarning<LevelManager>(
                    "[Level] Ignorando RequestLevelInPlaceAsync com LevelPlan inválido.");
                return;
            }

            if (Interlocked.CompareExchange(ref _inProgress, 1, 0) == 1)
            {
                DebugUtility.LogWarning<LevelManager>(
                    "[Level] Já existe mudança de nível em progresso. Ignorando RequestLevelInPlaceAsync.");
                return;
            }

            var normalizedReason = NormalizeReason(reason);
            var normalizedOptions = NormalizeOptions(options);

            DebugUtility.Log<LevelManager>(
                $"[OBS][Level] LevelChangeRequested levelId='{plan.LevelId}' phaseId='{plan.PhaseId}' mode='InPlace' reason='{normalizedReason}' contentSig='{plan.ContentSignature}'.",
                DebugUtility.Colors.Info);

            try
            {
                DebugUtility.Log<LevelManager>(
                    $"[OBS][Level] LevelChangeStarted levelId='{plan.LevelId}' phaseId='{plan.PhaseId}' mode='InPlace' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Info);

                await _phaseChangeService.RequestPhaseInPlaceAsync(
                    plan.ToPhasePlan(),
                    normalizedReason,
                    normalizedOptions.PhaseOptions);

                DebugUtility.Log<LevelManager>(
                    $"[OBS][Level] LevelChangeCompleted levelId='{plan.LevelId}' phaseId='{plan.PhaseId}' mode='InPlace' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Info);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<LevelManager>(
                    $"[Level] Falha ao mudar nível (InPlace). levelId='{plan.LevelId}' ex='{ex.GetType().Name}: {ex.Message}'.");
            }
            finally
            {
                Interlocked.Exchange(ref _inProgress, 0);
            }
        }

        public async Task RequestLevelWithTransitionAsync(LevelPlan plan, SceneTransitionRequest transition, string reason, LevelChangeOptions? options = null)
        {
            if (!plan.IsValid)
            {
                DebugUtility.LogWarning<LevelManager>(
                    "[Level] Ignorando RequestLevelWithTransitionAsync com LevelPlan inválido.");
                return;
            }

            if (transition == null)
            {
                DebugUtility.LogWarning<LevelManager>(
                    "[Level] Transition request nulo. RequestLevelWithTransitionAsync ignorado.");
                return;
            }

            if (Interlocked.CompareExchange(ref _inProgress, 1, 0) == 1)
            {
                DebugUtility.LogWarning<LevelManager>(
                    "[Level] Já existe mudança de nível em progresso. Ignorando RequestLevelWithTransitionAsync.");
                return;
            }

            var normalizedReason = NormalizeReason(reason);
            var normalizedOptions = NormalizeOptions(options);

            DebugUtility.Log<LevelManager>(
                $"[OBS][Level] LevelChangeRequested levelId='{plan.LevelId}' phaseId='{plan.PhaseId}' mode='SceneTransition' reason='{normalizedReason}' contentSig='{plan.ContentSignature}'.",
                DebugUtility.Colors.Info);

            try
            {
                DebugUtility.Log<LevelManager>(
                    $"[OBS][Level] LevelChangeStarted levelId='{plan.LevelId}' phaseId='{plan.PhaseId}' mode='SceneTransition' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Info);

                await _phaseChangeService.RequestPhaseWithTransitionAsync(
                    plan.ToPhasePlan(),
                    transition,
                    normalizedReason,
                    normalizedOptions.PhaseOptions);

                DebugUtility.Log<LevelManager>(
                    $"[OBS][Level] LevelChangeCompleted levelId='{plan.LevelId}' phaseId='{plan.PhaseId}' mode='SceneTransition' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Info);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<LevelManager>(
                    $"[Level] Falha ao mudar nível (SceneTransition). levelId='{plan.LevelId}' ex='{ex.GetType().Name}: {ex.Message}'.");
            }
            finally
            {
                Interlocked.Exchange(ref _inProgress, 0);
            }
        }

        private static string NormalizeReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return "LevelChange/GoTo";
            }

            var trimmed = reason.Trim();
            if (trimmed.StartsWith(LevelChangePrefix, StringComparison.Ordinal)
                || trimmed.StartsWith(QaLevelPrefix, StringComparison.Ordinal))
            {
                return trimmed;
            }

            return $"{LevelChangePrefix}{trimmed}";
        }

        private static LevelChangeOptions NormalizeOptions(LevelChangeOptions? options)
        {
            return options?.Clone() ?? LevelChangeOptions.Default.Clone();
        }
    }
}
