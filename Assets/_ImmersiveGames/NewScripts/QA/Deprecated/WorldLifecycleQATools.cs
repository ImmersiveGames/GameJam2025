#if UNITY_EDITOR || DEVELOPMENT_BUILD
// DEPRECATED QA TOOL — ver Docs/Reports/QA-Audit-2025-12-27.md
﻿using System.Linq;
using System.Text;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.Cameras;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Execution.Gate;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.Fade;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.Loading;
using _ImmersiveGames.NewScripts.Infrastructure.State;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.QA
{
    [DisallowMultipleComponent]
    [System.Obsolete("Deprecated QA tool; see QA-Audit-2025-12-27", false)]
    public sealed class WorldLifecycleQaTools : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private WorldLifecycleController controller;

        [Header("QA/Reset")]
        [SerializeField] private string resetReason = "ContextMenu/ResetWorldNow";
        [SerializeField] private string softResetPlayersReason = "ContextMenu/SoftResetPlayers";

        private string _sceneName = string.Empty;

        private void Awake()
        {
            _sceneName = gameObject.scene.name;

            if (controller == null)
            {
                controller = GetComponent<WorldLifecycleController>();
            }
        }

        // ----------------------------
        // QA: Diagnostics
        // ----------------------------

        [ContextMenu("QA/WorldLifecycle/Diagnostics/Snapshot (Scene/DI/HUD/GameLoop)")]
        public void Snapshot()
        {
            var sb = new StringBuilder(1536);

            sb.AppendLine("[QA Snapshot] ------------------------------");
            sb.AppendLine($"scene(go)='{_sceneName}', activeScene='{SceneManager.GetActiveScene().name}', sceneCount={SceneManager.sceneCount}");

            sb.Append("loaded=[");
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(SceneManager.GetSceneAt(i).name);
            }
            sb.AppendLine("]");

            // 1) Find HUD controller
            var hudCtrl = FindFirstObjectByType<NewScriptsLoadingHudController>(FindObjectsInactive.Include);
            sb.AppendLine(hudCtrl != null
                ? $"hudCtrl=FOUND (go='{hudCtrl.gameObject.name}', scene='{hudCtrl.gameObject.scene.name}', active={hudCtrl.gameObject.activeInHierarchy})"
                : "hudCtrl=NOT_FOUND (NewScriptsLoadingHudController)");

            // 3) WorldLifecycleControllers
            var wlCtrl = FindObjectsByType<WorldLifecycleController>(FindObjectsSortMode.None);
            sb.AppendLine($"worldLifecycleControllers={wlCtrl.Length}");
            foreach (var c in wlCtrl.Take(5))
            {
                sb.AppendLine($" - {c.gameObject.scene.name}/{c.gameObject.name} (active={c.gameObject.activeInHierarchy})");
            }
            if (wlCtrl.Length > 5)
            {
                sb.AppendLine(" - (more...)");
            }

            sb.AppendLine("[DI: Global] --------------------------------------");
            AppendDiLine<ISceneTransitionService>(sb, "ISceneTransitionService");
            AppendDiLine<ISceneTransitionCompletionGate>(sb, "ISceneTransitionCompletionGate");
            AppendDiLine<INewScriptsFadeService>(sb, "INewScriptsFadeService");
            AppendDiLine<INewScriptsLoadingHudService>(sb, "INewScriptsLoadingHudService");
            AppendDiLine<SceneFlowLoadingService>(sb, "SceneFlowLoadingService");
            AppendDiLine<ISimulationGateService>(sb, "ISimulationGateService");
            AppendDiLine<IStateDependentService>(sb, "IStateDependentService");
            AppendDiLine<ICameraResolver>(sb, "ICameraResolver");
            AppendDiLine<IGameLoopService>(sb, "IGameLoopService");
            // Se quiser, você pode adicionar IGameNavigationService aqui também,
            // mas só se este arquivo já tiver acesso ao tipo sem inventar namespace.

            sb.AppendLine("------------------------------------------");

            DebugUtility.Log(typeof(WorldLifecycleQaTools), sb.ToString(), DebugUtility.Colors.Info);
        }

        private static void AppendDiLine<T>(StringBuilder sb, string label) where T : class
        {
            if (DependencyManager.Provider.TryGetGlobal<T>(out var service) && service != null)
            {
                var typeName = service.GetType().FullName;

                if (service is Component comp)
                {
                    sb.AppendLine($"{label}=OK type='{typeName}' go='{comp.gameObject.name}' scene='{comp.gameObject.scene.name}' active={comp.gameObject.activeInHierarchy}");
                }
                else
                {
                    sb.AppendLine($"{label}=OK type='{typeName}'");
                }
            }
            else
            {
                sb.AppendLine($"{label}=MISSING");
            }
        }

        // ----------------------------
        // QA: Reset
        // ----------------------------

        [ContextMenu("QA/WorldLifecycle/Reset/Reset World Now")]
        public async void ResetWorldNow()
        {
            if (!EnsureController())
            {
                return;
            }

            await controller.ResetWorldAsync(resetReason);
        }

        [ContextMenu("QA/WorldLifecycle/Reset/Soft Reset Players Now")]
        public async void ResetPlayersNow()
        {
            if (!EnsureController())
            {
                return;
            }

            await controller.ResetPlayersAsync(softResetPlayersReason);
        }

        private bool EnsureController()
        {
            if (controller != null)
            {
                return true;
            }

            controller = GetComponent<WorldLifecycleController>();
            if (controller != null)
            {
                return true;
            }

            DebugUtility.LogError(typeof(WorldLifecycleQaTools),
                $"WorldLifecycleController não encontrado para QA tools (scene='{_sceneName}').",
                this);

            return false;
        }
    }
}

#endif
