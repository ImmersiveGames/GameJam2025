using System;
using System.Collections.Generic;
using System.Linq;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime
{
    /// <summary>
    /// Descreve os detalhes de uma rota (cenas + ativa) resolvida pelo SceneFlow.
    /// </summary>
    public readonly struct SceneRouteDefinition
    {
        public IReadOnlyList<string> ScenesToLoad { get; }
        public IReadOnlyList<string> ScenesToUnload { get; }
        public string TargetActiveScene { get; }

        public SceneRouteDefinition(
            IReadOnlyList<string> scenesToLoad,
            IReadOnlyList<string> scenesToUnload,
            string targetActiveScene)
        {
            ScenesToLoad = scenesToLoad ?? Array.Empty<string>();
            ScenesToUnload = scenesToUnload ?? Array.Empty<string>();
            TargetActiveScene = targetActiveScene ?? string.Empty;
        }

        public bool HasSceneData =>
            ScenesToLoad.Count > 0 ||
            ScenesToUnload.Count > 0 ||
            !string.IsNullOrWhiteSpace(TargetActiveScene);

        public override string ToString()
            => $"active='{TargetActiveScene}', load=[{FormatList(ScenesToLoad)}], unload=[{FormatList(ScenesToUnload)}]";

        private static string FormatList(IEnumerable<string> list)
            => string.Join(", ", list.Where(entry => !string.IsNullOrWhiteSpace(entry)));
    }
}
