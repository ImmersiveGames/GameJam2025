using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    /// <summary>
    /// Definição de um nível e sua rota.
    ///
    /// F3/Fase 3 (Route como fonte única de Scene Data):
    /// - Este asset referencia a rota por id e/ou por referência direta opcional (<see cref="routeRef"/>).
    /// - Quando <see cref="routeRef"/> está setado, ele é a fonte de verdade para RouteId.
    /// </summary>
    [Serializable]
    public sealed class LevelDefinition
    {
        [Tooltip("Id canônico do nível.")]
        public LevelId levelId;

        [Tooltip("SceneRouteId associado ao nível (fallback quando routeRef não está setado).")]
        public SceneRouteId routeId;

        [Tooltip("Referência direta opcional para a rota canônica.")]
        public SceneRouteDefinitionAsset routeRef;

        public bool IsValid => levelId.IsValid && ResolveRouteId().IsValid;

        public SceneRouteId ResolveRouteId()
        {
            if (routeRef != null)
            {
                var routeRefId = routeRef.RouteId;
                if (!routeRefId.IsValid)
                {
                    return SceneRouteId.None;
                }

                if (routeId.IsValid && routeId != routeRefId)
                {
                    HandleRouteMismatch(levelId, routeId, routeRefId);
                }

                if (routeId.IsValid)
                {
                    DebugUtility.LogVerbose(typeof(LevelDefinition),
                        $"[OBS][Deprecated] Legacy field 'routeId' foi ignorado pois 'routeRef' está presente. " +
                        $"levelId='{levelId}', legacyRouteId='{routeId}', resolvedRouteId='{routeRefId}'.",
                        DebugUtility.Colors.Warning);
                }

                DebugUtility.LogVerbose(typeof(LevelDefinition),
                    $"[OBS][SceneFlow] RouteResolvedVia=AssetRef levelId='{levelId}', routeId='{routeRefId}', asset='{routeRef.name}'.",
                    DebugUtility.Colors.Info);

                return routeRefId;
            }

            if (routeId.IsValid)
            {
                DebugUtility.LogVerbose(typeof(LevelDefinition),
                    $"[OBS][SceneFlow] RouteResolvedVia=RouteId levelId='{levelId}', routeId='{routeId}'.",
                    DebugUtility.Colors.Info);
            }

            return routeId;
        }

        /// <summary>
        /// Retorna o payload adicional do nível.
        ///
        /// F3 Plan-v2: Scene Data não é parte do LevelDefinition.
        /// </summary>
        public SceneTransitionPayload ToPayload()
            => SceneTransitionPayload.Empty;

        public override string ToString()
            => $"levelId='{levelId}', routeId='{ResolveRouteId()}'";

        private static void HandleRouteMismatch(LevelId levelId, SceneRouteId routeId, SceneRouteId routeRefId)
        {
            string message =
                $"[FATAL][Config] LevelDefinition com routeId divergente de routeRef. " +
                $"levelId='{levelId}', routeId='{routeId}', routeRef.routeId='{routeRefId}'.";

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DebugUtility.LogWarning(typeof(LevelDefinition),
                $"{message} Em editor/dev, routeRef terá prioridade (RouteResolvedVia=AssetRef).");
#else
            DebugUtility.LogError(typeof(LevelDefinition), message);
            throw new InvalidOperationException(message);
#endif
        }
    }
}
