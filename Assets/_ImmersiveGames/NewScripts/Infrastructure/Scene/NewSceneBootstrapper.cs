using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using System.Linq;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Hooks;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Reset;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Reset.QA;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Spawn;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Infrastructure.Scene
{
    /// <summary>
    /// Inicializa serviços de escopo de cena para o NewScripts e garante limpeza determinística.
    /// </summary>
    public sealed class NewSceneBootstrapper : MonoBehaviour
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
                DebugUtility.LogVerbose(typeof(NewSceneBootstrapper),
                    $"Scene scope already created (ignored): {_sceneName}");
                return;
            }

            var provider = DependencyManager.Provider;

            provider.RegisterForScene<INewSceneScopeMarker>(
                _sceneName,
                new NewSceneScopeMarker(),
                allowOverride: false);

            var worldRoot = EnsureWorldRoot(scene);
            _worldSpawnContext = new WorldSpawnContext(_sceneName, worldRoot);

            provider.RegisterForScene(
                _sceneName,
                _worldSpawnContext,
                allowOverride: false);

            DebugUtility.Log(typeof(NewSceneBootstrapper),
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

            // Guardrail: o registry de lifecycle deve ser criado apenas aqui no bootstrapper.
            // Nunca criar o WorldLifecycleHookRegistry no controller/orchestrator.
            WorldLifecycleHookRegistry hookRegistry;
            if (provider.TryGetForScene<WorldLifecycleHookRegistry>(_sceneName, out var existingRegistry))
            {
                DebugUtility.LogError(typeof(NewSceneBootstrapper),
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
                DebugUtility.LogVerbose(typeof(NewSceneBootstrapper),
                    $"WorldLifecycleHookRegistry registrado para a cena '{_sceneName}'.");
            }

            var playersResetParticipant = new PlayersResetParticipant();
            provider.RegisterForScene<IResetScopeParticipant>(
                _sceneName,
                playersResetParticipant,
                allowOverride: false);
            DebugUtility.LogVerbose(typeof(NewSceneBootstrapper),
                $"PlayersResetParticipant registrado para a cena '{_sceneName}'.");

            RegisterSceneLifecycleHooks(hookRegistry, worldRoot);

            RegisterSpawnServicesFromDefinition(provider, spawnRegistry, actorRegistry, _worldSpawnContext);

            _registered = true;
            DebugUtility.Log(typeof(NewSceneBootstrapper), $"Scene scope created: {_sceneName}");
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
            DebugUtility.Log(typeof(NewSceneBootstrapper), $"Scene scope cleared: {_sceneName}");

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
                DebugUtility.LogVerbose(typeof(NewSceneBootstrapper),
                    $"WorldDefinition não atribuída (scene='{_sceneName}'). " +
                    "Isto é permitido em cenas sem spawn (ex.: Ready). Serviços de spawn não serão registrados.");

                DebugUtility.Log(typeof(NewSceneBootstrapper),
                    "Spawn services registered from definition: 0");
                return;
            }

            DebugUtility.Log(typeof(NewSceneBootstrapper),
                $"WorldDefinition loaded: {worldDefinition.name}");

            int registeredCount = 0;
            int enabledCount = 0;
            int createdCount = 0;
            int skippedDisabledCount = 0;
            int failedCreateCount = 0;

            var entries = worldDefinition.Entries;
            var totalEntries = entries?.Count ?? 0;

            DebugUtility.LogVerbose(typeof(NewSceneBootstrapper),
                $"WorldDefinition entries count: {totalEntries}");

            if (entries != null)
            {
                for (var index = 0; index < entries.Count; index++)
                {
                    var entry = entries[index];
                    var entryKind = entry.Kind.ToString();
                    var entryPrefabName = entry.Prefab != null ? entry.Prefab.name : "<null>";

                    DebugUtility.LogVerbose(typeof(NewSceneBootstrapper),
                        $"Spawn entry #{index}: Enabled={entry.Enabled}, Kind={entryKind}, Prefab={entryPrefabName}");

                    if (!entry.Enabled)
                    {
                        skippedDisabledCount++;
                        DebugUtility.LogVerbose(typeof(NewSceneBootstrapper),
                            $"Spawn entry #{index} SKIPPED_DISABLED: Kind={entryKind}, Prefab={entryPrefabName}");
                        continue;
                    }

                    enabledCount++;

                    var service = _spawnServiceFactory.Create(entry, provider, actorRegistry, context);
                    if (service == null)
                    {
                        failedCreateCount++;
                        // Mantém WARNING: aqui já é indicação real de configuração/entrada problemática.
                        DebugUtility.LogWarning(typeof(NewSceneBootstrapper),
                            $"Spawn entry #{index} FAILED_CREATE: Kind={entryKind}, Prefab={entryPrefabName}");
                        continue;
                    }

                    createdCount++;
                    registry.Register(service);
                    registeredCount++;

                    DebugUtility.LogVerbose(typeof(NewSceneBootstrapper),
                        $"Spawn entry #{index} REGISTERED: {service.Name} (Kind={entryKind}, Prefab={entryPrefabName})");
                }

                DebugUtility.Log(typeof(NewSceneBootstrapper),
                    $"Spawn services registered from definition: {registeredCount}");
            }

            DebugUtility.LogVerbose(typeof(NewSceneBootstrapper),
                $"Spawn services summary => Total={totalEntries}, Enabled={enabledCount}, Disabled={skippedDisabledCount}, " +
                $"Created={createdCount}, FailedCreate={failedCreateCount}");
        }

        private Transform EnsureWorldRoot(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid())
            {
                // Isso não deveria acontecer no fluxo normal; mantemos WARNING.
                DebugUtility.LogWarning(typeof(NewSceneBootstrapper),
                    $"EnsureWorldRoot recebeu uma cena inválida. Usando ActiveScene como fallback. bootstrapScene='{_sceneName}'");

                scene = SceneManager.GetActiveScene();
            }

            var rootObjects = scene.GetRootGameObjects();
            GameObject selectedRoot = null;
            var foundCount = 0;

            foreach (var root in rootObjects)
            {
                if (root == null || root.name != "WorldRoot")
                {
                    continue;
                }

                foundCount++;
                if (selectedRoot == null)
                {
                    selectedRoot = root;
                }
            }

            if (foundCount == 0)
            {
                var worldRootGo = new GameObject("WorldRoot");
                SceneManager.MoveGameObjectToScene(worldRootGo, scene);
                return worldRootGo.transform;
            }

            if (foundCount > 1)
            {
                // Situação anômala; mantém WARNING.
                DebugUtility.LogWarning(typeof(NewSceneBootstrapper),
                    $"Multiple WorldRoot objects found in scene '{scene.name}': {foundCount}");

                foreach (var root in rootObjects)
                {
                    if (root != null && root.name == "WorldRoot")
                    {
                        DebugUtility.LogWarning(typeof(NewSceneBootstrapper),
                            $"WorldRoot candidate: {BuildTransformPath(root.transform)}");
                    }
                }

                DebugUtility.LogWarning(typeof(NewSceneBootstrapper),
                    $"WorldRoot selected: {BuildTransformPath(selectedRoot?.transform)}");
            }

            if (selectedRoot != null && selectedRoot.scene != scene)
            {
                SceneManager.MoveGameObjectToScene(selectedRoot, scene);
            }

            return selectedRoot?.transform;
        }

        private void RegisterSceneLifecycleHooks(
            WorldLifecycleHookRegistry hookRegistry,
            Transform worldRoot)
        {
            if (hookRegistry == null)
            {
                // Anômalo; mantém WARNING.
                DebugUtility.LogWarning(typeof(NewSceneBootstrapper),
                    "WorldLifecycleHookRegistry ausente; hooks de cena não serão registrados.");
                return;
            }

            if (worldRoot == null)
            {
                // Anômalo; mantém WARNING.
                DebugUtility.LogWarning(typeof(NewSceneBootstrapper),
                    "WorldRoot ausente; hooks de cena não serão adicionados.");
                return;
            }

            var hookA = EnsureHookComponent<SceneLifecycleHookLoggerA>(worldRoot);
            var hookB = EnsureHookComponent<SceneLifecycleHookLoggerB>(worldRoot);

            RegisterHookIfMissing(hookRegistry, hookA);
            RegisterHookIfMissing(hookRegistry, hookB);
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
                DebugUtility.LogWarning(typeof(NewSceneBootstrapper),
                    "Hook de cena nulo; registro ignorado.");
                return;
            }

            if (registry.Hooks.Contains(hook))
            {
                DebugUtility.LogVerbose(typeof(NewSceneBootstrapper),
                    $"Hook de cena já registrado: {hook.GetType().Name}");
                return;
            }

            registry.Register(hook);
            DebugUtility.LogVerbose(typeof(NewSceneBootstrapper),
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
