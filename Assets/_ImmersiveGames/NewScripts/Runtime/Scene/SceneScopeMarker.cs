namespace _ImmersiveGames.NewScripts.Runtime.Scene
{
    /// <summary>
    /// Marcador explícito para identificar o escopo de serviços da cena atual.
    /// </summary>
    public interface ISceneScopeMarker
    {
    }

    /// <summary>
    /// Implementação simples do marcador de escopo de cena.
    /// </summary>
    public sealed class SceneScopeMarker : ISceneScopeMarker
    {
    }
}
