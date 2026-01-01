using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Gameplay.PostGame
{
    /// <summary>
    /// Driver de QA para disparar fim de run via hotkeys (Victory/Defeat).
    /// </summary>
    [DisallowMultipleComponent]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PostGameQaHotkeys : MonoBehaviour
    {
        private const KeyCode VictoryKey = KeyCode.F7;
        private const KeyCode DefeatKey  = KeyCode.F6;

        private const string VictoryReason = "QA_ForcedVictory";
        private const string DefeatReason  = "QA_ForcedDefeat";

        private void Update()
        {
            if (Input.GetKeyDown(VictoryKey))
            {
                if (DependencyManager.Provider.TryGetGlobal<IGameRunOutcomeService>(out var _))
                {
                    // Caminho recomendado: request event -> GameRunOutcomeService (via bridge) -> GameRunEndedEvent.
                    EventBus<GameRunEndRequestedEvent>.Raise(
                        new GameRunEndRequestedEvent(GameRunOutcome.Victory, VictoryReason));
                }
                else
                {
                    // Fallback para cenários onde o DI global não está inicializado (testes isolados).
                    EventBus<GameRunEndedEvent>.Raise(
                        new GameRunEndedEvent(GameRunOutcome.Victory, VictoryReason));
                }

                DebugUtility.LogVerbose<PostGameQaHotkeys>(
                    "[PostGame QA] F7 pressionado -> request Victory (QA_ForcedVictory).",
                    DebugUtility.Colors.Info);
            }

            if (Input.GetKeyDown(DefeatKey))
            {
                if (DependencyManager.Provider.TryGetGlobal<IGameRunOutcomeService>(out var _))
                {
                    // Caminho recomendado: request event -> GameRunOutcomeService (via bridge) -> GameRunEndedEvent.
                    EventBus<GameRunEndRequestedEvent>.Raise(
                        new GameRunEndRequestedEvent(GameRunOutcome.Defeat, DefeatReason));
                }
                else
                {
                    // Fallback para cenários onde o DI global não está inicializado (testes isolados).
                    EventBus<GameRunEndedEvent>.Raise(
                        new GameRunEndedEvent(GameRunOutcome.Defeat, DefeatReason));
                }

                DebugUtility.LogVerbose<PostGameQaHotkeys>(
                    "[PostGame QA] F6 pressionado -> request Defeat (QA_ForcedDefeat).",
                    DebugUtility.Colors.Info);
            }
        }
    }
}
