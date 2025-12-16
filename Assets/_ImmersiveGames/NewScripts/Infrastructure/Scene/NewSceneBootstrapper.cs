using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.NewScripts.Infrastructure.World;
using _ImmersiveGames.NewScripts.Infrastructure.Ids;
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
        private string _sceneName = string.Empty;
        private bool _registered;

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

            RegisterDummySpawnService(provider, spawnRegistry, actorRegistry);

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

        private void RegisterDummySpawnService(
            IDependencyProvider provider,
            IWorldSpawnServiceRegistry registry,
            IActorRegistry actorRegistry)
        {
            provider.TryGetGlobal<IUniqueIdFactory>(out var uniqueIdFactory);
            if (uniqueIdFactory == null)
            {
                DebugUtility.LogError(typeof(NewSceneBootstrapper),
                    "IUniqueIdFactory global ausente. Verifique o bootstrap global.");
                return;
            }
            var dummySpawnService = new DummyActorSpawnService(uniqueIdFactory, actorRegistry);
            registry.Register(dummySpawnService);

            DebugUtility.Log(typeof(NewSceneBootstrapper),
                $"Dummy spawn service registrado no escopo da cena '{_sceneName}'.");
        }
    }
}
