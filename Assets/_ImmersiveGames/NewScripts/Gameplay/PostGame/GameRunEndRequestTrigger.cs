using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Gameplay.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Gameplay.PostGame
{
    [DisallowMultipleComponent]
    public sealed class GameRunEndRequestTrigger : MonoBehaviour
    {
        [Header("Defaults (UnityEvents sem parâmetros)")]
        [SerializeField] private GameRunOutcome defaultOutcome = GameRunOutcome.Victory;

        [SerializeField] private string defaultReason = "Trigger";

        [Header("Safety")]
        [SerializeField] private bool requireActiveGameplayScene = true;

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = true;

        public void Trigger() => Request(defaultOutcome, defaultReason);

        public void TriggerVictory() => Request(GameRunOutcome.Victory, defaultReason);

        public void TriggerDefeat() => Request(GameRunOutcome.Defeat, defaultReason);

        public void RequestVictory(string reason) => Request(GameRunOutcome.Victory, reason);

        public void RequestDefeat(string reason) => Request(GameRunOutcome.Defeat, reason);

        [ContextMenu("QA/Test3 - ForcePostPlay")]
        private void QA_ForcePostPlay()
        {
            DebugUtility.Log<GameRunEndRequestTrigger>(
                "[QA][Test3] ForcePostPlay acionado.",
                DebugUtility.Colors.Info);
            Request(GameRunOutcome.Victory, "QA/Test3/ForcePostPlay");
        }

        [ContextMenu("QA/PostGame/Trigger after phase change")]
        private void QA_PostGame_AfterPhaseChange()
        {
            DebugUtility.Log<GameRunEndRequestTrigger>(
                "[QA][PostGame] Trigger after phase change solicitado (expect PostGame rearmed).",
                DebugUtility.Colors.Info);
            Request(GameRunOutcome.Victory, "QA/PostGame/AfterPhaseChange");
        }

        public void Request(GameRunOutcome outcome, string reason)
        {
            if (requireActiveGameplayScene && !IsActiveGameplayScene())
            {
                if (verboseLogs)
                {
                    DebugUtility.LogWarning<GameRunEndRequestTrigger>(
                        $"Ignorando request ({outcome}) fora da GameplayScene. scene='{SceneManager.GetActiveScene().name}'");
                }
                return;
            }

            if (verboseLogs)
            {
                DebugUtility.Log<GameRunEndRequestTrigger>(
                    $"Publishing GameRunEndRequestedEvent(outcome={outcome}, reason='{reason ?? string.Empty}')");
            }

            EventBus<GameRunEndRequestedEvent>.Raise(new GameRunEndRequestedEvent(outcome, reason));
        }

        private static bool IsActiveGameplayScene()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameplaySceneClassifier>(out var classifier) &&
                classifier != null)
            {
                return classifier.IsGameplayScene();
            }

            return string.Equals(SceneManager.GetActiveScene().name, "GameplayScene", System.StringComparison.Ordinal);
        }
    }
}
