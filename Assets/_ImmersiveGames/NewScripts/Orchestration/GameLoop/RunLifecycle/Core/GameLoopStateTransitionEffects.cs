using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Orchestration.SessionIntegration.Runtime;
namespace _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core
{
    /// <summary>
    /// Efeitos auxiliares do GameLoop.
    ///
    /// Mantém apenas a projeção de input mode do estado Playing.
    /// O handoff de pós-run pertence ao rail canônico de RunResultStage/RunDecision.
    /// </summary>
    public sealed class GameLoopStateTransitionEffects
    {
        public void ApplyGameplayInputMode()
        {
            DebugUtility.Log<GameLoopStateTransitionEffects>(
                "[OBS][InputMode] Request mode='Gameplay' map='Player' phase='Playing' reason='GameLoop/Playing' source='SessionIntegration'.",
                DebugUtility.Colors.Info);

            if (!DependencyManager.Provider.TryGetGlobal<ISessionIntegrationContextService>(out var sessionIntegration) || sessionIntegration == null)
            {
                HardFailFastH1.Trigger(typeof(GameLoopStateTransitionEffects),
                    "[FATAL][H1][SessionIntegration] ISessionIntegrationContextService indisponivel para aplicar input mode gameplay do GameLoop.");
                return;
            }

            sessionIntegration.RequestGameplayInputMode("GameLoop/Playing", "GameLoop");
        }
    }
}

