using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    public readonly struct PlanetDefenseEngagedEvent : IEvent
    {
        public PlanetsMaster Planet { get; }
        public IDetector Detector { get; }
        public DetectionType DetectionType { get; }

        public PlanetDefenseEngagedEvent(PlanetsMaster planet, IDetector detector, DetectionType detectionType)
        {
            Planet = planet;
            Detector = detector;
            DetectionType = detectionType;
        }
    }

    public readonly struct PlanetDefenseDisengagedEvent : IEvent
    {
        public PlanetsMaster Planet { get; }
        public IDetector Detector { get; }
        public DetectionType DetectionType { get; }

        public PlanetDefenseDisengagedEvent(PlanetsMaster planet, IDetector detector, DetectionType detectionType)
        {
            Planet = planet;
            Detector = detector;
            DetectionType = detectionType;
        }
    }

    public readonly struct PlanetDefenseDisabledEvent : IEvent
    {
        public PlanetsMaster Planet { get; }

        public PlanetDefenseDisabledEvent(PlanetsMaster planet)
        {
            Planet = planet;
        }
    }
}
