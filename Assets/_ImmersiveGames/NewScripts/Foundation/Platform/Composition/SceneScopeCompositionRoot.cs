using System;
using System.Linq;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.ActorsSystem.Semantic;
using _ImmersiveGames.NewScripts.Foundation.Platform.Config;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.GameplayRuntime.ActorRegistry;
using _ImmersiveGames.NewScripts.GameplayRuntime.Spawn;
using _ImmersiveGames.NewScripts.ResetFlow.SceneReset.Hooks;
using _ImmersiveGames.NewScripts.SceneFlow.Authoring.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.RuntimeCore;
using _ImmersiveGames.NewScripts.SceneFlow.NavigationDispatch.NavigationMacro;
using _ImmersiveGames.NewScripts.SceneFlow.Readiness.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Foundation.Platform.Composition
{
    /// <summary>
    /// Inicializa servicos de escopo de cena para o NewScripts e garante limpeza deterministica.
    /// </summary>
    public sealed partial class SceneScopeCompositionRoot : MonoBehaviour
    {
        private string _sceneName = string.Empty;
        private bool _registered;
        private readonly WorldSpawnServiceFactory _spawnServiceFactory = new();
        private IWorldSpawnContext _worldSpawnContext;

        private void Awake()
        {
            var scene = gameObject.scene;
            _sceneName = scene.name;

            if (_registered)
            {
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
            provider.RegisterForScene<IWorldSpawnServiceRegistryReadPort>(
                _sceneName,
                spawnRegistry,
                allowOverride: false);

            SceneResetHookRegistry hookRegistry;
            if (provider.TryGetForScene<SceneResetHookRegistry>(_sceneName, out var existingRegistry))
            {
                DebugUtility.LogError(typeof(SceneScopeCompositionRoot),
                    $"SceneResetHookRegistry ja existe para a cena '{_sceneName}'. Segundo registro bloqueado.");
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
            RegisterSpawnServicesFromCanonicalActorSet(provider, spawnRegistry, actorRegistry, _worldSpawnContext);

            _registered = true;
            DebugUtility.Log(typeof(SceneScopeCompositionRoot), $"Scene scope created: {_sceneName}");
        }

        private void OnDestroy()
        {
            if (!_registered)
            {
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

        private void RegisterSpawnServicesFromCanonicalActorSet(
            IDependencyProvider provider,
            IWorldSpawnServiceRegistry registry,
            IActorRegistry actorRegistry,
            IWorldSpawnContext context)
        {
            if (!provider.TryGetGlobal<ISceneFlowRouteActorSetRefContext>(out var actorSetRefContext) || actorSetRefContext == null)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsExecution] Missing ISceneFlowRouteActorSetRefContext for scene='{_sceneName}'. SceneScopeCompositionRoot nao escolhe elenco localmente.");
            }

            if (!provider.TryGetGlobal<IActorSetSelectionService>(out var actorSetSelectionService) || actorSetSelectionService == null)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsExecution] Missing IActorSetSelectionService for scene='{_sceneName}'.");
            }

            if (!actorSetRefContext.TryGetCurrent(out ActorSetRef actorSetRef, out SceneRouteKind routeKind, out string source))
            {
                if (routeKind == SceneRouteKind.Unspecified)
                {
                    SceneCanonicalClassification classification = ResolveSceneCanonicalClassificationOrFail(provider);
                    if (classification == SceneCanonicalClassification.Gameplay)
                    {
                        throw new InvalidOperationException(
                            $"[FATAL][Config][ActorsExecution] Route context not resolved before gameplay scene scope spawn registration. scene='{_sceneName}' source='{AsText(source)}'.");
                    }

                    DebugUtility.Log(typeof(SceneScopeCompositionRoot),
                        $"[OBS][ActorsExecution] Route context ainda nao resolvido; spawn canonico adiado para cena non-gameplay scene='{_sceneName}' classification='{classification}' source='{AsText(source)}'.",
                        DebugUtility.Colors.Info);
                    DebugUtility.Log(typeof(SceneScopeCompositionRoot),
                        "Spawn services registered from canonical actor set: 0");
                    return;
                }

                if (routeKind == SceneRouteKind.Gameplay)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][Config][ActorsExecution] Missing ActorSetRef in gameplay route context for scene='{_sceneName}'. source='{AsText(source)}'.");
                }

                DebugUtility.Log(typeof(SceneScopeCompositionRoot),
                    $"[OBS][ActorsExecution] Non-gameplay scene sem ActorSetRef. Nenhum spawn registrado scene='{_sceneName}' routeKind='{routeKind}' source='{AsText(source)}'.",
                    DebugUtility.Colors.Info);
                DebugUtility.Log(typeof(SceneScopeCompositionRoot),
                    "Spawn services registered from canonical actor set: 0");
                return;
            }

            if (!actorSetSelectionService.TryResolve(actorSetRef, out ActorSetResolvedSelection selection) || !selection.HasEntries)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsExecution] ActorSetRef sem resolucao actorSetRef='{actorSetRef.Value}' scene='{_sceneName}' routeKind='{routeKind}' source='{AsText(source)}'.");
            }

            int registeredCount = 0;
            for (int i = 0; i < selection.OrderedSpecs.Length; i += 1)
            {
                ActorSpecRecord spec = selection.OrderedSpecs[i];
                IWorldSpawnService service = _spawnServiceFactory.CreateFromActorSpec(spec, provider, actorRegistry, context);
                if (service == null)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][Config][ActorsExecution] Falha ao criar spawn service via ActorSpec actorSpecId='{spec.ActorSpecId}' actorSetRef='{actorSetRef.Value}' scene='{_sceneName}'.");
                }

                registry.Register(service);
                registeredCount += 1;

                DebugUtility.LogVerbose(typeof(SceneScopeCompositionRoot),
                    $"[OBS][ActorsExecution] CanonicalActorSetMemberRegistered actorSetRef='{actorSetRef.Value}' order='{i}' actorSpecId='{spec.ActorSpecId}' recipe='{spec.OperationalRecipeKind}'.");
            }

            DebugUtility.Log(typeof(SceneScopeCompositionRoot),
                $"[OBS][ActorsExecution] ActorSelectionResolvedViaCanonicalContext actorSetRef='{actorSetRef.Value}' routeKind='{routeKind}' source='{AsText(source)}' registered='{registeredCount}' scene='{_sceneName}'.",
                DebugUtility.Colors.Info);
            DebugUtility.Log(typeof(SceneScopeCompositionRoot),
                $"Spawn services registered from canonical actor set: {registeredCount}");
        }

        private Transform EnsureWorldRoot(Scene scene)
        {
            var targetScene = scene.IsValid() ? scene : SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                DebugUtility.LogWarning(typeof(SceneScopeCompositionRoot),
                    $"EnsureWorldRoot recebeu uma cena invalida. Usando ActiveScene como fallback. bootstrapScene='{_sceneName}'");
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

        private static string AsText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }

        private SceneCanonicalClassification ResolveSceneCanonicalClassificationOrFail(IDependencyProvider provider)
        {
            if (provider == null)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsExecution] IDependencyProvider ausente ao classificar cena='{_sceneName}'.");
            }

            if (!provider.TryGetGlobal<BootstrapConfigAsset>(out var bootstrapConfig) ||
                bootstrapConfig == null ||
                bootstrapConfig.NavigationCatalog == null)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsExecution] BootstrapConfigAsset/NavigationCatalog obrigatorio ausente para classificar cena='{_sceneName}' sem RouteActorSetRef resolvido.");
            }

            SceneRouteDefinitionAsset gameplayRouteRef = bootstrapConfig.NavigationCatalog.ResolveGameplayRouteRefOrFail();
            SceneRouteDefinition gameplayRoute = gameplayRouteRef.ToDefinition();
            if (IsSceneActiveTargetOfRoute(_sceneName, gameplayRoute))
            {
                return SceneCanonicalClassification.Gameplay;
            }

            GameNavigationEntry menuEntry = bootstrapConfig.NavigationCatalog.ResolveCoreOrFail(GameNavigationIntentKind.Menu);
            if (menuEntry.RouteRef != null && IsSceneActiveTargetOfRoute(_sceneName, menuEntry.RouteRef.ToDefinition()))
            {
                return SceneCanonicalClassification.Frontend;
            }

            return SceneCanonicalClassification.BootstrapOrAuxiliary;
        }

        private static bool IsSceneActiveTargetOfRoute(string sceneName, SceneRouteDefinition routeDefinition)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            return string.Equals(routeDefinition.TargetActiveScene, sceneName.Trim(), StringComparison.Ordinal);
        }

        private enum SceneCanonicalClassification
        {
            BootstrapOrAuxiliary = 0,
            Frontend = 1,
            Gameplay = 2
        }
    }
}
