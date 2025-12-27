using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Cameras;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Execution.Gate;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.Fade;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.Loading;
using _ImmersiveGames.NewScripts.Infrastructure.State;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.QA
{
    [DisallowMultipleComponent]
    public sealed class WorldLifecycleQaTools : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private WorldLifecycleController controller;

        [Header("QA/Reset")]
        [SerializeField] private string resetReason = "ContextMenu/ResetWorldNow";
        [SerializeField] private string softResetPlayersReason = "ContextMenu/SoftResetPlayers";

        [Header("QA/SceneFlow ContextMenu")]
        [SerializeField] private string menuSceneName = "MenuScene";
        [SerializeField] private string uiGlobalSceneName = "UIGlobalScene";
        [SerializeField] private string bootstrapSceneName = "NewBootstrap";
        [SerializeField] private string gameplaySceneName = "GameplayScene";
        [SerializeField] private string menuProfileName = "startup";
        [SerializeField] private string gameplayProfileName = "gameplay";
        [SerializeField] private bool sceneFlowUseFade = true;

        [Header("QA/GameLoop")]
        [SerializeField] private bool enterPlayingAfterGameplayTransition = true;

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

        [ContextMenu("QA/Diagnostics/Snapshot (Scene/DI/HUD/GameLoop)")]
        public void Snapshot()
        {
            var sb = new StringBuilder(1536);

            sb.AppendLine("[QA Snapshot] ------------------------------");
            sb.AppendLine($"scene(go)='{_sceneName}', activeScene='{SceneManager.GetActiveScene().name}', sceneCount={SceneManager.sceneCount}");

            sb.Append("loaded=[");
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }
                sb.Append(SceneManager.GetSceneAt(i).name);
            }
            sb.AppendLine("]");

            // 1) Find HUD controller
            var hudCtrl = FindFirstObjectByType<SceneLoadingHudController>(FindObjectsInactive.Include);
            sb.AppendLine(hudCtrl != null
                ? $"hudCtrl=FOUND (go='{hudCtrl.gameObject.name}', scene='{hudCtrl.gameObject.scene.name}', active={hudCtrl.gameObject.activeInHierarchy})"
                : "hudCtrl=NOT_FOUND (SceneLoadingHudController)");

            // 2) Find any MonoBehaviour implementing ISceneLoadingHud
            var hudIface = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).FirstOrDefault(mb => mb is ISceneLoadingHud) as ISceneLoadingHud;
            if (hudIface is Component hudComp)
            {
                sb.AppendLine($"hudIface=FOUND (type='{hudIface.GetType().FullName}', go='{hudComp.gameObject.name}', scene='{hudComp.gameObject.scene.name}', active={hudComp.gameObject.activeInHierarchy})");
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

        [ContextMenu("QA/LoadingHud/Force Rebind ISceneLoadingHud (Global)")]
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

        [ContextMenu("QA/Reset World Now")]
        public async void ResetWorldNow()
        {
            if (!EnsureController())
            {
                return;
            }

            await controller.ResetWorldAsync(resetReason);
        }

        [ContextMenu("QA/Soft Reset Players Now")]
        public async void ResetPlayersNow()
        {
            if (!EnsureController())
            {
                return;
            }

            await controller.ResetPlayersAsync(softResetPlayersReason);
        }

        // ----------------------------
        // QA: SceneFlow Context Menus
        // ----------------------------

        [ContextMenu("QA/SceneFlow/Transition -> Ready (startup)")]
        public async void SceneFlowToMenuNow()
        {
            var request = new SceneTransitionRequest(
                scenesToLoad: new[] { menuSceneName, uiGlobalSceneName }.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray(),
                scenesToUnload: new[] { bootstrapSceneName, gameplaySceneName }.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray(),
                targetActiveScene: menuSceneName,
                useFade: sceneFlowUseFade,
                transitionProfileName: menuProfileName);

            await RunSceneFlowTransitionAsync(request, "ContextMenu/SceneFlowToMenu", afterTransition: null);
        }

        [ContextMenu("QA/SceneFlow/Transition -> Gameplay (gameplay)")]
        public async void SceneFlowToGameplayNow()
        {
            var request = new SceneTransitionRequest(
                scenesToLoad: new[] { gameplaySceneName, uiGlobalSceneName }.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray(),
                scenesToUnload: new[] { bootstrapSceneName, menuSceneName }.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray(),
                targetActiveScene: gameplaySceneName,
                useFade: sceneFlowUseFade,
                transitionProfileName: gameplayProfileName);

            await RunSceneFlowTransitionAsync(
                request,
                "ContextMenu/SceneFlowToGameplay",
                afterTransition: enterPlayingAfterGameplayTransition ? TryEnterPlayingAfterGameplay : null);
        }

        private async Task RunSceneFlowTransitionAsync(SceneTransitionRequest request, string reason, Func<Task> afterTransition)
        {
            if (request == null)
            {
                DebugUtility.LogError(typeof(WorldLifecycleQaTools),
                    $"[SceneFlow QA] Request nulo. reason='{reason}'.");
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISceneTransitionService>(out var sceneFlow) || sceneFlow == null)
            {
                DebugUtility.LogError(typeof(WorldLifecycleQaTools),
                    $"[SceneFlow QA] ISceneTransitionService indisponível no DI global. reason='{reason}'.");
                return;
            }

            try
            {
                DebugUtility.Log(typeof(WorldLifecycleQaTools),
                    "[SceneFlow QA] Disparando TransitionAsync via ContextMenu. " +
                    $"reason='{reason}', targetActive='{request.TargetActiveScene}', profile='{request.TransitionProfileName}'.",
                    DebugUtility.Colors.Info);

                await sceneFlow.TransitionAsync(request);

                DebugUtility.Log(typeof(WorldLifecycleQaTools),
                    $"[SceneFlow QA] TransitionAsync concluído. reason='{reason}'.",
                    DebugUtility.Colors.Success);

                if (afterTransition != null)
                {
                    await afterTransition();
                }
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(WorldLifecycleQaTools),
                    $"[SceneFlow QA] Falha ao executar TransitionAsync. reason='{reason}', ex={ex}",
                    this);
            }
        }

        private Task TryEnterPlayingAfterGameplay()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var loop) || loop == null)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleQaTools),
                    "[QA][GameLoop] IGameLoopService indisponível no DI global. Não foi possível tentar entrar em Playing.");
                return Task.CompletedTask;
            }

            DebugUtility.Log(typeof(WorldLifecycleQaTools),
                "[QA][GameLoop] Tentando avançar para Playing chamando IGameLoopService.RequestStart() após transição para Gameplay.",
                DebugUtility.Colors.Info);

            loop.RequestStart();
            return Task.CompletedTask;
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
