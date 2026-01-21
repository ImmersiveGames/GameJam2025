namespace _ImmersiveGames.NewScripts.Infrastructure.Scene
{
    /// <summary>
    /// Marcador explícito para identificar o escopo de serviços da cena atual.
    /// </summary>
    public interface INewSceneScopeMarker
    {
    }

    /// <summary>
    /// Implementação simples do marcador de escopo de cena.
    /// </summary>
    public sealed class NewSceneScopeMarker : INewSceneScopeMarker
    {
    }
}
