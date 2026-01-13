#nullable enable
using System;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;
using _ImmersiveGames.NewScripts.Gameplay.Scene;
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

        [ContextMenu("QA/Test3/ForcePregameNow")]
        private async void QA_ForcePregameNow()
        {
            DebugUtility.Log<PregameQATester>("[QA][Test3] ForcePregameNow acionado.", DebugUtility.Colors.Info);

            var coordinator = ResolveCoordinator();
            if (coordinator == null)
            {
                DebugUtility.LogWarning<PregameQATester>(
                    "[QA][Test3] IPregameCoordinator não encontrado no DI global.");
                return;
            }

            var classifier = ResolveGameplaySceneClassifier();
            if (classifier != null && !classifier.IsGameplayScene())
            {
                DebugUtility.LogWarning<PregameQATester>(
                    $"[QA][Test3] ForcePregameNow ignorado (scene_not_gameplay). scene='{SceneManager.GetActiveScene().name}'.");
                return;
            }

            var activeScene = SceneManager.GetActiveScene().name;
            var signature = ResolveSignatureFallback(activeScene);
            var context = new PregameContext(
                contextSignature: signature,
                profileId: SceneFlowProfileId.Gameplay,
                targetScene: activeScene,
                reason: "QA/Test3/ForcePregameNow");

            try
            {
                await coordinator.RunPregameAsync(context);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<PregameQATester>(
                    $"[QA][Test3] Falha ao executar pregame forçado. ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

        private static IPregameCoordinator? ResolveCoordinator()
        {
            return DependencyManager.Provider.TryGetGlobal<IPregameCoordinator>(out var coordinator)
                ? coordinator
                : null;
        }

        private static IGameplaySceneClassifier? ResolveGameplaySceneClassifier()
        {
            return DependencyManager.Provider.TryGetGlobal<IGameplaySceneClassifier>(out var classifier)
                ? classifier
                : null;
        }

        private static string ResolveSignatureFallback(string sceneName)
        {
            if (DependencyManager.Provider.TryGetGlobal<ISceneFlowSignatureCache>(out var cache) &&
                cache != null &&
                cache.TryGetLast(out var signature, out _, out _))
            {
                return signature;
            }

            return $"qa.test3|scene:{sceneName}|frame:{Time.frameCount}";
        }
    }
}
