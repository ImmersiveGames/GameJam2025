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
    /// <summary>
    /// QA mínimo para Baseline A–E (Level):
    /// - NextLevel (swap local)
    /// - RestartLevel (level reset)
    ///
    /// Observabilidade:
    /// - Prova de "sem transição macro" via contagem de SceneTransitionStartedEvent.
    /// </summary>
    public sealed class LevelFlowDevContextMenu : MonoBehaviour
    {
        private const string ColorInfo = "#A8DEED";
        private const string ColorWarn = "#FFC107";
        private const string ColorError = "#F44336";

        private const string ReasonNextLevel = "QA/LevelFlow/PostLevel/NextLevel";
        private const string ReasonRestartLevel = "QA/LevelFlow/PostLevel/RestartLevel";

        [ContextMenu("QA/LevelFlow/NextLevel")]
        private void Qa_NextLevel()
        {
            _ = NextLevelAsync();
        }

        [ContextMenu("QA/LevelFlow/RestartLevel")]
        private void Qa_RestartLevel()
        {
            _ = RestartLevelAsync();
        }

        private static async Task NextLevelAsync()
        {
            var postLevel = ResolveGlobal<IPostLevelActionsService>("IPostLevelActionsService");
            if (postLevel == null)
            {
                return;
            }

            int transitionStartedDuringAction = 0;
            var binding = new EventBinding<SceneTransitionStartedEvent>(_ => transitionStartedDuringAction++);

            EventBus<SceneTransitionStartedEvent>.Register(binding);

            try
            {
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    $"[INFO][QA][LevelFlow] NextLevelRequested reason='{ReasonNextLevel}'.",
                    ColorInfo);

                await postLevel.NextLevelAsync(ReasonNextLevel, CancellationToken.None);

                bool noMacroTransition = transitionStartedDuringAction == 0;
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    $"[OBS][QA][LevelFlow] NextLevelCompleted reason='{ReasonNextLevel}' noMacroTransition='{noMacroTransition.ToString().ToLowerInvariant()}' transitionStartedCount='{transitionStartedDuringAction}'.",
                    noMacroTransition ? ColorInfo : ColorWarn);
            }
            catch (System.Exception ex)
            {
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    $"[WARN][QA][LevelFlow] NextLevelCompleted success=False reason='{ReasonNextLevel}' transitionStartedCount='{transitionStartedDuringAction}' notes='{ex.GetType().Name}'.",
                    ColorWarn);
            }
            finally
            {
                EventBus<SceneTransitionStartedEvent>.Unregister(binding);
            }
        }

        private static async Task RestartLevelAsync()
        {
            var postLevel = ResolveGlobal<IPostLevelActionsService>("IPostLevelActionsService");
            if (postLevel == null)
            {
                return;
            }

            int transitionStartedDuringAction = 0;
            var binding = new EventBinding<SceneTransitionStartedEvent>(_ => transitionStartedDuringAction++);

            EventBus<SceneTransitionStartedEvent>.Register(binding);

            try
            {
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    $"[INFO][QA][LevelFlow] RestartLevelRequested reason='{ReasonRestartLevel}'.",
                    ColorInfo);

                await postLevel.RestartLevelAsync(ReasonRestartLevel, CancellationToken.None);

                bool noMacroTransition = transitionStartedDuringAction == 0;
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    $"[OBS][QA][LevelFlow] RestartLevelCompleted reason='{ReasonRestartLevel}' noMacroTransition='{noMacroTransition.ToString().ToLowerInvariant()}' transitionStartedCount='{transitionStartedDuringAction}'.",
                    noMacroTransition ? ColorInfo : ColorWarn);
            }
            catch (System.Exception ex)
            {
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    $"[WARN][QA][LevelFlow] RestartLevelCompleted success=False reason='{ReasonRestartLevel}' transitionStartedCount='{transitionStartedDuringAction}' notes='{ex.GetType().Name}'.",
                    ColorWarn);
            }
            finally
            {
                EventBus<SceneTransitionStartedEvent>.Unregister(binding);
            }
        }

        private static T? ResolveGlobal<T>(string label) where T : class
        {
            if (DependencyManager.Provider == null)
            {
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    "[ERROR][QA][LevelFlow] DependencyManager.Provider is null.",
                    ColorError);
                return null;
            }

            if (!DependencyManager.Provider.TryGetGlobal<T>(out var service) || service == null)
            {
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    $"[WARN][QA][LevelFlow] Missing global service='{label}'.",
                    ColorWarn);
                return null;
            }

            return service;
        }
    }
}
