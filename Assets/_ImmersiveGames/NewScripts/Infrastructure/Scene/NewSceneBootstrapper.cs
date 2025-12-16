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

            RegisterSpawnServicesFromDefinition(provider, spawnRegistry, actorRegistry);

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
            IActorRegistry actorRegistry)
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
                foreach (var entry in entries)
                {
                    if (!entry.Enabled)
                    {
                        skippedDisabledCount++;
                        continue;
                    }

                    enabledCount++;
                    var service = _spawnServiceFactory.Create(entry, provider, actorRegistry);
                    if (service == null)
                    {
                        failedCreateCount++;
                        continue;
                    }

                    createdCount++;
                    registry.Register(service);
                    registeredCount++;
                }
            }

            DebugUtility.Log(typeof(NewSceneBootstrapper),
                $"Spawn services registered from definition: {registeredCount}");

            DebugUtility.LogVerbose(typeof(NewSceneBootstrapper),
                $"Spawn services summary => Total={totalEntries}, Enabled={enabledCount}, Disabled={skippedDisabledCount}, Created={createdCount}, FailedCreate={failedCreateCount}");
        }
    }
}
