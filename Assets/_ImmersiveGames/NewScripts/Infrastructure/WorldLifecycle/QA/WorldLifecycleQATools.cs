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

namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.QA
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
            var hudCtrl = FindFirstObjectByType<SceneLoadingHudController>(FindObjectsInactive.Include);
            sb.AppendLine(hudCtrl != null
                ? $"hudCtrl=FOUND (go='{hudCtrl.gameObject.name}', scene='{hudCtrl.gameObject.scene.name}', active={hudCtrl.gameObject.activeInHierarchy})"
                : "hudCtrl=NOT_FOUND (SceneLoadingHudController)");

            // 2) Find any MonoBehaviour implementing ISceneLoadingHud
            var hudIface = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .FirstOrDefault(mb => mb is ISceneLoadingHud) as ISceneLoadingHud;

            if (hudIface is Component hudComp)
            {
                sb.AppendLine(
                    $"hudIface=FOUND (type='{hudIface.GetType().FullName}', go='{hudComp.gameObject.name}', scene='{hudComp.gameObject.scene.name}', active={hudComp.gameObject.activeInHierarchy})");
            }
            else
            {
                sb.AppendLine("hudIface=NOT_FOUND (no MonoBehaviour implements ISceneLoadingHud)");
            }

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
            AppendDiLine<ISceneLoadingHud>(sb, "ISceneLoadingHud");
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

        [ContextMenu("QA/WorldLifecycle/LoadingHud/Force Rebind ISceneLoadingHud (Global)")]
        public void ForceRebindLoadingHudToGlobal()
        {
            // Diagnóstico: encontra um HUD “real” na cena e tenta garantir que o DI global enxerga.
            var hudCtrl = FindFirstObjectByType<SceneLoadingHudController>(FindObjectsInactive.Include);
            if (hudCtrl == null)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleQaTools),
                    "[QA][LoadingHud] SceneLoadingHudController NÃO encontrado. Não é possível rebind.",
                    this);
                return;
            }

            // Se já resolve globalmente, loga e sai.
            if (DependencyManager.Provider.TryGetGlobal<ISceneLoadingHud>(out var existing) && existing != null)
            {
                DebugUtility.Log(typeof(WorldLifecycleQaTools),
                    $"[QA][LoadingHud] ISceneLoadingHud já está registrado no DI global (type='{existing.GetType().FullName}'). Nenhuma ação necessária.",
                    DebugUtility.Colors.Success);
                return;
            }

            // Aqui a ação depende da API do seu Provider.
            // Eu NÃO vou inventar assinatura. Então:
            // - Se seu Provider expõe algum RegisterGlobal em runtime, substitua abaixo.
            // - Caso contrário, este botão serve para confirmar que o problema é “registrar no global”.
            DebugUtility.LogWarning(typeof(WorldLifecycleQaTools),
                "[QA][LoadingHud] ISceneLoadingHud NÃO está no DI global. " +
                "O HUD controller existe na cena, então a causa mais provável é registro em escopo diferente do global. " +
                "Ajuste o bootstrap do HUD (UIGlobalScene) para registrar ISceneLoadingHud no DI GLOBAL, ou exponha uma API de registro global em runtime para este QA.",
                this);
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
