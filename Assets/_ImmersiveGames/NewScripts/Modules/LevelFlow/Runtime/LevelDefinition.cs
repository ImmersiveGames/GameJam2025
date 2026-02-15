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
    /// - Este asset referencia a rota por id (legado) e por referência direta opcional (<see cref="routeRef"/>).
    /// - Quando <see cref="routeRef"/> está setado, ele é a fonte de verdade para RouteId.
    /// </summary>
    [Serializable]
    public sealed class LevelDefinition
    {
        private static readonly string[] CriticalLevelIds = { "level.1", "level.2" };

        [Tooltip("Id canônico do nível.")]
        public LevelId levelId;

        [Tooltip("SceneRouteId associado ao nível (fallback quando routeRef não está setado).")]
        public SceneRouteId routeId;

        [Tooltip("Referência direta opcional para a rota canônica.")]
        public SceneRouteDefinitionAsset routeRef;

        public bool IsValid => levelId.IsValid && ResolveRouteId().IsValid;

        public SceneRouteId ResolveRouteId()
        {
            if (!levelId.IsValid)
            {
                FailFast("LevelDefinition inválido: levelId vazio/inválido.");
            }

            bool isCriticalLevel = IsCriticalLevel(levelId);

            if (routeRef != null)
            {
                SceneRouteId routeRefId = routeRef.RouteId;
                if (!routeRefId.IsValid)
                {
                    FailFast($"routeRef inválido. levelId='{levelId}', asset='{routeRef.name}', routeRef.routeId vazio/inválido.");
                }

                if (routeId.IsValid && routeId != routeRefId)
                {
                    FailFast(
                        $"LevelDefinition com routeId divergente de routeRef. levelId='{levelId}', routeId='{routeId}', routeRef.routeId='{routeRefId}'.");
                }

                return routeRefId;
            }

            if (isCriticalLevel)
            {
                FailFast($"Nível crítico exige routeRef (AssetRef). levelId='{levelId}'.");
            }

            if (!routeId.IsValid)
            {
                FailFast($"LevelDefinition sem rota válida. levelId='{levelId}' exige routeId válido ou routeRef.");
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

        private static bool IsCriticalLevel(LevelId levelId)
        {
            if (!levelId.IsValid)
            {
                return false;
            }

            string value = levelId.Value;
            for (int i = 0; i < CriticalLevelIds.Length; i++)
            {
                if (string.Equals(CriticalLevelIds[i], value, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void FailFast(string detail)
        {
            string message = $"[FATAL][Config] {detail}";
            DebugUtility.LogError(typeof(LevelDefinition), message);
            throw new InvalidOperationException(message);
        }
    }
}
