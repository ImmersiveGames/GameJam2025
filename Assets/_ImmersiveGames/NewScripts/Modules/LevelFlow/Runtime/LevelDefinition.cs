using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    /// <summary>
    /// Definição de um nível e sua rota.
    ///
    /// F3 (Route como fonte única de Scene Data):
    /// - Este asset só referencia <see cref="SceneRouteId"/>.
    /// - Dados de cena (ScenesToLoad/Unload/Active) são resolvidos via <see cref="SceneRouteCatalogAsset"/>
    ///   dentro do fluxo de navegação.
    ///
    /// Campos LEGACY existem apenas para migração e são ignorados.
    /// </summary>
    [Serializable]
    public sealed class LevelDefinition
    {
        [Tooltip("Id canônico do nível.")]
        public LevelId levelId;

        [Tooltip("SceneRouteId associado ao nível.")]
        public SceneRouteId routeId;

        [Header("LEGACY (ignored) — use SceneRouteCatalogAsset")]
        [Tooltip("LEGACY: cenas a carregar (por nome). Ignorado a partir da F3.")]
        [HideInInspector]
        public string[] scenesToLoad;

        [Tooltip("LEGACY: cenas a descarregar (por nome). Ignorado a partir da F3.")]
        [HideInInspector]
        public string[] scenesToUnload;

        [Tooltip("LEGACY: cena que deve ficar ativa ao final da transição. Ignorado a partir da F3.")]
        [HideInInspector]
        public string targetActiveScene;

        private static bool _warnedLegacySceneData;

        public bool IsValid => levelId.IsValid && routeId.IsValid;

        /// <summary>
        /// Retorna o payload do nível.
        ///
        /// A partir da F3, o payload NÃO carrega Scene Data.
        /// </summary>
        public SceneTransitionPayload ToPayload()
        {
            WarnIfLegacySceneDataIsPopulated();
            return SceneTransitionPayload.Empty;
        }

        public override string ToString()
            => $"levelId='{levelId}', routeId='{routeId}'";

        public bool HasLegacySceneData()
        {
            return (scenesToLoad != null && scenesToLoad.Length > 0) ||
                   (scenesToUnload != null && scenesToUnload.Length > 0) ||
                   !string.IsNullOrWhiteSpace(targetActiveScene);
        }

        private void WarnIfLegacySceneDataIsPopulated()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_warnedLegacySceneData)
                return;

            bool hasLegacy = HasLegacySceneData();

            if (!hasLegacy)
                return;

            _warnedLegacySceneData = true;

            DebugUtility.LogVerbose(typeof(LevelDefinition),
                "[OBS] LevelDefinition contém Scene Data LEGACY (ScenesToLoad/Unload/Active), " +
                "mas a política atual (F3) ignora esses campos: a rota (SceneRouteId) é a fonte única de Scene Data. " +
                "Use Tools/NewScripts/Navigation/Clear Legacy Scene Data in LevelDefinitions para limpar os assets. " +
                $"(levelId='{levelId}', routeId='{routeId}')");
#endif
        }
    }
}
