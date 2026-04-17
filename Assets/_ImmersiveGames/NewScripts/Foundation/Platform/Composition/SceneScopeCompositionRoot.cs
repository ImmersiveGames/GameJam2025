using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.GameplayRuntime.ActorRegistry;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Content.Definitions.Worlds.Config;
using _ImmersiveGames.NewScripts.GameplayRuntime.Spawn;
using _ImmersiveGames.NewScripts.ResetFlow.SceneReset.Hooks;
using _ImmersiveGames.NewScripts.SceneFlow.Readiness.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.Composition
{
    /// <summary>
    /// Inicializa serviços de escopo de cena para o NewScripts e garante limpeza determinística.
    /// </summary>
    public sealed partial class SceneScopeCompositionRoot : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Input de authoring/bootstrap de conteudo para o escopo de cena. Nao e owner semantico do gameplay.")]
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
                $"Scene bootstrap root ready: {BuildTransformPath(worldRoot)}");

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
            // Nunca criar o SceneResetHookRegistry no controller/orchestrator.
            SceneResetHookRegistry hookRegistry;
            if (provider.TryGetForScene<SceneResetHookRegistry>(_sceneName, out var existingRegistry))
            {
                DebugUtility.LogError(typeof(SceneScopeCompositionRoot),
                    $"SceneResetHookRegistry já existe para a cena '{_sceneName}'. Segundo registro bloqueado.");
                hookRegistry = existingRegistry;
            }
            else
            {
                hookRegistry = new SceneResetHookRegistry();
                provider.RegisterForScene(
                    _sceneName,
                    hookRegistry,
                    allowOverride: false);
                DebugUtility.LogVerbose(typeof(SceneScopeCompositionRoot),
                    $"SceneResetHookRegistry registrado para a cena '{_sceneName}'.");
            }

            RegisterActorGroupGameplayResetServices(provider, hookRegistry, worldRoot);

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
                    "Spawn services registered from authoring input: 0");
                return;
            }

            DebugUtility.Log(typeof(SceneScopeCompositionRoot),
                $"Authoring input loaded: {worldDefinition.name}");

            int registeredCount = 0;
            int enabledCount = 0;
            int createdCount = 0;
            int skippedDisabledCount = 0;
            int failedCreateCount = 0;

            IReadOnlyList<WorldDefinition.SpawnEntry> entries = worldDefinition.Entries;
            int totalEntries = entries?.Count ?? 0;

            DebugUtility.LogVerbose(typeof(SceneScopeCompositionRoot),
                $"Authoring input entries count: {totalEntries}");

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
                    $"Spawn services registered from authoring input: {registeredCount}");
            }

            DebugUtility.LogVerbose(typeof(SceneScopeCompositionRoot),
                $"Spawn services summary => Total={totalEntries}, Enabled={enabledCount}, Disabled={skippedDisabledCount}, " +
                $"Created={createdCount}, FailedCreate={failedCreateCount}");
        }

        private Transform EnsureWorldRoot(Scene scene)
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

        private Transform CreateWorldRoot(Scene scene)
        {
            var worldRootGo = new GameObject("WorldRoot");
            SceneManager.MoveGameObjectToScene(worldRootGo, scene);
            return worldRootGo.transform;
        }

        private void LogMultipleWorldRoots(Scene scene, GameObject[] allRoots, int foundCount, GameObject selectedRoot)
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
            SceneResetHookRegistry hookRegistry,
            Transform worldRoot)
        {
            _ = hookRegistry;
            _ = worldRoot;
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









