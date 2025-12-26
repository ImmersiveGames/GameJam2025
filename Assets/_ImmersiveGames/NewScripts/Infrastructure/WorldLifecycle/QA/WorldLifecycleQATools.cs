using System;
using System.Linq;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.QA
{
    /// <summary>
    /// Ferramentas de QA/Dev para acionar reset e transições SceneFlow via ContextMenu.
    /// Mantém QA fora do WorldLifecycleController (produção).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldLifecycleQaTools : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private WorldLifecycleController controller;

        [Header("QA/Reset")]
        [SerializeField] private string resetReason = "ContextMenu/ResetWorldNow";

        [SerializeField] private string softResetPlayersReason = "ContextMenu/SoftResetPlayers";

        [Header("QA/SceneFlow ContextMenu")]
        [Tooltip("Nome da cena de Ready (usado pelos context menus de transição).")]
        [SerializeField] private string menuSceneName = "MenuScene";

        [Tooltip("Nome da cena UI global (usado pelos context menus de transição).")]
        [SerializeField] private string uiGlobalSceneName = "UIGlobalScene";

        [Tooltip("Nome da cena de bootstrap (usado pelos context menus de transição).")]
        [SerializeField] private string bootstrapSceneName = "NewBootstrap";

        [Tooltip("Nome da cena de Gameplay (ajuste para o nome real do seu projeto).")]
        [SerializeField] private string gameplaySceneName = "GameplayScene";

        [Tooltip("Profile do SceneFlow para Ready/startup (deve casar com as regras do WorldLifecycleRuntimeCoordinator).")]
        [SerializeField] private string menuProfileName = "startup";

        [Tooltip("Profile do SceneFlow para gameplay (deve ser diferente de 'startup').")]
        [SerializeField] private string gameplayProfileName = "gameplay";

        [Tooltip("Usar fade na transição (se houver adapter disponível).")]
        [SerializeField] private bool sceneFlowUseFade = true;

        private string _sceneName = string.Empty;

        private void Awake()
        {
            _sceneName = gameObject.scene.name;

            if (controller == null)
            {
                controller = GetComponent<WorldLifecycleController>();
            }
        }

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

            await RunSceneFlowTransitionAsync(request, "ContextMenu/SceneFlowToMenu");
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

            await RunSceneFlowTransitionAsync(request, "ContextMenu/SceneFlowToGameplay");
        }

        [ContextMenu("QA/SceneFlow/Transition (Log Current Settings)")]
        public void SceneFlowLogSettings()
        {
            DebugUtility.Log(typeof(WorldLifecycleQaTools),
                "[SceneFlow QA] Settings => " +
                $"menuSceneName='{menuSceneName}', uiGlobalSceneName='{uiGlobalSceneName}', bootstrapSceneName='{bootstrapSceneName}', " +
                $"gameplaySceneName='{gameplaySceneName}', menuProfileName='{menuProfileName}', gameplayProfileName='{gameplayProfileName}', " +
                $"useFade={sceneFlowUseFade}, scene='{_sceneName}'.",
                DebugUtility.Colors.Info);
        }

        private async Task RunSceneFlowTransitionAsync(SceneTransitionRequest request, string reason)
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
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(WorldLifecycleQaTools),
                    $"[SceneFlow QA] Falha ao executar TransitionAsync. reason='{reason}', ex={ex}",
                    this);
            }
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
