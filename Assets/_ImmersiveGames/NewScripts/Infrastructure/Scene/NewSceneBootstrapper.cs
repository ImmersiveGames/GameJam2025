using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.NewScripts.Infrastructure.World;
using _ImmersiveGames.Scripts.Utils;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
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
            _sceneName = gameObject.scene.name;

            if (_registered)
            {
                DebugUtility.LogWarning(typeof(NewSceneBootstrapper),
                    $"Scene scope already created (ignored): {_sceneName}");
                return;
            }

            var provider = DependencyManager.Provider;

            provider.RegisterForScene<INewSceneScopeMarker>(
                _sceneName,
                new NewSceneScopeMarker(),
                allowOverride: false);

            var worldRoot = EnsureWorldRoot();
            _worldSpawnContext = new WorldSpawnContext(_sceneName, worldRoot);

            provider.RegisterForScene<IWorldSpawnContext>(
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
                DebugUtility.LogError(typeof(NewSceneBootstrapper),
                    "WorldDefinition não atribuída. Serviços de spawn não serão registrados.");
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
                $"Spawn services summary => Total={totalEntries}, Enabled={enabledCount}, Disabled={skippedDisabledCount}, Created={createdCount}, FailedCreate={failedCreateCount}");
        }

        private Transform EnsureWorldRoot()
        {
            var activeScene = SceneManager.GetActiveScene();
            var rootObjects = activeScene.GetRootGameObjects();
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
                SceneManager.MoveGameObjectToScene(worldRootGo, activeScene);
                return worldRootGo.transform;
            }

            if (foundCount > 1)
            {
                DebugUtility.LogWarning(typeof(NewSceneBootstrapper),
                    $"Multiple WorldRoot objects found: {foundCount}");

                foreach (var root in rootObjects)
                {
                    if (root != null && root.name == "WorldRoot")
                    {
                        DebugUtility.LogWarning(typeof(NewSceneBootstrapper),
                            $"WorldRoot candidate: {BuildTransformPath(root.transform)}");
                    }
                }

                DebugUtility.LogWarning(typeof(NewSceneBootstrapper),
                    $"WorldRoot selected: {BuildTransformPath(selectedRoot.transform)}");
            }

            if (selectedRoot.scene != activeScene)
            {
                SceneManager.MoveGameObjectToScene(selectedRoot, activeScene);
            }

            return selectedRoot.transform;
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
