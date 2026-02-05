using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors.Runtime;
using _ImmersiveGames.NewScripts.Modules.Gameplay.RunRearm;
using _ImmersiveGames.NewScripts.Modules.Gameplay.RunRearm.Interop;
using _ImmersiveGames.NewScripts.Modules.Gameplay.RunRearm.Runtime;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Spawning.Definitions;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Readiness;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Dev;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Hooks;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Spawn;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    /// <summary>
    /// Inicializa serviços de escopo de cena para o NewScripts e garante limpeza determinística.
    /// </summary>
    public sealed class SceneScopeCompositionRoot : MonoBehaviour
    {
        [SerializeField]
        private WorldDefinition worldDefinition;

        private string _sceneName = string.Empty;
        private bool _registered;
        private readonly WorldSpawnServiceFactory _spawnServiceFactory = new();
        private IWorldSpawnContext _worldSpawnContext;

        private void Awake()
        {
            // A cena correta para o escopo é a cena do GameObject, não a ActiveScene (especialmente em Additive).
            var scene = gameObject.scene;
            _sceneName = scene.name;

            if (_registered)
            {
                // Não é um problema em fluxo additive; reduzimos ruído (warning) para verbose.
                DebugUtility.LogVerbose(typeof(SceneScopeCompositionRoot),
                    $"Scene scope already created (ignored): {_sceneName}");
                return;
            }

            var provider = DependencyManager.Provider;

            provider.RegisterForScene<ISceneScopeMarker>(
                _sceneName,
                new SceneScopeMarker(),
                allowOverride: false);

            var worldRoot = EnsureWorldRoot(scene);
            _worldSpawnContext = new WorldSpawnContext(_sceneName, worldRoot);

            provider.RegisterForScene(
                _sceneName,
                _worldSpawnContext,
                allowOverride: false);

            DebugUtility.Log(typeof(SceneScopeCompositionRoot),
                $"WorldRoot ready: {BuildTransformPath(worldRoot)}");

            var actorRegistry = new ActorRegistry();
            provider.RegisterForScene<IActorRegistry>(
                _sceneName,
                actorRegistry,
                allowOverride: false);

            var spawnRegistry = new WorldSpawnServiceRegistry();
            provider.RegisterForScene<IWorldSpawnServiceRegistry>(
                _sceneName,
                spawnRegistry,
                allowOverride: false);

            // ----------------------------
            // Gameplay Reset (Groups/Targets)
            // ----------------------------
            // Classificador de alvos (Players/Eater/ActorIdSet/All) para reset de gameplay.
            if (!provider.TryGetForScene<IRunRearmTargetClassifier>(_sceneName, out var classifier) || classifier == null)
            {
                provider.TryGetGlobal<IRuntimeModeProvider>(out var runtimeModeProvider);
                provider.TryGetGlobal<IDegradedModeReporter>(out var degradedModeReporter);

                classifier = new DefaultRunRearmTargetClassifier(runtimeModeProvider, degradedModeReporter);
                provider.RegisterForScene(_sceneName, classifier, allowOverride: false);

                DebugUtility.LogVerbose(typeof(SceneScopeCompositionRoot),
                    $"IRunRearmTargetClassifier registrado para a cena '{_sceneName}'.");
            }

            // Orquestrador de reset de gameplay (por fases) acionável por participantes do WorldLifecycle.
            if (!provider.TryGetForScene<IRunRearmOrchestrator>(_sceneName, out var gameplayReset) || gameplayReset == null)
            {
                gameplayReset = new RunRearmOrchestrator(_sceneName);
                provider.RegisterForScene(_sceneName, gameplayReset, allowOverride: false);

                DebugUtility.LogVerbose(typeof(SceneScopeCompositionRoot),
                    $"IRunRearmOrchestrator registrado para a cena '{_sceneName}'.");
            }

            // Guardrail: o registry de lifecycle deve ser criado apenas aqui no bootstrapper.
            // Nunca criar o WorldLifecycleHookRegistry no controller/orchestrator.
            WorldLifecycleHookRegistry hookRegistry;
            if (provider.TryGetForScene<WorldLifecycleHookRegistry>(_sceneName, out var existingRegistry))
            {
                DebugUtility.LogError(typeof(SceneScopeCompositionRoot),
                    $"WorldLifecycleHookRegistry já existe para a cena '{_sceneName}'. Segundo registro bloqueado.");
                hookRegistry = existingRegistry;
            }
            else
            {
                hookRegistry = new WorldLifecycleHookRegistry();
                provider.RegisterForScene(
                    _sceneName,
                    hookRegistry,
                    allowOverride: false);
                DebugUtility.LogVerbose(typeof(SceneScopeCompositionRoot),
                    $"WorldLifecycleHookRegistry registrado para a cena '{_sceneName}'.");
            }

            // Ponte WorldLifecycle soft reset -> Gameplay reset
            var playersResetParticipant = new PlayersRunRearmWorldParticipant();
            provider.RegisterForScene<IRunRearmWorldParticipant>(
                _sceneName,
                playersResetParticipant,
                allowOverride: false);
            DebugUtility.LogVerbose(typeof(SceneScopeCompositionRoot),
                $"IRunRearmWorldBridge registrado para a cena '{_sceneName}'.");

            RegisterSceneLifecycleHooks(hookRegistry, worldRoot);

            RegisterSpawnServicesFromDefinition(provider, spawnRegistry, actorRegistry, _worldSpawnContext);

            _registered = true;
            DebugUtility.Log(typeof(SceneScopeCompositionRoot), $"Scene scope created: {_sceneName}");
        }

        private void OnDestroy()
        {
            if (!_registered)
            {
                // Nothing to clear for this instance.
                return;
            }

            if (string.IsNullOrEmpty(_sceneName))
            {
                _sceneName = gameObject.scene.name;
            }

            DependencyManager.Provider.ClearSceneServices(_sceneName);
            DebugUtility.Log(typeof(SceneScopeCompositionRoot), $"Scene scope cleared: {_sceneName}");

            _registered = false;
        }

        private void RegisterSpawnServicesFromDefinition(
            IDependencyProvider provider,
            IWorldSpawnServiceRegistry registry,
            IActorRegistry actorRegistry,
            IWorldSpawnContext context)
        {
            if (worldDefinition == null)
            {
                // Esperado em cenas sem spawn (Ready). Evita WARNING no fluxo normal.
                DebugUtility.LogVerbose(typeof(SceneScopeCompositionRoot),
                    $"WorldDefinition não atribuída (scene='{_sceneName}'). " +
                    "Isto é permitido em cenas sem spawn (ex.: Ready). Serviços de spawn não serão registrados.");

                DebugUtility.Log(typeof(SceneScopeCompositionRoot),
                    "Spawn services registered from definition: 0");
                return;
            }

            DebugUtility.Log(typeof(SceneScopeCompositionRoot),
                $"WorldDefinition loaded: {worldDefinition.name}");

            int registeredCount = 0;
            int enabledCount = 0;
            int createdCount = 0;
            int skippedDisabledCount = 0;
            int failedCreateCount = 0;

            IReadOnlyList<WorldDefinition.SpawnEntry> entries = worldDefinition.Entries;
            int totalEntries = entries?.Count ?? 0;

            DebugUtility.LogVerbose(typeof(SceneScopeCompositionRoot),
                $"WorldDefinition entries count: {totalEntries}");

            if (entries != null)
            {
                for (int index = 0; index < entries.Count; index++)
                {
                    var entry = entries[index];
                    string entryKind = entry.Kind.ToString();
                    string entryPrefabName = entry.Prefab != null ? entry.Prefab.name : "<null>";

                    DebugUtility.LogVerbose(typeof(SceneScopeCompositionRoot),
                        $"Spawn entry #{index}: Enabled={entry.Enabled}, Kind={entryKind}, Prefab={entryPrefabName}");

                    if (!entry.Enabled)
                    {
                        skippedDisabledCount++;
                        DebugUtility.LogVerbose(typeof(SceneScopeCompositionRoot),
                            $"Spawn entry #{index} SKIPPED_DISABLED: Kind={entryKind}, Prefab={entryPrefabName}");
                        continue;
                    }

                    enabledCount++;

                    var service = _spawnServiceFactory.Create(entry, provider, actorRegistry, context);
                    if (service == null)
                    {
                        failedCreateCount++;
                        // Mantém WARNING: aqui já é indicação real de configuração/entrada problemática.
                        DebugUtility.LogWarning(typeof(SceneScopeCompositionRoot),
                            $"Spawn entry #{index} FAILED_CREATE: Kind={entryKind}, Prefab={entryPrefabName}");
                        continue;
                    }

                    createdCount++;
                    registry.Register(service);
                    registeredCount++;

                    DebugUtility.LogVerbose(typeof(SceneScopeCompositionRoot),
                        $"Spawn entry #{index} REGISTERED: {service.Name} (Kind={entryKind}, Prefab={entryPrefabName})");
                }

                DebugUtility.Log(typeof(SceneScopeCompositionRoot),
                    $"Spawn services registered from definition: {registeredCount}");
            }

            DebugUtility.LogVerbose(typeof(SceneScopeCompositionRoot),
                $"Spawn services summary => Total={totalEntries}, Enabled={enabledCount}, Disabled={skippedDisabledCount}, " +
                $"Created={createdCount}, FailedCreate={failedCreateCount}");
        }

        private Transform EnsureWorldRoot(UnityEngine.SceneManagement.Scene scene)
        {
            var targetScene = scene.IsValid() ? scene : SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                DebugUtility.LogWarning(typeof(SceneScopeCompositionRoot),
                    $"EnsureWorldRoot recebeu uma cena inválida. Usando ActiveScene como fallback. bootstrapScene='{_sceneName}'");
            }

            GameObject[] rootObjects = targetScene.GetRootGameObjects();
            var worldRoots = rootObjects.Where(r => r != null && r.name == "WorldRoot").ToList();

            if (worldRoots.Count == 0)
            {
                return CreateWorldRoot(targetScene);
            }

            var selectedRoot = worldRoots[0];
            if (worldRoots.Count > 1)
            {
                LogMultipleWorldRoots(targetScene, rootObjects, worldRoots.Count, selectedRoot);
            }
            if (selectedRoot.scene != targetScene)
            {
                SceneManager.MoveGameObjectToScene(selectedRoot, targetScene);
            }

            return selectedRoot.transform;
        }

        private Transform CreateWorldRoot(UnityEngine.SceneManagement.Scene scene)
        {
            var worldRootGo = new GameObject("WorldRoot");
            SceneManager.MoveGameObjectToScene(worldRootGo, scene);
            return worldRootGo.transform;
        }

        private void LogMultipleWorldRoots(UnityEngine.SceneManagement.Scene scene, GameObject[] allRoots, int foundCount, GameObject selectedRoot)
        {
            DebugUtility.LogWarning(typeof(SceneScopeCompositionRoot),
                $"Multiple WorldRoot objects found in scene '{scene.name}': {foundCount}");

            foreach (var root in allRoots)
            {
                if (root != null && root.name == "WorldRoot")
                {
                    DebugUtility.LogWarning(typeof(SceneScopeCompositionRoot),
                        $"WorldRoot candidate: {BuildTransformPath(root.transform)}");
                }
            }

            DebugUtility.LogWarning(typeof(SceneScopeCompositionRoot),
                $"WorldRoot selected: {BuildTransformPath(selectedRoot?.transform)}");
        }

        private void RegisterSceneLifecycleHooks(
            WorldLifecycleHookRegistry hookRegistry,
            Transform worldRoot)
        {
            if (hookRegistry == null)
            {
                // Anômalo; mantém WARNING.
                DebugUtility.LogWarning(typeof(SceneScopeCompositionRoot),
                    "WorldLifecycleHookRegistry ausente; hooks de cena não serão registrados.");
                return;
            }

            if (worldRoot == null)
            {
                // Anômalo; mantém WARNING.
                DebugUtility.LogWarning(typeof(SceneScopeCompositionRoot),
                    "WorldRoot ausente; hooks de cena não serão adicionados.");
                return;
            }

            /* Aqui é ume exemplo de hook no ciclo do mundo.*/
             var hookA = EnsureHookComponent<WorldLifecycleHookLoggerA>(worldRoot);

            RegisterHookIfMissing(hookRegistry, hookA);
        }

        private static T EnsureHookComponent<T>(Transform worldRoot)
            where T : Component
        {
            var existing = worldRoot.GetComponent<T>();
            if (existing != null)
            {
                return existing;
            }

            return worldRoot.gameObject.AddComponent<T>();
        }

        private static void RegisterHookIfMissing(
            WorldLifecycleHookRegistry registry,
            IWorldLifecycleHook hook)
        {
            if (hook == null)
            {
                // Anômalo; mantém WARNING.
                DebugUtility.LogWarning(typeof(SceneScopeCompositionRoot),
                    "Hook de cena nulo; registro ignorado.");
                return;
            }

            if (registry.Hooks.Contains(hook))
            {
                DebugUtility.LogVerbose(typeof(SceneScopeCompositionRoot),
                    $"Hook de cena já registrado: {hook.GetType().Name}");
                return;
            }

            registry.Register(hook);
            DebugUtility.LogVerbose(typeof(SceneScopeCompositionRoot),
                $"Hook de cena registrado: {hook.GetType().Name}");
        }

        private static string BuildTransformPath(Transform transform)
        {
            if (transform == null)
            {
                return "<null>";
            }

            return $"{transform.gameObject.scene.name}/{transform.name}";
        }
    }
}


