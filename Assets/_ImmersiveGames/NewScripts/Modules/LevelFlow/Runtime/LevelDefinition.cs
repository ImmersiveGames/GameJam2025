using System;
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
    /// </summary>
    [Serializable]
    public sealed class LevelDefinition
    {
        [Tooltip("Id canônico do nível.")]
        public LevelId levelId;

        [Tooltip("SceneRouteId associado ao nível.")]
        public SceneRouteId routeId;

        public bool IsValid => levelId.IsValid && routeId.IsValid;

        /// <summary>
        /// Retorna o payload adicional do nível.
        ///
        /// F3 Plan-v2: Scene Data não é mais parte do LevelDefinition.
        /// </summary>
        public SceneTransitionPayload ToPayload()
            => SceneTransitionPayload.Empty;

        public override string ToString()
            => $"levelId='{levelId}', routeId='{routeId}'";
    }
}
