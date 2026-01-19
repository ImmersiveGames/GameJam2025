// Assets/_ImmersiveGames/NewScripts/QA/Baseline22/Baseline22QaContextMenu.cs
// QA unificado (Baseline 2.2): ContentSwap + Level Manager.
// Comentários PT; código EN.

#nullable enable
using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Gameplay.Levels;
using _ImmersiveGames.NewScripts.Gameplay.Phases;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _ImmersiveGames.NewScripts.QA.Baseline22
{
    public sealed class Baseline22QaContextMenu : MonoBehaviour
    {
        private const string ColorInfo = "#A8DEED";
        private const string ColorOk = "#4CAF50";
        private const string ColorWarn = "#FFC107";
        private const string ColorErr = "#F44336";

        [Header("ContentSwap Defaults")]
        [SerializeField] private string contentSwapPhaseId = "phase.2";

        [Header("Level Defaults")]
        [SerializeField] private string levelId = "level.1";
        [SerializeField] private string levelPhaseId = "phase.2";
        [SerializeField] private string levelContentSignature = "content:level.1";

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

        private const string ReasonContentSwapG01 = "QA/ContentSwap/InPlace/NoVisuals";
        private const string ReasonLevelInPlace = "QA/Level/InPlace/DefaultIntroStage";
        private const string ReasonLevelWithTransition = "QA/Level/WithTransition/DefaultIntroStage";

        private const string RequestedBy = "QA/Baseline22/ContextMenu";

        [ContextMenu("QA/ContentSwap/G01-InPlace (NoVisuals)")]
        private void Qa_ContentSwap_G01_InPlace()
        {
            _ = RunContentSwapInPlaceAsync();
        }

        [ContextMenu("QA/Level/G02-GoToLevel (InPlace + IntroStage)")]
        private void Qa_Level_G02_InPlace()
        {
            _ = RunLevelInPlaceAsync();
        }

        [ContextMenu("QA/Level/G03-GoToLevel (WithTransition + IntroStage)")]
        private void Qa_Level_G03_WithTransition()
        {
            _ = RunLevelWithTransitionAsync();
        }

#if UNITY_EDITOR
        [MenuItem("Tools/NewScripts/QA/Baseline22/Select QA_Baseline22 Object", priority = 10)]
        private static void SelectQaObject()
        {
            var obj = GameObject.Find("QA_Baseline22");
            if (obj != null)
            {
                Selection.activeObject = obj;
            }
            else
            {
                DebugUtility.Log(typeof(Baseline22QaContextMenu),
                    "[QA][Baseline22] QA_Baseline22 não encontrado no Hierarchy (Play Mode).",
                    ColorWarn);
            }
        }
#endif

        private async Task RunContentSwapInPlaceAsync()
        {
            var phaseService = ResolveGlobal<IPhaseChangeService>("ContentSwap");
            if (phaseService == null)
            {
                return;
            }

            var phaseId = string.IsNullOrWhiteSpace(contentSwapPhaseId) ? "phase.2" : contentSwapPhaseId.Trim();

            DebugUtility.Log(typeof(Baseline22QaContextMenu),
                $"[QA][ContentSwap] G01 start phaseId='{phaseId}' reason='{ReasonContentSwapG01}'.",
                ColorInfo);

            try
            {
                await phaseService.RequestPhaseInPlaceAsync(phaseId, ReasonContentSwapG01, null);

                DebugUtility.Log(typeof(Baseline22QaContextMenu),
                    $"[QA][ContentSwap] G01 done phaseId='{phaseId}'.",
                    ColorOk);
            }
            catch (Exception ex)
            {
                DebugUtility.Log(typeof(Baseline22QaContextMenu),
                    $"[QA][ContentSwap] G01 failed phaseId='{phaseId}' ex='{ex.GetType().Name}: {ex.Message}'.",
                    ColorErr);
            }
        }

        private async Task RunLevelInPlaceAsync()
        {
            var levelManager = ResolveGlobal<ILevelManager>("Level");
            if (levelManager == null)
            {
                return;
            }

            var plan = BuildLevelPlan();

            DebugUtility.Log(typeof(Baseline22QaContextMenu),
                $"[QA][Level] G02 start levelId='{plan.LevelId}' mode=InPlace reason='{ReasonLevelInPlace}'.",
                ColorInfo);

            try
            {
                var options = new LevelChangeOptions
                {
                    Mode = PhaseChangeMode.InPlace,
                    TransitionRequest = null,
                    PhaseOptions = null
                };

                await levelManager.GoToLevelAsync(plan, ReasonLevelInPlace, options);
                await TryAutoCompleteIntroStageAsync("QA/Level/AutoComplete/InPlace");

                DebugUtility.Log(typeof(Baseline22QaContextMenu),
                    $"[QA][Level] G02 done levelId='{plan.LevelId}'.",
                    ColorOk);
            }
            catch (Exception ex)
            {
                DebugUtility.Log(typeof(Baseline22QaContextMenu),
                    $"[QA][Level] G02 failed levelId='{plan.LevelId}' ex='{ex.GetType().Name}: {ex.Message}'.",
                    ColorErr);
            }
        }

        private async Task RunLevelWithTransitionAsync()
        {
            var levelManager = ResolveGlobal<ILevelManager>("Level");
            if (levelManager == null)
            {
                return;
            }

            var plan = BuildLevelPlan();

            DebugUtility.Log(typeof(Baseline22QaContextMenu),
                $"[QA][Level] G03 start levelId='{plan.LevelId}' mode=WithTransition reason='{ReasonLevelWithTransition}'.",
                ColorInfo);

            try
            {
                var request = BuildTransitionRequest();
                var options = new LevelChangeOptions
                {
                    Mode = PhaseChangeMode.SceneTransition,
                    TransitionRequest = request,
                    PhaseOptions = null
                };

                await levelManager.GoToLevelAsync(plan, ReasonLevelWithTransition, options);
                await TryAutoCompleteIntroStageAsync("QA/Level/AutoComplete/WithTransition");

                DebugUtility.Log(typeof(Baseline22QaContextMenu),
                    $"[QA][Level] G03 done levelId='{plan.LevelId}'.",
                    ColorOk);
            }
            catch (Exception ex)
            {
                DebugUtility.Log(typeof(Baseline22QaContextMenu),
                    $"[QA][Level] G03 failed levelId='{plan.LevelId}' ex='{ex.GetType().Name}: {ex.Message}'.",
                    ColorErr);
            }
        }

        private LevelPlan BuildLevelPlan()
        {
            var id = string.IsNullOrWhiteSpace(levelId) ? "level.1" : levelId.Trim();
            var phaseId = string.IsNullOrWhiteSpace(levelPhaseId) ? "phase.2" : levelPhaseId.Trim();
            var signature = string.IsNullOrWhiteSpace(levelContentSignature) ? string.Empty : levelContentSignature.Trim();
            return new LevelPlan(id, phaseId, signature);
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
                DebugUtility.Log(typeof(Baseline22QaContextMenu),
                    "[QA][Level] IntroStage não ativa; AutoComplete ignorado.",
                    ColorWarn);
                return;
            }

            controlService.CompleteIntroStage(reason);
            DebugUtility.Log(typeof(Baseline22QaContextMenu),
                $"[QA][Level] CompleteIntroStage solicitado. reason='{reason}'.",
                ColorInfo);
        }

        private static T? ResolveGlobal<T>(string domain) where T : class
        {
            if (DependencyManager.Provider == null)
            {
                DebugUtility.Log(typeof(Baseline22QaContextMenu),
                    $"[QA][{domain}] DependencyManager.Provider é null (infra global não inicializada?).",
                    ColorErr);
                return null;
            }

            if (!DependencyManager.Provider.TryGetGlobal<T>(out var service) || service == null)
            {
                DebugUtility.Log(typeof(Baseline22QaContextMenu),
                    $"[QA][{domain}] Serviço global ausente: {typeof(T).Name}.",
                    ColorErr);
                return null;
            }

            return service;
        }
    }
}
