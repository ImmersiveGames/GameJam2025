using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime
{
    /// <summary>
    /// Payload associado a uma rota de transição.
    ///
    /// Observação:
    /// - Contém dados de compatibilidade (legacy) usados apenas em cenários de debug/migração.
    /// </summary>
    public sealed class SceneTransitionPayload
    {
        public IReadOnlyList<string> ScenesToLoad { get; }
        public IReadOnlyList<string> ScenesToUnload { get; }
        public string TargetActiveScene { get; }
        public bool UseFade { get; }
        public SceneFlowProfileId LegacyProfileId { get; }

        public bool HasSceneData =>
            ScenesToLoad.Count > 0 ||
            ScenesToUnload.Count > 0 ||
            !string.IsNullOrWhiteSpace(TargetActiveScene);

        public bool HasLegacyStyle =>
            LegacyProfileId.IsValid || UseFade;

        private SceneTransitionPayload(
            IReadOnlyList<string> scenesToLoad,
            IReadOnlyList<string> scenesToUnload,
            string targetActiveScene,
            bool useFade,
            SceneFlowProfileId legacyProfileId)
        {
            ScenesToLoad = scenesToLoad ?? Array.Empty<string>();
            ScenesToUnload = scenesToUnload ?? Array.Empty<string>();
            TargetActiveScene = targetActiveScene ?? string.Empty;
            UseFade = useFade;
            LegacyProfileId = legacyProfileId;
        }

        public static SceneTransitionPayload Empty { get; } =
            new SceneTransitionPayload(Array.Empty<string>(), Array.Empty<string>(), string.Empty, false, SceneFlowProfileId.None);

        public static SceneTransitionPayload CreateSceneData(
            IReadOnlyList<string> scenesToLoad,
            IReadOnlyList<string> scenesToUnload,
            string targetActiveScene)
        {
            var load = SanitizeList(scenesToLoad);
            var unload = SanitizeList(scenesToUnload);
            var active = string.IsNullOrWhiteSpace(targetActiveScene) ? string.Empty : targetActiveScene.Trim();
            return new SceneTransitionPayload(load, unload, active, false, SceneFlowProfileId.None);
        }

        public SceneTransitionPayload WithSceneData(SceneRouteDefinition definition)
        {
            return new SceneTransitionPayload(
                definition.ScenesToLoad,
                definition.ScenesToUnload,
                definition.TargetActiveScene,
                UseFade,
                LegacyProfileId);
        }

        public override string ToString()
            => $"active='{TargetActiveScene}', useFade={UseFade}, legacyProfile='{LegacyProfileId}', " +
               $"load=[{FormatList(ScenesToLoad)}], unload=[{FormatList(ScenesToUnload)}]";

        private static IReadOnlyList<string> SanitizeList(IReadOnlyList<string> list)
        {
            if (list == null || list.Count == 0)
            {
                return Array.Empty<string>();
            }

            return list
                .Where(entry => !string.IsNullOrWhiteSpace(entry))
                .Select(entry => entry.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        private static string FormatList(IEnumerable<string> list)
            => string.Join(", ", list.Where(entry => !string.IsNullOrWhiteSpace(entry)));
    }
}
