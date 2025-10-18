using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [DebugLevel(DebugLevel.Logs)]
    public sealed class PlanetsMaster : ActorMaster, IPlanetActor
    {
        public IActor PlanetActor => this;
    }
    public interface IPlanetActor
    {
        IActor PlanetActor { get; }
    }
}