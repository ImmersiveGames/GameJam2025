#nullable enable
using System;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// QA helper para disparar Pregame manualmente (sem depender do SceneFlow).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PregameQATester : MonoBehaviour
    {
        [SerializeField]
        private string qaSignature = "qa.pregame";

        [SerializeField]
        private string qaReason = "QA/PregameOptional";

        [ContextMenu("QA/Pregame/Run Optional (TestCase: PregameOptional)")]
        private async void QA_RunPregameOptional()
        {
            var coordinator = ResolveCoordinator();
            if (coordinator == null)
            {
                DebugUtility.LogWarning<PregameQATester>(
                    "[QA][Pregame] IPregameCoordinator não encontrado no DI global.");
                return;
            }

            var activeScene = SceneManager.GetActiveScene().name;
            var context = new PregameContext(
                contextSignature: qaSignature,
                profileId: SceneFlowProfileId.Gameplay,
                targetScene: activeScene,
                reason: qaReason);

            try
            {
                await coordinator.RunPregameAsync(context);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<PregameQATester>(
                    $"[QA][Pregame] Falha ao executar pregame QA. ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

        private static IPregameCoordinator? ResolveCoordinator()
        {
            return DependencyManager.Provider.TryGetGlobal<IPregameCoordinator>(out var coordinator)
                ? coordinator
                : null;
        }
    }
}
