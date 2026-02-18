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
    /// - Este asset referencia a rota por AssetRef obrigatório (<see cref="routeRef"/>).
    /// - <see cref="routeId"/> é campo legado apenas para migração assistida no Editor.
    /// </summary>
    [Serializable]
    public sealed class LevelDefinition
    {
        [Tooltip("Id canônico do nível.")]
        public LevelId levelId;

        [Obsolete("Campo legado apenas para migração. Use routeRef.")]
        [HideInInspector]
        [Tooltip("SceneRouteId legado do nível (somente migração).")]
        public SceneRouteId routeId;

        [Tooltip("Referência direta obrigatória para a rota canônica.")]
        public SceneRouteDefinitionAsset routeRef;

        public bool IsValid => levelId.IsValid && ResolveRouteId().IsValid;

        public SceneRouteId ResolveRouteId()
        {
            if (!levelId.IsValid)
            {
                FailFast("LevelDefinition inválido: levelId vazio/inválido.");
            }

            if (routeRef == null)
            {
                FailFast($"LevelDefinition exige routeRef obrigatório. levelId='{levelId}'.");
            }

            SceneRouteId routeRefId = routeRef.RouteId;
            if (!routeRefId.IsValid)
            {
                FailFast($"routeRef inválido. levelId='{levelId}', asset='{routeRef.name}', routeRef.routeId vazio/inválido.");
            }

            if (routeId.IsValid && routeId != routeRefId)
            {
                FailFast(
                    $"LevelDefinition com routeId legado divergente de routeRef. levelId='{levelId}', routeId='{routeId}', routeRef.routeId='{routeRefId}'.");
            }

            return routeRefId;
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

        private static void FailFast(string detail)
        {
            string message = $"[FATAL][Config] {detail}";
            DebugUtility.LogError(typeof(LevelDefinition), message);
            throw new InvalidOperationException(message);
        }
    }
}
