// Assets/_ImmersiveGames/NewScripts/Gameplay/Phases/PhaseChangeService.cs
#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime;

namespace _ImmersiveGames.NewScripts.Gameplay.Phases
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PhaseChangeService : IPhaseChangeService
    {
        private readonly IPhaseContextService _phaseContext;
        private readonly IWorldResetRequestService _worldReset;
        private readonly ISceneTransitionService _sceneFlow;

        private int _inProgress;

        public PhaseChangeService(
            IPhaseContextService phaseContext,
            IWorldResetRequestService worldReset,
            ISceneTransitionService sceneFlow)
        {
            _phaseContext = phaseContext ?? throw new ArgumentNullException(nameof(phaseContext));
            _worldReset = worldReset ?? throw new ArgumentNullException(nameof(worldReset));
            _sceneFlow = sceneFlow ?? throw new ArgumentNullException(nameof(sceneFlow));
        }

        public async Task RequestPhaseInPlaceAsync(PhasePlan plan, string reason)
        {
            if (!plan.IsValid)
            {
                DebugUtility.LogWarning<PhaseChangeService>(
                    "[PhaseChange] Ignorando RequestPhaseInPlaceAsync com PhasePlan inválido.");
                return;
            }

            if (Interlocked.CompareExchange(ref _inProgress, 1, 0) == 1)
            {
                DebugUtility.LogWarning<PhaseChangeService>(
                    "[PhaseChange] Já existe uma troca de fase em progresso. Ignorando (InPlace).");
                return;
            }

            try
            {
                _phaseContext.SetPending(plan, reason);

                var resetReason = $"PhaseChange/InPlace plan='{plan}' reason='{reason ?? "n/a"}'";
                DebugUtility.Log<PhaseChangeService>(
                    $"[PhaseChange] InPlace -> pending set. Disparando WorldReset. {resetReason}",
                    DebugUtility.Colors.Info);

                await _worldReset.RequestResetAsync(resetReason);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<PhaseChangeService>(
                    $"[PhaseChange] Falha no InPlace. Limpando pending por segurança. ex={ex}");

                _phaseContext.ClearPending($"PhaseChange/InPlace failed: {ex.GetType().Name}");
                throw;
            }
            finally
            {
                Interlocked.Exchange(ref _inProgress, 0);
            }
        }

        public async Task RequestPhaseWithTransitionAsync(PhasePlan plan, SceneTransitionRequest transition, string reason)
        {
            if (!plan.IsValid)
            {
                DebugUtility.LogWarning<PhaseChangeService>(
                    "[PhaseChange] Ignorando RequestPhaseWithTransitionAsync com PhasePlan inválido.");
                return;
            }

            if (transition.ScenesToLoad == null || transition.ScenesToUnload == null)
            {
                DebugUtility.LogError<PhaseChangeService>(
                    "[PhaseChange] Transition request inválido (ScenesToLoad/ScenesToUnload nulos). Abortando.");
                return;
            }

            if (Interlocked.CompareExchange(ref _inProgress, 1, 0) == 1)
            {
                DebugUtility.LogWarning<PhaseChangeService>(
                    "[PhaseChange] Já existe uma troca de fase em progresso. Ignorando (WithTransition).");
                return;
            }

            try
            {
                _phaseContext.SetPending(plan, reason);

                DebugUtility.Log<PhaseChangeService>(
                    $"[PhaseChange] WithTransition -> pending set. Iniciando SceneFlow. " +
                    $"plan='{plan}', reason='{reason ?? "n/a"}', profile='{transition.TransitionProfileName}', active='{transition.TargetActiveScene}'.",
                    DebugUtility.Colors.Info);

                await _sceneFlow.TransitionAsync(transition);

                // Commit ocorrerá no WorldLifecycleController no início do reset (após ScenesReady).
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<PhaseChangeService>(
                    $"[PhaseChange] Falha no WithTransition. Limpando pending por segurança. ex={ex}");

                _phaseContext.ClearPending($"PhaseChange/WithTransition failed: {ex.GetType().Name}");
                throw;
            }
            finally
            {
                Interlocked.Exchange(ref _inProgress, 0);
            }
        }
    }
}
