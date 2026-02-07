using System;
using System.Linq;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
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

        [Tooltip("Quando true, aplica fade (se o SceneFlow suportar).")]
        public bool useFade = true;

        [Tooltip("Profile legado do SceneFlow (fallback).")]
        public SceneFlowProfileId legacyProfileId = SceneFlowProfileId.Gameplay;

        public bool IsValid => levelId.IsValid && routeId.IsValid;

        public SceneTransitionPayload ToPayload()
        {
            return SceneTransitionPayload.FromLegacy(
                scenesToLoad: Sanitize(scenesToLoad),
                scenesToUnload: Sanitize(scenesToUnload),
                targetActiveScene: targetActiveScene,
                useFade: useFade,
                legacyProfileId: legacyProfileId);
        }

        public override string ToString()
            => $"levelId='{levelId}', routeId='{routeId}', active='{targetActiveScene}', " +
               $"useFade={useFade}, legacyProfile='{legacyProfileId}', " +
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
