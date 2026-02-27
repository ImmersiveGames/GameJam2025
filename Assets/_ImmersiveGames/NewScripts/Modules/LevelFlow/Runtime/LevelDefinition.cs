using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    /// <summary>
    /// Definição de um nível e sua macro rota.
    ///
    /// F3/Fase 3 (Route como fonte única de Scene Data):
    /// - A navegação usa exclusivamente <see cref="macroRouteRef"/>.
    /// - <see cref="routeRef"/> e <see cref="routeId"/> permanecem apenas para compatibilidade/migração.
    /// </summary>
    [Serializable]
    public sealed class LevelDefinition
    {
        [Tooltip("Id canônico do nível.")]
        public LevelId levelId;

        [Obsolete("Campo legado apenas para migração/serialização.")]
        [HideInInspector]
        [Tooltip("SceneRouteId legado do nível (somente migração).")]
        public SceneRouteId routeId;

        [Tooltip("Rota legada do level (compat/migração). Não usada para navegação.")]
        public SceneRouteDefinitionAsset routeRef;

        [Tooltip("Referência direta obrigatória para a macro rota canônica do nível (ADR-0024).")]
        public SceneRouteDefinitionAsset macroRouteRef;

        [Tooltip("ContentId associado ao nível (observability/compat).")]
        public string contentId = LevelFlowContentDefaults.DefaultContentId;

        public bool IsValid => levelId.IsValid && ResolveMacroRouteId().IsValid;

        public SceneRouteId ResolveRouteId()
        {
            if (!levelId.IsValid)
            {
                FailFast("LevelDefinition inválido: levelId vazio/inválido.");
            }

            if (routeRef != null)
            {
                SceneRouteId routeRefId = routeRef.RouteId;
                if (routeRefId.IsValid)
                {
                    return routeRefId;
                }
            }

            return routeId;
        }

        public string ResolveContentId()
            => LevelFlowContentDefaults.Normalize(contentId);

        public SceneRouteId ResolveMacroRouteId()
        {
            if (!levelId.IsValid)
            {
                FailFast("LevelDefinition inválido: levelId vazio/inválido.");
            }

            if (macroRouteRef == null)
            {
                SceneRouteId currentRouteId = routeRef != null ? routeRef.RouteId : routeId;
                FailFast($"LevelDefinition exige macroRouteRef obrigatório. levelId='{levelId}', routeId='{currentRouteId}'.");
            }

            SceneRouteId macroRouteId = macroRouteRef.RouteId;
            if (!macroRouteId.IsValid)
            {
                FailFast($"macroRouteRef inválido. levelId='{levelId}', asset='{macroRouteRef.name}', macroRouteRef.routeId vazio/inválido.");
            }

            return macroRouteId;
        }

        /// <summary>
        /// Retorna o payload adicional do nível.
        ///
        /// F3 Plan-v2: Scene Data não é parte do LevelDefinition.
        /// </summary>
        public SceneTransitionPayload ToPayload()
            => SceneTransitionPayload.Empty;

        public override string ToString()
            => $"levelId='{levelId}', macroRouteId='{ResolveMacroRouteId()}'";

        private static void FailFast(string detail)
        {
            string message = $"[FATAL][Config] {detail}";
            DebugUtility.LogError(typeof(LevelDefinition), message);
            throw new InvalidOperationException(message);
        }
    }
}
