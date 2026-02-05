// Assets/_ImmersiveGames/NewScripts/QA/SceneFlow/SceneFlowQaContextMenu.cs
// QA de SceneFlow/WorldLifecycle: ações objetivas para evidência.

#nullable enable
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Navigation;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Core;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.QA
{
    public sealed class SceneFlowQaContextMenu : MonoBehaviour
    {
        private const string ColorInfo = "#A8DEED";
        private const string ColorErr = "#F44336";

        private const string ReasonEnterGameplay = "QA/SceneFlow/EnterGameplay";
        private const string ReasonForceReset = "QA/WorldLifecycle/ForceResetWorld";

        [ContextMenu("QA/SceneFlow/EnterGameplay (TC: Menu->Gameplay ResetWorld)")]
        private void Qa_EnterGameplay()
        {
            var navigation = ResolveGlobal<IGameNavigationService>("IGameNavigationService");
            if (navigation == null)
            {
                return;
            }

            DebugUtility.Log(typeof(SceneFlowQaContextMenu),
                "[QA][SceneFlow] Solicitação de transição Menu -> Gameplay enviada.",
                ColorInfo);

            _ = navigation.RequestGameplayAsync(ReasonEnterGameplay);
        }

        [ContextMenu("QA/WorldLifecycle/ForceResetWorld (TC: Manual ResetWorld)")]
        private void Qa_ForceResetWorld()
        {
            var resetService = ResolveGlobal<IWorldResetRequestService>("IWorldResetRequestService");
            if (resetService == null)
            {
                return;
            }

            DebugUtility.Log(typeof(SceneFlowQaContextMenu),
                "[QA][WorldLifecycle] Forçando ResetWorld via serviço global.",
                ColorInfo);

            _ = resetService.RequestResetAsync(ReasonForceReset);
        }

        private static T? ResolveGlobal<T>(string label) where T : class
        {
            if (DependencyManager.Provider == null)
            {
                DebugUtility.Log(typeof(SceneFlowQaContextMenu),
                    "[QA][SceneFlow] DependencyManager.Provider é null (infra global não inicializada?).",
                    ColorErr);
                return null;
            }

            if (!DependencyManager.Provider.TryGetGlobal<T>(out var service) || service == null)
            {
                DebugUtility.Log(typeof(SceneFlowQaContextMenu),
                    $"[QA][SceneFlow] Serviço global ausente: {label}.",
                    ColorErr);
                return null;
            }

            return service;
        }
    }
}


