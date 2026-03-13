using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    /// <summary>
    /// Definicao canonica para vincular um level a uma macro rota.
    /// </summary>
    [Serializable]
    public sealed class LevelDefinition
    {
        [Tooltip("Referencia canonica do level (levelRef).")]
        public LevelDefinitionAsset levelRef;

        [Tooltip("Referencia direta obrigatoria para a macro rota canonica do level.")]
        public SceneRouteDefinitionAsset macroRouteRef;

        public LevelDefinitionAsset LevelRef => levelRef;
        public SceneRouteDefinitionAsset MacroRouteRef => macroRouteRef;
        public bool HasLevelRef => levelRef != null;
        public bool IsValid => levelRef != null && TryResolveMacroRouteId(out _);

        public SceneRouteId ResolveMacroRouteId()
        {
            if (!TryResolveMacroRouteId(out SceneRouteId macroRouteId))
            {
                string levelLabel = levelRef != null ? levelRef.name : "<null-levelRef>";
                FailFast($"LevelDefinition exige macroRouteRef valido. levelRef='{levelLabel}'.");
            }

            return macroRouteId;
        }

        public string ResolveLevelSignature()
        {
            if (levelRef == null)
            {
                FailFast("LevelDefinition invalido: levelRef obrigatorio e nao configurado.");
            }

            string levelSignature = levelRef.name?.Trim();
            if (string.IsNullOrWhiteSpace(levelSignature))
            {
                FailFast("levelRef invalido para assinatura canonica.");
            }

            return levelSignature;
        }

        public SceneTransitionPayload ToPayload()
            => SceneTransitionPayload.Empty;

        public override string ToString()
        {
            string levelLabel = levelRef != null ? levelRef.name : "<none>";
            return $"levelRef='{levelLabel}', macroRouteId='{ResolveMacroRouteId()}'";
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
