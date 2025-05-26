using _ImmersiveGames.Scripts.Utils.BusEventSystems;
namespace _ImmersiveGames.Scripts.PlanetSystems
{
    public class PlanetDestroyedEvent :IEvent
    {
        public Planet Planet { get; }

        public PlanetDestroyedEvent(Planet planet)
        {
            Planet = planet;
        }
    }

    public class GameOverEvent :IEvent{ }
}