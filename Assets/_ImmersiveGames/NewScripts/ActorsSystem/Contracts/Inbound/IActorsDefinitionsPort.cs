using _ImmersiveGames.NewScripts.ActorsSystem.Models;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Contracts.Inbound
{
    /// <summary>
    /// Inbound port for canonical actor definitions owned by ActorsSystem.
    /// </summary>
    public interface IActorsDefinitionsPort
    {
        bool TryGetCurrent(out ActorsDefinitionsSnapshot snapshot);
    }
}
