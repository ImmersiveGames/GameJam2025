// Assets/_ImmersiveGames/NewScripts/QA/Levels/LevelQaContextMenu.cs
// QA de LevelManager (Baseline 2.2): ações objetivas para evidência.
// Comentários PT; código EN.

#nullable enable
using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Gameplay.Levels;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _ImmersiveGames.NewScripts.QA.Levels
{
    public sealed class LevelQaContextMenu : MonoBehaviour
    {
        private const string ColorInfo = "#A8DEED";
        private const string ColorOk = "#4CAF50";
        private const string ColorWarn = "#FFC107";
        private const string ColorErr = "#F44336";

        [Header("Level Defaults")]
        [SerializeField] private string levelId = "level.1";
        [SerializeField] private string phaseId = "phase.2";
        [SerializeField] private string contentSignature = "content:level.1";

        [Header("WithTransition SceneFlow Request")]
        [SerializeField] private string profileId = "gameplay";
        [SerializeField] private string targetActiveScene = "GameplayScene";
        [SerializeField] private string[] scenesToLoad = { "GameplayScene", "UIGlobalScene" };
        [SerializeField] private string[] scenesToUnload = Array.Empty<string>();
        [SerializeField] private bool useFade = true;

        [Header("IntroStage QA")]
        [SerializeField] private bool autoCompleteIntroStage = true;
        [SerializeField] private float autoCompleteDelaySeconds = 0.5f;
        [SerializeField] private float autoCompleteWaitSeconds = 5f;

        private const string ReasonInPlace = "QA/Levels/InPlace/DefaultIntroStage";
        private const string ReasonWithTransition = "QA/Levels/WithTransition/DefaultIntroStage";
        private const string RequestedBy = "QA/Levels/LevelQaContextMenu";

        [ContextMenu("QA/Levels/L01-GoToLevel (InPlace + IntroStage)")]
        private void Qa_L01_InPlace()
        {
            _ = RunInPlaceAsync();
        }

        [ContextMenu("QA/Levels/L02-GoToLevel (WithTransition + IntroStage)")]
        private void Qa_L02_WithTransition()
        {
            _ = RunWithTransitionAsync();
        }

#if UNITY_EDITOR
        [MenuItem("Tools/NewScripts/QA/Levels/Select QA_Level Object", priority = 10)]
        private static void SelectQaObject()
        {
            var obj = GameObject.Find("QA_Level");
            if (obj != null)
            {
                Selection.activeObject = obj;
            }
            else
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    "[QA][Level] QA_Level não encontrado no Hierarchy (Play Mode).",
                    ColorWarn);
            }
        }
#endif

        private async Task RunInPlaceAsync()
        {
            var manager = ResolveGlobal<ILevelManager>("Level");
            if (manager == null)
            {
                return;
            }

            var plan = BuildPlan();

            DebugUtility.Log(typeof(LevelQaContextMenu),
                $"[QA][Level] L01 start levelId='{plan.LevelId}' mode=InPlace reason='{ReasonInPlace}'.",
                ColorInfo);

            try
            {
                await manager.RequestLevelInPlaceAsync(plan, ReasonInPlace, null);
                await TryAutoCompleteIntroStageAsync("QA/Levels/AutoComplete/InPlace");

                DebugUtility.Log(typeof(LevelQaContextMenu),
                    $"[QA][Level] L01 done levelId='{plan.LevelId}'.",
                    ColorOk);
            }
            catch (Exception ex)
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    $"[QA][Level] L01 failed levelId='{plan.LevelId}' ex='{ex.GetType().Name}: {ex.Message}'.",
                    ColorErr);
            }
        }

        private async Task RunWithTransitionAsync()
        {
            var manager = ResolveGlobal<ILevelManager>("Level");
            if (manager == null)
            {
                return;
            }

            var plan = BuildPlan();

            DebugUtility.Log(typeof(LevelQaContextMenu),
                $"[QA][Level] L02 start levelId='{plan.LevelId}' mode=WithTransition reason='{ReasonWithTransition}'.",
                ColorInfo);

            try
            {
                var request = BuildTransitionRequest();
                await manager.RequestLevelWithTransitionAsync(plan, request, ReasonWithTransition, null);
                await TryAutoCompleteIntroStageAsync("QA/Levels/AutoComplete/WithTransition");

                DebugUtility.Log(typeof(LevelQaContextMenu),
                    $"[QA][Level] L02 done levelId='{plan.LevelId}'.",
                    ColorOk);
            }
            catch (Exception ex)
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    $"[QA][Level] L02 failed levelId='{plan.LevelId}' ex='{ex.GetType().Name}: {ex.Message}'.",
                    ColorErr);
            }
        }

        private LevelPlan BuildPlan()
        {
            var id = string.IsNullOrWhiteSpace(levelId) ? "level.1" : levelId.Trim();
            var phase = string.IsNullOrWhiteSpace(phaseId) ? "phase.2" : phaseId.Trim();
            var signature = string.IsNullOrWhiteSpace(contentSignature) ? string.Empty : contentSignature.Trim();
            return new LevelPlan(id, phase, signature);
        }

        private SceneTransitionRequest BuildTransitionRequest()
        {
            var pid = new SceneFlowProfileId(profileId);

            return new SceneTransitionRequest(
                scenesToLoad: scenesToLoad ?? Array.Empty<string>(),
                scenesToUnload: scenesToUnload ?? Array.Empty<string>(),
                targetActiveScene: string.IsNullOrWhiteSpace(targetActiveScene) ? "GameplayScene" : targetActiveScene.Trim(),
                useFade: useFade,
                transitionProfileId: pid,
                contextSignature: null,
                requestedBy: RequestedBy
            );
        }

        private async Task TryAutoCompleteIntroStageAsync(string reason)
        {
            if (!autoCompleteIntroStage)
            {
                return;
            }

            var controlService = ResolveGlobal<IIntroStageControlService>("IntroStage");
            if (controlService == null)
            {
                return;
            }

            var delayMs = Mathf.Max(0f, autoCompleteDelaySeconds) * 1000f;
            if (delayMs > 0f)
            {
                await Task.Delay((int)delayMs);
            }

            var waitMs = Mathf.Max(0f, autoCompleteWaitSeconds) * 1000f;
            var elapsed = 0f;
            while (!controlService.IsIntroStageActive && elapsed < waitMs)
            {
                await Task.Delay(100);
                elapsed += 100f;
            }

            if (!controlService.IsIntroStageActive)
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    "[QA][Level] IntroStage não ativa; AutoComplete ignorado.",
                    ColorWarn);
                return;
            }

            controlService.CompleteIntroStage(reason);
            DebugUtility.Log(typeof(LevelQaContextMenu),
                $"[QA][Level] CompleteIntroStage solicitado. reason='{reason}'.",
                ColorInfo);
        }

        private static T? ResolveGlobal<T>(string domain) where T : class
        {
            if (DependencyManager.Provider == null)
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    $"[QA][{domain}] DependencyManager.Provider é null (infra global não inicializada?).",
                    ColorErr);
                return null;
            }

            if (!DependencyManager.Provider.TryGetGlobal<T>(out var service) || service == null)
            {
                DebugUtility.Log(typeof(LevelQaContextMenu),
                    $"[QA][{domain}] Serviço global ausente: {typeof(T).Name}.",
                    ColorErr);
                return null;
            }

            return service;
        }
    }
}
