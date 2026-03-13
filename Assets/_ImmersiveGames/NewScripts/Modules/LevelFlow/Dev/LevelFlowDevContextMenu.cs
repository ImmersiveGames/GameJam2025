#nullable enable
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Dev
{
    public sealed class LevelFlowDevContextMenu : MonoBehaviour
    {
        private const string ColorInfo = "#A8DEED";
        private const string ColorWarn = "#FFC107";

        private const string ReasonNextLevel = "QA/LevelFlow/NextLevel";
        private const string ReasonRestartCurrentLevelLocal = "QA/LevelFlow/RestartCurrentLevelLocal";

        [ContextMenu("QA/LevelFlow/NextLevel")]
        private void Qa_NextLevel()
        {
            _ = NextLevelAsync();
        }

        [ContextMenu("QA/LevelFlow/RestartCurrentLevelLocal")]
        private void Qa_RestartCurrentLevelLocal()
        {
            _ = RestartCurrentLevelLocalAsync();
        }

        private static async Task NextLevelAsync()
        {
            if (DependencyManager.Provider == null ||
                !DependencyManager.Provider.TryGetGlobal<IPostLevelActionsService>(out var postLevel) ||
                postLevel == null)
            {
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    "[WARN][QA][LevelFlow] Missing global service='IPostLevelActionsService'.",
                    ColorWarn);
                return;
            }

            int transitionStartedCount = 0;
            var binding = new EventBinding<SceneTransitionStartedEvent>(_ => transitionStartedCount++);
            EventBus<SceneTransitionStartedEvent>.Register(binding);

            try
            {
                await postLevel.NextLevelAsync(ReasonNextLevel, CancellationToken.None);
            }
            finally
            {
                EventBus<SceneTransitionStartedEvent>.Unregister(binding);
                bool noMacroTransition = transitionStartedCount == 0;
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    $"[OBS][QA][LevelFlow] NextLevelCompleted reason='{ReasonNextLevel}' noMacroTransition='{noMacroTransition.ToString().ToLowerInvariant()}' transitionStartedCount='{transitionStartedCount}'.",
                    noMacroTransition ? ColorInfo : ColorWarn);
            }
        }

        private static async Task RestartCurrentLevelLocalAsync()
        {
            if (DependencyManager.Provider == null ||
                !DependencyManager.Provider.TryGetGlobal<IRestartContextService>(out var restartContext) ||
                restartContext == null ||
                !DependencyManager.Provider.TryGetGlobal<ILevelSwapLocalService>(out var swapLocalService) ||
                swapLocalService == null)
            {
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    "[WARN][QA][LevelFlow] Missing global service='IPostLevelActionsService'.",
                    ColorWarn);
                return;
            }

            if (!restartContext.TryGetCurrent(out GameplayStartSnapshot snapshot) ||
                !snapshot.IsValid ||
                !snapshot.HasLevelRef ||
                snapshot.LevelRef == null)
            {
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    "[WARN][QA][LevelFlow] Missing global service='IPostLevelActionsService'.",
                    ColorWarn);
                return;
            }

            int transitionStartedCount = 0;
            var binding = new EventBinding<SceneTransitionStartedEvent>(_ => transitionStartedCount++);
            EventBus<SceneTransitionStartedEvent>.Register(binding);

            try
            {
                await swapLocalService.SwapLocalAsync(snapshot.LevelRef, ReasonRestartCurrentLevelLocal, CancellationToken.None);
            }
            finally
            {
                EventBus<SceneTransitionStartedEvent>.Unregister(binding);
                bool noMacroTransition = transitionStartedCount == 0;
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    $"[OBS][QA][LevelFlow] RestartCurrentLevelLocalCompleted reason='{ReasonRestartCurrentLevelLocal}' noMacroTransition='{noMacroTransition.ToString().ToLowerInvariant()}' transitionStartedCount='{transitionStartedCount}'.",
                    noMacroTransition ? ColorInfo : ColorWarn);
            }
        }
    }
}
