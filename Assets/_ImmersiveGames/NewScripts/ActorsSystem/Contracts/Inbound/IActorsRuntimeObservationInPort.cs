using _ImmersiveGames.NewScripts.ActorsSystem.Models;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Contracts.Inbound
{
    /// <summary>
    /// Inbound read-only runtime observation port. This is not the axis registry boundary.
    /// </summary>
    public interface IActorsRuntimeObservationInPort
    {
        bool TryGetCurrent(out ActorsRuntimeObservationSnapshot snapshot);
    }
}
