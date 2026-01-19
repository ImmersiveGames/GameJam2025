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
        private readonly IPhaseChangeService _phaseChangeService;
        private int _inProgress;

        public LevelManager(IPhaseChangeService phaseChangeService)
        {
            _phaseChangeService = phaseChangeService ?? throw new ArgumentNullException(nameof(phaseChangeService));
        }

        public async Task GoToLevelAsync(LevelPlan plan, string reason, LevelChangeOptions? options = null)
        {
            if (!plan.IsValid)
            {
                DebugUtility.LogWarning<LevelManager>(
                    "[Level] Ignorando GoToLevelAsync com LevelPlan inválido.");
                return;
            }

            if (Interlocked.CompareExchange(ref _inProgress, 1, 0) == 1)
            {
                DebugUtility.LogWarning<LevelManager>(
                    "[Level] Já existe mudança de nível em progresso. Ignorando GoToLevelAsync.");
                return;
            }

            var normalizedReason = NormalizeReason(reason);
            var normalizedOptions = NormalizeOptions(options);
            var mode = ResolveMode(normalizedOptions);

            DebugUtility.Log<LevelManager>(
                $"[OBS][Level] LevelChangeRequested levelId='{plan.LevelId}' phaseId='{plan.PhaseId}' mode='{mode}' reason='{normalizedReason}' contentSig='{plan.ContentSignature}'.",
                DebugUtility.Colors.Info);

            try
            {
                DebugUtility.Log<LevelManager>(
                    $"[OBS][Level] LevelChangeStarted levelId='{plan.LevelId}' phaseId='{plan.PhaseId}' mode='{mode}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Info);

                if (mode == PhaseChangeMode.SceneTransition)
                {
                    var transition = normalizedOptions.TransitionRequest;
                    if (transition == null)
                    {
                        DebugUtility.LogWarning<LevelManager>(
                            "[Level] TransitionRequest ausente para SceneTransition. GoToLevelAsync ignorado.");
                        return;
                    }

                    await _phaseChangeService.RequestPhaseWithTransitionAsync(
                        plan.ToPhasePlan(),
                        transition,
                        normalizedReason,
                        normalizedOptions.PhaseOptions);
                }
                else
                {
                    await _phaseChangeService.RequestPhaseInPlaceAsync(
                        plan.ToPhasePlan(),
                        normalizedReason,
                        normalizedOptions.PhaseOptions);
                }

                DebugUtility.Log<LevelManager>(
                    $"[OBS][Level] LevelChangeCompleted levelId='{plan.LevelId}' phaseId='{plan.PhaseId}' mode='{mode}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Info);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<LevelManager>(
                    $"[Level] Falha ao mudar nível. levelId='{plan.LevelId}' ex='{ex.GetType().Name}: {ex.Message}'.");
            }
            finally
            {
                Interlocked.Exchange(ref _inProgress, 0);
            }
        }

        public Task AdvanceAsync(string reason, LevelChangeOptions? options = null)
        {
            DebugUtility.LogWarning<LevelManager>(
                "[Level] AdvanceAsync não está configurado (sequência de níveis ausente). Use GoToLevelAsync explicitamente.");
            return Task.CompletedTask;
        }

        public Task BackAsync(string reason, LevelChangeOptions? options = null)
        {
            DebugUtility.LogWarning<LevelManager>(
                "[Level] BackAsync não está configurado (sequência de níveis ausente). Use GoToLevelAsync explicitamente.");
            return Task.CompletedTask;
        }

        public Task RestartLevelAsync(string reason, LevelChangeOptions? options = null)
        {
            DebugUtility.LogWarning<LevelManager>(
                "[Level] RestartLevelAsync não está configurado (nível atual desconhecido). Use GoToLevelAsync explicitamente.");
            return Task.CompletedTask;
        }

        private static string NormalizeReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "Level/GoTo" : reason.Trim();
        }

        private static LevelChangeOptions NormalizeOptions(LevelChangeOptions? options)
        {
            return options?.Clone() ?? LevelChangeOptions.Default.Clone();
        }

        private static PhaseChangeMode ResolveMode(LevelChangeOptions options)
        {
            if (options.TransitionRequest != null)
            {
                return PhaseChangeMode.SceneTransition;
            }

            return options.Mode;
        }
    }
}
