using System;
using System.ComponentModel;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    /// <summary>
    /// Definicao serializavel de compatibilidade para vincular um level a uma macro rota.
    ///
    /// Contrato canonico atual:
    /// - <see cref="levelRef"/> identifica o level quando disponivel.
    /// - <see cref="macroRouteRef"/> define o vinculo macro obrigatorio.
    ///
    /// Campos legacy permanecem apenas para compatibilidade/migracao de assets antigos.
    /// </summary>
    [Serializable]
    public sealed class LevelDefinition
    {
        [Tooltip("Referencia canonica do level (levelRef).")]
        public LevelDefinitionAsset levelRef;

        [Tooltip("Referencia direta obrigatoria para a macro rota canonica do level.")]
        public SceneRouteDefinitionAsset macroRouteRef;

        // Compat temporaria com assets legados; nao faz parte do contrato canonico.
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Compat temporaria apenas. Canon usa levelRef.")]
        [HideInInspector]
        [Tooltip("Id legado do level (somente migracao/serializacao).")]
        public LevelId levelId;

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Compat temporaria apenas. Canon usa macroRouteRef.")]
        [HideInInspector]
        [Tooltip("SceneRouteId legado do level (somente migracao/serializacao).")]
        public SceneRouteId routeId;

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Compat temporaria apenas. Canon usa macroRouteRef.")]
        [HideInInspector]
        [Tooltip("Rota legada do level (somente migracao/serializacao).")]
        public SceneRouteDefinitionAsset routeRef;

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Compat temporaria apenas. Canon nao usa contentId neste contrato.")]
        [HideInInspector]
        [Tooltip("ContentId legado associado ao level (compat/observability).")]
        public string contentId = LevelFlowContentDefaults.DefaultContentId;

        public LevelDefinitionAsset LevelRef => levelRef;
        public SceneRouteDefinitionAsset MacroRouteRef => macroRouteRef;
        public bool HasLevelRef => levelRef != null;
        public bool IsValid => TryResolveCanonicalLevelId(out _) && TryResolveMacroRouteId(out _);

        public LevelId ResolveCanonicalLevelId()
        {
            if (levelRef != null)
            {
                LevelId resolvedLevelId = LevelId.FromName(levelRef.name);
                if (resolvedLevelId.IsValid)
                {
                    return resolvedLevelId;
                }

                FailFast($"levelRef invalido para identidade canonica. asset='{levelRef.name}'.");
            }

            if (levelId.IsValid)
            {
                return levelId;
            }

            FailFast("LevelDefinition invalido: levelRef nulo e levelId legado vazio/invalido.");
            return LevelId.None;
        }

        public SceneRouteId ResolveMacroRouteId()
        {
            if (!TryResolveMacroRouteId(out SceneRouteId macroRouteId))
            {
                LevelId canonicalLevelId = TryResolveCanonicalLevelId(out LevelId resolvedLevelId)
                    ? resolvedLevelId
                    : LevelId.None;
                SceneRouteId legacyRouteId = routeRef != null ? routeRef.RouteId : routeId;

                FailFast($"LevelDefinition exige macroRouteRef valido. levelId='{canonicalLevelId}', routeId='{legacyRouteId}'.");
            }

            return macroRouteId;
        }

        /// <summary>
        /// F3 Plan-v2: Scene Data nao e parte do LevelDefinition.
        /// </summary>
        public SceneTransitionPayload ToPayload()
            => SceneTransitionPayload.Empty;

        // Compat temporaria com assets legados; nao faz parte do contrato canonico.
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Compat temporaria apenas. Canon usa macroRouteRef.")]
        public SceneRouteId ResolveRouteId()
        {
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

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Compat temporaria apenas. Canon nao usa contentId neste contrato.")]
        public string ResolveContentId()
            => LevelFlowContentDefaults.Normalize(contentId);

        public override string ToString()
        {
            string levelLabel = levelRef != null ? levelRef.name : ResolveCanonicalLevelId().ToString();
            return $"levelRef='{levelLabel}', macroRouteId='{ResolveMacroRouteId()}'";
        }

        private bool TryResolveCanonicalLevelId(out LevelId resolvedLevelId)
        {
            if (levelRef != null)
            {
                resolvedLevelId = LevelId.FromName(levelRef.name);
                return resolvedLevelId.IsValid;
            }

            resolvedLevelId = levelId;
            return resolvedLevelId.IsValid;
        }

        private bool TryResolveMacroRouteId(out SceneRouteId resolvedMacroRouteId)
        {
            resolvedMacroRouteId = SceneRouteId.None;
            if (macroRouteRef == null)
            {
                return false;
            }

            resolvedMacroRouteId = macroRouteRef.RouteId;
            return resolvedMacroRouteId.IsValid;
        }

        private static void FailFast(string detail)
        {
            string message = $"[FATAL][Config] {detail}";
            DebugUtility.LogError(typeof(LevelDefinition), message);
            throw new InvalidOperationException(message);
        }
    }
}
