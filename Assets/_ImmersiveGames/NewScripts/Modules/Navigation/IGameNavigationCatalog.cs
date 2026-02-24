using System.Collections.Generic;

namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    /// <summary>
    /// Catálogo de rotas de navegação.
    /// Objetivo: permitir que as rotas sejam configuráveis (ex.: via ScriptableObject),
    /// mantendo o GameNavigationService desacoplado de implementações hardcoded.
    /// </summary>
    public interface IGameNavigationCatalog
    {
        IReadOnlyCollection<string> RouteIds { get; }

        bool TryGet(string routeId, out GameNavigationEntry entry);
    }
}
