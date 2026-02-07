using System;
using System.Linq;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    /// <summary>
    /// Definição de um nível e sua rota/payload padrão.
    /// </summary>
    [Serializable]
    public sealed class LevelDefinition
    {
        [Tooltip("Id canônico do nível.")]
        public LevelId levelId;

        [Tooltip("SceneRouteId associado ao nível.")]
        public SceneRouteId routeId;

        [Tooltip("Cenas a carregar (por nome).")]
        public string[] scenesToLoad;

        [Tooltip("Cenas a descarregar (por nome).")]
        public string[] scenesToUnload;

        [Tooltip("Cena que deve ficar ativa ao final da transição.")]
        public string targetActiveScene;

        public bool IsValid => levelId.IsValid && routeId.IsValid;

        public SceneTransitionPayload ToPayload()
        {
            return SceneTransitionPayload.CreateSceneData(
                scenesToLoad: Sanitize(scenesToLoad),
                scenesToUnload: Sanitize(scenesToUnload),
                targetActiveScene: targetActiveScene);
        }

        public override string ToString()
            => $"levelId='{levelId}', routeId='{routeId}', active='{targetActiveScene}', " +
               $"load=[{FormatArray(scenesToLoad)}], unload=[{FormatArray(scenesToUnload)}]";

        private static string[] Sanitize(string[] scenes)
        {
            if (scenes == null || scenes.Length == 0)
            {
                return Array.Empty<string>();
            }

            return scenes
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        private static string FormatArray(string[] arr)
            => arr == null ? "" : string.Join(", ", arr.Where(s => !string.IsNullOrWhiteSpace(s)));
    }
}
