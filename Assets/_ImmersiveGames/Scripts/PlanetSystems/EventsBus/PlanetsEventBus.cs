using _ImmersiveGames.Scripts.Utils.BusEventSystems;
namespace _ImmersiveGames.Scripts.PlanetSystems.EventsBus
{
    public class PlanetMarkedEvent : IEvent
    {
        public Planets Planet { get; }
        public PlanetMarkedEvent(Planets planet) => Planet = planet;
    }

    public class PlanetUnmarkedEvent : IEvent
    {
        public Planets Planet { get; }
        public PlanetUnmarkedEvent(Planets planet) => Planet = planet;
    }
    
}