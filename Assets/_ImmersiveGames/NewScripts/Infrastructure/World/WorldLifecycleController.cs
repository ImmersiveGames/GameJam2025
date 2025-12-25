using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Execution.Gate;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.World
{
    [DisallowMultipleComponent]
    public sealed class WorldLifecycleController : MonoBehaviour
    {
        [Header("Lifecycle")]
        [Tooltip("Quando true, o controller executa ResetWorldAsync automaticamente no Start(). " +
                 "Para testes automatizados (Opção B), o AutoTestRunner deve desligar isto antes do Start().")]
        [SerializeField] private bool autoInitializeOnStart = true;

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = true;

        [Header("QA/SceneFlow ContextMenu")]
        [Tooltip("Nome da cena de Ready (usado pelos context menus de transição).")]
        [SerializeField] private string menuSceneName = "MenuScene";

        [Tooltip("Nome da cena UI global (usado pelos context menus de transição).")]
        [SerializeField] private string uiGlobalSceneName = "UIGlobalScene";

        [Tooltip("Nome da cena de bootstrap (usado pelos context menus de transição).")]
        [SerializeField] private string bootstrapSceneName = "NewBootstrap";

        [Tooltip("Nome da cena de Gameplay (ajuste para o nome real do seu projeto).")]
        [SerializeField] private string gameplaySceneName = "GameplayScene";

        [Tooltip("Profile do SceneFlow para Ready/startup (deve casar com as regras do WorldLifecycleRuntimeDriver).")]
        [SerializeField] private string menuProfileName = "startup";

        [Tooltip("Profile do SceneFlow para gameplay (deve ser diferente de 'startup').")]
        [SerializeField] private string gameplayProfileName = "gameplay";

        [Tooltip("Usar fade na transição (se houver adapter disponível).")]
        [SerializeField] private bool sceneFlowUseFade = true;

        [Inject] private ISimulationGateService _gateService;
        [Inject] private IWorldSpawnServiceRegistry _spawnRegistry;
        [Inject] private IActorRegistry _actorRegistry;

        // Guardrail: este controller apenas consome o WorldLifecycleHookRegistry criado no bootstrapper.
        [Inject] private WorldLifecycleHookRegistry _hookRegistry;

        private readonly List<IWorldSpawnService> _spawnServices = new();
        private WorldLifecycleOrchestrator _orchestrator;
        private bool _dependenciesInjected;
        private bool _isResetting;
        private string _sceneName = string.Empty;

        public bool AutoInitializeOnStart
        {
            get => autoInitializeOnStart;
            set => autoInitializeOnStart = value;
        }

        private void Awake()
        {
            _sceneName = gameObject.scene.name;
            // IMPORTANT: Do NOT inject here. Scene services may not be registered yet.
        }

        private void Start()
        {
            EnsureDependenciesInjected();
            if (!HasCriticalDependencies())
            {
                return;
            }

            if (!autoInitializeOnStart)
            {
                if (verboseLogs)
                {
                    DebugUtility.Log(typeof(WorldLifecycleController),
                        $"AutoInitializeOnStart desabilitado — aguardando acionamento externo (scene='{_sceneName}').");
                }
                return;
            }

            _ = InitializeWorldAsync();
        }

        /// <summary>
        /// API para automação/QA. Garante não concorrência e executa um reset completo.
        /// </summary>
        public async Task ResetWorldAsync(string reason)
        {
            if (_isResetting)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleController),
                    $"Reset ignorado (já em andamento). reason='{reason}', scene='{_sceneName}'.");
                return;
            }

            _isResetting = true;
            try
            {
                EnsureDependenciesInjected();
                if (!HasCriticalDependencies())
                {
                    return;
                }

                if (verboseLogs)
                {
                    DebugUtility.Log(typeof(WorldLifecycleController),
                        $"Reset iniciado. reason='{reason}', scene='{_sceneName}'.");
                }

                BuildSpawnServices();
                _orchestrator = CreateOrchestrator();

                await _orchestrator.ResetWorldAsync();

                if (verboseLogs)
                {
                    DebugUtility.Log(typeof(WorldLifecycleController),
                        $"Reset concluído. reason='{reason}', scene='{_sceneName}'.");
                }
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(WorldLifecycleController),
                    $"Exception during world reset in scene '{_sceneName}' (reason='{reason}'): {ex}",
                    this);
            }
            finally
            {
                _isResetting = false;
            }
        }

        /// <summary>
        /// Soft reset focado apenas no escopo de jogadores (Players).
        /// </summary>
        public async Task ResetPlayersAsync(string reason = "QA/PlayersSoftReset")
        {
            if (_isResetting)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleController),
                    $"Soft reset ignorado (já em andamento). reason='{reason}', scene='{_sceneName}'.");
                return;
            }

            _isResetting = true;
            try
            {
                EnsureDependenciesInjected();
                if (!HasCriticalDependencies())
                {
                    return;
                }

                if (verboseLogs)
                {
                    DebugUtility.Log(typeof(WorldLifecycleController),
                        $"Soft reset (Players) iniciado. reason='{reason}', scene='{_sceneName}'.");
                }

                BuildSpawnServices();
                _orchestrator = CreateOrchestrator();

                await _orchestrator.ResetScopesAsync(
                    new List<ResetScope> { ResetScope.Players },
                    reason);

                if (verboseLogs)
                {
                    DebugUtility.Log(typeof(WorldLifecycleController),
                        $"Soft reset (Players) concluído. reason='{reason}', scene='{_sceneName}'.");
                }
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(WorldLifecycleController),
                    $"Exception during players soft reset in scene '{_sceneName}' (reason='{reason}'): {ex}",
                    this);
            }
            finally
            {
                _isResetting = false;
            }
        }

        [ContextMenu("QA/Reset World Now")]
        public async void ResetWorldNow()
        {
            await ResetWorldAsync("ContextMenu/ResetWorldNow");
        }

        [ContextMenu("QA/Soft Reset Players Now")]
        public async void ResetPlayersNow()
        {
            await ResetPlayersAsync("ContextMenu/SoftResetPlayers");
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
            DebugUtility.Log(typeof(WorldLifecycleController),
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
                DebugUtility.LogError(typeof(WorldLifecycleController),
                    $"[SceneFlow QA] Request nulo. reason='{reason}'.");
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISceneTransitionService>(out var sceneFlow) || sceneFlow == null)
            {
                DebugUtility.LogError(typeof(WorldLifecycleController),
                    $"[SceneFlow QA] ISceneTransitionService indisponível no DI global. reason='{reason}'.");
                return;
            }

            try
            {
                DebugUtility.Log(typeof(WorldLifecycleController),
                    "[SceneFlow QA] Disparando TransitionAsync via ContextMenu. " +
                    $"reason='{reason}', targetActive='{request.TargetActiveScene}', profile='{request.TransitionProfileName}'.",
                    DebugUtility.Colors.Info);

                await sceneFlow.TransitionAsync(request);

                DebugUtility.Log(typeof(WorldLifecycleController),
                    $"[SceneFlow QA] TransitionAsync concluído. reason='{reason}'.",
                    DebugUtility.Colors.Success);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(WorldLifecycleController),
                    $"[SceneFlow QA] Falha ao executar TransitionAsync. reason='{reason}', ex={ex}",
                    this);
            }
        }

        private async Task InitializeWorldAsync()
        {
            await ResetWorldAsync("AutoInitialize/Start");
        }

        private void EnsureDependenciesInjected()
        {
            if (_dependenciesInjected)
            {
                return;
            }

            DependencyManager.Provider.InjectDependencies(this);
            _dependenciesInjected = true;

            if (_hookRegistry == null)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleController),
                    $"WorldLifecycleHookRegistry não encontrado para a cena '{_sceneName}'. Hooks via registry ficarão desativados.");
            }

            if (_gateService == null)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleController),
                    $"ISimulationGateService não injetado para a cena '{_sceneName}'. Reset seguirá sem gate.");
            }

            if (_spawnRegistry == null)
            {
                DebugUtility.LogError(typeof(WorldLifecycleController),
                    $"IWorldSpawnServiceRegistry não encontrado para a cena '{_sceneName}'.");
            }

            if (_actorRegistry == null)
            {
                DebugUtility.LogError(typeof(WorldLifecycleController),
                    $"IActorRegistry não encontrado para a cena '{_sceneName}'.");
            }
        }

        private void BuildSpawnServices()
        {
            _spawnServices.Clear();

            if (_spawnRegistry == null)
            {
                DebugUtility.LogError(typeof(WorldLifecycleController),
                    $"Nenhum IWorldSpawnServiceRegistry encontrado para a cena '{_sceneName}'. Lista ficará vazia.",
                    this);
                return;
            }

            foreach (var service in _spawnRegistry.Services)
            {
                if (service != null)
                {
                    _spawnServices.Add(service);
                }
                else
                {
                    DebugUtility.LogWarning(typeof(WorldLifecycleController),
                        $"Serviço de spawn nulo detectado na cena '{_sceneName}'. Ignorado.");
                }
            }

            DebugUtility.Log(typeof(WorldLifecycleController),
                $"Spawn services coletados para a cena '{_sceneName}': {_spawnServices.Count} (registry total: {_spawnRegistry.Services.Count}).");

            if (_actorRegistry != null)
            {
                DebugUtility.Log(typeof(WorldLifecycleController),
                    $"ActorRegistry count antes de orquestrar: {_actorRegistry.Count}");
            }
        }

        private WorldLifecycleOrchestrator CreateOrchestrator()
        {
            EnsureDependenciesInjected();

            return new WorldLifecycleOrchestrator(
                _gateService,
                _spawnServices,
                _actorRegistry,
                provider: DependencyManager.Provider,
                sceneName: _sceneName,
                hookRegistry: _hookRegistry);
        }

        private bool HasCriticalDependencies()
        {
            var valid = true;

            if (_spawnRegistry == null)
            {
                DebugUtility.LogError(typeof(WorldLifecycleController),
                    $"Sem IWorldSpawnServiceRegistry para a cena '{_sceneName}'. Ciclo de vida não pode continuar.");
                valid = false;
            }

            if (_actorRegistry == null)
            {
                DebugUtility.LogError(typeof(WorldLifecycleController),
                    $"Sem IActorRegistry para a cena '{_sceneName}'. Ciclo de vida não pode continuar.");
                valid = false;
            }

            return valid;
        }
    }
}
