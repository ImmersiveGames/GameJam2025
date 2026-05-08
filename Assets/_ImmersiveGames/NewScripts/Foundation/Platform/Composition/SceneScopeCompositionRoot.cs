using System;
using System.Linq;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.ActorsSystem.Semantic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.GameplayRuntime.ActorRegistry;
using _ImmersiveGames.NewScripts.GameplayRuntime.Spawn;
using _ImmersiveGames.NewScripts.SceneRouting.Contracts.RuntimeCore;
using _ImmersiveGames.NewScripts.SceneRouting.Readiness.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Foundation.Platform.Composition
{
    /// <summary>
    /// Inicializa servicos de escopo de cena para o NewScripts e garante limpeza deterministica.
    /// </summary>
    public sealed partial class SceneScopeCompositionRoot : MonoBehaviour
    {
        private static readonly HashSet<string> CanonicalNoActorScopeScenes = new(StringComparer.OrdinalIgnoreCase)
        {
            "NewBootstrap",
            "MenuScene",
            "SessionActivitySandboxScene"
        };

        private string _sceneName = string.Empty;
        private bool _registered;
        private WorldSpawnServiceFactory _spawnServiceFactory;
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

            if (ShouldSkipActorsScope(provider, out string skipReason))
            {
                DebugUtility.Log(typeof(SceneScopeCompositionRoot),
                    $"[OBS][ActorsExecution][SceneScope] actors_scope_skipped reason='{skipReason}' scene='{_sceneName}'.",
                    DebugUtility.Colors.Info);

                _registered = true;
                DebugUtility.Log(typeof(SceneScopeCompositionRoot), $"Scene scope created: {_sceneName}");
                return;
            }

            if (!provider.TryGetGlobal<IActorSpawnArchetypeRegistry>(out var spawnArchetypeRegistry) || spawnArchetypeRegistry == null)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsExecution] IActorSpawnArchetypeRegistry ausente antes de compor scene scope scene='{_sceneName}'.");
            }
            _spawnServiceFactory = new WorldSpawnServiceFactory(spawnArchetypeRegistry);

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
            if (!provider.TryGetGlobal<ISceneRoutingRouteActorSetRefContext>(out var actorSetRefContext) || actorSetRefContext == null)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsExecution] Missing ISceneRoutingRouteActorSetRefContext for scene='{_sceneName}'. SceneScopeCompositionRoot nao escolhe elenco localmente.");
            }

            if (!provider.TryGetGlobal<IActorSetSelectionService>(out var actorSetSelectionService) || actorSetSelectionService == null)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsExecution] Missing IActorSetSelectionService for scene='{_sceneName}'.");
            }

            if (!actorSetRefContext.TryGetCurrent(out ActorSetRef actorSetRef, out string routeIdentity, out string source))
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsExecution] Missing ActorSetRef in route context for scene='{_sceneName}'. routeIdentity='{AsText(routeIdentity)}' source='{AsText(source)}'.");
            }

            if (!actorSetSelectionService.TryResolve(actorSetRef, out ActorSetResolvedSelection selection) || !selection.HasEntries)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsExecution] ActorSetRef sem resolucao actorSetRef='{actorSetRef.Value}' scene='{_sceneName}' routeIdentity='{AsText(routeIdentity)}' source='{AsText(source)}'.");
            }

            int registeredCount = 0;
            var archetypes = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < selection.Members.Length; i += 1)
            {
                ActorSetResolvedMember member = selection.Members[i];
                if (!member.IsValid)
                {
                    continue;
                }

                ActorSpecRecord spec = member.Spec;
                if (string.IsNullOrWhiteSpace(spec.SpawnArchetypeId))
                {
                    throw new InvalidOperationException(
                        $"[FATAL][Config][ActorsExecution] ActorSpec sem spawnArchetypeId resolvido actorSpecId='{spec.ActorSpecId}' actorSetRef='{actorSetRef.Value}' scene='{_sceneName}'.");
                }
                if (!archetypes.Add(spec.SpawnArchetypeId))
                {
                    throw new InvalidOperationException(
                        $"[FATAL][Config][ActorsExecution] Duplicate spawnArchetypeId no registro scene-local. actorSetRef='{actorSetRef.Value}' spawnArchetypeId='{spec.SpawnArchetypeId}' scene='{_sceneName}'.");
                }
                IWorldSpawnService service = _spawnServiceFactory.CreateFromActorSpec(spec, provider, actorRegistry, context);
                if (service == null)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][Config][ActorsExecution] Falha ao criar spawn service via ActorSpec actorSpecId='{spec.ActorSpecId}' actorSetRef='{actorSetRef.Value}' scene='{_sceneName}'.");
                }

                registry.Register(service);
                registeredCount += 1;

                DebugUtility.LogVerbose(typeof(SceneScopeCompositionRoot),
                    $"[OBS][ActorsExecution] CanonicalActorSetMemberRegistered actorSetRef='{actorSetRef.Value}' order='{member.Order}' actorSpecId='{spec.ActorSpecId}' spawnArchetypeId='{spec.SpawnArchetypeId}' recipe='{spec.OperationalRecipeKind}'.");
            }

            DebugUtility.Log(typeof(SceneScopeCompositionRoot),
                $"[OBS][ActorsExecution] ActorSelectionResolvedViaCanonicalContext actorSetRef='{actorSetRef.Value}' routeIdentity='{AsText(routeIdentity)}' source='{AsText(source)}' registered='{registeredCount}' scene='{_sceneName}'.",
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

        private bool ShouldSkipActorsScope(IDependencyProvider provider, out string reason)
        {
            reason = string.Empty;

            if (CanonicalNoActorScopeScenes.Contains(_sceneName))
            {
                reason = "base11_sandbox_no_actor_set";
                return true;
            }

            if (!IsCanonicalProfile(provider))
            {
                return false;
            }

            if (!provider.TryGetGlobal<ISceneRoutingRouteActorSetRefContext>(out var actorSetRefContext) || actorSetRefContext == null)
            {
                reason = "base11_sandbox_no_actor_set";
                return true;
            }

            if (!actorSetRefContext.TryGetCurrent(out ActorSetRef actorSetRef, out _, out _))
            {
                reason = "base11_sandbox_no_actor_set";
                return true;
            }

            if (!actorSetRef.IsValid)
            {
                reason = "base11_sandbox_no_actor_set";
                return true;
            }

            return false;
        }

        private static bool IsCanonicalProfile(IDependencyProvider provider)
        {
            if (provider == null)
            {
                return false;
            }

            if (!provider.TryGetGlobal<RuntimeModeConfig>(out var runtimeModeConfig) || runtimeModeConfig == null)
            {
                return false;
            }

            return runtimeModeConfig.compositionProfile == CompositionProfileKind.Base11Sandbox;
        }

        private enum SceneCanonicalClassification
        {
            BootstrapOrAuxiliary = 0,
            Frontend = 1,
            Gameplay = 2
        }
    }
}
