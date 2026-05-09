using System;
using _ImmersiveGames.NewScripts.ActorsSystem.Authoring;
using _ImmersiveGames.NewScripts.ActorsSystem.Semantic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.GameplayRuntime.Spawn;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.ActorsSystem.Integration.Bootstrap
{
    public static class ActorsSystemBootstrap
    {
        private const string ActorSpecsCatalogResourcesPath = "ActorsSystem/ActorSpecsCatalog";
        private const string ActorSetCatalogResourcesPath = "ActorsSystem/ActorSetCatalog";

        private static bool _installed;
        private static bool _runtimeComposed;

        public static void ComposeInstallerPhase()
        {
            if (_installed)
            {
                return;
            }

            EnsureActorSpecsCatalogService();
            EnsureActorSetSelectionService();
            EnsureSpawnArchetypeRegistry();

            _installed = true;
            DebugUtility.Log(typeof(ActorsSystemBootstrap),
                "[OBS][ActorsSystem] Installer phase concluida (catalogo + selection + spawn registry).",
                DebugUtility.Colors.Info);
        }

        public static void EnsureActorSetSelectionInfrastructure()
        {
            EnsureActorSpecsCatalogService();
            EnsureActorSetSelectionService();
        }

        public static void ComposeRuntime()
        {
            CompositionPipelineExecutor.RequireBootstrapPhaseOpen(nameof(ActorsSystemBootstrap));

            if (_runtimeComposed)
            {
                return;
            }

            EnsureActorSpecsCatalogService();
            EnsureActorSetSelectionService();
            EnsureSpawnArchetypeRegistry();

            _runtimeComposed = true;
            DebugUtility.Log(typeof(ActorsSystemBootstrap),
                "[OBS][ActorsSystem] Runtime composition concluida (canonical minimal).",
                DebugUtility.Colors.Info);
        }

        private static void EnsureActorSpecsCatalogService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IActorSpecCatalogService>(out var existing) && existing != null)
            {
                return;
            }

            var catalog = Resources.Load<ActorSpecsCatalogAsset>(ActorSpecsCatalogResourcesPath);
            if (catalog == null)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsSystem] Missing required ActorSpecsCatalogAsset at Resources path '{ActorSpecsCatalogResourcesPath}'.");
            }

            DependencyManager.Provider.RegisterGlobal<IActorSpecCatalogService>(new ActorSpecsCatalogService(catalog));
        }

        private static void EnsureActorSetSelectionService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IActorSetSelectionService>(out var existing) && existing != null)
            {
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorSpecCatalogService>(out var actorSpecCatalogService) || actorSpecCatalogService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][ActorsSystem] IActorSpecCatalogService ausente para registrar IActorSetSelectionService.");
            }

            var catalog = Resources.Load<ActorSetCatalogAsset>(ActorSetCatalogResourcesPath);
            if (catalog == null)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsSystem] Missing required ActorSetCatalogAsset at Resources path '{ActorSetCatalogResourcesPath}'.");
            }

            DependencyManager.Provider.RegisterGlobal<IActorSetSelectionService>(new ActorSetSelectionService(catalog, actorSpecCatalogService));
        }

        private static void EnsureSpawnArchetypeRegistry()
        {
            if (DependencyManager.Provider.TryGetGlobal<IActorSpawnArchetypeRegistry>(out var existing) && existing != null)
            {
                return;
            }

            var registry = new ActorSpawnArchetypeRegistry();
            ActorSpawnArchetypeDefaults.RegisterDefaults(registry);
            DependencyManager.Provider.RegisterGlobal<IActorSpawnArchetypeRegistry>(registry);
        }
    }
}
