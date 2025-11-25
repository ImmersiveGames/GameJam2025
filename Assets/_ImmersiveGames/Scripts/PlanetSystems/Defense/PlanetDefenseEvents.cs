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
        public bool IsFirstEngagement { get; }
        public int ActiveDetectors { get; }

        public PlanetDefenseEngagedEvent(
            PlanetsMaster planet,
            IDetector detector,
            DetectionType detectionType,
            bool isFirstEngagement,
            int activeDetectors)
        {
            Planet = planet;
            Detector = detector;
            DetectionType = detectionType;
            IsFirstEngagement = isFirstEngagement;
            ActiveDetectors = activeDetectors;
        }
    }

    public readonly struct PlanetDefenseDisengagedEvent : IEvent
    {
        public PlanetsMaster Planet { get; }
        public IDetector Detector { get; }
        public DetectionType DetectionType { get; }
        public bool IsLastDisengagement { get; }
        public int ActiveDetectors { get; }

        public PlanetDefenseDisengagedEvent(
            PlanetsMaster planet,
            IDetector detector,
            DetectionType detectionType,
            bool isLastDisengagement,
            int activeDetectors)
        {
            Planet = planet;
            Detector = detector;
            DetectionType = detectionType;
            IsLastDisengagement = isLastDisengagement;
            ActiveDetectors = activeDetectors;
        }
    }

    public readonly struct PlanetDefenseDisabledEvent : IEvent
    {
        public PlanetsMaster Planet { get; }
        public int ActiveDetectors { get; }

        public PlanetDefenseDisabledEvent(PlanetsMaster planet, int activeDetectors)
        {
            Planet = planet;
            ActiveDetectors = activeDetectors;
        }
    }
}
