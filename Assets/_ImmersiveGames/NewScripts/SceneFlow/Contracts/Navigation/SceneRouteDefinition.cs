using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
namespace _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation
{
    public enum SceneRouteKind
    {
        Unspecified = 0,
        Frontend = 1,
        Gameplay = 2,
        Overlay = 3
    }

    /// <summary>
    /// Descreve os detalhes de uma rota (cenas + ativa) resolvida pelo SceneFlow.
    /// </summary>
    public readonly struct SceneRouteDefinition
    {
        public IReadOnlyList<string> ScenesToLoad { get; }
        public IReadOnlyList<string> ScenesToUnload { get; }
        public string TargetActiveScene { get; }
        public SceneRouteKind RouteKind { get; }
        public bool RequiresWorldReset { get; }
        public PhaseDefinitionCatalogAsset PhaseDefinitionCatalog { get; }

        public SceneRouteDefinition(
            IReadOnlyList<string> scenesToLoad,
            IReadOnlyList<string> scenesToUnload,
            string targetActiveScene,
            SceneRouteKind routeKind,
            bool requiresWorldReset,
            PhaseDefinitionCatalogAsset phaseDefinitionCatalog)
        {
            ScenesToLoad = scenesToLoad ?? Array.Empty<string>();
            ScenesToUnload = scenesToUnload ?? Array.Empty<string>();
            TargetActiveScene = targetActiveScene ?? string.Empty;
            RouteKind = routeKind;
            RequiresWorldReset = requiresWorldReset;
            PhaseDefinitionCatalog = phaseDefinitionCatalog;
        }

        public bool HasSceneData =>
            ScenesToLoad.Count > 0 ||
            ScenesToUnload.Count > 0 ||
            !string.IsNullOrWhiteSpace(TargetActiveScene);

        public override string ToString()
            => $"active='{TargetActiveScene}', kind='{RouteKind}', requiresWorldReset={RequiresWorldReset}, phaseCatalogPresent={PhaseDefinitionCatalog != null}, load=[{FormatList(ScenesToLoad)}], unload=[{FormatList(ScenesToUnload)}]";

        private static string FormatList(IEnumerable<string> list)
            => string.Join(", ", list.Where(entry => !string.IsNullOrWhiteSpace(entry)));
    }
}

